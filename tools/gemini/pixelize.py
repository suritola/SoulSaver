# Gemini 원본을 게임 도트로 바꾼다: 마젠타 배경 지우기 → 잘라 맞추기 → 줄이기 → 알파 0/255 → 팔레트 맞추기 → 외톨이 점 지우기
#   --frames N 이면 원본을 가로 N칸으로 나눠 프레임마다 처리하고 한 줄 시트로 붙인다 (모든 프레임 같은 배율 · 같은 기준점)
#   --palette-from 을 주면 그 그림의 색만 쓴다 (기존 그림체와 색을 맞출 때)
# 실행 예:
#   py -3.14 tools/gemini/pixelize.py <원본.png> --size 32x32 --frames 6 --palette-from Assets/Resources/FX/fx_soulstep.png -o <결과.png>
import argparse, sys
from PIL import Image

KEY = (255, 0, 255)


def key_out(im, tol):
    # 가장자리 색(중앙값)을 배경으로 보고 바깥에서부터 비슷한 색만 채워 지운다
    # (FLUX 등은 마젠타를 정확히 안 지키고, 물체 안의 비슷한 색은 남겨야 함)
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    border = [px[x, y][:3] for x in range(w) for y in (0, h - 1)] + [px[x, y][:3] for y in range(h) for x in (0, w - 1)]
    bg = tuple(sorted(c[i] for c in border)[len(border) // 2] for i in range(3))
    dist = lambda c: max(abs(c[0] - bg[0]), abs(c[1] - bg[1]), abs(c[2] - bg[2]))
    seen = bytearray(w * h)
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        x, y = stack.pop()
        if not (0 <= x < w and 0 <= y < h) or seen[y * w + x]:
            continue
        seen[y * w + x] = 1
        d = dist(px[x, y])
        if d > tol * 2:
            continue
        px[x, y] = (0, 0, 0, 0) if d <= tol else px[x, y][:3] + (int(255 * (d - tol) / tol),)
        if d <= tol:
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    return im


def key_out_magenta(im, tol):
    im = im.convert("RGBA")
    px = im.load()
    w, h = im.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            d = max(abs(r - KEY[0]), abs(g - KEY[1]), abs(b - KEY[2]))
            if d <= tol:
                px[x, y] = (0, 0, 0, 0)
            elif d <= tol * 2 and r > g + 60 and b > g + 60:      # 가장자리 분홍 번짐 → 반투명으로 낮춤
                px[x, y] = (r, g, b, int(a * (d - tol) / tol))
    return im


def fit(frames, size, margin):
    # 모든 프레임의 합친 테두리 상자로 같은 배율을 쓴다 (프레임끼리 흔들리지 않게)
    boxes = [f.getbbox() for f in frames]
    boxes = [b for b in boxes if b]
    if not boxes:
        sys.exit("배경을 지우니 아무것도 안 남음: --tol 을 낮추거나 원본 배경 확인")
    w0 = max(b[2] - b[0] for b in boxes)
    h0 = max(b[3] - b[1] for b in boxes)
    tw, th = size[0] - margin * 2, size[1] - margin * 2
    s = min(tw / w0, th / h0)
    out = []
    for f, b in zip(frames, [f.getbbox() for f in frames]):
        cell = Image.new("RGBA", size, (0, 0, 0, 0))
        if b:
            cx, cy = (b[0] + b[2]) / 2, (b[1] + b[3]) / 2
            # 프레임 칸 가운데 기준으로 원래 위치를 살려 배치 (움직임 보존)
            fx, fy = f.size[0] / 2, f.size[1] / 2
            crop = f.crop(b)
            nw, nh = max(1, round(crop.size[0] * s)), max(1, round(crop.size[1] * s))
            small = crop.resize((nw, nh), Image.BOX)
            x = round(size[0] / 2 + (cx - fx) * s - nw / 2)
            y = round(size[1] / 2 + (cy - fy) * s - nh / 2)
            cell.alpha_composite(small, (max(0, x), max(0, y)))
        out.append(cell)
    return out


def hard_alpha(im, cut):
    px = im.load()
    for y in range(im.size[1]):
        for x in range(im.size[0]):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= cut else (0, 0, 0, 0)
    return im


def palette_of(path, limit=64):
    im = Image.open(path).convert("RGBA")
    cols = [c for n, c in sorted(im.getcolors(1 << 20) or [], reverse=True) if c[3] >= 128]
    return [c[:3] for c in cols[:limit]]


def quantize(frames, n, pal):
    if not pal:
        # 모든 프레임을 한 장에 모아 공통 팔레트를 뽑는다
        w = sum(f.size[0] for f in frames)
        strip = Image.new("RGB", (w, frames[0].size[1]))
        x = 0
        for f in frames:
            strip.paste(f.convert("RGB"), (x, 0))
            x += f.size[0]
        q = strip.quantize(colors=n, method=Image.MEDIANCUT)
        p = q.getpalette()[:n * 3]
        pal = [tuple(p[i:i + 3]) for i in range(0, len(p), 3)]
    for f in frames:
        px = f.load()
        for y in range(f.size[1]):
            for x in range(f.size[0]):
                r, g, b, a = px[x, y]
                if a:
                    c = min(pal, key=lambda c: (c[0] - r) ** 2 * 3 + (c[1] - g) ** 2 * 4 + (c[2] - b) ** 2 * 2)
                    px[x, y] = c + (255,)
    return frames


def despeckle(im):
    # 상하좌우 이웃이 하나도 없는 외톨이 점을 지운다
    px = im.load()
    w, h = im.size
    lone = [(x, y) for y in range(h) for x in range(w) if px[x, y][3] and
            not any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for x, y in lone:
        px[x, y] = (0, 0, 0, 0)
    return im


def outline(im, col):
    px = im.load()
    w, h = im.size
    edge = [(x, y) for y in range(h) for x in range(w) if not px[x, y][3] and
            any(0 <= x + dx < w and 0 <= y + dy < h and px[x + dx, y + dy][3] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for x, y in edge:
        px[x, y] = col + (255,)
    return im


def main():
    ap = argparse.ArgumentParser(description="Gemini 원본 → 게임 도트")
    ap.add_argument("src")
    ap.add_argument("-o", "--out", required=True)
    ap.add_argument("--size", required=True, help="프레임 한 칸 크기 WxH (기존 그림과 같게)")
    ap.add_argument("--frames", type=int, default=1)
    ap.add_argument("--colors", type=int, default=16, help="팔레트 색 수 (--palette-from 이 없을 때)")
    ap.add_argument("--palette-from", default="", help="이 그림의 색만 쓴다")
    ap.add_argument("--margin", type=int, default=1, help="칸 가장자리 여백 (픽셀)")
    ap.add_argument("--tol", type=int, default=48, help="배경 판정 허용치")
    ap.add_argument("--magenta", action="store_true", help="가장자리 채우기 대신 마젠타(#FF00FF)만 전부 지움")
    ap.add_argument("--alpha-cut", type=int, default=110, help="이 알파 이상만 남김")
    ap.add_argument("--outline", default="", help="바깥 테두리 색 (예 1a1020) · 비우면 안 그림")
    a = ap.parse_args()

    size = tuple(int(v) for v in a.size.lower().split("x"))
    src = (key_out_magenta if a.magenta else key_out)(Image.open(a.src), a.tol)
    cw = src.size[0] // a.frames
    frames = [src.crop((i * cw, 0, (i + 1) * cw, src.size[1])) for i in range(a.frames)]
    frames = [hard_alpha(f, a.alpha_cut) for f in fit(frames, size, a.margin)]
    frames = quantize(frames, a.colors, palette_of(a.palette_from) if a.palette_from else None)
    frames = [despeckle(f) for f in frames]
    if a.outline:
        oc = tuple(int(a.outline[i:i + 2], 16) for i in (0, 2, 4))
        frames = [outline(f, oc) for f in frames]
    sheet = Image.new("RGBA", (size[0] * a.frames, size[1]), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f, (i * size[0], 0))
    sheet.save(a.out)
    print("저장:", a.out, sheet.size, "프레임", a.frames)


if __name__ == "__main__":
    main()
