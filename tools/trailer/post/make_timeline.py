#!/usr/bin/env python
"""릴스 편집표 만들기: 아래 CUTS(박자 단위)에서 tools/trailer/post/timeline.json 을 쓴다.

  py -3.14 tools/trailer/post/make_timeline.py [테이크 폴더]

음악은 게임 OST bgm_developer(150 BPM) 한 곡 — 1박 = 24프레임, 1마디 = 96프레임이라 컷이 프레임 단위로 박자에 맞는다.
  곡 7마디(팀파니 몰아치기) → 8~23마디(본론 I) → [이음] 40~55마디(절정) → 56마디(끝 화음, 로고 카드) → 민공방 로고
컷 길이는 박(beat) 수로 적는다. 컷의 "in"은 테이크 안에서 쓰기 시작하는 프레임(takes.py 의 best 를 보고 직접 프레임을 확인해 정함).
"""
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__))
TAKES = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.environ["LOCALAPPDATA"], "SoulSaverBuild", "Reels", "takes")
IDENT = r"C:\Users\User\Desktop\Deepfold\Marketing\trailer\work\shots"   # 민공방 게임즈 로고 애니메이션(세로 209프레임)과 소리
BEAT, BAR = 24, 96
INK = "0B0A10"
CROP_UI = [1080, 1920, 1167, 0]   # 가로 3414×1920 으로 찍은 UI 컷의 가운데

# (테이크, in 프레임, 박 수[, 덧붙일 칸]) — 순서대로 이어 붙는다
UI = {"crop": CROP_UI}
CUTS = [
    # ── 붙잡기 (곡 7마디 + 8마디 앞): 첫 프레임부터 무한 모드의 난전 → 조준선이 표적을 옮겨 다니며 쏨
    ("endless_s1", 306, 4),
    ("aim2_s1", 84, 6),
    # ── 직접 조준 · 직접 장전
    ("aim1_s1", 130, 6),
    ("reload2_s1", 153, 6),
    ("aim0_s2", 290, 4),
    # ── 레벨업 카드 (세로로 직접 찍음 · 창 전체가 화면 너비에 들어옴 · HUD 는 숨김)
    ("cards_s1.{lang}", 24, 8),      # 카드를 누르는 순간(217프레임)에 컷 — 창이 닫힌 뒤 화살표 커서가 남지 않게
    ("ult_gunner_s1", 265, 4),
    # ── 네 개의 세계
    ("horde_meadow_s1", 200, 4),
    ("horde_catacomb_s1", 150, 4),
    ("horde_hell_s2", 190, 4),
    ("horde_abyss_s2", 160, 4),
    # ── 영웅 14명 (거너는 앞에서 · 나머지 13명을 반 마디씩)
    ("hero_Swordsman_s1", 110, 2), ("hero_Archer_s1", 110, 2), ("hero_Rogue_s1", 110, 2), ("hero_Alchemist_s1", 110, 2),
    ("hero_Cartographer_s1", 110, 2), ("hero_Clockmaker_s1", 110, 2), ("hero_Postman_s1", 110, 2),
    # ── [곡 이음: 24 → 40마디] 절정
    ("hero_Ghost_s1", 110, 2), ("hero_Wrestler_s1", 110, 2), ("hero_Foreman_s1", 110, 2), ("hero_Puppeteer_s1", 110, 2),
    ("hero_Undertaker_s1", 110, 2), ("hero_Photographer_s1", 110, 2),
    # ── 보스의 결계
    ("barrier1_s1", 150, 6),
    ("barrier0_s1", 130, 6),
    ("barrier2_s1", 90, 4),
    ("barrier1_s2", 100, 4),
    # ── 쓰러뜨리면 그 자리에서 무기 진화
    ("break2_s1", 236, 4),
    ("evo_s1.{lang}", 200, 8),
    # ── 무한 모드 → 가장 센 보스 장면에서 하드 컷
    ("endless_s2", 40, 4),
    ("dev_s2", 20, 4),
    ("barrier0_s2", 100, 6),
    ("barrier1_s1", 390, 6),
]

# (시작 박, 길이 박, 한국어, 영어[, 덧붙일 칸]) — 토막마다 한 줄 이하
TEXT = [
    (0.25, 8.75, "자동 조준은 없다.", "No auto-aim.", {"size": 0.1, "fadeIn": 3}),
    (10.5, 14.5, "조준도, 장전도 직접.", "You aim. You reload."),
    (34.25, 3.75, "판마다 다른 빌드", "A new build every run"),      # 카드 창에는 게임 글자가 있어 다음 컷에
    (38.5, 14.5, "네 개의 세계", "Four worlds"),
    (54.5, 24.5, "영웅 14명", "14 heroes"),
    (80.5, 18.5, "보스의 결계", "Inside the boss's barrier"),
    (100.25, 3.75, "쓰러뜨리면, 그 자리에서 진화", "Beat a boss. Evolve on the spot.", {"size": 0.07}),      # 진화 창에는 게임 글자가 있어 앞 컷에만
    (112.5, 7.5, "그리고, 무한 모드", "And then, Endless Mode"),
]


def build():
    clips, at = [], 0
    for c in CUTS:
        take, start, beats = c[0], c[1], c[2]
        row = {"tags": ["reels"], "src": take, "in": start, "frames": int(beats * BEAT)}
        if len(c) > 3:
            row.update(c[3])
        meta = os.path.join(TAKES, take + ".meta.json")
        if os.path.isfile(meta):
            n = json.load(open(meta))["frames"]
            if start + row["frames"] > n:
                sys.exit(f"{take}: in {start} + {row['frames']} > 찍은 길이 {n}")
        clips.append(row)
        at += row["frames"]
    game = at                                   # 게임 화면 끝 = 곡 56마디 첫 박
    assert game % BAR == 0, f"게임 화면 길이 {game}프레임이 마디(96)에 맞지 않는다"
    part1 = 17 * BAR                            # 곡 7~23마디
    assert game > part1, "절정 구간이 없다"
    card = 120                                  # 로고 카드 2.0초
    clips.append({"tags": ["reels"], "color": INK, "frames": card})
    clips.append({"tags": ["reels"], "src": os.path.join(IDENT, "ident_1080x1920"), "frames": 209, "in": 0, "fadeOut": 18})
    total = game + card + 209
    xf = 10                                     # 같은 곡 안의 이음: 마디 경계에서 10프레임 겹침
    music = [
        {"tags": ["reels"], "file": "bgm_developer", "at": 0, "in": 7 * BAR, "frames": part1 + xf, "fadeOut": xf},
        {"tags": ["reels"], "file": "bgm_developer", "at": part1 - xf, "in": 40 * BAR - xf, "frames": game - part1 + xf + card, "fadeIn": xf, "fadeOut": 96},
    ]
    text = [dict({"tags": ["reels"], "at": int(t[0] * BEAT), "frames": int(t[1] * BEAT), "ko": t[2], "en": t[3], "y": 0.2, "size": 0.085, "fadeIn": 5, "fadeOut": 10},
                 **(t[4] if len(t) > 4 else {})) for t in TEXT]
    text += [
        {"tags": ["reels"], "at": game, "frames": card, "image": "steam_assets/library_logo.png", "pixel": True, "h": 0.3, "y": 0.4, "fadeIn": 0, "fadeOut": 0},
        {"tags": ["reels"], "at": game + 14, "frames": card - 14, "ko": "Steam에서 지금 플레이", "en": "Out now on Steam", "y": 0.62, "size": 0.05, "fadeIn": 10, "fadeOut": 0},
        {"tags": ["reels"], "at": game + 24, "frames": card - 24, "ko": "₩1,700", "en": "$1.50", "y": 0.69, "size": 0.042, "fadeIn": 10, "fadeOut": 0},   # 스팀 정가(11-11 확인)
    ]
    tl = {
        "_doc": "tools/trailer/post/make_timeline.py 가 쓴다(손으로 고치지 않는다). 시간은 프레임(60fps).",
        "out": "docs/marketing/trailer",
        "shots": TAKES,
        "ink": INK,
        "sounds": [os.path.join(TAKES, "sounds"), os.path.join(IDENT, "sounds")],
        "mix": {"music": -7, "amb": -18, "sfx": -9, "vo": 0, "duckDb": 0, "duckThreshold": 0.03, "attackMs": 20, "releaseMs": 600},
        "cuts": {"reels": {"w": 1080, "h": 1920, "crf": 17, "grade": ""}},
        "clips": clips, "text": text, "music": music,
        "vo": [{"tags": ["reels"], "key": "ident", "at": game + card, "gainDb": 2}],
    }
    with open(os.path.join(HERE, "timeline.json"), "w", encoding="utf-8") as f:
        json.dump(tl, f, ensure_ascii=False, indent=1)
    print(f"timeline.json · 컷 {len(CUTS)} · 게임 화면 {game}프레임({game / 60:.1f}초) · 전체 {total}프레임({total / 60:.1f}초)")


if __name__ == "__main__":
    build()
