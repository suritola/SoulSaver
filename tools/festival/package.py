# 출품 폴더 만들기: 실행 빌드 + 소스(git HEAD) → <출력폴더>/3_실행파일 · 4_소스, 작업 폴더에 읽어보기.txt
# 보고서 PDF · 시연 영상(유튜브 링크)은 따로 제출
# 사용법: py -X utf8 tools/festival/package.py <작업폴더> <빌드폴더> <출력폴더>
import os, re, sys, shutil, subprocess, zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
work, build, out = sys.argv[1:4]
VER = re.search(r"bundleVersion: (\S+)", open(os.path.join(ROOT, "ProjectSettings", "ProjectSettings.asset"), encoding="utf8").read())[1]
for d in ("3_실행파일", "4_소스"):
    shutil.rmtree(os.path.join(out, d), ignore_errors=True)   # 이 스크립트가 만드는 하위 폴더만 지움
gdir = os.path.join(out, "3_실행파일", "SoulSaver")
shutil.copytree(build, gdir, ignore=shutil.ignore_patterns("*DoNotShip*"))
# 심사용 빌드는 제품 이름이 「Soul Saver Festival」(저장 분리)이라 실행 파일도 그 이름 → 제출본은 「Soul Saver.exe」로 (exe 와 _Data 폴더 이름만 맞으면 Unity 가 실행됨)
for f in os.listdir(gdir):
    if f.startswith("Soul Saver Festival"):
        os.rename(os.path.join(gdir, f), os.path.join(gdir, f.replace("Soul Saver Festival", "Soul Saver")))
srczip = os.path.join(work, "src.zip")   # 이름이 깨진 에셋이 있어 tar 대신 zip 으로 받아 파이썬으로 풂
subprocess.run(["git", "-C", ROOT, "archive", "--format=zip", "-o", srczip, "HEAD", "Assets", "Packages", "ProjectSettings", "tools",
                # 에이전트 규칙 · AI 작업 메모 · 출품 조립 스크립트는 소스에서 뺌
                ":!tools/festival", ":!tools/gemini/README.md", ":!tools/release.ps1"], check=True)
with zipfile.ZipFile(srczip) as zs:
    zs.extractall(os.path.join(out, "4_소스"))
os.remove(srczip)
open(os.path.join(work, "읽어보기.txt"), "w", encoding="utf8").write(f"""2026 한동 AI·SW페스티벌 · 스마트 SW 공모전 게임 부문 출품작
작품: Soul Saver (소울 세이버) v{VER} / 출품자: 박민근

구성
 제작보고서: [4]박민근_SoulSaver_출품보고서.pdf
 시연 영상: 유튜브 링크로 따로 제출 (1080p · 한국어 설명 자막 · 약 4분 50초)
 {os.path.basename(out)}/3_실행파일/SoulSaver/Soul Saver.exe  <- 더블클릭으로 실행 (Windows 10 64비트, 스팀 불필요)
 {os.path.basename(out)}/4_소스/ (Unity 6 프로젝트 소스: Assets · Packages · ProjectSettings · tools)

심사용 빌드: 스팀 없이 실행 · 모든 영웅 · 스킨 · 모드 해금 · 포인트 99,999,999
            메인 메뉴 「테스터 메뉴」에서 영웅 · 난이도 · 갈래 · 스킬 · 영혼 트리 · 보스를 골라 바로 체험할 수 있음
            (저장 데이터는 정식판과 따로: 제품 이름 Soul Saver Festival)

조작: 이동 WASD · 조준 마우스 · 공격 좌클릭 · 필살기 우클릭 · 장전 R / 게임패드 지원
소스는 표절 검토용으로 제출하며, 지식재산권은 개발자에게 있습니다.
""")
n = sum(len(fs) for _, _, fs in os.walk(out))
print("OK", out, n, "files")
