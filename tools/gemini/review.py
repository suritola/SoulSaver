# 검토용 비교 그림: 지금 게임 그림 · 후보들을 크게 키워 어두운 바닥 · 밝은 바닥 · 실제 크기로 나란히 보여 준다
#   이 한 장만 열어 보고 고를 후보 · 고칠 점을 정한다 (원본 여러 장을 따로 열지 않아 사용량 절약)
# 실행 예:
#   py -3.14 tools/gemini/review.py --current Assets/Resources/FX/fx_soulstep.png <후보1.png> <후보2.png> -o <비교.png>
import argparse, os
from PIL import Image, ImageDraw, ImageFont

DARK, LIGHT = (24, 20, 32), (200, 196, 186)
try:
    FONT = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 13)       # 한글 이름표
except OSError:
    FONT = ImageFont.load_default()


def checker(w, h, a, b, s=8):
    im = Image.new("RGB", (w, h), a)
    d = ImageDraw.Draw(im)
    for y in range(0, h, s):
        for x in range(0, w, s):
            if (x // s + y // s) % 2:
                d.rectangle([x, y, x + s - 1, y + s - 1], fill=b)
    return im


def main():
    ap = argparse.ArgumentParser(description="후보 비교 그림")
    ap.add_argument("cands", nargs="+")
    ap.add_argument("--current", default="", help="지금 게임에 들어 있는 그림")
    ap.add_argument("--scale", type=int, default=0, help="확대 배율 (0 = 자동)")
    ap.add_argument("-o", "--out", required=True)
    a = ap.parse_args()

    items = ([("지금 게임", a.current)] if a.current and os.path.exists(a.current) else []) + \
            [("후보 %d" % (i + 1), p) for i, p in enumerate(a.cands)]
    ims = [(t, Image.open(p).convert("RGBA")) for t, p in items]
    mw = max(im.size[0] for _, im in ims)
    sc = a.scale or max(1, min(8, 640 // mw))
    rows = []
    for t, im in ims:
        big = im.resize((im.size[0] * sc, im.size[1] * sc), Image.NEAREST)
        w, h = big.size
        row = Image.new("RGB", (w * 2 + im.size[0] + 40, h + 22), (12, 12, 16))
        dk = checker(w, h, DARK, (34, 30, 44)); dk.paste(big, (0, 0), big)
        lt = checker(w, h, LIGHT, (178, 174, 164)); lt.paste(big, (0, 0), big)
        row.paste(dk, (0, 22)); row.paste(lt, (w + 20, 22))
        real = Image.new("RGB", im.size, DARK); real.paste(im, (0, 0), im)
        row.paste(real, (w * 2 + 40, 22))
        ImageDraw.Draw(row).text((4, 4), "%s  %dx%d" % (t, im.size[0], im.size[1]), fill=(240, 240, 240), font=FONT)
        rows.append(row)
    W = max(r.size[0] for r in rows)
    out = Image.new("RGB", (W, sum(r.size[1] + 8 for r in rows)), (12, 12, 16))
    y = 0
    for r in rows:
        out.paste(r, (0, y)); y += r.size[1] + 8
    out.save(a.out)
    print("저장:", a.out, out.size, "배율", sc)


if __name__ == "__main__":
    main()
