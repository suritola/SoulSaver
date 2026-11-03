# 한동 AI·SW페스티벌 게임 부문 제작보고서(PDF) 만들기
# 사용법: py -X utf8 tools/festival/make_report.py <출력.pdf>
# 스크린샷은 docs/steam/art/screenshots (_koreana) 를 줄여서 쓴다.
import sys, os, tempfile
import fitz
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SHOTS = os.path.join(ROOT, "docs", "steam", "art", "screenshots")
out = sys.argv[1]
tmp = tempfile.mkdtemp()

shots = [
    ("01_gunner_catacombs_horde", "카타콤에서 몰려드는 대군과 싸우는 거너. 자동 조준이 없어서 모든 탄을 직접 겨눠 쏜다."),
    ("02_level_up_cards", "레벨업마다 3장의 카드 중 하나를 골라 그 판만의 빌드를 만든다."),
    ("03_weapon_evolution", "보스를 쓰러뜨리면 무기가 부서지며 전혀 다른 무기로 진화한다 (영웅마다 진화 갈래가 다름)."),
    ("04_skill_evolution", "필살기(우클릭)도 진화한다. 영웅마다 고유한 필살기와 갈래를 가진다."),
    ("05_soul_tree", "100개가 넘는 노드의 영혼 트리. 무기 · 궁극기 · 운명 · 생존 · 영혼 · 재물 갈래로 영구 성장."),
    ("06_swordsman_lich_king", "검사와 두 번째 왕 '리치 왕'의 결계 전투."),
    ("07_archer_demon_lord_barrier", "결계 속에서 보스의 빛나는 '빈틈'을 직접 조준해 찌르는 궁수."),
    ("08_cartographer_king_slime", "지도 제작자와 첫 번째 왕 '킹 슬라임'. 슬라임은 깎일수록 작아지고 빨라진다."),
    ("09_ghost_mirror_sovereign", "유령과 '거울의 군주'. 거울의 군주는 플레이어의 스킬을 그대로 따라 한다."),
    ("10_endless_developer", "무한 모드의 최종 보스 '개발자'. 한계 돌파 이후 끝없이 강해지는 적과의 싸움."),
    ("11_nine_heroes", "손맛이 모두 다른 아홉 영웅."),
    ("12_skin_shop", "캐릭터 · 무기 · 이펙트 · 커서 스킨 상점."),
]
for name, _ in shots:
    im = Image.open(os.path.join(SHOTS, name + "_koreana.png")).convert("RGB")
    im.thumbnail((1280, 720))
    im.save(os.path.join(tmp, name + ".jpg"), quality=85)

fig = "".join(
    f'<div class="fig"><img src="{n}.jpg" width="440"/><p class="cap">그림 {i}. {c}</p></div>'
    for i, (n, c) in enumerate(shots, 1)
)

html = f"""
<div class="cover">
<p class="small">2026 한동 AI·SW페스티벌 · 스마트 SW 공모전 게임 부문</p>
<h1>Soul Saver</h1>
<p class="sub">자동 조준이 없는 도트 그래픽 서바이버 로그라이트</p>
<p class="meta">제작보고서</p>
<p class="meta">출품자: 박민근 (1인 개발)<br/>소속 · 학번: [소속학부 / 학번 기입]<br/>버전: 2.0.1 (Windows)<br/>스팀 상점: store.steampowered.com/app/5328770</p>
</div>

<h2 class="pb">1. 게임 소개</h2>
<p><b>Soul Saver</b>는 수백 마리의 적이 사방에서 몰려오는 서바이버류(탄막 서바이벌) 액션에
<b>직접 조준하고 직접 장전하는 손맛</b>을 더한 게임이다. 일반적인 서바이버 게임은 자동으로 공격하지만,
Soul Saver는 마우스(또는 패드의 오른쪽 스틱)로 모든 공격을 직접 겨눠야 한다.
영웅 9명이 각자 다른 방식으로 싸우고, 초원에서 영혼의 심연까지 네 개의 세계를 내려가며 왕 넷을 쓰러뜨리면
끝없이 이어지는 무한 모드가 열린다.</p>
<table>
<tr><th>장르</th><td>액션 · 로그라이트 · 서바이버 (탑다운 슈터)</td></tr>
<tr><th>플랫폼</th><td>Windows PC (마우스 · 키보드 / 게임패드)</td></tr>
<tr><th>엔진</th><td>Unity 6 (6000.3), C#</td></tr>
<tr><th>언어</th><td>한국어 · English · 日本語 · 简体中文</td></tr>
<tr><th>플레이 시간</th><td>한 판 약 15~30분 (무한 모드는 그 이상)</td></tr>
<tr><th>개발 규모</th><td>C# 스크립트 약 588개 · 12만 줄, 커밋 1,000건 이상 (2026-09-24 시작, 1인 개발)</td></tr>
</table>

<h2>2. 게임 설계</h2>
<h3>2.1 핵심 루프</h3>
<ol>
<li>영웅을 고르고 전장에 들어간다. 적을 쓰러뜨리면 경험치(영혼)를 얻는다.</li>
<li>레벨이 오르면 카드 3장 중 하나를 골라 능력을 쌓는다. 판마다 빌드가 달라진다.</li>
<li>보스는 거대한 <b>결계</b>를 펼쳐 맵을 어둠에 잠그고 공격을 쏟아붓는다.</li>
<li>보스 몸에 열리는 빛나는 <b>빈틈</b>은 직접 조준한 공격만 맞힐 수 있다.</li>
<li>보스를 쓰러뜨리면 무기가 그 자리에서 <b>진화</b>한다. 진화는 판당 두 번, 되돌릴 수 없는 선택이다.</li>
<li>판이 끝나면 얻은 영혼으로 <b>영혼 트리</b>(영구 성장)와 스킨을 해금하고 다시 도전한다.</li>
</ol>
<h3>2.2 설계 의도</h3>
<ul>
<li><b>숙련이 보상받는 구조</b> — 조준 실력이 곧 화력이다. 보스의 약점(빈틈)은 자동 공격으로는 닿지 않는다. 패드의 보조 조준도 빈틈에는 적용되지 않는다.</li>
<li><b>보스가 플레이어를 기억한다</b> — 보스는 이번 싸움에서 가장 많이 맞은 공격을 기억해 막는다. 같은 공격만 반복하면 막히고, 다른 공격으로 바꾸면 기억이 깨진다. 한 가지 전략으로 끝까지 밀어붙이는 플레이를 막기 위한 설계다.</li>
<li><b>영웅마다 다른 동사</b> — 거너(쏘고 장전) · 검사(베기) · 도적 · 궁수(시위를 당겨 '완벽!' 타이밍) · 연금술사 · 지도 제작자 · 시계공(시간을 되감음) · 우체부 · 유령(적에게 빙의). 같은 게임이 영웅마다 다른 게임처럼 느껴지게 했다.</li>
<li><b>보조 모드를 두지 않음</b> — 난이도를 낮추는 편의 기능 대신, 영혼 트리와 카드 빌드의 폭으로 도전 수준을 조절한다.</li>
</ul>
<h3>2.3 세계와 보스</h3>
<table>
<tr><th>세계</th><th>왕</th><th>특징</th></tr>
<tr><td>초원</td><td>킹 슬라임</td><td>깎일수록 작아지고 빨라진다</td></tr>
<tr><td>카타콤</td><td>리치 왕</td><td>결계를 펼쳐 맵을 어둠에 잠금</td></tr>
<tr><td>불타는 지옥</td><td>마왕</td><td>결계를 펼쳐 맵을 어둠에 잠금</td></tr>
<tr><td>영혼의 심연</td><td>거울의 군주</td><td>플레이어의 스킬을 그대로 복사</td></tr>
<tr><td>불타는 사막(무한 모드)</td><td>개발자</td><td>한계 돌파 이후 끝없이 강해지는 적</td></tr>
</table>

<h2 class="pb">3. 주요 기능 스크린샷과 설명</h2>
{fig}

<h2 class="pb">4. 개발 과정과 기술</h2>
<h3>4.1 개발 과정</h3>
<p>2026년 9월 24일에 시작해 1인으로 개발해 스팀 상점 페이지에 등록했고(무료 체험판 포함), 버전 2.0.1까지 업데이트를 이어 왔다. 주요 개발 단계는 다음과 같다.</p>
<ul>
<li>핵심 전투(조준 · 장전 · 대군 처리)와 영웅 9명의 고유 동작 구현</li>
<li>보스 시스템: 결계 · 빈틈 · 보스의 기억(가장 많이 맞은 공격 차단) · 무기 진화</li>
<li>영혼 트리 · 카드 · 스킨 · 도전 과제 38개 · 순위표(무한 · 일일 도전)</li>
<li>4개 언어 현지화, 게임패드 지원, 스팀 연동</li>
<li>그래픽 가독성 전면 개편과 메인 메뉴 개편, 2.0 대규모 업데이트</li>
</ul>
<h3>4.2 기술적 특징</h3>
<ul>
<li><b>자동 플레이 봇으로 밸런스 검증</b> — 사람 대신 이동 · 조준 · 공격 · 카드 선택을 수행하는 자동 봇(<code>tools/autoplay</code>)을 만들어
영웅 9명이 비슷한 시간(약 30분)에 같은 목표를 달성하도록 수치를 조정했다. 측정 과정은 <code>docs/balance</code>에 기록했다.</li>
<li><b>플레이 데이터 수집 서버</b> — 실제 플레이 통계를 모아 밸런스에 반영하기 위한 서버(Cloudflare Workers + D1)를 구성했다.</li>
<li><b>배치 모드 트레일러 녹화</b> — 실제 게임 플레이를 자동으로 연출 · 녹화하는 도구로 시연 영상을 만든다.</li>
<li><b>성능 점검</b> — 오브젝트 증식으로 인한 프레임 저하를 추적해 해결한 기록을 남겼다.</li>
</ul>

<h2>5. 사용한 AI 도구 (공통 필수 기재)</h2>
<p>모든 결과물은 개발자가 직접 검토하고 수정했다. AI가 만든 것을 그대로 쓰지 않았다.</p>
<table>
<tr><th>도구</th><th>쓴 곳</th><th>어떻게</th></tr>
<tr><td>Claude Code (Anthropic)</td><td>코드 · 문서 · 밸런스 점검</td><td>기능 구현 보조, 버그 점검, 패치노트와 상점 문구 초안, 자동 봇 측정 결과 정리. 설계와 최종 판단은 개발자가 했다.</td></tr>
<tr><td>Google Gemini 이미지 생성</td><td>도트 그래픽</td><td>캐릭터 · 몹 · 아이콘 시안 생성 → 도트 변환 스크립트로 가공 → 한 장씩 검수하고 승인한 것만 적용.</td></tr>
<tr><td>AI 코딩 보조로 작성한 합성 스크립트</td><td>음악 · 효과음</td><td>파이썬 합성 스크립트(<code>tools/trailer/make_trailer_music.py</code> 등)로 음악을 직접 생성. 외부 음원은 쓰지 않았다.</td></tr>
<tr><td>AI 번역 보조</td><td>일본어 · 중국어 번역</td><td>초안을 만든 뒤 개발자가 검토.</td></tr>
</table>
<p><b>AI 기반 기능:</b> 게임 안에는 실행 중 외부 AI 서비스를 호출하는 기능은 없다.
다만 게임 밸런스를 검증하는 자동 플레이 봇(개발 도구)을 만들어 AI 에이전트가 설계 · 측정 · 조정 과정을 돕게 했다.</p>

<h2 class="pb">6. 기대효과</h2>
<ul>
<li><b>플레이어:</b> 자동 공격 중심의 서바이버 장르에서 '조준 숙련'이라는 새로운 재미를 제공한다. 짧은 판(15~30분)과 영구 성장으로 반복 플레이 동기를 준다.</li>
<li><b>시장:</b> 스팀에서 서바이버 장르는 포화 상태이며, '자동 조준 없음'과 '약점은 직접 조준만 가능'이라는 차별점으로 구분된다.</li>
<li><b>개발 역량:</b> 1인이 기획 · 구현 · 그래픽 · 음악 · 현지화 · 배포 · 운영까지 한 사례로, AI 도구를 쓰면서도 검증(자동 봇 · 데이터 수집)으로 품질을 책임지는 개발 방식을 보여준다.</li>
</ul>

<h2>7. 추가 개발 계획</h2>
<ul>
<li>수집한 플레이 데이터를 바탕으로 한 지속적인 밸런스 조정</li>
<li>신규 영웅 · 무기 진화 갈래 · 콘텐츠 추가</li>
<li>스팀 덱과 패드 조작성 개선</li>
<li>시작 로딩 화면과 연출 개선</li>
<li>순위표와 일일 도전을 이용한 커뮤니티 기능 확대</li>
</ul>

<h2>8. 그 밖의 특장점</h2>
<ul>
<li>음악을 포함한 모든 사운드를 직접 제작해 저작권 문제가 없다.</li>
<li>4개 언어 지원과 풀 게임패드 지원(스팀 덱 포함).</li>
<li>스팀 상점 페이지가 공개되어 있어 외부에서도 확인 가능(<code>store.steampowered.com/app/5328770</code>).</li>
</ul>

<h2 class="pb">9. 실행 방법과 조작</h2>
<ol>
<li>제출한 zip을 풀고 <code>Soul Saver.exe</code>를 실행한다. (Windows 10 64비트 · DirectX 11 · 메모리 4GB · 저장 공간 300MB)</li>
<li>이 제출 빌드는 스팀 연동을 끈 버전이라 스팀 없이 바로 실행된다.</li>
<li>조작: 이동 WASD · 조준 마우스 · 공격 좌클릭 · 필살기 우클릭 · 장전 R (설정에서 변경 가능). 패드는 왼쪽 스틱 이동, 오른쪽 스틱 조준.</li>
<li>처음 시작하면 거너로 초원 1장부터 플레이한다.</li>
</ol>
"""

css = """
@font-face { font-family: mg; src: url(malgun.ttf); }
@font-face { font-family: mg; font-weight: bold; src: url(malgunbd.ttf); }
body { font-family: mg; font-size: 9.5pt; line-height: 1.4; color: #222; }
h1 { font-size: 40pt; margin: 0; color: #5b2a86; }
h2 { font-size: 16pt; color: #5b2a86; margin-top: 14pt; }
h3 { font-size: 11.5pt; color: #333; margin-bottom: 2pt; }
.pb { page-break-before: always; }
.cover { text-align: center; margin-top: 190pt; }
.cover .sub { font-size: 14pt; color: #444; }
.cover .small { font-size: 11pt; color: #666; }
.cover .meta { font-size: 11pt; margin-top: 30pt; line-height: 1.8; }
table { border-collapse: collapse; width: 100%; margin: 4pt 0; }
th, td { border: 0.6pt solid #aaa; padding: 3pt 5pt; text-align: left; vertical-align: top; font-size: 9.5pt; }
th { background-color: #eee6f5; }
.fig { text-align: center; margin: 6pt 0; page-break-inside: avoid; }
.cap { font-size: 8.5pt; color: #444; margin: 0; }
code { font-size: 9pt; color: #444; }
"""

arch = fitz.Archive()
arch.add(r"C:\Windows\Fonts")
arch.add(tmp)
story = fitz.Story(html=html, user_css=css, archive=arch)
writer = fitz.DocumentWriter(out)
media = fitz.paper_rect("a4")
where = media + (50, 50, -50, -50)
more = 1
while more:
    dev = writer.begin_page(media)
    more, _ = story.place(where)
    story.draw(dev)
    writer.end_page()
writer.close()
print("OK", out, fitz.open(out).page_count, "pages")
