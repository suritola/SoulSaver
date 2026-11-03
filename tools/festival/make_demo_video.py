# 한동 AI·SW페스티벌 시연 영상(MP4, 1080p, 한국어 설명 자막) 조립
# 구성: 타이틀 → 실제 플레이 트레일러(한국어 자막) → 기능 설명 슬라이드(자막) → 조작 · AI 도구 엔딩
# 사용법: py -X utf8 tools/festival/make_demo_video.py <트레일러.mp4> <출력.mp4>
import sys, os, subprocess, tempfile
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SHOTS = os.path.join(ROOT, "docs", "steam", "art", "screenshots")
FF = imageio_ffmpeg.get_ffmpeg_exe()
trailer, out = sys.argv[1], sys.argv[2]
tmp = tempfile.mkdtemp()
W, H = 1920, 1080
FONT = r"C:\Windows\Fonts\malgunbd.ttf"
f = lambda s: ImageFont.truetype(FONT, s)

slides = [
    ("01_gunner_catacombs_horde", "자동 조준이 없습니다. 수백 마리의 적을 마우스로 직접 겨눠 쏘고, 탄창이 비면 직접 장전합니다."),
    ("02_level_up_cards", "레벨업마다 카드 3장 중 하나를 골라, 판마다 전혀 다른 빌드를 만듭니다."),
    ("03_weapon_evolution", "보스를 쓰러뜨리는 순간 무기가 부서지며 전혀 다른 무기로 진화합니다."),
    ("04_skill_evolution", "필살기(우클릭)도 진화합니다. 영웅마다 고유한 필살기와 진화 갈래가 있습니다."),
    ("05_soul_tree", "100칸이 넘는 영혼 트리로 영구 성장합니다. 무기 · 궁극기 · 운명 · 생존 · 영혼 · 재물 갈래."),
    ("06_swordsman_lich_king", "보스는 결계를 펼쳐 맵을 어둠에 잠그고 공격을 쏟아붓습니다. 사진은 검사와 리치 왕."),
    ("07_archer_demon_lord_barrier", "보스 몸에 열리는 빛나는 빈틈은 직접 조준한 공격만 맞힙니다."),
    ("08_cartographer_king_slime", "첫 번째 왕 킹 슬라임은 깎일수록 작아지고 빨라집니다."),
    ("09_ghost_mirror_sovereign", "마지막 심연의 거울의 군주는 플레이어의 스킬을 그대로 따라 합니다."),
    ("10_endless_developer", "무한 모드: 최종 보스 '개발자'를 넘으면 한계 돌파, 끝없이 강해지는 적과의 싸움입니다."),
    ("11_nine_heroes", "영웅 9명, 저마다 다른 손맛. 거너 · 검사 · 도적 · 궁수 · 연금술사 · 지도 제작자 · 시계공 · 우체부 · 유령."),
    ("12_skin_shop", "캐릭터 · 무기 · 이펙트 · 커서 스킨 상점으로 나만의 영웅을 꾸밉니다."),
]


def card(name, lines, sizes, bg=(24, 14, 38)):
    im = Image.new("RGB", (W, H), bg)
    d = ImageDraw.Draw(im)
    total = sum(s + 28 for s in sizes)
    y = (H - total) // 2
    for t, s in zip(lines, sizes):
        w = d.textlength(t, font=f(s))
        d.text(((W - w) / 2, y), t, font=f(s), fill=(255, 255, 255))
        y += s + 28
    p = os.path.join(tmp, name + ".png")
    im.save(p)
    return p


def wrap(d, text, font, maxw):
    lines, cur = [], ""
    for ch in text:
        if d.textlength(cur + ch, font=font) > maxw and cur:
            lines.append(cur); cur = ch.lstrip()
        else:
            cur += ch
    return lines + [cur]


def slide(i, name, text):
    im = Image.open(os.path.join(SHOTS, name + "_koreana.png")).convert("RGB").resize((W, H))
    ov = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    lines = wrap(d, text, f(46), W - 200)
    bh = 40 + len(lines) * 66
    d.rectangle([0, H - bh - 30, W, H], fill=(0, 0, 0, 190))
    for k, t in enumerate(lines):
        w = d.textlength(t, font=f(46))
        d.text(((W - w) / 2, H - bh - 10 + k * 66), t, font=f(46), fill=(255, 255, 255, 255))
    im = Image.alpha_composite(im.convert("RGBA"), ov).convert("RGB")
    p = os.path.join(tmp, f"slide{i:02d}.png")
    im.save(p)
    return p


def still(png, sec, name):
    o = os.path.join(tmp, name + ".mp4")
    subprocess.run([FF, "-y", "-loglevel", "error", "-loop", "1", "-t", str(sec), "-i", png,
                    "-f", "lavfi", "-t", str(sec), "-i", "anullsrc=r=44100:cl=stereo",
                    "-vf", "fps=30,format=yuv420p", "-c:v", "libx264", "-crf", "18", "-c:a", "aac", "-shortest", o], check=True)
    return o


parts = []
parts.append(still(card("title", ["Soul Saver", "자동 조준이 없는 도트 서바이버 로그라이트", "2026 한동 AI·SW페스티벌 · 게임 부문 · 박민근"], [120, 48, 36]), 4, "p_title"))

# 트레일러: 1080p · 30fps · AAC. 촬영 도구 자막은 한글 글자가 깨지므로(글꼴 문제) 같은 자리를 가리는 한국어 자막을 직접 입힌다
import re
info = subprocess.run([FF, "-i", trailer], capture_output=True, text=True).stderr
m = re.search(r"Duration: (\d+):(\d+):([\d.]+)", info)
dur = int(m[1]) * 3600 + int(m[2]) * 60 + float(m[3])
subs = ["자동 조준이 없습니다. 모든 공격을 직접 겨눠 쏩니다", "레벨업 카드로 판마다 전혀 다른 빌드를 만듭니다",
        "보스를 쓰러뜨리면 그 자리에서 무기가 진화합니다", "보스의 빈틈은 직접 조준한 공격만 맞힙니다",
        "영웅 9명 · 네 개의 세계 · 네 명의 왕", "100칸이 넘는 영혼 트리와 끝없는 무한 모드"]
seg = dur / len(subs)
inputs, chain, last = [], [], "[0:v]scale=%d:%d,fps=30,format=yuv420p[b]" % (W, H)
chain.append(last); prev = "b"
for i, tx in enumerate(subs):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.rectangle([0, H - 190, W, H], fill=(0, 0, 0, 255))
    w = d.textlength(tx, font=f(52)); d.text(((W - w) / 2, H - 150), tx, font=f(52), fill=(255, 255, 255, 255))
    pp = os.path.join(tmp, f"sub{i}.png"); im.save(pp); inputs += ["-i", pp]
    a, b = i * seg, (i + 1) * seg if i < len(subs) - 1 else dur + 1
    chain.append(f"[{prev}][{i+1}:v]overlay=enable='between(t,{a:.2f},{b:.2f})'[o{i}]"); prev = f"o{i}"
t = os.path.join(tmp, "p_trailer.mp4")
subprocess.run([FF, "-y", "-loglevel", "error", "-i", trailer] + inputs + ["-filter_complex", ";".join(chain), "-map", f"[{prev}]", "-map", "0:a",
                "-c:v", "libx264", "-crf", "20", "-c:a", "aac", "-ar", "44100", "-ac", "2", t], check=True)
parts.append(t)

parts.append(still(card("intro", ["기능 소개", "아래 장면과 설명은 실제 게임 화면입니다"], [90, 44]), 3, "p_intro"))
for i, (n, tx) in enumerate(slides, 1):
    parts.append(still(slide(i, n, tx), 7, f"p_s{i:02d}"))

parts.append(still(card("end", ["조작", "이동 WASD · 조준 마우스 · 공격 좌클릭 · 필살기 우클릭 · 장전 R", "패드: 왼쪽 스틱 이동 · 오른쪽 스틱 조준",
                              "AI 도구: Claude Code(코드) · Gemini(그림 시안) · 파이썬 합성(음악) — 모두 개발자가 검토", "감사합니다"],
                             [80, 42, 42, 36, 60]), 8, "p_end"))

lst = os.path.join(tmp, "list.txt")
open(lst, "w", encoding="utf8").write("".join(f"file '{p.replace(chr(92), '/')}'\n" for p in parts))
subprocess.run([FF, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", lst, "-c", "copy", out], check=True)
print("OK", out, round(os.path.getsize(out) / 1e6, 1), "MB")
