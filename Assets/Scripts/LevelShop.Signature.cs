using System.Collections.Generic;
using UnityEngine;

// 캐릭터 고유 레벨업 스킬 (2.1~): 캐릭터마다 10종 (카드 번호 15 ~ 24), 각 4단계
// 영혼 트리 · 기존 레벨업 카드 · 특수 능력과 겹치지 않는 효과만 (효과는 SignatureSkills)
// 그리고 스킬 진화: 정해진 2 ~ 3개의 스킬을 모두 최대 단계로 올리면 하나로 합쳐지며 새 능력이 생김
//   재료는 새 스킬 · 기존 캐릭터 전용 카드 · 공용 카드 모두 (캐릭터마다 7종 + 모두 공통 2종)
// 레벨은 50 (무한 모드 100) 까지라 모든 스킬을 다 올릴 수는 없음 → 어떤 진화를 노릴지가 빌드
public partial class LevelShop
{
    public const int SigFirstId = 15, SigCount = 10, SigMaxLevel = 4;
    public static bool IsSig(int id) => id >= SigFirstId && id < SigFirstId + SigCount;
    // 최대 레벨: 다 올리지 못하게 (무한 모드는 더 길게)
    public static int LevelCap => GameMode.IsEndless ? 100 : 50;

    // 새 스킬 효과 키 (SignatureSkills 가 이 키로 단계를 읽음)
    public static readonly string[][] SigKeys =
    {
        new[] { "g.mine", "g.shrapnel", "g.heat", "g.soul", "g.flash", "g.counter", "g.quick", "g.runreload", "g.scavenge", "g.threat" },
        new[] { "s.riposte", "s.trail", "s.bloodguard", "s.whirl", "s.will", "s.dance", "s.crack", "s.giant", "s.echo", "s.soul" },
        new[] { "r.smoke", "r.feast", "r.burst", "r.ambush", "r.boomerang", "r.path", "r.practice", "r.overflow", "r.knot", "r.rain" },
        new[] { "a.wind", "a.breath", "a.seed", "a.ring", "a.spread", "a.pierce", "a.retreat", "a.ambush", "a.high", "a.meteor" },
        new[] { "l.ignite", "l.frost", "l.catalyst", "l.amplify", "l.tinker", "l.glassrain", "l.salve", "l.refine", "l.embers", "l.concentrate" },
    };

    static int Who(CharacterId who) => Mathf.Clamp((int)who, 0, SigKeys.Length - 1);
    public static string SigKey(int id) => SigKey(CharacterData.Selected, id);
    public static string SigKey(CharacterId who, int id) => IsSig(id) && (int)who < SigKeys.Length ? SigKeys[Who(who)][id - SigFirstId] : null;
    public static int SigIcon(CharacterId who, int id) => 120 + Who(who) * 10 + (id - SigFirstId);

    // 다른 스킬을 먼저 배워야 나오는 스킬 (없으면 -1)
    public static int SigRequires(CharacterId who, int id) => who == CharacterId.Alchemist && id == SigFirstId + 4 ? KnockId : -1;

    static KitCard SigCardOf(CharacterId who, int id)
    {
        if (!IsSig(id)) return null;
        KitCard c = SigCardRaw(who, id - SigFirstId);
        if (c != null) c.icon = SigIcon(who, id);
        return c;
    }

    static KitCard SigCardRaw(CharacterId who, int k)
    {
        switch (who)
        {
            case CharacterId.Gunner:
                switch (k)
                {
                    case 0: return Card("탄피 지뢰", "장전을 시작하면 발밑에 탄피 지뢰를 흩뿌립니다. 적이 밟으면 터집니다. (공격력 80%)", 0, "지뢰 수", "|2|개", "|3|개", "|4|개", "|5|개");
                    case 1: return Card("처치 파편", "적을 쓰러뜨리면 그 자리에서 파편이 사방으로 튑니다. (공격력 40%씩)", 0, "파편 수", "|3|개", "|4|개", "|5|개", "|6|개");
                    case 2: return Card("총열 과열", "쉬지 않고 쏠수록 총열이 달아올라 총알의 피해가 증가합니다. 1.5초 쉬면 식습니다.", 0, "최대 피해", "|+20%|", "|+30%|", "|+40%|", "|+50%|");
                    case 3: return Card("영혼 탄환", "필살기 게이지가 가득 찬 동안과 필살기로 쏜 총알이 영혼탄이 되어 더 아프고 적을 하나 더 꿰뚫습니다. (총알이 아닌 필살기는 피해만 증가)", 0, "영혼탄 피해", "|+25%|", "|+40%|", "|+55%|", "|+70%|");
                    case 4: return Card("섬광 장전", "장전을 마치는 순간 섬광이 터져 주변 적을 기절시킵니다.", 0, "기절 시간", "|0.6|초", "|0.9|초", "|1.2|초", "|1.5|초");
                    case 5: return Card("반격 사격", "맞으면 가까운 적들에게 자동으로 총알을 쏩니다. (공격력 100%씩)", 0, "반격 탄 수", "|3|발", "|5|발", "|7|발", "|9|발");
                    case 6: return Card("속사 장전", "장전을 마친 직후 몇 발은 쉬지 않고 연달아 나갑니다.", 0, "연사 탄 수", "|2|발", "|3|발", "|4|발", "|5|발");
                    case 7: return Card("달리며 장전", "장전하는 동안 이동 속도가 오릅니다.", 0, "장전 중 이동 속도", "|+15%|", "|+25%|", "|+35%|", "|+45%|");
                    case 8: return Card("전리품 탄약", "적을 쓰러뜨리면 확률로 탄창에 한 발이 돌아옵니다.", 0, "회수 확률", "|15%|", "|25%|", "|35%|", "|45%|");
                    case 9: return Card("위협 사격", "필살기를 쓰면 주변 적이 겁에 질려 4초 동안 느려지고 받는 피해가 늘어납니다.", 0, "받는 피해", "|+10%|", "|+18%|", "|+26%|", "|+34%|");
                }
                break;
            case CharacterId.Swordsman:
                switch (k)
                {
                    case 0: return Card("역습", "적에게 맞은 뒤 1.5초 동안 주는 모든 피해가 늘어납니다.", 0, "역습 피해", "|+20%|", "|+30%|", "|+40%|", "|+50%|");
                    case 1: return Card("검의 궤적", "휘두른 자리에 1초 동안 칼날 잔상이 남아 닿는 적을 0.25초마다 벱니다.", 0, "잔상 피해 (0.25초마다)", "공격력 |8%|", "공격력 |11%|", "공격력 |14%|", "공격력 |17%|");
                    case 2: return Card("혈갑", "벤 적 하나당 최대 체력의 0.6 ~ 1.5%만큼 보호막이 쌓여 받는 피해를 먼저 막습니다. (한 번 휘두를 때 최대 3마리, 보호막이 맞은 뒤 1.5초는 쌓이지 않음 · 몸이 붉게 빛남)", 0, "보호막 최대", "최대 체력 |6%|", "최대 체력 |9%|", "최대 체력 |12%|", "최대 체력 |15%|");
                    case 3: return Card("회전 가속", "필살기를 쓴 뒤 1.5 ~ 4초 동안 휘두르기가 빨라집니다. 오래 모았을수록 더 오래 이어집니다.", 0, "공격 속도", "|+20%|", "|+30%|", "|+40%|", "|+50%|");
                    case 4: return Card("강철 의지", "필살기를 모으는 동안 범위 안의 적을 초당 2.5칸씩 끌어당깁니다. (보스 제외)", 0, "끌어당기는 범위", "|5|칸", "|6|칸", "|7|칸", "|8|칸");
                    case 5: return Card("검무", "적을 벤 휘두르기마다 1.5초 동안 이동 속도가 오릅니다. (최대 5중첩, 계속 베면 유지)", 0, "중첩당 이동 속도", "|+4%|", "|+6%|", "|+8%|", "|+10%|");
                    case 6: return Card("균열", "평타로 같은 적을 세 번 벨 때마다 균열이 터져 그 적에게 추가 피해를 줍니다.", 0, "균열 피해", "공격력 |80%|", "공격력 |120%|", "공격력 |160%|", "공격력 |200%|");
                    case 7: return Card("거인 사냥꾼", "보스 · 중간 보스에게 주는 피해가 늘어납니다.", 0, "보스에게 주는 피해", "|+8%|", "|+13%|", "|+18%|", "|+23%|");
                    case 8: return Card("잔향 베기", "필살기 0.45초 뒤 범위 85%의 잔향 베기가 한 번 더 일어납니다.", 0, "잔향 피해", "필살기의 |40%|", "필살기의 |55%|", "필살기의 |70%|", "필살기의 |85%|");
                    case 9: return Card("영혼 흡수", "적을 쓰러뜨릴 때마다 검에 영혼이 깃들어 다음 베기의 피해가 증가합니다. (최대 8, 벨 때 모두 씀)", 0, "영혼 하나당 피해", "|+3%|", "|+4%|", "|+5%|", "|+6%|");
                }
                break;
            case CharacterId.Rogue:
                switch (k)
                {
                    case 0: return Card("연막", "필살기를 시작한 자리에 연막이 퍼져 안의 적이 크게 느려집니다.", 0, "연막 지속", "|2|초", "|3|초", "|4|초", "|5|초");
                    case 1: return Card("피의 향연", "출혈 중인 적을 쓰러뜨리면 체력을 회복합니다.", 0, "회복", "|2|", "|3|", "|4|", "|5|");
                    case 2: return Card("출혈 폭발", "출혈이 3초 넘게 이어진 적은 남은 출혈이 한꺼번에 터집니다.", 0, "폭발 피해", "남은 출혈의 |80%|", "남은 출혈의 |100%|", "남은 출혈의 |120%|", "남은 출혈의 |140%|");
                    case 3: return Card("급습 표창", "필살기가 끝난 뒤 2초 동안 표창의 피해가 증가합니다.", 0, "급습 피해", "|+30%|", "|+45%|", "|+60%|", "|+75%|");
                    case 4: return Card("회전 표창", "던진 표창이 잠시 뒤 되돌아오며 한 번 더 벱니다.", 0, "되돌아오는 표창 피해", "|40%|", "|55%|", "|70%|", "|85%|");
                    case 5: return Card("궤적 칼날", "필살기가 지나간 길에 칼날 자국이 남아 닿는 적을 벱니다.", 0, "자국 지속", "|1.5|초", "|2|초", "|2.5|초", "|3|초");
                    case 6: return Card("던지기 연습", "같은 적을 연달아 맞힐수록 피해가 오릅니다. (최대 5중첩)", 0, "중첩당 피해", "|+4%|", "|+6%|", "|+8%|", "|+10%|");
                    case 7: return Card("넘치는 기세", "필살기 게이지가 가득 차는 순간 몸 주위로 표창이 사방에 퍼집니다. (공격력 100%씩)", 0, "표창 수", "|6|개", "|9|개", "|12|개", "|15|개");
                    case 8: return Card("그림자 매듭", "필살기로 벤 적들이 4초 동안 그림자 줄로 묶여, 하나가 받은 피해의 일부를 나머지도 받습니다.", 0, "나눠 받는 피해", "|15%|", "|22%|", "|29%|", "|36%|");
                    case 9: return Card("빈 주머니의 비", "표창을 다 던져 주머니가 비는 순간, 하늘에서 표창이 주변 적들에게 쏟아집니다. (공격력 100%씩)", 0, "떨어지는 표창", "|4|개", "|6|개", "|8|개", "|10|개");
                }
                break;
            case CharacterId.Archer:
                switch (k)
                {
                    case 0: return Card("바람 읽기", "가득 당긴 화살이 지나간 길에 3초 동안 바람길이 남아, 그 위에 서 있으면 빨라집니다.", 0, "바람길 이동 속도", "|+15%|", "|+22%|", "|+29%|", "|+36%|");
                    case 1: return Card("집중 호흡", "가득 당긴 화살을 연달아 쏠수록 다음 화살의 피해가 증가합니다. (최대 5중첩, 덜 당겨 쏘면 사라짐)", 0, "중첩당 피해", "|+6%|", "|+9%|", "|+12%|", "|+15%|");
                    case 2: return Card("가시 씨앗", "화살이 맞힌 자리에 작은 가시 덤불이 자라 적을 느리게 하고 찌릅니다.", 0, "덤불 지속", "|1.5|초", "|2|초", "|2.5|초", "|3|초");
                    case 3: return Card("울림 화살촉", "화살이 적을 맞히면 잠시 뒤 같은 자리에 울림이 퍼져 주변에 피해를 줍니다.", 0, "울림 피해", "|25%|", "|35%|", "|45%|", "|55%|");
                    case 4: return Card("표식 전염", "가득 당긴 화살로 적을 쓰러뜨리면 가까운 적들에게 표식을 남깁니다. (4초 동안 받는 피해 +20%)", 0, "표식을 남기는 수", "|1|", "|2|", "|3|", "|4|");
                    case 5: return Card("꿰뚫는 시선", "화살이 적을 꿰뚫을 때마다 그다음 적에게 주는 피해가 오릅니다.", 0, "꿰뚫을 때마다", "|+15%|", "|+22%|", "|+29%|", "|+36%|");
                    case 6: return Card("후퇴 사격", "가득 당긴 화살을 쏘면 반대쪽으로 짧게 뛰어 물러납니다.", 0, "물러나는 거리", "|2|칸", "|2.5|칸", "|3|칸", "|3.5|칸");
                    case 7: return Card("매복", "2초 동안 움직이지 않으면 발밑에 덫이 깔려 다가오는 적을 묶습니다.", 0, "묶는 시간", "|1|초", "|1.5|초", "|2|초", "|2.5|초");
                    case 8: return Card("높은 자리", "필살기가 쏟아지는 동안 그 안의 적이 받는 피해가 늘어납니다.", 0, "받는 피해", "|+15%|", "|+25%|", "|+35%|", "|+45%|");
                    case 9: return Card("유성 화살", "필살기가 끝나면 가운데에 거대한 유성 화살이 떨어집니다.", 0, "유성 피해", "공격력 |200%|", "공격력 |300%|", "공격력 |400%|", "공격력 |500%|");
                }
                break;
            case CharacterId.Alchemist:
                switch (k)
                {
                    case 0: return Card("인화성 기체", "화염 시약이 산성 웅덩이에 닿으면 웅덩이가 불길에 휩싸여 크게 터집니다.", 0, "불길 폭발", "공격력 |120%|", "공격력 |170%|", "공격력 |220%|", "공격력 |270%|");
                    case 1: return Card("서리 결정", "빙결 시약에 맞은 적이 쓰러지면 얼음 파편이 사방으로 튑니다. (공격력 50%씩)", 0, "파편 수", "|4|개", "|5|개", "|6|개", "|7|개");
                    case 2: return Card("촉매 반응", "한 적이 화염 · 빙결 · 산성 시약에 모두 맞으면 원소 붕괴가 일어나 큰 피해를 줍니다.", 0, "붕괴 피해", "공격력 |250%|", "공격력 |350%|", "공격력 |450%|", "공격력 |550%|");
                    case 3: return Card("증폭 용액", "플라스크로 쓰러뜨린 적 수만큼 다음 필살기의 피해가 증가합니다. (최대 20)", 0, "적 하나당", "|+3%|", "|+4%|", "|+5%|", "|+6%|");
                    case 4: return Card("조수 개조", "호문쿨루스가 던지는 플라스크에 화염 · 빙결 · 산성 시약을 번갈아 채우고 더 세게 터뜨립니다. (호문쿨루스 필요)", 0, "조수 플라스크 피해", "|+30%|", "|+50%|", "|+70%|", "|+90%|");
                    case 5: return Card("유리 비", "필살기를 모으는 동안 주변 적에게 작은 플라스크가 떨어져 터집니다. (공격력 50%)", 0, "떨어지는 간격", "|0.6|초", "|0.5|초", "|0.4|초", "|0.3|초");
                    case 6: return Card("응급 연고", "산성 웅덩이 위에 서 있으면 체력이 회복됩니다.", 0, "초당 회복", "|1|", "|1.5|", "|2|", "|2.5|");
                    case 7: return Card("폭발 정제", "불안정한 플라스크가 터질 때마다 체력을 회복합니다.", 0, "회복", "|2|", "|3|", "|4|", "|5|");
                    case 8: return Card("연소 흔적", "걸어간 자리에 작은 불길이 남아 밟은 적을 태웁니다.", 0, "불길 피해 (초당)", "공격력 |15%|", "공격력 |22%|", "공격력 |29%|", "공격력 |36%|");
                    case 9: return Card("시약 농축", "시약 세 가지를 한 바퀴 던질 때마다 다음 플라스크가 농축되어 더 크게 터집니다.", 0, "농축 플라스크 피해 · 범위", "|+40%|", "|+60%|", "|+80%|", "|+100%|");
                }
                break;
        }
        return null;
    }

    // ================================================================= 스킬 진화
    public class SkillEvo
    {
        public string key, name, desc;
        public int[] parts;         // 재료 (레벨업 카드 번호)
        public int icon;            // Resources/Icons/ability_<icon>
        public int Id;              // HUD 칸 번호 (1000 + 순서)
    }

    static SkillEvo Evo(string key, string name, string desc, int icon, params int[] parts) => new SkillEvo { key = key, name = name, desc = desc, icon = icon, parts = parts };
    static int S(int k) => SigFirstId + k;

    static readonly SkillEvo[][] CharEvos =
    {
        new[]   // 거너
        {
            Evo("ge.thunder", "천둥 장전", "장전을 마칠 때 주변 적 여섯에게 번개가 떨어지고 (공격력 150%), 탄피 지뢰가 두 배로 깔립니다.", 170, ExtraBId, S(4), S(0)),
            Evo("ge.furnace", "용광로 탄두", "총열이 가장 뜨거울 때 모든 총알이 맞은 자리에서 불꽃 폭발을 일으키고 적을 태웁니다.", 171, MultiId, KnockId, S(2)),
            Evo("ge.storm", "파편 폭풍", "파편이 두 배로 튀고, 파편이 맞힌 적에게서 한 번 더 가까운 적에게 튕깁니다.", 172, PierceId, S(1)),
            Evo("ge.seeker", "영혼 추적자", "영혼탄이 적을 쫓아가고, 맞힌 적에게서 영혼 구슬이 튀어나와 가까운 적을 덮칩니다.", 173, GlareId, S(3)),
            Evo("ge.thunderback", "뇌격 반격", "맞으면 반격과 함께 몸 주위에 번개 폭풍이 터져 주변 적에게 큰 피해를 주고 기절시킵니다. (공격력 200%)", 174, ExtraAId, S(5)),
            Evo("ge.dance", "총잡이의 춤", "장전을 마치면 가까운 적 여섯에게 자동으로 총알을 한 발씩 쏩니다.", 175, S(6), S(7)),
            Evo("ge.outlaw", "무법자", "겁에 질린 적을 쓰러뜨리면 탄창이 가득 차고, 겁이 가까운 적들에게 번집니다.", 176, S(8), S(9)),
        },
        new[]   // 검사
        {
            Evo("se.counter", "완벽한 반격", "맞거나 투사체를 쳐내면 다음 베기가 앞으로 날아가는 거대한 반격 검기가 됩니다. (공격력 250%)", 177, KnockId, S(0), ExtraAId),
            Evo("se.trail", "천검 궤적", "칼날 잔상이 사라질 때 그 자리에서 검기가 사방으로 날아갑니다.", 178, PierceId, S(1)),
            Evo("se.blood", "피의 성채", "보호막 최대치가 1.5배가 되고, 보호막이 깨지는 순간 피의 파동이 터집니다. (공격력 200%)", 179, MultiId, S(2)),
            Evo("se.eye", "폭풍의 눈", "필살기 뒤 3초 동안 소용돌이가 주변 적을 빨아들이며 계속 벱니다.", 180, GlareId, S(3), S(4)),
            Evo("se.dance", "광검무", "검무가 최대 중첩일 때 모든 베기가 강한 일격(두 배 피해 · 더 멀리)이 됩니다.", 181, ExtraBId, S(5)),
            Evo("se.giant", "거인 처단", "보스는 두 번만 베어도 균열이 터지고, 균열이 주변 적에게도 퍼집니다.", 182, S(6), S(7)),
            Evo("se.vortex", "영혼 회오리", "잔향 베기가 흡수한 영혼만큼 커지고 강해지며, 쓴 영혼의 절반을 되돌려 줍니다.", 183, S(8), S(9)),
        },
        new[]   // 도적
        {
            Evo("re.phantom", "환영 연막", "연막 안에 서 있는 동안 적의 공격이 모두 빗나갑니다.", 184, KnockId, S(0)),
            Evo("re.sea", "피바다", "출혈 폭발이 주변 적에게도 같은 피해를 주고, 터질 때마다 체력을 회복합니다.", 185, MultiId, S(1), S(2)),
            Evo("re.executioner", "처형자의 길", "급습 시간 동안 표창을 던질 때마다 모든 적을 꿰뚫는 그림자 표창이 함께 날아가고, 묶인 적이 쓰러지면 급습 시간이 다시 채워집니다.", 186, ExtraAId, S(3), S(8)),
            Evo("re.return", "귀환하는 칼날", "되돌아오는 표창이 두 개가 되고, 되돌아올 때마다 탄창에 한 발이 돌아옵니다.", 187, ExtraBId, S(4)),
            Evo("re.road", "칼날 폭풍길", "칼날 자국에서 표창이 계속 솟구쳐 가까운 적에게 날아갑니다.", 188, GlareId, S(5)),
            Evo("re.acrobat", "칼날 곡예", "던지기 연습이 최대 중첩일 때 맞힌 적 둘레로 칼날 고리가 터집니다. (공격력 150%)", 189, PierceId, S(6)),
            Evo("re.peak", "사냥의 절정", "필살기 게이지가 가득 찬 동안이나 필살기 중에 적을 쓰러뜨리면 그 자리에 표창 비가 내립니다.", 190, HungerId, S(7), S(9)),
        },
        new[]   // 궁수
        {
            Evo("ae.storm", "폭풍의 길", "바람길 위에서는 시위를 순식간에 가득 당기고, 물러난 자리에 돌풍이 터져 적을 밀어냅니다.", 191, KnockId, S(0), S(6)),
            Evo("ae.master", "명궁의 호흡", "중첩이 가득 차면 다음 화살이 모든 적을 꿰뚫고, 덫에 걸린 적은 두 배 피해를 받습니다.", 192, ExtraAId, S(1), S(7)),
            Evo("ae.forest", "가시 숲", "가시 덤불이 1.5배 크게 자라고, 덤불 안의 적은 받는 피해가 20% 늘어납니다.", 193, GlareId, S(2)),
            Evo("ae.echo", "천 개의 메아리", "울림이 한 번 더 퍼지고, 울림에 맞은 적에게서 작은 화살이 튀어나갑니다.", 194, MultiId, S(3)),
            Evo("ae.brand", "사냥꾼의 낙인", "표식이 붙은 적이 쓰러지면 폭발하고, 표식 효과가 두 배가 됩니다.", 195, ExtraBId, S(4)),
            Evo("ae.bow", "만궁", "다섯 번째 적을 꿰뚫는 화살이 폭발하고, 분열한 화살도 꿰뚫을수록 피해가 증가합니다.", 196, PierceId, S(5)),
            Evo("ae.sky", "천공의 사냥", "유성이 세 개 떨어지고, 떨어진 자리에 별빛이 3초 동안 남아 적을 태웁니다.", 197, S(8), S(9)),
        },
        new[]   // 연금술사
        {
            Evo("le.swamp", "타오르는 늪", "불붙은 웅덩이가 4초 동안 타오르며 그 위의 적을 계속 태웁니다.", 198, ExtraBId, S(0), S(6)),
            Evo("le.frost", "영구 동토", "얼음 파편이 맞힌 적도 얼어붙고, 얼어붙은 적은 받는 피해가 30% 늘어납니다.", 199, MultiId, S(1)),
            Evo("le.collapse", "삼원소 붕괴", "원소 붕괴가 두 배 넓게 번지고, 휘말린 적에게는 원소 두 가지가 묻습니다.", 200, ExtraAId, S(2)),
            Evo("le.chain", "연쇄 대폭발", "연쇄 폭발로 쓰러진 적도 다시 터지고 (최대 3번), 연쇄될 때마다 체력을 조금 회복합니다.", 201, PierceId, S(3), S(7)),
            Evo("le.twins", "쌍둥이 조수", "조수가 하나 더 생겨 함께 플라스크를 던집니다.", 202, KnockId, S(4)),
            Evo("le.glass", "유리 폭풍", "필살기가 터진 뒤 3초 동안 주변에 유리 비가 계속 쏟아집니다.", 203, GlareId, S(5)),
            Evo("le.march", "불꽃 행진", "농축 플라스크가 터진 자리에 불길 고리가 퍼지고, 연소 흔적이 두 배로 커집니다.", 204, S(8), S(9)),
        },
    };

    // 모든 캐릭터 공통 (공용 카드끼리)
    static readonly SkillEvo[] CommonEvos =
    {
        Evo("ce.gold", "황금 시대", "코인을 주울 때마다 경험치를 조금 얻고, 코인 40개를 모을 때마다 황금 파동이 터져 주변 적에게 피해를 줍니다. (공격력 300%)", 205, 1, 5, 4),
        Evo("ce.body", "불멸의 육체", "체력이 가득 차 있으면 3초마다 심장 박동 파동이 퍼져 주변 적을 밀어내고 최대 체력에 비례한 피해를 줍니다.", 206, 8, 9, 10),
    };

    // 이 캐릭터가 노릴 수 있는 진화 (캐릭터 7종 + 공통 2종)
    public static List<SkillEvo> EvosFor(CharacterId who)
    {
        List<SkillEvo> list = new List<SkillEvo>();
        int w = (int)who;
        if (w >= 0 && w < CharEvos.Length) list.AddRange(CharEvos[w]);
        list.AddRange(CommonEvos);
        for (int i = 0; i < list.Count; i++) list[i].Id = 1000 + i;
        return list;
    }

    // ================================================================= 카드 이름 · 단계
    // 레벨업 카드 번호의 이름 (진화 재료 표시 · 도감용, 캐릭터마다 다름)
    public static string CardName(CharacterId who, int id)
    {
        KitCard c = IsSig(id) ? SigCardOf(who, id) : KitCardOf(who, id);
        if (c != null) return Loc.T(c.name);
        return id switch
        {
            1 => Loc.T("코인충"), 4 => Loc.T("더 많은 경험치"), 5 => Loc.T("코인 자석"),
            8 => Loc.T("강철같은 심장"), 9 => Loc.T("단단한 신체"), 10 => Loc.T("생명의 샘"), 11 => Loc.T("피의 굶주림"),
            _ => "",
        };
    }

    public static Sprite CardIcon(CharacterId who, int id)
    {
        KitCard c = IsSig(id) ? SigCardOf(who, id) : KitCardOf(who, id);
        if (c != null) return Resources.Load<Sprite>("Icons/ability_" + c.icon);
        if (id == 5) return Resources.Load<Sprite>("Icons/ability_104");
        CodexData data = CodexData.Load();
        return data != null && data.abilityIcons != null && id >= 0 && id < data.abilityIcons.Length ? data.abilityIcons[id] : null;
    }

    // 도감용 카드 설명 (게임 밖에서도): 한 문장 + 단계별 수치 전부
    public static string CardInfo(CharacterId who, int id)
    {
        KitCard c = KitCardOf(who, id);
        if (c != null)
        {
            string[] v = new string[c.max];
            for (int i = 0; i < c.max; i++) v[i] = Val(c.values[i]);
            return Loc.T(c.desc) + "\n<color=#F5D478>" + Loc.T(c.stat) + "  " + string.Join(" / ", v) + "</color>";
        }
        string d = id switch
        {
            1 => "코인을 주울 때 더 많이 얻습니다.", 4 => "적을 처치할 때 얻는 경험치가 늘어납니다.", 5 => "주변의 코인을 끌어옵니다.",
            8 => "최대 체력이 늘어나고, 선택하는 순간 체력을 모두 회복합니다.", 9 => "적에게 받는 피해가 줄어듭니다.",
            10 => "시간이 지나면 체력이 조금씩 회복됩니다.", _ => "",
        };
        int max = MaxLevelOf(who, id);
        return Loc.T(d) + (max > 0 ? "\n<color=#A89C86>" + Loc.T("최대") + " Lv " + max + "</color>" : "");
    }

    // 최대 단계 (0 = 끝없음)
    public static int MaxLevelOf(CharacterId who, int id)
    {
        if (IsSig(id)) return SigMaxLevel;
        KitCard c = KitCardOf(who, id);
        if (c != null) return c.max;
        return id switch
        {
            1 => CoinMaxLevel, 4 => ExpMaxLevel, 5 => MagnetMaxLevel, 8 => HeartMaxLevel,
            9 => DefMaxLevel, 10 => RegenMaxLevel, 11 => HealOnKillMaxLevel, _ => 0,
        };
    }

    public int LevelOf(int id) => id >= 0 && id < ability_level.Length ? ability_level[id] : 0;
    public bool Maxed(int id)
    {
        int max = MaxLevelOf(CharacterData.Selected, id);
        return max > 0 && LevelOf(id) >= max;
    }

    // ================================================================= 레벨업 흐름
    void SigSetAbilitys()
    {
        for (int id = SigFirstId; id < SigFirstId + SigCount && id < ability_name.Length; id++)
        {
            KitCard c = SigCardOf(CharacterData.Selected, id);
            if (c == null) continue;
            ability_name[id] = Loc.T(c.name);
            ability_content[id] = KitText(c, ability_level[id]);
        }
    }

    // 아직 고를 수 없는 새 스킬 (먼저 배워야 하는 스킬이 없음)
    bool SigLocked(int id)
    {
        int need = SigRequires(CharacterData.Selected, id);
        return need >= 0 && LevelOf(need) <= 0;
    }

    bool SigApply(int id)
    {
        if (!IsSig(id)) return false;
        if (ability_level[id] >= SigMaxLevel) ability_selected[id] = true;
        SignatureSkills.Ensure()?.Refresh(this);
        return true;
    }

    // ================================================================= 진화 확인
    readonly HashSet<string> evolved = new HashSet<string>();
    public bool IsEvolved(string key) => evolved.Contains(key);
    List<SkillEvo> myEvos;
    public List<SkillEvo> MyEvos => myEvos ??= EvosFor(CharacterData.Selected);

    // 고른 카드로 재료가 모두 최대가 된 진화를 차례로
    void CheckEvolutions()
    {
        foreach (SkillEvo e in MyEvos)
        {
            if (evolved.Contains(e.key)) continue;
            bool all = true;
            foreach (int p in e.parts) if (!Maxed(p)) { all = false; break; }
            if (!all) continue;
            evolved.Add(e.key);
            EvolveAchievements(e);
            SignatureSkills.Ensure()?.Refresh(this);
            if (abilityHUD != null) abilityHUD.Merge(e.parts, e.Id);
            SkillEvolutionUI.Queue(e, CharacterData.Selected);
        }
    }

    // 레벨업 카드 뽑기 가중치: 이 카드가 재료인 진화가 가까울수록 올라감
    // 진행도 = 재료 단계 합 / 최대 단계 합 (아직 시작하지 않은 조합은 0), 가중치 1 + 0.8 × 진행도 (최대 1.8배)
    // 2.1.6: 진행 중인 진화의 재료는 거기에 1.4배 더 (진화에 가까운 스킬이 40% 더 자주 나옴, 최대 2.52배)
    float DrawWeight(int id)
    {
        float best = 0f;
        foreach (SkillEvo e in MyEvos)
        {
            if (evolved.Contains(e.key) || System.Array.IndexOf(e.parts, id) < 0) continue;
            int have = 0, need = 0;
            foreach (int p in e.parts)
            {
                int max = MaxLevelOf(CharacterData.Selected, p);
                need += max;
                have += Mathf.Min(LevelOf(p), max);
            }
            if (need > 0) best = Mathf.Max(best, have / (float)need);
        }
        return best > 0f ? (1f + 0.8f * best) * EvoDrawBoost : 1f;
    }

    const float EvoDrawBoost = 1.4f;

    // 이 카드를 고른 뒤의 진화 진행도 (가장 가까운 진화 기준, 고르면 바로 진화하면 1을 더함 · 진화 재료가 아니면 0)
    float EvoProgressAfter(int id)
    {
        float best = 0f;
        foreach (SkillEvo e in MyEvos)
        {
            if (evolved.Contains(e.key) || System.Array.IndexOf(e.parts, id) < 0) continue;
            int have = 0, need = 0;
            foreach (int p in e.parts)
            {
                int max = MaxLevelOf(CharacterData.Selected, p);
                need += max;
                have += Mathf.Min(LevelOf(p) + (p == id ? 1 : 0), max);
            }
            if (need <= 0) continue;
            best = Mathf.Max(best, have / (float)need + (have >= need ? 1f : 0f));
        }
        return best;
    }

    // 업적: 첫 스킬 진화 · 한 판에 3번 · 서로 다른 진화 15종 (캐릭터별 진화를 이 PC에 누적)
    void EvolveAchievements(SkillEvo e)
    {
        SteamAchievements.Unlock(SteamAchievements.SkillEvolve);
        if (evolved.Count >= 3) SteamAchievements.Unlock(SteamAchievements.SkillEvolve3);
        if (GameInput.Auto) return;
        const string Key = "stats.skillEvos";
        HashSet<string> seen = new HashSet<string>(Prefs.GetString(Key, "").Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries));
        if (seen.Add(CharacterData.Selected + ":" + e.key))
        {
            Prefs.SetString(Key, string.Join("|", seen));
            Prefs.Save();
        }
        if (seen.Count >= 15) SteamAchievements.Unlock(SteamAchievements.SkillEvolve15);
    }

    // 카드에 붙는 진화 미리보기: 이 카드가 재료인 (아직 안 된) 진화
    public SkillEvo EvoUsing(int id)
    {
        foreach (SkillEvo e in MyEvos)
            if (!evolved.Contains(e.key) && System.Array.IndexOf(e.parts, id) >= 0) return e;
        return null;
    }

    public SkillEvo EvoById(int hudId)
    {
        foreach (SkillEvo e in MyEvos) if (e.Id == hudId) return e;
        return null;
    }

    public static string EvoTooltip(SkillEvo e, CharacterId who)
    {
        List<string> names = new List<string>();
        foreach (int p in e.parts) names.Add(CardName(who, p));
        return Loc.T(e.desc) + "\n<color=#C9A0FF>" + Loc.T("재료") + "  " + string.Join(" + ", names) + "</color>";
    }
}
