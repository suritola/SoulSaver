# 그림 후보를 만든다 (원본 해상도 그대로 · 도트화는 pixelize.py)
#   --provider cloudflare (기본 · 무료): Workers AI FLUX.1 schnell · 하루 무료 한도 안 · 참고 그림은 못 넣음
#       토큰: 환경 변수 CF_API_TOKEN 또는 tools/gemini/.cf_token (git 제외 · 권한 Workers AI Read+Edit 짜리를 따로 만듦)
#       계정: 환경 변수 CF_ACCOUNT_ID (기본 = 플레이 데이터 서버 계정)
#   --provider gemini (유료 · 결제 연결 필요): 키는 환경 변수 GEMINI_API_KEY 또는 tools/gemini/.key (git 제외)
#   배경은 단색 마젠타(#FF00FF)로 그리게 해서 pixelize.py 가 지운다
# 실행 예:
#   py -3.14 tools/gemini/gen.py --name fx_soulstep --prompt "푸른 영혼불이 타오르는 그을린 발자국" \
#       --ref Assets/Resources/FX/fx_soulstep.png --n 4 --out <스크래치>/gemini/fx_soulstep
#   py -3.14 tools/gemini/gen.py --list-models
import argparse, base64, io, json, os, sys, urllib.error, urllib.request

API = "https://generativelanguage.googleapis.com/v1beta"
DEFAULT_MODEL = "gemini-3-pro-image"
CF_API = "https://api.cloudflare.com/client/v4/accounts/%s/ai/run/%s"
CF_MODEL = "@cf/black-forest-labs/flux-1-schnell"
CF_ACCOUNT = "dc63d914d0b8e932107d93a4abc32624"
HERE = os.path.dirname(os.path.abspath(__file__))

STYLE = (
    "Game asset for a 2D top-down pixel-art roguelike (Soul Saver). "
    "Draw ONE subject, centered, filling most of the canvas, fully inside the frame. "
    "Background must be a perfectly flat solid magenta #FF00FF with no gradient, no shadow, no floor, no vignette. "
    "Crisp hard edges, bold readable silhouette, strong dark outline, limited saturated palette, "
    "no text, no watermark, no border, no anti-aliased glow bleeding into the background."
)
STRIP = (
    " Lay out exactly {n} animation frames side by side in one horizontal row, equal width cells, "
    "same size and position in every cell, only the motion changes between frames."
)
REF = " Match the art style, palette and proportions of the attached reference image(s)."


def api_key():
    k = os.environ.get("GEMINI_API_KEY", "").strip()
    p = os.path.join(HERE, ".key")
    if not k and os.path.exists(p):
        k = open(p, encoding="utf-8").read().strip()
    if not k:
        sys.exit("Gemini 키 없음: 환경 변수 GEMINI_API_KEY 또는 tools/gemini/.key 에 넣기")
    return k


def cf_token():
    k = os.environ.get("CF_API_TOKEN", "").strip()
    p = os.path.join(HERE, ".cf_token")
    if not k and os.path.exists(p):
        k = open(p, encoding="utf-8").read().strip()
    if not k:
        sys.exit("Cloudflare 토큰 없음: dash.cloudflare.com → API 토큰 → 「Workers AI」 템플릿으로 만들어 tools/gemini/.cf_token 에 넣기")
    return k


def call(url, key, body=None, bearer=False):
    auth = {"Authorization": "Bearer " + key} if bearer else {"x-goog-api-key": key}
    req = urllib.request.Request(url, data=None if body is None else json.dumps(body).encode(),
                                 headers=dict(auth, **{"Content-Type": "application/json"}))
    try:
        with urllib.request.urlopen(req, timeout=180) as r:
            return json.load(r)
    except urllib.error.HTTPError as e:
        msg = e.read().decode("utf-8", "replace")
        if e.code == 429 and "free_tier" in msg and "limit: 0" in msg:
            sys.exit("Gemini 429: 무료 등급은 그림 모델 한도가 0 → AI Studio 에서 이 키의 프로젝트에 결제(Billing)를 연결해야 함")
        sys.exit("그림 API 오류 %d: %s" % (e.code, msg[:800]))


def main():
    ap = argparse.ArgumentParser(description="Gemini 그림 후보 생성")
    ap.add_argument("--name", help="결과 파일 이름 앞부분 (게임 그림 이름과 같게)")
    ap.add_argument("--prompt", help="그릴 내용 (한국어 · 영어 모두 가능)")
    ap.add_argument("--ref", action="append", default=[], help="그림체 참고 이미지 (여러 번 가능)")
    ap.add_argument("--frames", type=int, default=1, help="애니 프레임 수 (2 이상이면 가로 한 줄로 그리게 함)")
    ap.add_argument("--aspect", default="", help="화면비 (예 1:1, 16:9) · 비우면 모델 기본")
    ap.add_argument("--n", type=int, default=2, help="후보 개수")
    ap.add_argument("--out", default="", help="저장 폴더")
    ap.add_argument("--provider", choices=["cloudflare", "gemini"], default=os.environ.get("IMG_PROVIDER", "cloudflare"))
    ap.add_argument("--model", default="", help="모델 (비우면 cloudflare = FLUX.1 schnell · gemini = %s)" % DEFAULT_MODEL)
    ap.add_argument("--list-models", action="store_true", help="그림을 만들 수 있는 모델 목록")
    a = ap.parse_args()
    if a.provider == "cloudflare":
        return cloudflare(a)
    a.model = a.model or os.environ.get("GEMINI_IMAGE_MODEL", DEFAULT_MODEL)
    key = api_key()

    if a.list_models:
        for m in call(API + "/models?pageSize=200", key).get("models", []):
            if "image" in m["name"] or "imagen" in m["name"]:
                print(m["name"].split("/")[-1], "-", m.get("displayName", ""))
        return
    if not (a.name and a.prompt and a.out):
        sys.exit("--name · --prompt · --out 필요")

    text = STYLE + (STRIP.format(n=a.frames) if a.frames > 1 else "") + (REF if a.ref else "") + "\nSubject: " + a.prompt
    parts = [{"text": text}]
    for r in a.ref:
        parts.append({"inline_data": {"mime_type": "image/png", "data": base64.b64encode(open(r, "rb").read()).decode()}})
    cfg = {"responseModalities": ["IMAGE"]}
    if a.aspect:
        cfg["imageConfig"] = {"aspectRatio": a.aspect}
    body = {"contents": [{"parts": parts}], "generationConfig": cfg}

    os.makedirs(a.out, exist_ok=True)
    made = 0
    for i in range(a.n):
        res = call("%s/models/%s:generateContent" % (API, a.model), key, body)
        imgs = [p["inlineData"] for c in res.get("candidates", []) for p in c.get("content", {}).get("parts", []) if "inlineData" in p]
        if not imgs:
            print("후보 %d: 그림 없음 (%s)" % (i + 1, json.dumps(res, ensure_ascii=False)[:300]))
            continue
        path = os.path.join(a.out, "%s_raw_%d.png" % (a.name, i + 1))
        open(path, "wb").write(base64.b64decode(imgs[0]["data"]))
        made += 1
        print("저장:", path)
    save_spec(a)
    print("후보 %d / %d" % (made, a.n))


def save_spec(a):
    json.dump({"name": a.name, "prompt": a.prompt, "frames": a.frames, "ref": a.ref, "provider": a.provider, "model": a.model},
              open(os.path.join(a.out, a.name + "_spec.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)


def cloudflare(a):
    # FLUX 는 글로만 받는다 (참고 그림 X · 영어 프롬프트가 잘 먹음) → 그림체는 pixelize --palette-from 으로 맞춘다
    from PIL import Image
    if not (a.name and a.prompt and a.out):
        sys.exit("--name · --prompt · --out 필요")
    if a.list_models:
        sys.exit("cloudflare 는 목록 대신 --model 에 @cf/... 이름을 직접 넣기 (기본 %s)" % CF_MODEL)
    a.model = a.model or CF_MODEL
    if a.ref:
        print("참고: cloudflare 는 --ref 그림을 못 보냄 → pixelize --palette-from 으로 색을 맞출 것")
    key = cf_token()
    text = "pixel art 2D game asset. " + a.prompt + ". " + STYLE + (STRIP.format(n=a.frames) if a.frames > 1 else "")
    os.makedirs(a.out, exist_ok=True)
    made = 0
    for i in range(a.n):
        res = call(CF_API % (os.environ.get("CF_ACCOUNT_ID", CF_ACCOUNT), a.model), key,
                   {"prompt": text[:2048], "steps": 8}, bearer=True)
        img = (res.get("result") or {}).get("image")
        if not img:
            print("후보 %d: 그림 없음 (%s)" % (i + 1, json.dumps(res, ensure_ascii=False)[:300]))
            continue
        path = os.path.join(a.out, "%s_raw_%d.png" % (a.name, i + 1))
        Image.open(io.BytesIO(base64.b64decode(img))).convert("RGB").save(path)
        made += 1
        print("저장:", path)
    save_spec(a)
    print("후보 %d / %d" % (made, a.n))


if __name__ == "__main__":
    main()
