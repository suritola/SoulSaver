using System.Collections.Generic;
using UnityEngine;

// 스킨 (1.8.3~): 메인 메뉴 스킨 상점에서 포인트로 사서 장착. 겉모습 + 소리가 바뀌고 판정 · 크기는 그대로
//   캐릭터 스킨: 몸 그림 · 피격 소리 (영웅부터 이동 잔상, 전설은 오라 · 처치 연출 · 필살기 소리)
//   무기 스킨:   손에 든 무기 · 총알/투사체 색 · 공격 소리
//   이펙트 스킨: 명중 불꽃 · 처치 효과 · 코인 효과와 소리 (모든 캐릭터 공통)
//   커서 스킨 (1.8.7~): 마우스 포인터 색 · 모양 (모든 캐릭터 공통, CursorSkin)
// 등급: 0 일반 · 1 희귀 · 2 영웅 · 3 전설 (가격 5천 · 1만 5천 · 4만 · 10만)
public enum SkinKind { Character, Weapon, Effect, Cursor }

public class SkinDef
{
    public string id;               // 저장 · 그림 이름 (예: swordsman__dragon)
    public SkinKind kind;
    public int who = -1;            // 캐릭터 (-1 = 모든 캐릭터)
    public int tier;
    public string name, desc;       // 한국어 (Loc.T 로 번역)
    public Color color;             // 테마 색 (잔상 · 오라 · 투사체 · 불꽃)
    public string attackSound;      // 공격 소리를 바꿀 이름 (없으면 원래 소리 + 음높이 · 덧소리)
    public float pitch = 1f;        // 공격 소리 음높이
    public string layer;            // 공격할 때 겹쳐 나는 소리
    public string hurtSound;        // 피격 소리
    public string killSound;        // 처치 소리 (이펙트 스킨 · 전설)
    public bool achievement;        // 2.1.0~ 업적형 전설: 상점에서 못 사고 업적으로만 얻음 (SkinData.UnlockByAchievement)
    public int Price => SkinData.Prices[tier];
}

public static class SkinData
{
    public static readonly int[] Prices = { 5000, 15000, 40000, 100000 };
    public static readonly string[] TierNames = { "skin.tier.common", "희귀", "영웅", "전설" };      // 2.0.1~ 「일반」 키는 설정 탭(General)과 겹쳐 등급만 따로
    public static readonly Color[] TierColors =
    {
        new Color(0.72f, 0.72f, 0.76f), new Color(0.35f, 0.65f, 1f), new Color(0.75f, 0.45f, 1f), new Color(1f, 0.8f, 0.3f),
    };

    static SkinDef C(string id, int who, int tier, string name, string desc, Color color, string hurt = null, string attack = null, float pitch = 1f, string layer = null)
        => new SkinDef { id = id, kind = SkinKind.Character, who = who, tier = tier, name = name, desc = desc, color = color, hurtSound = hurt, attackSound = attack, pitch = pitch, layer = layer };
    // 업적형 전설 (상점 가격 대신 잠금 표시만 · 얻는 판정은 사막 비밀 작업에서 UnlockByAchievement 로 연결)
    static SkinDef CA(string id, int who, string name, string desc, Color color, string hurt)
        => new SkinDef { id = id, kind = SkinKind.Character, who = who, tier = 3, name = name, desc = desc, color = color, hurtSound = hurt, achievement = true };
    static SkinDef W(string id, int who, int tier, string name, string desc, Color color, float pitch, string layer, string attack = null)
        => new SkinDef { id = id, kind = SkinKind.Weapon, who = who, tier = tier, name = name, desc = desc, color = color, pitch = pitch, layer = layer, attackSound = attack };
    static SkinDef E(string id, int tier, string name, string desc, Color color, string kill)
        => new SkinDef { id = id, kind = SkinKind.Effect, tier = tier, name = name, desc = desc, color = color, killSound = kill };
    static SkinDef P(string id, int tier, string name, string desc, Color color)
        => new SkinDef { id = id, kind = SkinKind.Cursor, tier = tier, name = name, desc = desc, color = color };

    // 그 영웅의 스킨이 하나라도 있는가 (스킨 상점 · 없으면 「준비 중」)
    public static bool HasSkins(int who)
    {
        foreach (SkinDef s in All) if (!IsGlobal(s.kind) && s.who == who) return true;
        return false;
    }

    // 모든 캐릭터가 같이 쓰는 종류 (캐릭터 고르기 없음)
    public static bool IsGlobal(SkinKind kind) => kind == SkinKind.Effect || kind == SkinKind.Cursor;

    const int G = 0, S = 1, R = 2, A = 3, L = 4, M = 5, K = 6, P_ = 7, H = 8, WR = 9, FM = 10, PU = 11, UT = 12, PH = 13;      // 거너 · 검사 · 도적 · 궁수 · 연금술사 · 지도 제작자 · 시계공 · 우체부 · 유령 · 씨름꾼 · 공사 반장 · 인형사 · 장의사 · 사진사 (CharacterId 순서)

    public static readonly SkinDef[] All =
    {
        // ---------------- 캐릭터
        C("gunner_nogun__desert", G, 0, "사막 방랑자", "모래빛 코트와 먼지 낀 모자. 걸을 때 부츠 자국과 모래 사각 소리, 레벨업하면 모래 회오리가 솟고, 쓰러지면 모래로 흩날리며 모자만 굴러갑니다.", new Color(0.85f, 0.72f, 0.45f), "sk_hurt_low"),
        C("gunner_nogun__bounty", G, 1, "붉은 현상금 사냥꾼", "검은 가죽 코트에 붉은 목도리. 걸음마다 박차가 찰캉, 필살기엔 휘파람과 함께 탄피가 떨어지고, 레벨업하면 수배지가 떠오르며 이기면 수배지에 총알 구멍이 뚫립니다.", new Color(0.9f, 0.25f, 0.3f), "sk_hurt_whistle"),
        C("gunner_nogun__ghost", G, 2, "유령 총잡이", "빛바랜 푸른 코트의 유령. 움직이면 잔상이 남고, 맞으면 소리가 메아리치며, 쉬면 분신이 곁에 서고, 이기면 분신 셋이 모자를 던져 올립니다.", new Color(0.7f, 0.95f, 1f), "sk_hurt_echo"),
        C("gunner_nogun__gold", G, 3, "황금 심판자", "황금빛 심판자. 쉬면 머리 위에 저울이 뜨고, 걸음마다 금빛이 번쩍이며, 필살기에 종소리가 울리고, 이기면 판사봉 「쾅」과 함께 금화가 쏟아집니다.", new Color(1f, 0.82f, 0.3f), "sk_hurt_bell"),

        C("swordsman__bronze", S, 0, "청동 기사", "오래된 청동 갑옷. 걸을 때 철컥거리고 휘두르면 녹 가루가 흩날리며, 레벨업하면 나팔이 울리고 쓰러지면 청동 조각이 와르르 쏟아집니다.", new Color(0.85f, 0.6f, 0.35f), "sk_hurt_low"),
        C("swordsman__black", S, 1, "흑기사", "칠흑 갑옷에 붉은 깃털. 걸음이 쿵 철컥 무겁고 붉은 깃털이 흩날리며, 레벨업하면 북과 나팔 속에 투구 문장이 솟고, 쓰러지면 갑옷이 와르르 무너집니다.", new Color(0.8f, 0.15f, 0.2f), "sk_hurt_metal"),
        C("swordsman__paladin", S, 2, "성기사", "흰 갑옷과 금빛 장식, 푸른 망토. 움직이면 빛 잔상이 남고, 레벨업하면 빛기둥에 날개가 펼쳐지며 성가가 울리고, 쓰러지면 깃털이 내려앉습니다.", new Color(1f, 0.92f, 0.6f), "sk_hurt_chime"),
        C("swordsman__dragon", S, 3, "용기사", "용의 뿔 투구와 붉은 비늘 갑옷. 불티가 감돌고, 필살기에 용의 포효와 함께 날개가 펼쳐지며, 레벨업하면 용이 솟고 쓰러지면 잿빛으로 식어 버립니다.", new Color(1f, 0.45f, 0.15f), "sk_hurt_roar"),

        C("rogue__sand", R, 0, "모래 도적", "모래빛 두건의 조용한 도적. 발소리는 사박사박 작고, 던질 때 모래 한 줌이 흩어지며, 쓰러지면 단검만 남기고 모래 소용돌이로 사라집니다.", new Color(0.85f, 0.7f, 0.45f), "sk_hurt_light"),
        C("rogue__raven", R, 1, "밤까마귀", "검은 두건과 붉은 목도리, 하얗게 빛나는 눈. 쉬면 어깨에 까마귀가 앉고, 맞으면 「까악」 깃털이 터지며, 쓰러지면 까마귀 떼로 흩어집니다.", new Color(0.75f, 0.1f, 0.2f), "sk_hurt_flap"),
        C("rogue__ninja", R, 2, "닌자", "남색 복면과 길게 휘날리는 붉은 목도리. 발소리가 거의 없고 출발 · 멈춤마다 연막이 「펑」, 필살기엔 표창 그림자가 솟고 되살아날 땐 통나무만 남깁니다.", new Color(0.3f, 0.4f, 0.8f), "sk_hurt_smoke"),
        C("rogue__void", R, 3, "그림자 군주", "허공의 보랏빛 망토와 빛나는 분홍 눈. 그림자 연기가 감돌고, 쉬면 그림자가 딴 데를 보며, 필살기에 속삭임, 레벨업하면 왕관이 내려앉고 쓰러지면 바닥으로 스며듭니다.", new Color(0.65f, 0.3f, 1f), "sk_hurt_void"),

        C("archer__autumn", A, 0, "가을 사냥꾼", "단풍빛 두건. 걸음마다 단풍잎이 떨어지며 바스락거리고, 레벨업하면 단풍잎 회오리, 필살기에 사냥 뿔피리가 울리고 이기면 단풍잎이 폭죽처럼 터집니다.", new Color(0.95f, 0.55f, 0.2f), "sk_hurt_light"),
        C("archer__snow", A, 1, "설원 궁수", "흰 모피 두건과 얼음빛 옷. 걸음마다 눈이 뽀득, 쉬면 입김이 나고, 레벨업하면 얼음 결정 기둥이 솟으며 이기면 오로라가 걸립니다.", new Color(0.7f, 0.88f, 1f), "sk_hurt_ice"),
        C("archer__elf", A, 2, "엘프 레인저", "에메랄드 두건과 금빛 머리띠. 걸음마다 새싹이 돋고, 쉬면 금빛 잎이 팔랑이며, 레벨업하면 잎이 감아 오르며 꽃잎이 터지고, 이기면 꽃잎이 흩날립니다.", new Color(0.35f, 0.9f, 0.5f), "sk_hurt_wind"),
        C("archer__sun", A, 3, "태양의 궁수", "태양빛 두건과 흰 옷. 쉬면 머리 위에 작은 태양이 돌고, 걸음마다 햇살이 번지며, 필살기에 성가와 빛기둥이 솟고, 쓰러지면 노을빛으로 저뭅니다.", new Color(1f, 0.85f, 0.35f), "sk_hurt_bell"),

        C("alchemist__herb", L, 0, "초록 약사", "약초빛 옷. 걸을 때 주머니 속 유리병이 짤랑이고, 던질 때 코르크가 「뽁」, 레벨업하면 초록 거품 기둥이 보글보글 솟고 이기면 거품 분수가 터집니다.", new Color(0.45f, 0.8f, 0.45f), "sk_hurt_bubble"),
        C("alchemist__plague", L, 1, "역병 의사", "검은 외투와 하얀 부리 가면. 두 걸음마다 지팡이가 「톡」, 향 연기가 따라오고, 레벨업하면 촛불이 뜨며 오르간이 울리고, 쓰러지면 가면만 떨어집니다.", new Color(0.8f, 0.2f, 0.25f), "sk_hurt_metal"),
        C("alchemist__mercury", L, 2, "수은 학자", "은빛 외투와 푸른 고글. 걸음마다 수은 방울이 발로 모여들고, 맞으면 유리 울림이 「쨍」, 쉬면 손 옆에 수정이 돌며 이기면 수은이 솟고 은빛 왕관이 뜹니다.", new Color(0.75f, 0.85f, 0.95f), "sk_hurt_chime"),
        C("alchemist__stone", L, 3, "현자의 돌", "진홍 외투와 금빛 머리. 쉬면 붉은 돌이 곁을 돌고, 걸음마다 금빛 문양이 빛나며, 필살기에 신비로운 공명이 울리고, 쓰러지면 금가루로 바스러집니다.", new Color(1f, 0.3f, 0.25f), "sk_hurt_void"),

        // ---------------- 캐릭터: 새 영웅 넷 (2.0.0~ · 지도 제작자 · 시계공 · 우체부 · 유령)
        C("cartographer__drafter", M, 0, "제도사", "카키 조끼와 올리브 모자, 등에 멘 도면통. 걸음마다 연필 낙서와 사각 소리, 레벨업하면 도면이 촤악 펼쳐지고 이기면 측량 깃발이 꽂힙니다.", new Color(0.78f, 0.7f, 0.48f), "sk_hurt_light"),
        C("cartographer__hydro", M, 1, "해도사", "남색 외투와 선장 모자, 손에 육분의. 걸음마다 물방울이 튀고, 쉬면 갈매기가 지나가며, 레벨업하면 나침반이 돌고 쓰러지면 모자만 물에 뜹니다.", new Color(0.25f, 0.6f, 0.75f), "sk_hurt_chime"),
        C("cartographer__starmap", M, 2, "별지도꾼", "별을 꽂은 밤하늘 망토. 걸을 때마다 별이 반짝이며 음계를 한 음씩 연주하고, 쉬면 유성이 떨어지며, 레벨업하면 별이 솟고 이기면 유성우가 쏟아집니다.", new Color(0.95f, 0.85f, 0.4f), "sk_hurt_wind"),
        C("cartographer__blank", M, 3, "백지도의 저자", "종이로 된 몸에 빨간 교정 펜. 종잇조각이 흩날리고, 평타마다 교정 체크가 「슥」, 필살기에 종이 찢는 소리, 쓰러지면 구겨진 종이 공이 굴러갑니다.", new Color(0.95f, 0.3f, 0.3f), "sk_hurt_paper"),

        C("clockmaker__apprentice", K, 0, "견습 시계공", "가죽 앞치마와 쇠 톱니. 걸음이 째깍째깍 울리고, 던질 때 나사가 튀며, 레벨업하면 톱니 셋이 맞물려 돌고 쓰러지면 째깍 소리가 느려지다 멈춥니다.", new Color(0.6f, 0.65f, 0.75f), "sk_hurt_metal"),
        C("clockmaker__hourglass", K, 1, "모래시계 수도사", "모래빛 후드와 모래시계 목걸이. 걸음마다 모래가 흘러내리고, 평타마다 모래시계가 뒤집히며, 레벨업하면 종과 성가가 울리고 쓰러지면 유리가 깨져 모래가 쏟아집니다.", new Color(0.9f, 0.75f, 0.4f), "sk_hurt_chime"),
        C("clockmaker__cuckoo", K, 2, "뻐꾸기 시계", "등에 멘 뻐꾸기 시계 집과 초록 모자. 걸음이 똑 · 딱 번갈아 울리고, 출발하면 뻐꾸기가 「뻐꾹」 튀어나오며, 맞아도 「뻐꾹」, 이기면 뻐꾸기 넷이 정시를 알립니다.", new Color(0.4f, 0.7f, 0.4f), "sk_hurt_cuckoo"),
        C("clockmaker__stopped", K, 3, "멈춘 시계의 주인", "11시 59분에 멈춘 깨진 시계판. 머리 위에 회중시계가 떠 있고, 출발할 때 시간이 잠깐 멈추며, 필살기에 큰 종소리, 쓰러지면 유리와 나사로 흩어집니다.", new Color(0.7f, 0.9f, 0.95f), "sk_hurt_bell"),

        C("postman__paperboy", P_, 0, "신문 배달부", "파란 야구 모자와 둘둘 만 신문. 운동화 발소리 사이로 자전거 벨이 따릉, 레벨업하면 신문 1면이 빙글 돌며 나타나고 맞으면 신문지가 흩날립니다.", new Color(0.35f, 0.55f, 0.9f), "sk_hurt_light"),
        C("postman__pigeon", P_, 1, "비둘기 조련사", "모자 위에 비둘기가 앉은 잿빛 외투. 여덟 걸음마다 「구구」, 맞으면 깃털이 흩날리고, 레벨업하면 비둘기 떼가 솟으며 이기면 흰 비둘기가 하늘로 날아갑니다.", new Color(0.6f, 0.65f, 0.75f), "sk_hurt_flap"),
        C("postman__courier", P_, 2, "택배 기사", "형광 조끼에 택배 상자와 단말기. 열두 걸음마다 바코드 「삑」, 레벨업하면 상자가 솟으며 「띵동」, 처치를 이으면 상자가 날아들고, 이기면 상자가 쌓입니다.", new Color(1f, 0.6f, 0.15f), "sk_hurt_beep"),
        C("postman__return", P_, 3, "반송 우체부", "검은 제복에 빨간 RETURN 도장과 밀랍 봉인. 봉투가 감돌고, 걸음마다 봉투 자국이 찍히며, 필살기에 쿵쿵쿵 도장 소리, 레벨업하면 머리 위에 도장이 「쾅」 찍힙니다.", new Color(0.9f, 0.15f, 0.2f), "sk_hurt_metal"),

        C("ghost__sheet", H, 0, "이불 유령", "체크무늬 이불에 잠옷 모자를 쓴 유령. 걸으면 이불 자락이 사락 끌리고, 쉬면 코를 골며, 레벨업하면 별 · 달 모빌이 자장가와 함께 내려옵니다.", new Color(0.7f, 0.82f, 0.95f), "sk_hurt_light"),
        C("ghost__wisp", H, 1, "도깨비불", "청록 불꽃으로 타오르는 유령. 걸으면 꺼진 불씨 자국이 남고, 쉬면 저만치 떨어진 곳에 불이 깜빡 떴다 사라지며, 쓰러지면 연기 한 줄기만 남습니다.", new Color(0.4f, 0.95f, 0.8f), "sk_hurt_smoke"),
        C("ghost__deepsea", H, 2, "심해 유령", "발광 반점과 낚싯대 불빛. 걸음마다 거품이 오르고, 쉬면 물고기가 곁을 맴돌며, 레벨업하면 해파리가 떠오르고, 쓰러지면 깊은 물속으로 가라앉습니다.", new Color(0.25f, 0.9f, 0.75f), "sk_hurt_bubble"),
        C("ghost__deleted", H, 3, "삭제된 장면", "반은 그려지고 반은 마젠타 와이어프레임. 걸음마다 픽셀 자국이 남고, 쉬면 깜빡이는 입력 커서가 뜨며, 필살기에 모뎀 소리, 쓰러지면 위에서부터 지워집니다.", new Color(1f, 0.2f, 0.85f), "sk_hurt_pixel"),

        // ---------------- 캐릭터: 새 영웅 둘 (2.1.0~ · 씨름꾼 · 공사 반장) — 전설은 업적형
        C("wrestler__local", WR, 0, "동네 장사", "흰 러닝셔츠에 운동회 머리띠, 마을 대회 단골 우승자. 잡을 때 손바닥이 「짝」, 레벨업하면 만국기가 펼쳐지고 이기면 황소 트로피와 함성이 터집니다.", new Color(0.85f, 0.25f, 0.3f), "sk_hurt_light"),
        C("wrestler__bronze", WR, 1, "청동 샅바", "샅바와 팔 토시가 청동이라 메칠 때마다 쇳소리가 납니다. 걸으면 청동이 번쩍이고, 필살기엔 징이 울리며, 이기면 청동 트로피가 솟습니다.", new Color(0.85f, 0.58f, 0.25f), "sk_hurt_metal"),
        C("wrestler__crypt", WR, 2, "묘역의 장사", "해골 샅바를 맨 묘역의 장사. 걸음마다 뼈가 달그락, 레벨업하면 해골 관중이 떠오르며 함성이 터지고, 쓰러지면 뼈가 와르르 무너집니다.", new Color(0.55f, 1f, 0.7f), "sk_hurt_wind"),
        CA("wrestler__referee", WR, "심판", "줄무늬 심판복에 호루라기 「삑」. 발밑에 모래가 튀고, 출발하면 깃발을 들며, 필살기에 호루라기와 함성이 울리고, 되살아날 땐 호루라기가 세 번 울립니다. (업적형)", new Color(0.91f, 0.76f, 0.48f), "sk_hurt_whistle"),
        C("foreman__night", FM, 0, "야간 작업", "야광 조끼에 이마 전등, 밤에도 공사는 계속된다. 발 앞을 전등 빛이 비추고, 레벨업하면 서치라이트가 엇갈리며 사이렌, 필살기에 후진 경보가 울립니다.", new Color(0.72f, 0.94f, 0.13f), "sk_hurt_beep"),
        C("foreman__snow", FM, 1, "제설차", "눈 쌓인 안전모에 빨간 목도리. 걸음마다 눈이 뽀득, 퍼낼 때 눈이 흩날리고, 레벨업하면 눈사람이 솟으며 쓰러지면 눈덩이처럼 굳어 버립니다.", new Color(0.65f, 0.85f, 1f), "sk_hurt_ice"),
        C("foreman__hell", FM, 2, "지옥 공사", "뿔 돋은 그을린 안전모에 용암빛 조끼. 걸음마다 불티가 오르고, 레벨업하면 모루가 솟아 「땅 땅」 울리며, 쓰러지면 잿빛으로 굳고 안전모만 남습니다.", new Color(1f, 0.38f, 0.1f), "sk_hurt_roar"),
        CA("foreman__beta", FM, "베타 테스터", "BETA 창틀 안전모, 처치하면 초록 체크 표시. 걸음마다 창틀 자국이 찍히고, 레벨업하면 진행 막대가 차오르며, 필살기에 딸깍, 쓰러지면 오류음과 함께 꺼집니다. (업적형)", new Color(0.45f, 0.85f, 0.5f), "sk_hurt_pixel"),

        // ---------------- 캐릭터: 새 영웅 셋 (2.2.0~ · 인형사 · 장의사 · 사진사) — 전설은 업적형
        C("puppeteer__shadow", PU, 0, "그림자극", "검은 종이 실루엣 연출가, 더미도 그림자 인형이 되어 벽에 비친 듯 납작합니다. 레벨업하면 새 실루엣이 날아오르며 징이 울리고, 쓰러지면 촛불이 꺼집니다.", new Color(0.4f, 0.35f, 0.6f), "sk_hurt_light"),
        C("puppeteer__paper", PU, 1, "종이 인형", "접은 종이 옷의 연출가, 더미는 접이식 종이 인형이고 줄은 실 대신 종이끈입니다. 출발하면 종이학이 날아오르고, 레벨업하면 종이꽃이 피며 오르골이 울립니다.", new Color(0.93f, 0.85f, 0.62f), "sk_hurt_paper"),
        C("puppeteer__crypt", PU, 2, "묘역 인형사", "묘역 무대의 연출가, 더미가 해골 인형이 되어 휘두를 때 뼈가 달그락거립니다. 움직이면 영혼불 잔상이 남고, 레벨업하면 해골이 춤추며 솟고, 필살기엔 막이 내립니다.", new Color(0.35f, 0.9f, 0.65f), "sk_hurt_wind"),
        CA("puppeteer__devhand", PU, "개발자의 손", "더미가 마우스 커서, 줄이 케이블, 커튼콜은 커서 여럿이 따닥따닥 클릭합니다. 처치하면 작은 창이 틱 닫히고 발밑에 작업 표시줄 띠가 깔리며, 레벨업하면 코드 줄이 써집니다. (업적형)", new Color(0.25f, 0.9f, 1f), "sk_hurt_pixel"),
        C("undertaker__keeper", UT, 0, "묘지기", "랜턴을 든 밤 묘지기, 구멍 가장자리에 랜턴 불빛이 비칩니다. 젖은 흙 발소리, 레벨업하면 반딧불 기둥과 부엉이 소리, 쓰러지면 랜턴이 떨어져 꺼집니다.", new Color(0.4f, 0.55f, 0.3f), "sk_hurt_light"),
        C("undertaker__mason", UT, 1, "석공", "돌가루 앉은 모자에 앞치마의 석공, 묘비 대신 작은 조각상이 솟습니다. 평타마다 정 소리 「탕」, 레벨업하면 조각상이 솟고 쓰러지면 돌로 굳습니다.", new Color(0.7f, 0.62f, 0.5f), "sk_hurt_metal"),
        C("undertaker__hell", UT, 2, "지옥 장의사", "지옥의 묘지기, 묘비가 불꽃 표식을 달고 솟습니다. 구덩이 안이 주황빛으로 타오르고, 쉬면 검은 백합이 피며, 레벨업하면 화환이 뜨며 조종이 울립니다.", new Color(1f, 0.45f, 0.15f), "sk_hurt_roar"),
        CA("undertaker__todo", UT, "미완성 묘비", "비문이 「TODO: 비문 쓰기」인 회색 상자 묘비. 매장 때 「저장 중…」 모래시계가 돌고 띵 저장음이 울리며, 처치하면 디스켓이 날아들고 이기면 디스켓이 쏟아집니다. (업적형)", new Color(0.78f, 0.78f, 0.82f), "sk_hurt_pixel"),
        C("photographer__press", PH, 0, "신문 기자", "기자 모자와 낡은 코트, 플래시가 조금 노랗습니다. 구두가 또각거리고, 레벨업하면 사진이 현상되듯 떠오르며, 필살기에 타자기 줄 바꿈 「띵」이 울립니다.", new Color(0.88f, 0.78f, 0.5f), "sk_hurt_light"),
        C("photographer__instant", PH, 1, "즉석 카메라", "무지개 줄무늬 재킷에 즉석 카메라, 인화 사본이 흰 테 사진으로 나옵니다. 셔터가 하얗게 번쩍이고, 평타마다 사진이 「지잉」 나오며, 이기면 사진이 분수처럼 쏟아집니다.", new Color(0.95f, 0.45f, 0.65f), "sk_hurt_beep"),
        C("photographer__ghost", PH, 2, "유령 사진가", "반투명한 몸에 보라 플래시, 찍힌 적 뒤로 희미한 영혼이 함께 찍힙니다. 셔터가 두 번 메아리치고, 레벨업하면 심령사진이 떨어지며, 쓰러지면 세피아빛으로 바랩니다.", new Color(0.7f, 0.55f, 1f), "sk_hurt_echo"),
        CA("photographer__f12", PH, "스크린샷 F12", "카메라가 F12 키, 셔터는 촤착, 적은 픽셀 캡처 조각이 되고 처치하면 썸네일로 줄어듭니다. 파노라마 때 모서리에 저장 완료 썸네일이 뜨고, 이기면 플래시가 연달아 터집니다. (업적형)", new Color(0.25f, 0.95f, 0.95f), "sk_hurt_pixel"),

        // ---------------- 무기 (공격 소리 · 투사체 색)
        W("pistol__silver", G, 1, "은장 리볼버", "은으로 새긴 권총. 총구가 은빛으로 번쩍이며 「탕—팅」 맑게 울리고, 맞히면 「팅」 금속음이 납니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.8f, 0.9f, 1f), 1.12f, "clank"),
        W("pistol__dragon", G, 2, "용의 숨결", "붉은 금빛 권총. 쏠 때 총구에서 작은 불꽃 용이 「탕—화륵」, 장전하면 불티가 튀고, 진화하면 불꽃 용이 감아 오릅니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(1f, 0.55f, 0.2f), 0.9f, "ignite"),
        W("sword__frost", S, 1, "푸른 서리검", "얼음빛 칼날. 휘두르면 서리가 흩날리며 「쉬잉—」, 맞히면 얼음이 「챙」 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.6f, 0.9f, 1f), 1.1f, "shimmer"),
        W("sword__obsidian", S, 2, "흑요석 마검", "검보랏빛 마검. 휘두르면 번개가 「지직」 튀고, 쉴 땐 칼날에 번개가 맴돌며, 큰 한 방엔 칼자국이 유리처럼 깨집니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.65f, 0.35f, 1f), 0.85f, "zap"),
        W("shuriken__gold", R, 1, "황금 표창", "금으로 만든 표창. 던지면 금빛이 반짝이며 「쉬잉—팅」, 맞히면 맑게 「팅」 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(1f, 0.85f, 0.35f), 1.15f, "sparkle"),
        W("shuriken__bloodmoon", R, 2, "혈월 표창", "핏빛 초승달 표창. 던지면 붉은 초승달이 「슈욱」 날고, 장전할 땐 손끝에 표창이 겹쳐 반짝이며, 진화하면 혈월이 떠오르고 징이 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(1f, 0.25f, 0.3f), 0.85f, "slash"),
        W("bow__silver", A, 1, "은빛 활", "은으로 감은 활. 시위가 「티잉」 맑게 울리며 은빛이 번쩍이고, 맞히면 금속음이 납니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.85f, 0.92f, 1f), 1.12f, "ding"),
        W("bow__worldtree", A, 2, "세계수 활", "살아 있는 나무 활. 쏠 때 잎이 날리며 바람과 방울 소리, 쉴 땐 활에서 새싹이 돋았다 지고, 진화하면 잎과 꽃잎이 피어오릅니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.5f, 1f, 0.55f), 0.95f, "chime"),
        W("flask__crystal", L, 1, "수정 플라스크", "투명한 수정 플라스크. 던질 때 수정이 반짝이며 「찡」, 터지면 유리 울림이 맑게 퍼집니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.7f, 0.9f, 1f), 1.15f, "shimmer"),
        W("flask__lava", L, 2, "용암 플라스크", "끓어오르는 용암 플라스크. 던지면 「보글—퉁」, 쉴 땐 플라스크가 보글거리고, 큰 한 방엔 불티 분수가 솟으며, 진화하면 금빛 거품이 오릅니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(1f, 0.5f, 0.15f), 0.85f, "thump"),

        W("quill__brush", M, 1, "붓", "대나무 자루 붓. 획마다 먹물이 번지며 방울이 튀고, 맞히면 「똑」 먹물 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.3f, 0.35f, 0.65f), 1.1f, "shimmer"),
        W("quill__chalk", M, 2, "분필", "하얀 분필. 획이 가루처럼 흩날리며 사각거리고, 쉴 땐 공중에 별 낙서가 뜨며, 진화하면 무지갯빛 분필 가루가 솟습니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.95f, 0.95f, 1f), 1.2f, "sparkle"),
        W("gear__coin", K, 1, "금화", "금화를 던집니다. 금화가 빙글 돌며 「팅~」, 맞히면 「짤랑」 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(1f, 0.82f, 0.3f), 1.15f, "ding"),
        W("gear__abacus", K, 2, "주판알", "윤나는 주판알. 던지면 「딱」, 맞히면 「딱딱」, 쉴 땐 손 위에서 알이 오가고, 진화하면 금빛 알이 차르륵 솟습니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.75f, 0.45f, 0.25f), 1.3f, "clank"),
        W("letter__plane", P_, 1, "종이비행기", "접은 종이비행기. 날릴 때 「슈웅」 바람을 가르고, 맞히면 종이 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.85f, 0.9f, 1f), 1.2f, "shimmer"),
        W("letter__bill", P_, 2, "청구서", "붉은 머리띠 청구서. 던지면 도장이 「쿵」, 맞히면 계산대 종이 「땡」, 쉴 땐 영수증이 길게 나풀거리고, 큰 한 방엔 금화가 튑니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.9f, 0.2f, 0.3f), 0.9f, "thump"),
        W("lantern__candle", H, 1, "촛대", "일렁이는 촛대. 쏠 때 촛불이 번쩍 「화륵」 불씨가 오르고, 맞히면 촛농 떨어지는 「똑」 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(1f, 0.8f, 0.4f), 1f, "ignite"),
        W("lantern__chain", H, 2, "쇠사슬 등불", "쇠사슬을 두른 등불. 쏠 때 「철커덩」, 쉴 땐 등불이 사슬에 흔들리고, 큰 한 방엔 주황빛이 번지며, 진화하면 쇠사슬이 금빛으로 솟습니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(1f, 0.6f, 0.2f), 0.85f, "clank"),

        W("satba__rope", WR, 1, "밧줄", "누런 밧줄 샅바. 던질 때 손끝이 누렇게 빛나며 「툭—쉭」, 맞히면 묵직하게 「퍽」 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.85f, 0.7f, 0.4f), 0.9f, "thump"),
        W("satba__silk", WR, 2, "비단 띠", "금실 수놓은 비단 띠, 던질 때 띠가 길게 나부낍니다. 쉴 땐 손에서 띠가 하늘거리고, 큰 한 방엔 함성이 터지며, 진화하면 띠가 용처럼 휘감깁니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.95f, 0.45f, 0.65f), 1.15f, "shimmer"),
        W("plow__broom", FM, 1, "빗자루", "커다란 마당 빗자루로 쓸어 담습니다. 쓸 때 「쓱싹」 흙알이 날리고, 맞히면 툭 털리는 소리가 납니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.9f, 0.75f, 0.4f), 1.1f, "slash"),
        W("plow__pitchfork", FM, 2, "쇠스랑", "갈퀴 이빨로 긁어모으는 쇠스랑. 휘두르면 「끼긱」 불티가 튀고, 쉴 땐 바닥을 긁으며, 큰 한 방엔 공중에 이빨 자국 세 줄이 남습니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.8f, 0.85f, 0.95f), 0.95f, "clank"),

        W("dummy__bear", PU, 1, "곰 인형", "더미가 낡은 곰 인형으로 바뀌고, 휘두를 때 솜이 날립니다. 폭신하게 「퍽」, 맞히면 둔탁하게 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.7f, 0.5f, 0.3f), 0.9f, "thump"),
        W("dummy__wood", PU, 2, "목각 인형", "더미가 관절 목각 인형으로 바뀌고, 휘두를 때 「딱딱」 나무 소리가 납니다. 쉴 땐 나무 조각이 달그락, 큰 한 방엔 인형이 고개 숙여 인사하며 박수가 터집니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.85f, 0.65f, 0.4f), 1.3f, "clank"),
        W("shovel__pickaxe", UT, 1, "곡괭이", "곡괭이로 팝니다. 흙 튀김이 돌 조각으로 바뀌고, 「깡」 쇳소리와 함께 맞히면 돌이 부서집니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.7f, 0.75f, 0.85f), 0.85f, "clank"),
        W("shovel__spoon", UT, 2, "은수저", "은수저로 팝니다. 「사각」 작은 소리에 구멍이 반짝이고, 큰 한 방엔 빛줄기가 솟으며, 진화하면 금빛으로 번쩍입니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.9f, 0.95f, 1f), 1.3f, "sparkle"),
        W("camera__disposable", PH, 1, "일회용 카메라", "종이 상자 카메라, 셔터 소리가 「지익 딸깍」 납니다. 찍을 때 필름 톱니가 돌고, 맞히면 짧은 셔터음이 울립니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.95f, 0.78f, 0.45f), 1.15f, "clank"),
        W("camera__film", PH, 2, "필름 카메라", "주름 상자가 달린 옛 대형 카메라, 플래시 가루가 「펑」 터집니다. 장전하면 필름을 감고, 큰 한 방엔 커다란 사진과 박수가 터집니다. 진화 전 기본 평타의 그림이 바뀝니다.", new Color(0.95f, 0.8f, 0.4f), 0.9f, "thump"),

        // ---------------- 이펙트 (모든 캐릭터)
        E("skin_bluefire", 0, "푸른 불꽃", "명중하면 세 갈래 푸른 불꽃이 일고, 적이 쓰러지면 푸른 불꽃이 세로로 「확」 솟으며 가스불 소리가 납니다. 코인도 푸른 불씨로 반짝이며 울립니다.", new Color(0.45f, 0.75f, 1f), null),
        E("skin_sakura", 1, "벚꽃", "명중하면 꽃잎이 흩날리고, 적이 쓰러지면 꽃잎 회오리와 맑은 방울 소리, 연속 처치엔 벚꽃 가지가 피고 상자를 열면 꽃잎 분수가 솟습니다.", new Color(1f, 0.6f, 0.75f), "sk_kill_petal"),
        E("skin_thunder", 2, "뇌전", "명중하면 노란 번개가 튀고, 적이 쓰러지면 불티가 「지직」, 연속 처치엔 번개 다섯 줄기가 내리치고 큰 적을 쓰러뜨리면 하늘 번개와 천둥이 울립니다.", new Color(1f, 0.9f, 0.35f), "sk_kill_zap"),
        E("skin_starlight", 3, "별의 축복", "명중하면 무지갯빛 별, 적이 쓰러지면 큰 별이 솟으며 종소리가 울립니다. 평타 · 스킬 · 필살기 처치는 작은 별 · 혜성 · 초신성으로, 연속 처치는 오르골 음계가 됩니다.", Color.white, "sk_kill_star"),

        // ---------------- 커서 (메뉴 화살표 + 전투 조준점, 설정에서 게임 커서를 켰을 때)
        P("cursor_bone", 0, "뼈 커서", "빛바랜 뼛빛 화살표와 조준점. 메뉴를 누르면 뼈 조각이 튀며 「딱」, 버튼 위에선 달그락거리고, 전투에선 평타마다 조준점이 오므라듭니다.", new Color(0.84f, 0.82f, 0.75f)),
        P("cursor_ember", 1, "불씨 커서", "타오르는 주황빛 화살표와 불꽃 조준점. 누르면 불티가 튀며 「틱」, 버튼 위에선 「치익」, 메뉴에선 불티 꼬리가 따라오고 전투에선 조준점이 일렁입니다.", new Color(0.92f, 0.43f, 0.18f)),
        P("cursor_soul", 2, "영혼 커서", "보랏빛 영혼 화살표와 푸른 빛이 도는 조준점. 누르면 영혼 연기가 「휘잉」, 버튼 위에선 속삭임, 연기 꼬리가 따라오고 필살기 조준 중엔 해골 조준점이 뜹니다.", new Color(0.59f, 0.37f, 0.92f)),
        P("cursor_gold", 3, "황금 커서", "황금빛 화살표와 조준점. 누르면 금화가 반짝 「팅」, 버튼 위에선 「짤랑」, 금가루 꼬리가 따라오고, 필살기 조준 중엔 왕관 조준점, 메뉴의 금가루는 커서로 모여듭니다.", new Color(0.96f, 0.78f, 0.31f)),
    };

    static readonly Dictionary<string, SkinDef> byId = new Dictionary<string, SkinDef>();
    public static SkinDef Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (byId.Count == 0) foreach (SkinDef s in All) byId[s.id] = s;
        return byId.TryGetValue(id, out SkinDef d) ? d : null;
    }

    // ================================================================= 저장 (PlayerPrefs)
    public static bool Owns(string id) => Prefs.GetInt("skin.own." + id, 0) == 1;

    static string EquipKey(SkinKind kind, int who) => "skin.eq." + (int)kind + "." + (IsGlobal(kind) ? -1 : who);

    // 장착한 스킨 (없으면 null = 기본)
    public static SkinDef Equipped(SkinKind kind, int who) => Get(Prefs.GetString(EquipKey(kind, who), ""));
    public static SkinDef Equipped(SkinKind kind) => Equipped(kind, (int)CharacterData.Selected);

    public static void Equip(SkinDef s)
    {
        if (s == null || !Owns(s.id)) return;
        Prefs.SetString(EquipKey(s.kind, s.who), s.id);
        Prefs.Save();
    }

    public static void Unequip(SkinKind kind, int who)
    {
        Prefs.SetString(EquipKey(kind, who), "");
        Prefs.Save();
    }

    // 스킨을 샀을 때 (업적)
    public static event System.Action<SkinDef> Bought;

    // 업적형 전설 해금 (사막 비밀 작업에서 부름: 인형사 = 사막 더미 비밀 다시 풀기 → puppeteer__devhand · 사진사 = 북적이는 장면 → photographer__f12 · 장의사 = 미완성 묘비 업적 → undertaker__todo · 씨름꾼 = 황소 비밀 다시 풀기 → wrestler__referee · 공사 반장 = 베타 비밀 → foreman__beta)
    public static void UnlockByAchievement(string id)
    {
        SkinDef s = Get(id);
        if (s == null || !s.achievement || Owns(id)) return;
        Prefs.SetInt("skin.own." + id, 1);
        Prefs.Save();
    }

    // 사면 true (포인트 차감 · 바로 장착)
    public static bool Buy(SkinDef s)
    {
        if (s == null || s.achievement || Owns(s.id) || CharacterData.Points < s.Price) return false;
        CharacterData.SpendPoints(s.Price);
        Prefs.SetInt("skin.own." + s.id, 1);
        Equip(s);
        Bought?.Invoke(s);
        return true;
    }

    // ================================================================= 게임에서 쓰는 값
    // 캐릭터 몸 그림 시트 이름 (Resources/Characters/…)
    public static string BodySheet(string baseSheet)
    {
        SkinDef s = Equipped(SkinKind.Character);
        return s != null ? s.id : baseSheet;
    }

    // 손에 든 기본 무기 그림 (Resources/Weapons/weapon_…)
    public static string HeldSprite(string held)
    {
        SkinDef s = Equipped(SkinKind.Weapon);
        if (s != null) return "Weapons/weapon_" + s.id;
        // 2026-11-04~ 캐릭터 스킨 물체 (설명 약속 — 종이 인형 · 해골 인형 · 손가락 커서 더미 · F12 키캡): 무기 스킨이 없을 때 그 그림이 있으면
        SkinDef c = SkinFx.Off ? null : Equipped(SkinKind.Character);
        if (c != null)
        {
            string path = "Weapons/weapon_" + held + "__" + c.id;
            if (Resources.LoadAll<Sprite>(path).Length > 0) return path;
        }
        return "Weapons/weapon_" + held;
    }
}
