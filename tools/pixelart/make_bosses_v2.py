"""보스 고해상도 다시 그리기 (그래픽 가독성 개편 2단계 · 묶음 2 · 4 · 5)

32칸 설계도 → 그림 1칸 = 월드 1/6칸 (PPU = 6 × 보스 배율) 고해상도 그림. 이미지 생성 모델 없이 스크립트로만 찍는다.
보스마다 tools/pixelart/hires/bosses/<보스>.py · 손 도트 지도 hires/handmaps/<보스>.txt.

사용법:
  py -3.14 -X utf8 tools/pixelart/make_bosses_v2.py slimeking [--out 폴더] [--apply]
  py -3.14 -X utf8 tools/pixelart/make_bosses_v2.py --icons
    --icons  도감 보스 아이콘 Resources/Bosses/icon_<보스>.png (32칸 원래 그림 · 도감 카드가 정수 배 ×4 로 그림 — 고해상도 그림을 140칸에 넣으면 ×1 이라 다른 카드와 섞임)
    --out    시트 · 설계도 · 확대 견본을 쓸 폴더 (기본 tools/pixelart/out/bosses_v2)
    --apply  Assets 의 시트 PNG 를 바꾸고 .meta 를 다시 씀 (guid · 쓰는 그림 ID 유지, 안 쓰는 그림은 참조 0 일 때만 지움)
설계도: hires/bosses/blueprints/<보스>_32.png (쓰는 칸만 4×2 · 32칸). 없으면 Assets 의 옛 32칸 시트에서 뽑아 저장한다
        (적용 뒤에는 Assets 그림이 고해상도라 설계도 파일이 유일한 원본 — 지우지 말 것).
"""
import argparse
import importlib
import os
import sys
import uuid

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)

from hires import blueprint as bp          # noqa: E402
from hires import export                   # noqa: E402

BLUEPRINTS = os.path.join(HERE, "hires", "bosses", "blueprints")


def design_sheet(mod):
    path = os.path.join(BLUEPRINTS, mod.NAME + "_32.png")
    if os.path.isfile(path):
        return bp.load(path)
    src = bp.load(os.path.join(ROOT, mod.ASSET))
    if src.shape[0] % 32 or src.shape[1] % 32 or src.shape[1] > 256:
        raise SystemExit(f"설계도가 없고 Assets 그림도 32칸 시트가 아님: {mod.ASSET} {src.shape[1]}×{src.shape[0]}")
    d = mod.design_frames(src)
    sheet = export.pack(d, 4)
    os.makedirs(BLUEPRINTS, exist_ok=True)
    export.save_png(path, sheet)
    print(f"설계도 저장: {os.path.relpath(path, ROOT)} (옛 그림에서 쓰는 {len(d)}칸)")
    return sheet


# 도감 아이콘: 이름 → (설계도 파일 이름 · 없으면 Assets 의 32칸 그림, 칸 열 · 행)
ICONS = {
    "lich": ("lich", "Assets/Sprites/enemy/boss.png", 0, 0),
    "hell": ("hell", "Assets/Sprites/enemy/hell/hell_lord.png", 0, 0),
    "slimeking": ("slimeking", "Assets/Sprites/enemy/meadow/meadow_slimeking.png", 0, 0),
    "mirror": ("mirror", "Assets/Resources/Abyss/mirrorlord_0.png", 0, 0),
    "developer": ("developer", "Assets/Resources/Abyss/developer_0.png", 0, 0),
}
ICON_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {sid}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def make_icons():
    """도감 보스 아이콘 (32칸 · PPU 32 · .meta 는 처음 한 번만 만들어 guid 유지)"""
    out_dir = os.path.join(ROOT, "Assets", "Resources", "Bosses")
    for name, (bp_name, src, col, row) in ICONS.items():
        blue = os.path.join(BLUEPRINTS, bp_name + "_32.png")
        if os.path.isfile(blue):
            cell = bp.cells(bp.load(blue), 32)[0]                 # 설계도 시트 첫 칸 = 살아 있는 그림
        else:
            im = bp.load(os.path.join(ROOT, src))
            if im.shape[1] > 256 or im.shape[0] % 32:
                raise SystemExit(f"아이콘 원본이 32칸 그림이 아님 (설계도도 없음): {src}")
            cell = im[row * 32:(row + 1) * 32, col * 32:(col + 1) * 32]
        path = os.path.join(out_dir, f"icon_{name}.png")
        export.save_png(path, np.ascontiguousarray(cell))
        if not os.path.isfile(path + ".meta"):
            with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
                f.write(ICON_META.format(guid=uuid.uuid4().hex, sid=uuid.uuid4().hex))
        print(f"아이콘: {os.path.relpath(path, ROOT)}")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("boss", nargs="?")
    ap.add_argument("--icons", action="store_true")
    ap.add_argument("--out", default=os.path.join(HERE, "out", "bosses_v2"))
    ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()
    if a.icons:
        make_icons()
        if not a.boss:
            return 0
    if not a.boss:
        ap.print_help()
        return 2
    mod = importlib.import_module("hires.bosses." + a.boss)
    res = mod.build(design_sheet(mod))
    sheet = export.pack(res["frames"], res["cols"])
    os.makedirs(a.out, exist_ok=True)
    out_png = os.path.join(a.out, f"{mod.NAME}_sheet.png")
    export.save_png(out_png, sheet)
    export.save_png(os.path.join(a.out, f"{mod.NAME}_design32.png"), export.pack(res["design"], res["cols"]))
    cols = len(np.unique(sheet[sheet[..., 3] > 0][:, :3], axis=0))
    print(f"시트: {out_png} ({sheet.shape[1]}×{sheet.shape[0]} · 칸 {res['cell']} · PPU {res['ppu']} · 돌출 {res['extrude']} · 색 {cols})")
    dens = res["scale"] / res["ppu"] * 1080 / 44.0
    print(f"1080p 화소/칸 = {res['scale']} ÷ {res['ppu']} × 1080 ÷ 44 = {dens:.2f}")
    if not a.apply:
        return 0
    asset = os.path.join(ROOT, res["asset"])
    meta_path = asset + ".meta"
    with open(meta_path, encoding="utf-8") as f:
        meta = f.read()
    guid = meta.split("guid: ", 1)[1].split()[0]
    used_drop, unused_keep = export.check_unused(meta, guid, res["sprites"])
    if used_drop:
        raise SystemExit(f"지울 그림 중 참조되는 것이 있음 — 멈춤: {used_drop}")
    print(f"참조 확인: 지울 그림 참조 0 · 남길 그림 중 참조 없는 것 {unused_keep or '없음'}")
    new_meta = export.rewrite_meta(meta, res["sprites"], res["cell"], res["cols"], sheet.shape[0], res["ppu"], res["extrude"])
    export.save_png(asset, sheet)
    with open(meta_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(new_meta)
    print(f"적용: {res['asset']} (+ .meta · guid {guid} 그대로 · 그림 {len(res['sprites'])}개)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
