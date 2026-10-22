# 고른 그림을 게임에 넣는다
#   이미 있는 그림: 크기가 같을 때만 덮어씀 (.meta 의 칸 나누기 · 필터 설정을 그대로 쓰기 위해)
#   새 그림: --like 로 비슷한 그림의 .meta 를 복사하고 guid 만 새로 만든다 (도트용 Point 필터 등 유지)
# 실행 예:
#   py -3.14 tools/gemini/apply.py <결과.png> Assets/Resources/FX/fx_soulstep.png
#   py -3.14 tools/gemini/apply.py <결과.png> Assets/Resources/FX/fx_new.png --like Assets/Resources/FX/fx_soulstep.png
import argparse, os, re, shutil, sys, uuid
from PIL import Image


def main():
    ap = argparse.ArgumentParser(description="게임에 그림 넣기")
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("--like", default="", help="새 그림일 때 .meta 를 빌려 올 그림")
    ap.add_argument("--resize-ok", action="store_true", help="크기가 달라도 덮어씀 (.meta 칸 설정을 직접 확인할 것)")
    a = ap.parse_args()

    new = Image.open(a.src)
    if os.path.exists(a.dst):
        old = Image.open(a.dst)
        if old.size != new.size and not a.resize_ok:
            sys.exit("크기 다름: 게임 %s · 새 그림 %s (칸 나누기가 깨짐) → pixelize --size 를 맞추거나 --resize-ok" % (old.size, new.size))
        old.close()
    elif not a.like:
        sys.exit("새 그림이면 --like <비슷한 그림.png> 필요 (Unity 기본 설정은 도트가 흐려짐)")
    shutil.copyfile(a.src, a.dst)
    if a.like and not os.path.exists(a.dst + ".meta"):
        meta = open(a.like + ".meta", encoding="utf-8").read()
        meta = re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, meta, count=1)
        open(a.dst + ".meta", "w", encoding="utf-8").write(meta)
        print(".meta 복사:", a.like + ".meta", "(guid 새로 만듦 · 칸 수가 다르면 sprites 항목 확인)")
    print("적용:", a.dst, new.size)


if __name__ == "__main__":
    main()
