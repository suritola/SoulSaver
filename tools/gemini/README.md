# Gemini 그림 작업 방식

Gemini 가 원본 그림을 그리고 → 도트로 바꾸고 → Claude 가 비교 그림 한 장으로 검토 · 수정 → 게임에 넣는다.
모든 명령은 `py -3.14 -X utf8` (Pillow 가 깔린 Windows 파이썬 · 한글 출력)으로 실행한다.

## 준비 (한 번)
- **기본 = Cloudflare Workers AI (무료)**: dash.cloudflare.com → 내 프로필 → API 토큰 → 「Workers AI」 템플릿으로 토큰을 만들어 `tools/gemini/.cf_token` 에 저장 (git 제외). 모델 FLUX.1 schnell, 하루 무료 한도 안. 참고 그림을 못 보내므로 그림체는 `pixelize --palette-from` 으로 맞춘다
- **Gemini (유료)**: `--provider gemini`
- Google AI Studio 에서 결제(Billing)가 연결된 프로젝트의 API 키를 만들어 (무료 등급은 그림 모델 한도 0) `tools/gemini/.key` 에 한 줄로 저장 (git 제외) 또는 환경 변수 `GEMINI_API_KEY`
- 모델 확인: `gen.py --list-models` → 기본은 `gemini-3-pro-image` (Nano Banana Pro · 싸게는 `gemini-3.1-flash-image`), 바꾸려면 `--model` 또는 환경 변수 `GEMINI_IMAGE_MODEL`

## 순서
작업 폴더는 세션 스크래치(`.../scratchpad/gemini/<이름>/`). 게임 파일은 4단계 전까지 건드리지 않는다.

1. **생성** `gen.py --name <그림 이름> --prompt "<내용>" --ref <지금 게임 그림.png> [--frames N] --n 3 --out <작업 폴더>`
   - 그림체를 맞추려면 `--ref` 로 지금 그림이나 같은 종류 그림을 꼭 넣는다
   - 애니메이션은 `--frames N` (가로 한 줄로 그리게 함). 프레임 사이 일관성이 약하면 프레임 수를 줄이고 나머지는 스크립트로 만든다
2. **도트화** (배경은 가장자리 색을 읽어 바깥부터 지움 · 마젠타만 지우려면 `--magenta`)
    `pixelize.py <raw.png> --size <칸 WxH> [--frames N] --palette-from <지금 그림.png> -o <후보_px.png>`
   - 칸 크기는 지금 게임 그림과 같게 (`.meta` 칸 나누기를 그대로 쓰기 위해)
   - 새 색을 써야 하면 `--palette-from` 대신 `--colors 12~24`, 테두리가 약하면 `--outline 1a1020`
3. **검토** `review.py --current <지금 그림.png> <후보_px...> -o <비교.png>` → 이 한 장만 열어 본다
4. **적용** `apply.py <고른 후보.png> <Assets 경로>` (새 그림이면 `--like <비슷한 그림.png>`)
5. 생성 스크립트로 만든 그림이면 `tools/pixelart/make_*.py` 를 다시 돌렸을 때 덮어써지지 않게 그 스크립트에서 해당 그림 출력을 빼거나 주석으로 남긴다

## Claude 검토 기준
- **실제 크기**(비교 그림 오른쪽)에서 무엇인지 바로 읽히는가 · 실루엣이 뭉개지지 않았는가
- **어두운 · 밝은 바닥 모두** 테두리 · 배경 찌꺼기(분홍 번짐 · 외톨이 점)가 없는가
- **지금 그림체와 어울리는가**: 색 수 · 명암 방향(왼쪽 위 빛) · 테두리 굵기 · 크기 비율
- **애니메이션**: 프레임마다 위치가 튀지 않는가 · 첫 프레임 ↔ 끝 프레임이 이어지는가(반복 그림)
- **이펙트 원칙**: 적 공격 · 스킬은 화면에서 가장 눈에 띄게, 강해질수록(`_t2` · `_t3`) 더 화려하게
- 고칠 게 작으면 PIL 로 픽셀을 직접 고치고(스크래치에 작은 스크립트), 크면 프롬프트를 고쳐 1단계부터 다시

## 주의
- Gemini 결과는 진짜 도트가 아니므로 2단계를 건너뛰고 넣지 않는다
- 스팀은 AI 로 만든 콘텐츠가 들어가면 상점 페이지 「AI 생성 콘텐츠 공개」 설문에 적어야 한다 (첫 적용 전에 Steamworks 에서 갱신)
