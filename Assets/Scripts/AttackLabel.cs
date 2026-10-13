using System.Collections.Generic;
using System.Reflection;

// 받은 피해의 출처를 스킬 이름까지 (1.0.5 · 결과 화면 「받은 피해」 · 「쓰러진 곳」, balance_log 의 last_hit)
// 예전엔 맞힌 코드가 있는 파일로만 갈라서, 보스 공격도 공용 판정(Hostile · HostileProjectile, EnemySkill.cs)을 거치면 모두 「적 스킬」이 됐음
// → 맞는 순간 호출 스택에서 어느 보스 · 적 스킬(코루틴)에서 왔는지 찾아 그 이름으로. 투사체 · 장판은 만들 때 이름을 붙여 둠 (Pending)
public static class AttackLabel
{
    // 투사체 · 장판이 맞히기 직전에 자기 이름을 넣어 둠 (맞힌 뒤 비움)
    public static string Pending;

    // 보스 스킬 코루틴 이름 → 보여 줄 이름 (BossSkills · BossSkills.Reborn)
    static readonly Dictionary<string, string> BossSkill = new Dictionary<string, string>
    {
        // 1장 킹 슬라임
        { "SlimeLeap", "대점프" }, { "GooBlobs", "점액 방울" }, { "SlimeRoll", "구르기 돌진" },
        // 2장 리치 왕
        { "SoulOrbs", "추적 영혼구" }, { "OrbChase", "추적 영혼구" }, { "BonePrison", "뼈 감옥" }, { "CursedTombs", "저주 묘비" },
        { "TombPulse", "저주 묘비" }, { "Blink", "영혼 화살" }, { "LichRitual", "망자의 의식" },
        // 3장 지옥의 군주
        { "DashCombo", "화염 돌진" }, { "FlameCharge", "화염 돌진" }, { "MeteorRain", "운석 낙하" }, { "Meteor", "운석 낙하" },
        { "FireWave", "화염 파동" }, { "HellWings", "날개 충격파" }, { "AirBarrage", "공중 탄막" }, { "DiveSlam", "내려찍기" },
        // 4장 거울의 군주
        { "MirrorStrike", "거울 공격" }, { "MirrorSkill", "흉내 낸 스킬" }, { "MirrorClones", "분신술" }, { "BrandCross", "십자 낙인" },
        { "ShardRings", "거울 조각 고리" }, { "MirrorUlt", "흉내 낸 필살기" }, { "ReflectedMemory", "되비친 기억" },
    };

    // 투사체 · 장판을 만든 함수 이름으로 (컴파일러가 채운 값, 1.0.7: 탄을 만들 때마다 호출 스택을 뒤지면 보스 탄막에서 렉이 생길 수 있었음)
    public static string FromCaller(string file, string member)
    {
        string f = System.IO.Path.GetFileNameWithoutExtension(file ?? "");
        if (f == "BossUltimate") return "보스 결계";
        if (f.StartsWith("BossSkills") && member != null && BossSkill.TryGetValue(member, out string boss)) return boss;
        if (f == "EnemySkill" && System.Enum.TryParse(member, out EnemySkillType type) && type != EnemySkillType.None) return EnemySkill.SkillName(type);
        return null;
    }

    // 지금 호출 스택에서 보스 · 적 스킬을 찾음 (없으면 null) — 맞는 순간에만 (드묾)
    public static string FromStack()
    {
        try
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(1, false);
            for (int i = 0; i < st.FrameCount; i++)
            {
                MethodBase m = st.GetFrame(i)?.GetMethod();
                System.Type t = m?.DeclaringType;
                if (t == null) continue;
                string method = m.Name;
                // 코루틴은 컴파일러가 만든 "<이름>d__12" 클래스의 MoveNext 로 보임
                if (t.Name.StartsWith("<") && t.DeclaringType != null)
                {
                    int end = t.Name.IndexOf('>');
                    if (end > 1) method = t.Name.Substring(1, end - 1);
                    t = t.DeclaringType;
                }
                if (t == typeof(BossUltimate)) return "보스 결계";
                if (t == typeof(BossSkills) && BossSkill.TryGetValue(method, out string boss)) return boss;
                if (t == typeof(EnemySkill) && System.Enum.TryParse(method, out EnemySkillType type) && type != EnemySkillType.None)
                    return EnemySkill.SkillName(type);
            }
        }
        catch { }
        return null;
    }

    // 여러 부분을 이은 출처 이름도 부분마다 번역
    public static string Translate(string key)
    {
        if (string.IsNullOrEmpty(key) || !key.Contains(" · ")) return Loc.T(key);
        string[] parts = key.Split(new[] { " · " }, System.StringSplitOptions.None);
        for (int i = 0; i < parts.Length; i++) parts[i] = Loc.T(parts[i]);
        return string.Join(" · ", parts);
    }
}
