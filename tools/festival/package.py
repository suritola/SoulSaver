# 출품 zip 만들기: 보고서 PDF · 시연 영상 · 실행 빌드 · 소스(git HEAD) → 1개 zip
# 사용법: py -X utf8 tools/festival/package.py <작업폴더(보고서 · 영상이 있는 곳)> <빌드폴더> <출력.zip>
import os, sys, shutil, subprocess, zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
work, build, out = sys.argv[1:4]
stage = os.path.join(work, "stage")
shutil.rmtree(stage, ignore_errors=True)   # 이 스크립트가 만든 임시 폴더
for d in ("1_제작보고서", "2_시연영상", "3_실행파일", "4_소스"):
    os.makedirs(os.path.join(stage, d), exist_ok=True)
shutil.copy(os.path.join(work, "SoulSaver_제작보고서.pdf"), os.path.join(stage, "1_제작보고서"))
shutil.copy(os.path.join(work, "SoulSaver_시연영상.mp4"), os.path.join(stage, "2_시연영상"))
shutil.copytree(build, os.path.join(stage, "3_실행파일", "SoulSaver"), ignore=shutil.ignore_patterns("*DoNotShip*"))
srczip = os.path.join(work, "src.zip")   # 이름이 깨진 에셋이 있어 풀지 않고 zip 끼리 합친다
subprocess.run(["git", "-C", ROOT, "archive", "--format=zip", "-o", srczip, "HEAD", "Assets", "Packages", "ProjectSettings", "tools", "AGENT.md"], check=True)
open(os.path.join(stage, "읽어보기.txt"), "w", encoding="utf8").write("""2026 한동 AI·SW페스티벌 · 스마트 SW 공모전 게임 부문 출품작
작품: Soul Saver (소울 세이버) v2.0.1 / 출품자: 박민근

구성
 1_제작보고서/SoulSaver_제작보고서.pdf
 2_시연영상/SoulSaver_시연영상.mp4 (1080p · 한국어 자막 · 약 2분 30초)
 3_실행파일/SoulSaver/Soul Saver.exe  <- 더블클릭으로 실행 (Windows 10 64비트, 스팀 불필요)
 4_소스/ (Unity 6 프로젝트 소스: Assets · Packages · ProjectSettings · tools)

조작: 이동 WASD · 조준 마우스 · 공격 좌클릭 · 필살기 우클릭 · 장전 R / 게임패드 지원
소스는 표절 검토용으로 제출하며, 지식재산권은 개발자에게 있습니다.
""")
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as zo:
    for dp, _, fs in os.walk(stage):
        for fn in fs:
            fp = os.path.join(dp, fn)
            zo.write(fp, os.path.relpath(fp, stage))
    with zipfile.ZipFile(srczip) as zs:
        for info in zs.infolist():
            if not info.is_dir():
                zo.writestr("4_소스/" + info.filename, zs.read(info))
z = zipfile.ZipFile(out)
print("OK", out, round(os.path.getsize(out) / 1e6), "MB", len(z.namelist()), "files", "손상:", z.testzip())
