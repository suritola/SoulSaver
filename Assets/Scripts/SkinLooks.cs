using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 스킨 시청각 레시피 표 (스킨 시청각 개편 · 기획 docs/plans/2026-11-04-skin-av-overhaul.md)
//   SkinDef(SkinData.All) 는 그대로 두고, 스킨마다 「칸 → 그림 레시피 · 소리 레시피」를 문자열로 적은 표를 따로 둠 (5-2절)
//   칸 이름: 캐릭터 step · start · stop · trail · acc · hurt · lvl · idle · ultc · revive · absorb · crisis · aura · death · win
//            무기 fire · whit · wtrail · reload · widle · high · evo / 이펙트 hit · kill · coin · combo · chest · big · kill.basic · kill.skill · kill.ult
//            커서 click · hover · aim · tail
//   줄 형식: "칸: 그림 레시피"  ·  "칸~ 소리 레시피"  ·  "opt: 낱말들" (특수 동작 표시)
//
// 그림 레시피 = 「움직임 그림 개수 크기 수명 키=값…」 을 ; 로 이음 (SkinFx.Emit 가 그림)
//   움직임: mark(발자국 · 바닥) burst(사방 튐) puff(퍼지는 연기) rise(위로 떠오름) column(위로 솟는 나선 기둥) fall(위에서 떨어짐)
//           fountain(분수 — 솟았다 떨어짐) roll(옆으로 굴러감) pop(튀어 올랐다 떨어짐) spin(그 자리에서 돎) float(그 자리에 떠 있음)
//           flash(빛 번짐) beam(세로 빛기둥) spray(평타 방향으로 뿜음 · dir=-1 이면 반대쪽) absorb(그 자리 → 몸으로 날아듦)
//           tint(몸 실루엣 안 덧칠) freeze(몸 모양 그대로 굳음) shrink(몸 모양이 줄어들며 사라짐) ghost(몸 잔상)
//   그림: SkinArt 글리프 이름 · "glow"(빛 점) · "body"(몸 그림) · "fx_…"(기존 시트 — 금지 목록 밖만, L4 · L5)
//   키: c=m(테마) a(보조) w(흰) k(검정) rb(무지개) #RRGGBB · r 퍼짐 반지름 · h 높이 · x · y 기준점 오프셋 · g 중력 · al 알파
//       sp 회전(도/초) · v 속도 배율 · rot 시작 각도 · at=feet|head|body|hand (기본 body)
//
// 소리 레시피 = 「음색 주파수 길이 @지연 x음량」 을 ; 로 이음, 변주를 직접 적으려면 | 로 나눔 (SkinAudio 가 합성)
//   주파수: 600 · 600>300(미끄럼) · 523,659,784(음표 차례로 — choir · organ 은 화음) · 음색에 따라 없음
//   음색 사전(4-0절): sine noise sand metal bell chime wind drop paper beep wood bone fire ice cloth glass thud roar choir chip
//                    pulse modem whistle bubble scratch breath clap shutter musicbox drum pluck brass pipe saw organ siren crowd gong
//   n12 = 개수(pulse · clap · glass) · f2200 = 주파수를 따로
public sealed class SkinLook
{
    public string id;
    public Color main = Color.white, accent = Color.white;
    public readonly Dictionary<string, string> fx = new Dictionary<string, string>();
    public readonly Dictionary<string, string> sfx = new Dictionary<string, string>();
    public readonly HashSet<string> opt = new HashSet<string>();

    public string Fx(string slot) => fx.TryGetValue(slot, out string v) ? v : null;
    public string Sfx(string slot) => sfx.TryGetValue(slot, out string v) ? v : null;
    public bool Has(string slot) => fx.ContainsKey(slot) || sfx.ContainsKey(slot);
}

public enum CueMotion { Mark, Burst, Puff, Rise, Column, Fall, Fountain, Roll, Pop, Spin, Float, Flash, Beam, Spray, Absorb, Tint, Freeze, Shrink, Ghost }

// 그림 레시피 한 마디 (해석 결과 · 캐시)
public sealed class Cue
{
    public CueMotion m;
    public string glyph = "dot";
    public int n = 1;
    public float size = 0.4f, life = 0.5f;
    public char cc = 'm';            // m 테마 · a 보조 · w 흰 · k 검정 · r 무지개 · c 직접
    public Color custom = Color.white;
    public float r = -1f, h = -1f, x, y, g, al = 1f, sp, v = 1f, rot;
    public int dir = 1;
    public char at = 'b';            // b 몸 가운데 · f 발 · h 머리 · n 손(평타 시작점)
}

public static partial class SkinLooks
{
    static readonly Dictionary<string, SkinLook> all = new Dictionary<string, SkinLook>();
    static bool built;

    public static SkinLook Get(SkinDef s) => s == null ? null : Get(s.id);
    public static SkinLook Get(string id)
    {
        if (!built) Build();
        return id != null && all.TryGetValue(id, out SkinLook l) ? l : null;
    }

    static void Build()
    {
        built = true;
        AddCommon();
        AddRare();
        AddHero();
        AddLegend();
        AddWeapons();
        AddEffects();
        AddCursors();
    }

    static partial void AddRare();
    static partial void AddHero();
    static partial void AddLegend();
    static partial void AddWeapons();

    // 한 스킨 등록: 색 둘 + 줄들
    static void L(string id, string main, string accent, params string[] lines)
    {
        SkinLook k = new SkinLook { id = id };
        if (ColorUtility.TryParseHtmlString(main, out Color m)) k.main = m;
        if (ColorUtility.TryParseHtmlString(accent, out Color a)) k.accent = a;
        foreach (string raw in lines)
        {
            if (string.IsNullOrEmpty(raw)) continue;
            int colon = raw.IndexOf(':'), tilde = raw.IndexOf('~');
            bool sound = tilde >= 0 && (colon < 0 || tilde < colon);
            int cut = sound ? tilde : colon;
            if (cut <= 0) { if (Debug.isDebugBuild) Debug.LogWarning("[SkinLooks] 줄 형식 오류: " + id + " · " + raw); continue; }
            string key = raw.Substring(0, cut).Trim(), val = raw.Substring(cut + 1).Trim();
            if (key == "opt") { foreach (string w in val.Split(' ')) if (w.Length > 0) k.opt.Add(w); continue; }
            if (sound) k.sfx[key] = val; else k.fx[key] = val;
        }
        all[id] = k;
    }

    // ================================================================= 그림 레시피 해석 (문자열마다 한 번)
    static readonly Dictionary<string, Cue[]> parsed = new Dictionary<string, Cue[]>();

    // 스킨이 쓰면 안 되는 시트 (2-3절 L4 범위 문법 · L5 적 공격 · 내 기술 그림)
    static readonly HashSet<string> Forbidden = new HashSet<string>
    {
        "fx_shock", "fx_explosion", "fx_icenova", "fx_chimering", "fx_soulburst", "fx_rune", "fx_target_rune", "fx_reticle", "fx_warn",
        "fx_puddle", "fx_poisonfog", "fx_firefloor", "fx_soulstep", "fx_codeblock", "fx_broom", "fx_slipper", "fx_scratcher", "fx_devcursor",
        "fx_keycap", "fx_puppet_swing", "fx_card", "fx_pigeon", "fx_cuckoo", "fx_bulb", "fx_crow", "fx_mercury", "fx_paperplane", "fx_tendril",
        "fx_soulchain", "fx_soulshackle", "fx_beam", "fx_tape",
    };
    public static bool IsForbidden(string sheet) => sheet != null && (Forbidden.Contains(sheet) || sheet.StartsWith("fx_ind_") || sheet.StartsWith("atk_") || sheet.StartsWith("prop_grave"));

    public static Cue[] Parse(string recipe)
    {
        if (string.IsNullOrEmpty(recipe)) return null;
        if (parsed.TryGetValue(recipe, out Cue[] got)) return got;
        List<Cue> list = new List<Cue>();
        foreach (string part in recipe.Split(';'))
        {
            string[] t = part.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 2) continue;
            if (!System.Enum.TryParse(t[0], true, out CueMotion m)) { if (Debug.isDebugBuild) Debug.LogWarning("[SkinLooks] 없는 움직임: " + t[0]); continue; }
            Cue c = new Cue { m = m, glyph = t[1] };
            if (IsForbidden(c.glyph)) { if (Debug.isDebugBuild) Debug.LogWarning("[SkinLooks] 금지 시트(L4 · L5): " + c.glyph); continue; }
            int pos = 0;
            for (int i = 2; i < t.Length; i++)
            {
                string w = t[i];
                int eq = w.IndexOf('=');
                if (eq < 0)
                {
                    float.TryParse(w, NumberStyles.Float, CultureInfo.InvariantCulture, out float num);
                    if (pos == 0) c.n = Mathf.Max(1, Mathf.RoundToInt(num));
                    else if (pos == 1) c.size = num;
                    else if (pos == 2) c.life = num;
                    pos++;
                    continue;
                }
                string key = w.Substring(0, eq), val = w.Substring(eq + 1);
                float f = 0f;
                if (key != "c" && key != "at") float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
                switch (key)
                {
                    case "c":
                        if (val.StartsWith("#") && ColorUtility.TryParseHtmlString(val, out Color cc)) { c.cc = 'c'; c.custom = cc; }
                        else c.cc = val == "rb" ? 'r' : val.Length > 0 ? val[0] : 'm';
                        break;
                    case "r": c.r = f; break;
                    case "h": c.h = f; break;
                    case "x": c.x = f; break;
                    case "y": c.y = f; break;
                    case "g": c.g = f; break;
                    case "al": c.al = f; break;
                    case "sp": c.sp = f; break;
                    case "v": c.v = f; break;
                    case "rot": c.rot = f; break;
                    case "dir": c.dir = f < 0f ? -1 : 1; break;
                    case "at": c.at = val.Length > 0 ? (val == "hand" ? 'n' : val[0]) : 'b'; break;
                }
            }
            list.Add(c);
        }
        Cue[] arr = list.ToArray();
        parsed[recipe] = arr;
        return arr;
    }
}
