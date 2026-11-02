using UnityEngine;
using S = Impact.Sounds;

// 2.0.0~ 영웅마다 다른 손맛 서명 (Impact 가 읽음) — 같은 「줌 · 슬로우 · 흔들림 · 소리」라도 영웅마다 결이 다르게
//   평타 특수 조건(Attack) · 고유 스킬(Skill) · 진화 스킬(Evolve) · 필살기(Ult)
//   평타 · 고유 스킬은 줌 없음 (줌은 진화 · 필살기만 — 2.0.0 기획 D15)
//   값은 Impact.Caps 상한 안에서만 의미가 있음 (넘으면 잘림) — 슬로우 · 줌은 짧고 옅게, 대신 「터지는 소리 · 흔들림」으로 세게
//   새 영웅은 여기 한 갈래를 더함 (없으면 단계별 기본값)
public static class HeroImpacts
{
    static ImpactStyle M(float zoom, float slow, float shake, float shakeFor, string sound, float pitch = 1f, float vol = 0.8f,
                         string pre = null, Color flash = default, bool dir = false)
        => ImpactStyle.Make(zoom, slow, shake, shakeFor, sound, vol, pitch, pre, flash, dir);

    static Color Tint(CharacterId who, float a) { Color c = CharacterData.Ally(who); c.a = a; return c; }

    public static ImpactStyle Get(CharacterId who, ImpactTier tier)
    {
        Color f = Tint(who, 0.18f), fu = Tint(who, 0.28f);
        switch (who)
        {
            // 거너: 짝! 하는 탄 소리 → 필살기는 콰가강 일제 사격 (방향 없이 사방)
            case CharacterId.Gunner:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.06f, 0.26f, 0.16f, S.Crack, 0.9f, dir: true),            // 저주받은 마지막 탄 명중
                    ImpactTier.Skill => M(0f, 0f, 0.22f, 0.12f, S.Crack, 1.2f, 0.6f, dir: true),
                    ImpactTier.Evolve => M(0.06f, 0.1f, 0.4f, 0.2f, S.Kwagang, 1.15f, flash: f),
                    _ => M(0.12f, 0.22f, 0.75f, 0.32f, S.Kwagang, 1f, 0.9f, S.Suck, fu),
                };
            // 검사: 챙— 칼날 울림, 벤 방향으로 튕김 → 필살기는 휘몰아친 뒤 콰가강
            case CharacterId.Swordsman:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.08f, 0.3f, 0.16f, S.Zing, 1f, dir: true),                  // 4번째 베기 · 받아치기
                    ImpactTier.Skill => M(0f, 0f, 0.26f, 0.14f, S.Zing, 0.85f, 0.6f, dir: true),
                    ImpactTier.Evolve => M(0.07f, 0.12f, 0.45f, 0.22f, S.Kwagang, 1.1f, flash: f, dir: true),
                    _ => M(0.14f, 0.24f, 0.8f, 0.34f, S.Kwagang, 0.95f, 0.9f, S.Whoosh, fu),
                };
            // 도적: 짧고 날카로운 짝, 급소로 꽂히는 방향 → 필살기는 빨려 들었다 찢김
            case CharacterId.Rogue:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.06f, 0.24f, 0.12f, S.Crack, 1.35f, dir: true),             // 출혈 급소
                    ImpactTier.Skill => M(0f, 0f, 0.2f, 0.1f, S.Crack, 1.15f, 0.6f, dir: true),
                    ImpactTier.Evolve => M(0.06f, 0.1f, 0.4f, 0.2f, S.Glass, 0.9f, flash: f),
                    _ => M(0.12f, 0.2f, 0.7f, 0.28f, S.Crack, 0.75f, 0.95f, S.Suck, fu, true),
                };
            // 궁수: 「완벽!」은 챙 하는 시위 + 꽂힘 → 필살기 화살비는 쿠웅
            case CharacterId.Archer:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.07f, 0.22f, 0.14f, S.Zing, 1.3f, dir: true),               // 완벽!
                    ImpactTier.Skill => M(0f, 0f, 0.22f, 0.12f, S.Thud, 1.2f, 0.65f),
                    ImpactTier.Evolve => M(0.06f, 0.1f, 0.4f, 0.22f, S.Chime, 1.1f, flash: f),
                    _ => M(0.1f, 0.22f, 0.85f, 0.36f, S.Boom, 1.05f, 0.9f, S.Whoosh, fu),
                };
            // 연금술사: 펑 · 쨍그랑 유리 → 필살기 큰 플라스크는 빨려 들었다 쿠웅
            case CharacterId.Alchemist:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.07f, 0.32f, 0.16f, S.Boom, 1.3f),                          // 불안정 폭발
                    ImpactTier.Skill => M(0f, 0f, 0.22f, 0.12f, S.Glass, 1.2f, 0.6f),
                    ImpactTier.Evolve => M(0.07f, 0.12f, 0.48f, 0.24f, S.Boom, 1f, flash: f),
                    _ => M(0.15f, 0.26f, 0.9f, 0.38f, S.Boom, 0.85f, 0.95f, S.Suck, fu),
                };
            // 지도 제작자: 촤악 종이 · 쾅 도장 → 필살기 지도 접기는 접힌 방향으로 크게
            case CharacterId.Cartographer:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.08f, 0.28f, 0.16f, S.Paper, 1f),                          // 도형이 닫힘
                    ImpactTier.Skill => M(0f, 0f, 0.2f, 0.12f, S.Paper, 1.25f, 0.6f),
                    ImpactTier.Evolve => M(0.07f, 0.12f, 0.44f, 0.22f, S.Stamp, 0.9f, flash: f),
                    _ => M(0.14f, 0.24f, 0.8f, 0.34f, S.Paper, 0.8f, 0.95f, S.Whoosh, fu, true),
                };
            // 시계공: 째깍 — 「정각」은 시간이 가장 오래 멈칫 → 필살기 되감기는 징
            case CharacterId.Clockmaker:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.1f, 0.3f, 0.16f, S.Tick, 1f),                              // 정각 톱니
                    ImpactTier.Skill => M(0f, 0f, 0.2f, 0.12f, S.Tick, 1.25f, 0.6f),
                    ImpactTier.Evolve => M(0.07f, 0.14f, 0.44f, 0.22f, S.Chime, 0.8f, flash: f),
                    _ => M(0.12f, 0.28f, 0.7f, 0.4f, S.Gong, 1f, 0.9f, S.Suck, fu),
                };
            // 우체부: 쾅 도장 → 필살기 속달은 날아가는 방향으로 콰가강
            case CharacterId.Postman:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.07f, 0.28f, 0.14f, S.Stamp, 1f, dir: true),               // 우편 보너스
                    ImpactTier.Skill => M(0f, 0f, 0.22f, 0.12f, S.Stamp, 1.2f, 0.6f),
                    ImpactTier.Evolve => M(0.06f, 0.1f, 0.42f, 0.2f, S.Chime, 1.3f, flash: f),
                    _ => M(0.12f, 0.2f, 0.8f, 0.32f, S.Kwagang, 1.2f, 0.9f, S.Whoosh, fu, true),
                };
            // 씨름꾼: 쿵 + 관중 함성 (상하 흔들림) — 링아웃 · 스트라이크 · 벽꿍 / 필살기 천하장사는 쿠—웅 쏴아
            case CharacterId.Wrestler:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.07f, 0.3f, 0.16f, S.Thud, 0.8f, dir: true),                // 링아웃 · 스트라이크 · 벽꿍 (엔진 쉬는 시간 3초)
                    ImpactTier.Skill => M(0f, 0f, 0.24f, 0.12f, S.Thud, 1.1f, 0.6f, dir: true),
                    ImpactTier.Evolve => M(0.07f, 0.12f, 0.46f, 0.22f, S.Cheer, 1f, flash: f, dir: true),
                    _ => M(0.14f, 0.26f, 0.9f, 0.38f, S.SandSlam, 1f, 0.95f, S.Suck, fu, true),
                };
            // 공사 반장: 끼이익—빠각, 미는 방향으로 흔들림 — 무게 6 이상 더미의 빠각 · 완전 압착 / 필살기 철거 공고는 쇠공 첫 착지
            case CharacterId.Foreman:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.08f, 0.32f, 0.16f, S.Crunch, 1f, pre: S.Squeal, dir: true),
                    ImpactTier.Skill => M(0f, 0f, 0.22f, 0.12f, S.Crunch, 1.2f, 0.6f, dir: true),
                    ImpactTier.Evolve => M(0.06f, 0.12f, 0.44f, 0.22f, S.Crunch, 0.85f, pre: S.Squeal, flash: f, dir: true),
                    _ => M(0.12f, 0.22f, 0.85f, 0.34f, S.Boom, 0.8f, 0.95f, S.Squeal, fu, true),
                };
            // 인형사: 씽— 현이 튕김 (줄이 쓴 방향) → 필살기 커튼콜은 박수와 함께 위로 확 끌려 올라감 (세로 흔들림 · dir = (0, 1))
            case CharacterId.Puppeteer:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.07f, 0.24f, 0.14f, S.String, 1f, dir: true),               // 한 번 쓸기로 다섯 이상
                    ImpactTier.Skill => M(0f, 0f, 0.2f, 0.12f, S.String, 1.3f, 0.6f),
                    ImpactTier.Evolve => M(0.06f, 0.1f, 0.42f, 0.22f, S.String, 0.75f, flash: f),
                    _ => M(0.12f, 0.22f, 0.72f, 0.32f, S.Applause, 1f, 0.9f, S.String, fu, true),
                };
            // 장의사: 흙이 쏟아지는 쿵 · 땅울림 → 필살기 장례는 땅이 울리다 댕—— (상하 흔들림 · dir = (0, -1)) — 한 번의 매장으로 갇힌 적 4마리 이상 / 장례 일제 매장 순간
            case CharacterId.Undertaker:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.08f, 0.3f, 0.16f, S.Dirt, 0.9f, dir: true),
                    ImpactTier.Skill => M(0f, 0f, 0.22f, 0.12f, S.Dirt, 1.15f, 0.6f),
                    ImpactTier.Evolve => M(0.06f, 0.12f, 0.44f, 0.22f, S.Bell, 1.4f, flash: f),
                    _ => M(0.12f, 0.26f, 0.85f, 0.4f, S.Bell, 1f, 0.95f, S.Rumble, fu, true),
                };
            // 사진사: 찰칵 -> 필살기 파노라마는 충전 휘잉 -> 드르르륵 찰칵 (커튼 진행 쪽 수평 흔들림 · dir = (±1, 0))
            case CharacterId.Photographer:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.08f, 0.24f, 0.12f, S.Shutter, 1f),                         // 걸작 사진 (틀 안 번쩍은 사진사 코드가 따로)
                    ImpactTier.Skill => M(0f, 0f, 0.2f, 0.1f, S.Shutter, 1.25f, 0.6f),
                    ImpactTier.Evolve => M(0.06f, 0.1f, 0.4f, 0.2f, S.Flash, 1f, flash: f),
                    _ => M(0.12f, 0.2f, 0.75f, 0.32f, S.Curtain, 1f, 0.9f, S.Flash, fu, true),
                };
            // 유령: 쨍그랑 얼음 · 우우웅 → 필살기 빙의는 울음이 빨려 들었다 얼음이 터짐
            case CharacterId.Ghost:
                return tier switch
                {
                    ImpactTier.Attack => M(0f, 0.08f, 0.25f, 0.16f, S.Glass, 1f),                          // 얼어붙음
                    ImpactTier.Skill => M(0f, 0f, 0.18f, 0.12f, S.Wail, 1.2f, 0.55f),
                    ImpactTier.Evolve => M(0.07f, 0.12f, 0.42f, 0.22f, S.Glass, 0.8f, flash: f),
                    _ => M(0.14f, 0.26f, 0.75f, 0.36f, S.Glass, 0.7f, 0.95f, S.Wail, fu),
                };
        }
        // 기본 (아직 서명이 없는 영웅)
        return tier switch
        {
            ImpactTier.Attack => M(0f, 0.06f, 0.25f, 0.14f, S.Thud, 1f, dir: true),
            ImpactTier.Skill => M(0f, 0f, 0.2f, 0.12f, S.Thud, 1.2f, 0.6f),
            ImpactTier.Evolve => M(0.06f, 0.1f, 0.4f, 0.2f, S.Chime, 1f, flash: f),
            _ => M(0.12f, 0.22f, 0.75f, 0.32f, S.Kwagang, 1f, 0.9f, S.Suck, fu),
        };
    }
}
