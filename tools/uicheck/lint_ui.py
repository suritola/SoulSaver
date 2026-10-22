"""UI 개편 금지 목록 검사 (docs/art/UI-개편-기획서.md 3-11 + 8절) — 2026-10-22~
UI 스크립트(UnityEngine.UI · TMPro · UIKit 을 쓰는 Assets/Scripts/*.cs)에서 규칙별 위반 수를 셈.
    python tools/uicheck/lint_ui.py              # 규칙별 · 파일별 수
    python tools/uicheck/lint_ui.py --baseline   # docs/art/ui-lint-baseline.txt 에 지금 수를 기록
    python tools/uicheck/lint_ui.py --check      # 기준선보다 늘어난 규칙이 있으면 종료 코드 1 (precheck 용)
새 부품(UiV2 · UiTokens · UiRules)은 검사하지 않음. 지금은 기준선 기록만 — P1 이 끝나면 0 으로 만들고 --check 를 precheck 에 연결.
"""
import glob, os, re, sys, collections

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BASE = os.path.join(ROOT, "docs", "art", "ui-lint-baseline.txt")
SKIP = {"UiV2.cs", "UiTokens.cs", "UiRules.cs"}
RULES = [
    ("ppu-multiplier", re.compile(r"pixelsPerUnitMultiplier\s*=(?!\s*1f)")),        # 1 로 고정하는 것은 허용
    ("bold", re.compile(r"(?<!~)FontStyles\.Bold")),                                # 굵게를 빼는 &= ~ 는 허용
    ("sine-pulse", re.compile(r"Mathf\.(Sin|PingPong)\(")),
    ("scale-state", re.compile(r"\.localScale\s*=(?!\s*Vector3\.one)")),                 # 제자리로 되돌리기는 허용
    ("gold-literal", re.compile(r"0\.96f,\s*0\.83f,\s*0\.47f")),
    ("text-size", re.compile(r"UIKit\.Text\([^;]*?,\s*([\d.]+)f\s*,")),
    ("glow-sprite", re.compile(r"GlowSprite")),
    ("gradient-shadow", re.compile(r"enableVertexGradient\s*=\s*true|AddComponent<(Shadow|Outline)>")),   # 끄기는 허용
    ("button-sprite", re.compile(r"\bButtonSprite\b")),
]
ALLOWED_SIZES = {"22", "26", "34", "48"}


def ui_files():
    for p in sorted(glob.glob(os.path.join(ROOT, "Assets", "Scripts", "**", "*.cs"), recursive=True)):
        if os.path.basename(p) in SKIP or os.sep + "Editor" + os.sep in p:
            continue
        src = open(p, encoding="utf-8-sig", errors="ignore").read()
        if "UnityEngine.UI" in src or "TMPro" in src or "UIKit." in src:
            yield p, src


def count():
    per = collections.Counter()
    files = collections.defaultdict(collections.Counter)
    for p, src in ui_files():
        name = os.path.relpath(p, ROOT).replace(os.sep, "/")
        for line in src.splitlines():
            code = line.split("//", 1)[0]
            for key, rx in RULES:
                for m in rx.finditer(code):
                    if key == "text-size" and m.group(1).rstrip("0").rstrip(".") in ALLOWED_SIZES:
                        continue
                    per[key] += 1
                    files[key][name] += 1
    return per, files


def main():
    per, files = count()
    if "--baseline" in sys.argv:
        with open(BASE, "w", encoding="utf-8", newline="\n") as f:
            f.write("# UI 린트 기준선 (tools/uicheck/lint_ui.py --baseline) — 이 수보다 늘어나면 안 됨\n")
            for key, _ in RULES:
                f.write("%s %d\n" % (key, per[key]))
        print("기준선 기록:", BASE)
    if "--check" in sys.argv:
        base = {}
        if os.path.exists(BASE):
            for line in open(BASE, encoding="utf-8"):
                if line.strip() and not line.startswith("#"):
                    k, v = line.split()
                    base[k] = int(v)
        worse = [(k, base.get(k, 0), per[k]) for k, _ in RULES if per[k] > base.get(k, 0)]
        for k, b, n in worse:
            print("늘어남: %s %d → %d" % (k, b, n))
        sys.exit(1 if worse else 0)
    for key, _ in RULES:
        top = ", ".join("%s %d" % (n.split("/")[-1], c) for n, c in files[key].most_common(4))
        print("%-16s %4d   %s" % (key, per[key], top))


if __name__ == "__main__":
    main()
