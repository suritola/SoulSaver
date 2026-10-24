# 게임 안 패치노트를 스팀 게시판 공지와 똑같이 (2026-10-18 사용자: 「인게임 버전 · 패치노트를 스팀 게시판 버전 · 패치노트와 일치」)
# docs/steam/announcements/*.md (BBCode, 4개 언어) → 게임 패치노트 형식 (# 제목 · ## 소제목 · - 목록 · **굵게** · === en/ja/zh 블록)
# 사용법: py tools/patchnotes_steam.py all <출력 PatchNotesAll.txt>      스팀에 올린 공지 전부 (최신이 위, 「@@@ 표시」 줄로 나눔)
#         py tools/patchnotes_steam.py one <공지 파일> <출력 .md>        공지 하나 (새 버전 게임 안 노트 docs/patch-notes/game/v*.md 만들 때)
# 새 공지를 스팀에 올리면 아래 POSTS 맨 위에 한 줄 더함 (표시 · 파일)
import io, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ANN = os.path.join(ROOT, "docs", "steam", "announcements")

# 스팀 게시판에 올라간 공지 (최신이 위) — 게임 안 「패치노트」 목록의 왼쪽 표시 · 공지 파일
POSTS = [
    ("v1.3.2", "v1.3.2.md"),
    ("v1.3.1", "v1.3.1.md"),
    ("v1.3", "v1.3.md"),
    ("v1.2.4", "v1.2.4.md"),
    ("v1.2.3", "v1.2.3.md"),
    ("v1.2.2", "v1.2.2.md"),
    ("v1.2.1", "v1.2.1.md"),
    ("v1.2", "v1.2.md"),
    ("v1.1.1", "v1.1.1.md"),
    ("Hotfix", "hotfix-frame-drop.md"),
    ("v1.1", "v1.1.md"),
]

LANGS = ["ko", "en", "ja", "zh"]
HEAD_B = {"한국어": "ko", "English": "en", "日本語": "ja", "中文": "zh", "简体中文": "zh"}
TITLE_KEYS = ("제목", "Title", "タイトル", "标题")
SUB_KEYS = ("부제", "Subtitle", "サブタイトル", "副标题")
BODY_KEYS = ("본문", "Body", "本文", "正文")


def split_langs(text):
    """언어별 (제목, 부제, BBCode 줄들)"""
    out, cur = {}, None
    for raw in text.replace("\r", "").split("\n"):
        line = raw.rstrip()
        m = re.match(r"^=== (ko|en|ja|zh)\s*$", line)
        m2 = re.match(r"^## (.+?)\s*$", line)
        if m:
            cur = m.group(1)
        elif m2 and m2.group(1) in HEAD_B:
            cur = HEAD_B[m2.group(1)]
        elif cur is None:
            continue
        else:
            d = out.setdefault(cur, {"title": "", "sub": "", "body": []})
            kv = re.match(r"^\**\s*([^:：*]+?)\s*:\**\s*(.*)$", line)
            if kv and kv.group(1).strip() in TITLE_KEYS:
                d["title"] = kv.group(2).strip().strip("*").strip()
            elif kv and kv.group(1).strip() in SUB_KEYS:
                d["sub"] = kv.group(2).strip().strip("*").strip()
            elif kv and kv.group(1).strip() in BODY_KEYS and not kv.group(2).strip():
                pass
            elif line.startswith("["):
                d["body"].append(line)
            continue
        out.setdefault(cur, {"title": "", "sub": "", "body": []})
    return out


def inline(s):
    s = re.sub(r"\[b\](.*?)\[/b\]", r"**\1**", s)
    s = re.sub(r"\[url=[^\]]*\](.*?)\[/url\]", r"\1", s)
    s = re.sub(r"\[/?(i|u|strike|spoiler|noparse|url)\]", "", s)
    s = re.sub(r"\[/?[a-z0-9]+(=[^\]]*)?\]", "", s)
    return s.strip()


def to_md(d):
    lines = []
    if d["title"]:
        lines.append("# " + d["title"])
    if d["sub"]:
        lines.append(d["sub"])
    for raw in d["body"]:
        s = raw.strip()
        if re.match(r"^\[(이미지|Image|画像|图片|圖片)\s*\d", s):       # 1.1 공지의 그림 자리 표시 (스팀에는 그림이 들어감)
            continue
        if "[img]" in s or "STEAM_CLAN_IMAGE" in s or s.startswith("[previewyoutube") or s in ("[list]", "[/list]", "[olist]", "[/olist]", "[hr][/hr]", "[hr]"):
            continue
        h = re.match(r"^\[h[123]\](.*)\[/h[123]\]$", s)
        if h:
            lines.append("")
            lines.append("## " + inline(h.group(1)))
            continue
        if s.startswith("[*]"):
            t = inline(s[3:])
            if t:
                lines.append("- " + t)
            continue
        p = re.match(r"^\[p\](.*)\[/p\]$", s)
        t = inline(p.group(1) if p else s)
        if t:
            lines.append("")
            lines.append(t)
    while lines and not lines[0]:
        lines.pop(0)
    return "\n".join(lines)


def convert(path):
    with io.open(path, encoding="utf-8") as f:
        langs = split_langs(f.read())
    blocks = []
    for lang in LANGS:
        if lang not in langs:
            continue
        md = to_md(langs[lang])
        blocks.append(md if lang == "ko" else "=== " + lang + "\n" + md)
    if not blocks:
        raise SystemExit("언어 블록이 없음: " + path)
    return "\n\n".join(blocks).rstrip() + "\n"


# 게임 글꼴(Cafe24Danjunghae SDF)에 없는 이모지는 빼고 씀 (스팀 공지에는 그대로 · 게임에서는 □ 로 바뀌고 TMP 경고가 뜸)
#   → · ↔ 같은 화살표(2190~21FF)는 글꼴에 있어서 남김
EMOJI = re.compile("[\U0001F000-\U0001FAFF☀-➿⬀-⯿️‍]")


def strip_emoji(text):
    text = EMOJI.sub("", text)
    return re.sub(r"(?m)^(#+) +", r"\1 ", text)     # 「## ✨ 새 기능」 → 「## 새 기능」


def write(path, text):
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(strip_emoji(text))


if __name__ == "__main__":
    # strip <입력> <출력>: 게임 안 노트 파일을 이모지 빼고 복사 (steam-upload.ps1 · release.ps1)
    if len(sys.argv) >= 4 and sys.argv[1] == "strip":
        with io.open(sys.argv[2], encoding="utf-8-sig") as f:
            write(sys.argv[3], f.read())
        sys.exit(0)
    if len(sys.argv) >= 3 and sys.argv[1] == "all":
        parts = []
        for label, name in POSTS:
            p = os.path.join(ANN, name)
            if not os.path.exists(p):
                print("건너뜀 (파일 없음): " + name)
                continue
            parts.append("@@@ " + label + "\n" + convert(p))
        write(sys.argv[2], "".join(parts))
        print("스팀 공지 %d개를 게임 패치노트로 묶었습니다: %s" % (len(parts), sys.argv[2]))
    elif len(sys.argv) >= 4 and sys.argv[1] == "one":
        write(sys.argv[3], convert(sys.argv[2]))
        print("변환: " + sys.argv[3])
    else:
        raise SystemExit(__doc__ or "사용법: all <출력> | one <공지> <출력>")
