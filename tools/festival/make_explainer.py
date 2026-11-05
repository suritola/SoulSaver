# 한동 AI·SW페스티벌 설명 영상 조립 (4분 이상): 타이틀 → 1. 기본 플레이 → 2. 기능 하나하나 → 마무리
# 입력: record.ps1 -Mode festival 의 SoulSaver_Festival.mp4 + segments.json (키 · 시작 초 · 끝 초)
# 사용법: py -X utf8 tools/festival/make_explainer.py <SoulSaver_Festival.mp4> <segments.json> <출력.mp4>
import sys, os, json, subprocess, tempfile
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

FF = imageio_ffmpeg.get_ffmpeg_exe()
src, segp, out = sys.argv[1:4]
tmp = tempfile.mkdtemp()
W, H = 1920, 1080
F = lambda s, b=True: ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf" if b else r"C:\Windows\Fonts\malgun.ttf", s)

# 구간 키 → (왼쪽 위 제목, [자막 줄들 — 구간 길이에 고르게 나눠 띄움])
TEXT = {
    "basic": ("1. 기본 플레이", [
        "거너로 1장 초원을 시작합니다",
        "자동 조준이 없어 마우스로 직접 겨누어 쏩니다",
        "탄창이 비면 R 키로 직접 장전합니다",
        "몰려오는 적과 거리를 두며 싸우는 것이 기본입니다",
        "적을 쓰러뜨려 경험치를 모으면 레벨이 오릅니다",
        "레벨업 카드 3장 중 하나를 골라 능력을 쌓습니다",
        "적을 충분히 쓰러뜨리면 그 장의 왕이 나타납니다",
        "보스의 빛나는 빈틈을 직접 조준해 맞힙니다",
        "보스를 쓰러뜨리면 무기가 그 자리에서 진화합니다",
    ]),
    "aim": ("① 직접 조준과 장전", ["가까운 적과 먼 적을 번갈아 겨누며 싸웁니다", "장전하는 동안은 쏠 수 없어, 언제 장전할지가 곧 판단입니다"]),
    "cards": ("② 레벨업 카드와 갈래", ["처음 두 번의 레벨업은 이번 판의 「갈래」를 고릅니다 (영웅마다 5개)", "이후에는 카드 3장 중 하나 — 카드 아래에 진화까지 남은 단계가 보입니다"]),
    "weapon_evo": ("③ 무기 진화", ["보스를 쓰러뜨릴 때마다 세 가지 진화 중 하나를 고릅니다", "판당 두 번, 되돌릴 수 없는 선택입니다"]),
    "skill_evo": ("④ 스킬 진화", ["재료 스킬을 모두 최대 레벨로 올리면", "하나의 상위 스킬로 합쳐집니다"]),
    "soul_tree": ("⑤ 영혼 트리", ["판이 끝나면 영혼 조각으로 영구 성장 노드를 배웁니다", "무기 · 궁극기 · 운명 · 생존 · 영혼 · 재물, 100개가 넘는 노드"]),
    "barrier": ("⑥ 보스 결계", ["보스는 결계를 펼쳐 맵을 어둠에 잠그고", "결계 안으로 공격을 쏟아붓습니다"]),
    "weak_point": ("⑦ 빈틈", ["보스 몸에 열리는 빛나는 빈틈은", "직접 조준한 공격만 맞힐 수 있습니다 — 자동 조준으로는 닿지 않습니다"]),
    "memory": ("⑧ 기억하는 보스", ["보스는 가장 많이 맞은 공격을 기억해 막습니다", "다른 공격으로 바꾸면 기억이 깨집니다"]),
    "mirror": ("⑨ 거울의 군주", ["마지막 세계의 왕은", "플레이어의 스킬을 그대로 흉내 냅니다"]),
    "developer": ("⑩ 무한 모드와 개발자", ["네 세계를 지나면 무한 모드가 열리고", "최종 보스 「개발자」가 기다립니다"]),
    "heroes": ("⑪ 영웅 14명", ["영웅마다 공격 방식이 완전히 다릅니다"]),
    "skins": ("⑫ 스킨 상점", ["포인트로 캐릭터 · 무기 · 이펙트 · 커서 스킨을 바꿉니다", "스킨마다 공격 모습과 효과음도 함께 바뀝니다"]),
    "modes": ("⑬ 도전 모드", ["일일 도전 · 영혼 릴레이(영웅 셋이 한 판을 이어 달리기)", "역류(완성된 몸에서 1분마다 힘을 하나씩 잃으며 내려가기)"]),
}
HERO = {
    "Gunner": "거너 — 리볼버를 쏘고 직접 장전",
    "Swordsman": "검사 — 장검으로 앞쪽 부채꼴을 베는 근접 영웅",
    "Rogue": "도적 — 끝없이 날아가는 표창과 무적 돌진",
    "Archer": "궁수 — 시위를 당겨 가득 찬 순간 「완벽!」",
    "Alchemist": "연금술사 — 화염 · 빙결 · 산성 플라스크",
    "Cartographer": "지도 제작자 — 깃펜 획과 저절로 닫히는 삼각형",
    "Clockmaker": "시계공 — 톱니 던지기와 3초 전으로 「되감기」",
    "Postman": "우체부 — 가장 먼 적을 찾아가는 편지",
    "Ghost": "유령 — 얼리는 손길과 적의 몸에 빙의",
    "Wrestler": "씨름꾼 (숨은 영웅) — 적을 들어 무리 한가운데로 메다꽂기",
    "Foreman": "공사 반장 (숨은 영웅) — 적을 삽날로 밀어 쌓아 짓누르기",
    "Puppeteer": "인형사 (숨은 영웅) — 커서의 더미와 사이의 줄로 베기",
    "Undertaker": "장의사 (숨은 영웅) — 구멍을 파 빠뜨리고 묻기",
    "Photographer": "사진사 (숨은 영웅) — 뷰파인더 안의 적을 종이로",
}


def run(args):
    subprocess.run([FF, "-y", "-loglevel", "error"] + args, check=True)


def card(name, lines, sizes, sec):
    im = Image.new("RGB", (W, H), (24, 14, 38))
    d = ImageDraw.Draw(im)
    y = (H - sum(s + 30 for s in sizes)) // 2
    for t, s in zip(lines, sizes):
        d.text(((W - d.textlength(t, font=F(s))) / 2, y), t, font=F(s), fill=(255, 255, 255))
        y += s + 30
    png = os.path.join(tmp, name + ".png"); im.save(png)
    o = os.path.join(tmp, name + ".mp4")
    run(["-loop", "1", "-t", str(sec), "-i", png, "-f", "lavfi", "-t", str(sec), "-i", "anullsrc=r=44100:cl=stereo",
         "-vf", "fps=30,format=yuv420p", "-c:v", "libx264", "-crf", "18", "-c:a", "aac", "-shortest", o])
    return o


def overlay(name, head, sub):
    """자막 띠 위 왼쪽 구간 제목 + 아래 자막 띠 (투명 PNG)"""
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if head:
        tw = d.textlength(head, font=F(44))
        d.rounded_rectangle([30, H - 262, 30 + tw + 48, H - 186], 14, fill=(20, 10, 34, 225))     # 자막 띠 바로 위 (왼쪽 위는 체력 칸과 겹침)
        d.text((54, H - 254), head, font=F(44), fill=(255, 214, 120, 255))
    if sub:
        d.rectangle([0, H - 170, W, H], fill=(0, 0, 0, 200))
        d.text(((W - d.textlength(sub, font=F(50))) / 2, H - 132), sub, font=F(50), fill=(255, 255, 255, 255))
    p = os.path.join(tmp, name + ".png"); im.save(p)
    return p


def clip(i, start, end, head, lines):
    """원본의 [start, end] 구간을 잘라 제목 · 자막을 입힘 (자막은 구간 길이에 고르게)"""
    dur = end - start
    o = os.path.join(tmp, f"seg{i:02d}.mp4")
    ins, chain, prev = [], [f"[0:v]scale={W}:{H},fps=30,format=yuv420p[b]"], "b"
    n = max(1, len(lines))
    for k in range(n):
        ins += ["-i", overlay(f"o{i}_{k}", head, lines[k] if lines else "")]
        a, b = dur * k / n, dur * (k + 1) / n + (1 if k == n - 1 else 0)
        chain.append(f"[{prev}][{k + 1}:v]overlay=enable='between(t,{a:.2f},{b:.2f})'[v{k}]"); prev = f"v{k}"
    run(["-ss", f"{start:.3f}", "-to", f"{end:.3f}", "-i", src] + ins +
        ["-filter_complex", ";".join(chain), "-map", f"[{prev}]", "-map", "0:a?", "-c:v", "libx264", "-crf", "20",
         "-c:a", "aac", "-ar", "44100", "-ac", "2", o])
    return o


segs = json.load(open(segp, encoding="utf8"))
segs = segs if isinstance(segs, list) else segs.get("segments", [])
if any(s["key"].startswith("hero_") for s in segs):
    segs = [s for s in segs if s["key"] != "heroes"]     # 영웅별 구간이 있으면 겹치는 「영웅 전체」 구간은 빼 (두 번 나옴)
parts = [card("title", ["Soul Saver", "자동 조준이 없는 도트 서바이버 로그라이트", "2026 한동 AI·SW페스티벌 · 게임 부문 · 22400258 박민근"], [130, 50, 38], 5)]
started2 = False
for i, s in enumerate(segs):
    key, a, b = s["key"], float(s["start"]), float(s["end"])
    if b - a < 0.5:
        continue
    if key == "basic":
        parts.append(card("sec1", ["1. 기본 플레이", "한 판의 흐름을 그대로 보여 드립니다"], [96, 46], 3))
    elif not started2:
        parts.append(card("sec2", ["2. 기능 소개", "게임의 핵심 기능을 하나씩 설명합니다"], [96, 46], 3))
        started2 = True
    if key.startswith("hero_"):
        hid = key[5:]
        head, lines = "⑪ 영웅 14명", [HERO.get(hid, hid)]
    else:
        head, lines = TEXT.get(key, ("", []))
    parts.append(clip(i, a, b, head, lines))
parts.append(card("end", ["조작", "이동 WASD · 조준 마우스 · 공격 좌클릭 · 필살기 우클릭 · 장전 R", "패드: 왼쪽 스틱 이동 · 오른쪽 스틱 조준",
                          "심사용 빌드: 메인 메뉴 「테스터 메뉴」에서 영웅 · 갈래 · 스킬 · 보스를 골라 바로 체험",
                          "AI 도구: Claude Code(코드) · Gemini(그림 시안) · 파이썬 합성(음악) — 모두 개발자가 검토", "감사합니다"],
                  [80, 42, 42, 38, 34, 60], 10))
lst = os.path.join(tmp, "list.txt")
open(lst, "w", encoding="utf8").write("".join(f"file '{p.replace(chr(92), '/')}'\n" for p in parts))
run(["-f", "concat", "-safe", "0", "-i", lst, "-c", "copy", out])
info = subprocess.run([FF, "-i", out], capture_output=True, text=True, encoding="utf8", errors="replace").stderr
print("OK", out, round(os.path.getsize(out) / 1e6, 1), "MB", [l.strip() for l in info.splitlines() if "Duration" in l][:1])
