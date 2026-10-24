# 그래픽 가독성 개편 2단계 묶음 3 — 메인 메뉴 영웅 초상을 128칸으로 다시 그림
#   지금 메뉴는 32칸 영웅 그림을 ×16 (512) / ×12 (384) 로 키워 그림 1칸이 16 · 12 화소 — 메뉴 배경 도트(4 화소)의 4배라 다른 게임처럼 보였음
#   → 32칸 그림을 설계도로 128칸에 부위 · 형태 모델 명암 · 덧그림으로 다시 그림 (그냥 확대 아님) → 메뉴에서 ×4 / ×3 = 4 · 3 화소/칸
#   그리는 법은 영웅마다 tools/pixelart/hires/heroes/<이름>.py (공용 붓 = hires/heroes/portrait.py)
#   내보내기: Assets/Resources/Characters/Menu/<시트 이름>.png (+ .meta — 있으면 guid 그대로, 없으면 새 guid · 점 필터 · 무압축 · 밉맵 끔)
#   게임 쪽: CharacterUI.MenuPortrait · MenuHeroPanel (없으면 예전 32칸 초상)
#   AI 생성 화소 0: 입력은 Assets 의 32칸 그림과 이 스크립트들뿐 — 경로에 gemini 가 들어가면 멈춤
# 실행: py -3.14 -X utf8 tools/pixelart/make_menu_portraits.py [시트 이름 …] [--compare 폴더] [--lint]
#   이름을 안 주면 그린 것 전부 (hires/heroes/__init__.py 의 MODULES)
#   --compare 폴더: compare_<이름>.png (지금 메뉴 ×16 · 새 초상 ×4 — 같은 512 칸 · 메뉴 배경 위 · 컬러 + 흑백 + 수치)
#   --lint: tools/art/lint.py --kind portrait --ui 512 로 검사 (설계도 = 그 시트 첫 칸)
import argparse
import os
import re
import subprocess
import sys
import uuid

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
from hires import heroes  # noqa: E402

OUT_DIR = os.path.join(ROOT, 'Assets', 'Resources', 'Characters', 'Menu')
META_TEMPLATE = os.path.join(ROOT, 'Assets', 'Resources', 'Bosses', 'ritual_lantern.png.meta')     # 낱장 도트 스프라이트 .meta
MENU_BG = os.path.join(ROOT, 'Assets', 'Sprites', 'UI', 'Theme', 'menu_bg.png')
UI = 512            # 메인 메뉴 초상 칸 (1920 폭 · 좁은 화면은 384)


def guard(path):
    if 'gemini' in path.replace('\\', '/').lower():
        sys.exit(f'멈춤: 이미지 생성 모델 산출물 경로는 읽지 않음 ({path})')
    return path


def design_of(mod):
    """설계도 = 시트의 맨 위 왼쪽 32칸 (게임의 32칸 초상 = 시트 첫 그림)"""
    im = Image.open(guard(os.path.join(ROOT, mod.DESIGN))).convert('RGBA')
    return im.crop((0, 0, 32, 32))


# ───────────────────────────── .meta ─────────────────────────────
def write_meta(png):
    meta = png + '.meta'
    if os.path.exists(meta):
        text = open(meta, encoding='utf-8').read()          # guid · spriteID 그대로
    else:
        text = open(META_TEMPLATE, encoding='utf-8').read()
        text = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + uuid.uuid4().hex, text, count=1, flags=re.M)
        text = re.sub(r'spriteID: [0-9a-f]+', 'spriteID: ' + uuid.uuid4().hex, text, count=1)
    # 낱장 스프라이트 · 점 필터 · 밉맵 끔 · 무압축 · 가운데 피벗 · 전체 사각(UI 그림 — 메시 모양 불필요)
    text = re.sub(r'spriteMode: \d', 'spriteMode: 1', text, count=1)
    text = re.sub(r'filterMode: -?\d+', 'filterMode: 0', text, count=1)
    text = re.sub(r'enableMipMap: \d', 'enableMipMap: 0', text, count=1)
    text = re.sub(r'textureCompression: \d', 'textureCompression: 0', text)
    text = re.sub(r'spritePixelsToUnits: [0-9.]+', 'spritePixelsToUnits: 32', text, count=1)
    text = re.sub(r'spriteMeshType: \d', 'spriteMeshType: 0', text, count=1)
    text = re.sub(r'alignment: \d+', 'alignment: 0', text, count=1)
    with open(meta, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)


def folder_meta(folder):
    meta = folder + '.meta'
    if os.path.exists(meta):
        return
    with open(meta, 'w', encoding='utf-8', newline='\n') as f:
        f.write('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)


# ───────────────────────────── 비교 그림 ─────────────────────────────
def font(sz):
    for f in ('C:/Windows/Fonts/malgun.ttf', 'C:/Windows/Fonts/gulim.ttc'):
        if os.path.exists(f):
            return ImageFont.truetype(f, sz)
    return ImageFont.load_default()


def luma(a):
    a = np.asarray(a).astype(float)
    return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]


def stats(im):
    a = np.asarray(im)
    op = a[..., 3] > 0
    rgb = a[..., :3][op]
    cols = len({tuple(c) for c in rgb})
    ys, xs = np.nonzero(op)
    return dict(colors=cols, y=float(luma(a)[op].mean()), box=(int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1)))


def compare(name, new, design, out_dir, lint_text):
    """지금 메뉴(32칸 ×16) · 새 초상(128칸 ×4) — 같은 512 칸 · 메뉴 배경(영웅이 서는 카펫 왼쪽) 위 · 컬러 / 흑백 · 수치"""
    bg = Image.open(guard(MENU_BG)).convert('RGBA')
    crop = bg.crop((640, 330, 640 + UI, 330 + UI))
    tiles = []
    for im, mul in ((design, UI // 32), (new, UI // 128)):
        t = crop.copy()
        t.alpha_composite(im.resize((im.width * mul, im.height * mul), Image.NEAREST))
        tiles.append(t)
    pad, head, foot = 24, 56, 430
    W = UI * 2 + pad * 3
    H = head + UI * 2 + pad * 3 + foot
    sheet = Image.new('RGBA', (W, H), (24, 20, 30, 255))
    d = ImageDraw.Draw(sheet)
    f, fs = font(22), font(19)
    d.text((pad, 16), '지금 메뉴 — 32칸 × 16 (그림 1칸 = 16 화소)', fill=(230, 220, 240), font=f)
    d.text((pad * 2 + UI, 16), '시안 — 128칸 × 4 (1칸 = 4 화소 = 배경 도트)', fill=(230, 220, 240), font=f)
    for i, t in enumerate(tiles):
        x = pad + i * (UI + pad)
        sheet.alpha_composite(t, (x, head))
        g = Image.fromarray(luma(t).clip(0, 255).astype(np.uint8), 'L').convert('RGBA')
        sheet.alpha_composite(g, (x, head + UI + pad))
    s0, s1 = stats(design), stats(new)
    y = head + UI * 2 + pad * 2
    lines = [
        f'{name}   불투명 상자 {s0["box"][0]}×{s0["box"][1]} → {s1["box"][0]}×{s1["box"][1]} (설계도 × 4)   색 {s0["colors"]} → {s1["colors"]}'
        f'   평균 Y (외곽 포함) {s0["y"]:.0f} → {s1["y"]:.0f}',
        '위: 1920×1080 메뉴 배경(영웅이 서는 카펫) 위 실제 크기 · 아래: 흑백 (Rec.601) · 1600×900 은 × 3 (384)',
    ]
    lines += [ln for ln in lint_text.splitlines() if ln.startswith('[')]
    for ln in lines:
        d.text((pad, y), ln, fill=(214, 206, 226) if not ln.startswith('[실패]') else (255, 120, 110), font=fs)
        y += 25
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, f'compare_{name}.png')
    sheet.convert('RGB').save(path)
    return path


# ───────────────────────────── 실행 ─────────────────────────────
def run_lint(png, design_png):
    r = subprocess.run([sys.executable, '-X', 'utf8', os.path.join(ROOT, 'tools', 'art', 'lint.py'), png,
                        '--design', design_png, '--kind', 'portrait', '--ui', str(UI)],
                       capture_output=True, text=True, encoding='utf-8')
    return r.returncode, r.stdout


def main():
    ap = argparse.ArgumentParser(description='메뉴 초상 128칸 (그래픽 개편 2단계 묶음 3)')
    ap.add_argument('names', nargs='*')
    ap.add_argument('--compare')
    ap.add_argument('--lint', action='store_true')
    a = ap.parse_args()
    names = a.names or [n for n in heroes.ALL if n in heroes.MODULES]
    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta(OUT_DIR)
    fails = 0
    for name in names:
        if name not in heroes.ALL:
            sys.exit(f'모르는 시트 이름: {name} (hires/heroes/__init__.py 의 ALL)')
        mod = heroes.load(name)
        if mod is None:
            print(f'{name}: 아직 안 그림 — 건너뜀 (게임은 32칸 초상)')
            continue
        os.chdir(ROOT)
        arr, dots = mod.build()
        assert arr.shape == (128, 128, 4), arr.shape
        assert set(np.unique(arr[..., 3])) <= {0, 255}, '반투명 칸'
        png = os.path.join(OUT_DIR, name + '.png')
        Image.fromarray(arr, 'RGBA').save(png)
        write_meta(png)
        print(f'{name}: {os.path.relpath(png, ROOT)}')
        if a.lint or a.compare:
            tmp = os.path.join(a.compare or OUT_DIR, f'_design_{name}.png') if a.compare else None
            if a.compare:
                os.makedirs(a.compare, exist_ok=True)
                design_of(mod).save(tmp)
            else:
                import tempfile
                tmp = os.path.join(tempfile.gettempdir(), f'_design_{name}.png')
                design_of(mod).save(tmp)
            code, text = run_lint(png, tmp)
            fails += code != 0
            print(text)
            if a.compare:
                print('비교 그림:', compare(name, Image.fromarray(arr, 'RGBA'), design_of(mod), a.compare, text))
    return 1 if fails else 0


if __name__ == '__main__':
    sys.exit(main())
