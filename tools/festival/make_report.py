# 한동 AI·SW페스티벌 게임 부문 제작보고서(PDF) 만들기
# 양식 · 문체: 2024 Freshmen 출품 보고서(앵두_주식 투자 시뮬레이터.pdf)와 같게 — 가운데 표지 · 점선 목차 · 「1-1」 소제목 ·
#   가운데 스크린샷 아래 「1. ~함」 번호 설명 · 기대효과는 문단 · 제작과정은 날짜별 · 맑은 고딕 · 아래 가운데 쪽 번호
# 사용법: py -X utf8 tools/festival/make_report.py <출력.pdf>
import sys, os, re, tempfile, datetime
import pymupdf as fitz
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
VER = re.search(r"bundleVersion: (\S+)", open(os.path.join(ROOT, "ProjectSettings", "ProjectSettings.asset"), encoding="utf8").read())[1]
SHOTS = os.path.join(ROOT, "docs", "steam", "art", "screenshots")
FONT = r"C:\Windows\Fonts\malgun.ttf"
out = sys.argv[1]
tmp = tempfile.mkdtemp()
TODAY = datetime.date.today().isoformat()


def shot(name):
    im = Image.open(os.path.join(SHOTS, name + "_koreana.png")).convert("RGB")
    im.thumbnail((1280, 720))
    im.save(os.path.join(tmp, name + ".jpg"), quality=85)
    return name + ".jpg"


def sec(no, title, img, items):
    """「3-1 제목」 + 가운데 스크린샷 + 1. 2. 설명 (참고 보고서의 기능 소개 칸과 같은 모양)"""
    lis = "".join(f"<li>{t}</li>" for t in items)
    pic = f'<p class="pic"><img src="{shot(img)}" width="400"/></p>' if img else ""
    k = int(no.split("-")[1])
    pb = " pb" if k > 1 and k % 2 == 1 else ""      # 두 칸마다 새 쪽 (쪽 끝에 걸린 그림이 작게 줄어들지 않게)
    return f'<div class="sec{pb}"><h3>{no} {title}</h3>{pic}<ol>{lis}</ol></div>'


features = "".join([
    sec("3-1", "직접 조준 전투", "01_gunner_catacombs_horde", [
        "자동 조준이 없어 모든 공격을 마우스(또는 패드 오른쪽 스틱)로 직접 겨누어 발사함.",
        "탄창이 비면 장전해야 하며, 장전하는 동안에는 공격할 수 없어 장전 시점이 곧 판단 요소가 됨.",
        "사방에서 수백 마리의 적이 몰려오며, 거리를 두고 움직이면서 쏘는 것이 기본 전투 방식임.",
    ]),
    sec("3-2", "보스의 빈틈", "01_aim_barrier", [
        "보스의 몸에 빛나는 「빈틈」이 열리며, 직접 조준한 공격만 빈틈을 맞힐 수 있음.",
        "패드의 보조 조준(가까운 적 자동 조준)으로는 빈틈을 맞힐 수 없어 조준 실력이 곧 화력이 됨.",
    ]),
    sec("3-3", "레벨업 카드", "02_level_up_cards", [
        "레벨이 오를 때마다 카드 3장 중 하나를 골라 능력을 쌓음.",
        "카드 아래에는 그 카드가 재료로 쓰이는 스킬 진화와 남은 단계가 표시되어, 어떤 조합을 노릴지 바로 판단할 수 있음.",
        "처음 두 번의 레벨업은 이번 판의 「갈래」(영웅마다 5개)를 고르는 카드가 나옴.",
    ]),
    sec("3-4", "무기 진화", "03_weapon_evolution", [
        "보스를 쓰러뜨리면 무기가 그 자리에서 부서지며 세 가지 진화 중 하나를 고름.",
        "판당 두 번, 되돌릴 수 없는 선택이며 영웅마다 진화 갈래가 다름.",
    ]),
    sec("3-5", "스킬 진화", "04_skill_evolution", [
        "정해진 재료 스킬을 모두 최대 레벨로 올리면 하나의 상위 스킬로 합쳐짐.",
        "재료 스킬의 효과는 그대로 이어지고, 진화한 스킬의 효과가 더해짐.",
    ]),
    sec("3-6", "영혼 트리", "05_soul_tree", [
        "판이 끝나면 모은 영혼 조각으로 영구 성장 노드를 배움.",
        "무기 · 궁극기 · 운명 · 생존 · 영혼 · 재물 여섯 갈래, 100개가 넘는 노드로 구성됨.",
    ]),
    sec("3-7", "보스 결계", "07_archer_demon_lord_barrier", [
        "보스는 체력이 줄면 거대한 결계를 펼쳐 맵을 어둠에 잠그고 공격을 쏟아부음.",
        "결계 안에서 버티며 빈틈을 노려 보스를 쓰러뜨려야 함.",
    ]),
    sec("3-8", "기억하는 보스", "06_swordsman_lich_king", [
        "보스는 이번 싸움에서 가장 많이 맞은 공격을 기억해 막음.",
        "같은 공격만 반복하면 막히고, 다른 공격으로 바꾸면 기억이 깨짐 — 한 가지 전략만 쓰는 플레이를 막기 위한 설계임.",
    ]),
    sec("3-9", "네 개의 세계와 왕", "08_cartographer_king_slime", [
        "초원 · 카타콤 · 불타는 지옥 · 영혼의 심연, 네 세계마다 왕이 기다림.",
        "첫 번째 왕 킹 슬라임은 깎일수록 작아지고 빨라짐.",
    ]),
    sec("3-10", "거울의 군주", "09_ghost_mirror_sovereign", [
        "마지막 세계의 왕으로, 플레이어가 쓰는 스킬을 그대로 따라 함.",
    ]),
    sec("3-11", "무한 모드와 최종 보스", "10_endless_developer", [
        "네 세계를 지나면 불타는 사막의 무한 모드가 열리며, 모든 적과 보스가 점점 빠르게 몰려옴.",
        "최종 보스 「개발자」를 쓰러뜨리면 한계 돌파 구간으로 넘어감.",
    ]),
    sec("3-12", "영웅 선택", "11_nine_heroes", [
        "처음 9명의 영웅으로 시작하며, 무한 모드 사막에 숨은 비밀 다섯 가지를 풀면 5명이 더 합류함(??? 칸).",
        "영웅마다 평타 · 필살기(우클릭) · 갈래 · 진화가 모두 달라 같은 게임이 다른 게임처럼 느껴짐.",
    ]),
    sec("3-13", "스킨 상점", "12_skin_shop", [
        "포인트로 캐릭터 · 무기 · 이펙트 · 커서 스킨을 구매해 장착함.",
        "스킨마다 공격 모습과 효과음이 함께 바뀜.",
    ]),
    sec("3-14", "도전 모드", None, [
        "영혼 릴레이: 영웅 셋이 한 판을 이어 달림. 쓰러지면 그 자리에서 다음 영웅이 일어나 앞 영웅의 「유품」을 물려받음.",
        "역류: 완성된 몸으로 시작해 1분마다 힘을 하나씩 내려놓으며 거꾸로 내려감. 마지막에는 맨몸으로 싸움.",
        "일일 도전: 날마다 바뀌는 규칙으로 한 판을 겨루고 스팀 순위표에 기록함.",
    ]),
])

heroes = [
    ("거너", "리볼버를 쏘고 직접 장전함. 필살기 「일제 사격」은 시간을 늦추며 여러 적을 조준한 뒤 한꺼번에 쏨."),
    ("검사", "장검으로 앞쪽 부채꼴을 벰. 유일한 근접 영웅으로, 휘두르는 동안 받는 피해가 절반이 됨."),
    ("도적", "끝없이 날아가는 표창과 무적 상태의 「출혈 돌진」으로 싸움."),
    ("궁수", "시위를 당겼다 놓아 쏨. 가득 찬 순간에 놓으면 「완벽!」 화살이 나감."),
    ("연금술사", "화염 · 빙결 · 산성 플라스크를 던짐. 던질수록 불안정도가 올라 폭발이 커짐."),
    ("지도 제작자", "깃펜으로 그은 획으로 적을 베고, 세 획마다 닫히는 삼각형 안의 적을 크게 다치게 함."),
    ("시계공", "톱니를 던지며, 필살기 「되감기」로 3초 전 자리로 돌아가 잃은 체력의 절반을 되찾음."),
    ("우체부", "가장 먼 적부터 찾아가는 편지를 날림. 「속달」은 표식을 붙인 적들을 한꺼번에 터뜨림."),
    ("유령", "차가운 손길로 적을 얼리고, 적의 몸에 빙의해 안에서 터뜨림."),
    ("씨름꾼 *", "적을 샅바째 들어 올려 무리 한가운데로 던짐."),
    ("공사 반장 *", "적을 죽이지 않고 삽날로 밀어 쌓은 뒤 짓누름."),
    ("인형사 *", "커서를 따라 떠다니는 더미를 조종하고, 영웅과 더미 사이의 줄로 적을 벰."),
    ("장의사 *", "적이 지나갈 자리에 구멍을 파서 빠뜨리고 묻은 뒤 묘비로 길을 막음."),
    ("사진사 *", "뷰파인더 안의 적을 찍어 납작한 종이로 만듦."),
]
hero_rows = "".join(f"<tr><td class='c'>{n}</td><td>{d}</td></tr>" for n, d in heroes)

timeline = [
    ("9/24", "개발 시작. Unity 2D 프로젝트 생성, 거너의 이동 · 마우스 조준 사격 구현"),
    ("9/25", "캐릭터 시스템 추가 — 거너 · 검사 · 도적 · 궁수 · 연금술사, 캐릭터 도감, 포인트로 영웅 구매"),
    ("10/13", "무한 모드 최종 국면과 최종 보스 「개발자」 추가, 도트 그래픽 전면 교체"),
    ("10/14", "게임패드 지원, 첫 구매자 시점 점검 81개 항목 수정"),
    ("10/15", "판마다 달라지는 이벤트 · 유물 · 맵 물체, 발견형 도감 추가"),
    ("10/17", "「한 판 더」 시스템(영혼 계약 · 난이도 사다리 · 무한 이정표), 전면 최적화와 그래픽 품질 옵션"),
    ("10/18", "차별점 구현 — 보스 빈틈(직접 조준 판정) · 기억하는 보스"),
    ("10/19", "영웅마다 빌드 갈래 5개 추가, 자동 플레이 봇으로 밸런스 측정 시작, 플레이 데이터 수집 서버 연결"),
    ("10/21 ~ 10/23", "UI 전면 개편(도트 UI 틀), 엔진을 Unity 6으로 교체"),
    ("10/25", "그래픽 가독성 전면 개편 — 보스 6종 고해상도로 다시 그림, 메뉴 초상 29장 · 아이콘 정리"),
    ("11/2", "2.0 대규모 업데이트 — 숨은 영웅 5명, 스킨 54종, 도전 모드(영혼 릴레이 · 역류), 결정적 순간의 특수효과"),
    (f"11/3 ~ {datetime.date.today().month}/{datetime.date.today().day}", f"메인 메뉴 개편, 창을 닫은 뒤 3초 재개 카운트다운, 스킨 92종 시청각 개편 (버전 {VER})"),
]
tl = "".join(f"<p class='tl'><b>{d}</b> – {t}</p>" for d, t in timeline)

html = f"""
<div class="cover">
<p class="t1">한동대학교 AI·SW 페스티벌</p>
<p class="t1">스마트 SW 공모전 게임 부문</p>
<p class="t2">소울 세이버(Soul Saver) 게임 제작 보고서</p>
<hr/>
<p class="t3">Unity 6 · C#을 활용한 탑다운 서바이버 로그라이트 게임</p>
<p class="t4">22400258 박민근<br/>{TODAY}</p>
<p class="abs">Unity 6(C#)을 활용하여, 사방에서 몰려오는 수백 마리의 적을 마우스로 직접 조준하고 장전하며 쓰러뜨리는
탑다운 서바이버 로그라이트 게임을 제작. 레벨업 카드 · 무기 진화 · 스킬 진화 · 영혼 트리로 판마다 다른 빌드를 만들고,
직접 조준한 공격만 맞는 보스의 「빈틈」과 가장 많이 맞은 공격을 기억해 막는 보스로 조준 실력과 전략이 함께 보상받도록 설계함.
영웅 14명 · 세계 4개 · 무한 모드 · 도전 모드 2종 · 4개 언어를 지원하며, 스팀(Steam)에 상점 페이지를 열고 버전 {VER}까지 업데이트를 이어 옴.</p>
</div>

<div class="pb"><p class="toc">목차</p></div>

<h2 class="pb">1. 게임 개요</h2>
<h3>1-1 게임 소개</h3>
<ol>
<li>장르: 탑다운 슈터 · 서바이버(탄막 서바이벌) · 로그라이트.</li>
<li>플랫폼: Windows PC (마우스 · 키보드 / 게임패드), 엔진 Unity 6(6000.3), 언어 C#.</li>
<li>지원 언어: 한국어 · 영어 · 일본어 · 중국어 간체.</li>
<li>한 판 약 15~30분, 무한 모드는 그 이상.</li>
<li>개발 규모: C# 스크립트 약 590개 · 12만 줄, 커밋 1,000건 이상 (2026-09-24 시작, 1인 개발).</li>
</ol>
<h3>1-2 조작 방법</h3>
<ol>
<li>이동 W · A · S · D, 조준 마우스, 공격 좌클릭, 필살기 우클릭, 장전 R (설정에서 키 변경 가능).</li>
<li>게임패드: 왼쪽 스틱 이동, 오른쪽 스틱 조준(기울이면 평타 자동 발사).</li>
<li>레벨업 카드는 클릭 또는 숫자 키 1 · 2 · 3으로 고름.</li>
</ol>

<h2>2. 게임 설계</h2>
<h3>2-1 핵심 루프</h3>
<ol>
<li>영웅을 고르고 전장에 들어가 적을 쓰러뜨려 경험치를 얻음.</li>
<li>레벨이 오르면 카드 3장 중 하나를 골라 능력을 쌓음.</li>
<li>보스는 결계를 펼쳐 공격하고, 몸에 열리는 빈틈을 직접 조준해 맞혀야 함.</li>
<li>보스를 쓰러뜨리면 무기가 그 자리에서 진화함 (판당 두 번).</li>
<li>판이 끝나면 영혼 조각으로 영혼 트리를, 포인트로 영웅 · 스킨을 해금하고 다시 도전함.</li>
</ol>
<h3>2-2 설계 의도</h3>
<ol>
<li><b>숙련이 보상받는 구조</b> — 일반적인 서바이버 게임은 공격이 자동이지만, 이 게임은 조준 실력이 곧 화력이 되도록 함.</li>
<li><b>보스가 플레이어를 기억함</b> — 가장 많이 맞은 공격을 막아, 한 가지 공격만 반복하는 단조로운 플레이를 막음.</li>
<li><b>영웅마다 다른 손맛</b> — 쏘기 · 베기 · 시위 당기기 · 던지기 · 그리기 · 되감기 · 빙의 · 들어 던지기 · 구멍 파기 등 영웅마다 조작 방식 자체가 다름.</li>
<li><b>난이도를 낮추는 편의 기능 대신 빌드의 폭</b> — 카드 · 갈래 · 진화 · 영혼 트리의 조합으로 도전 수준을 조절함.</li>
</ol>
<h3>2-3 세계와 보스</h3>
<table>
<tr><th>세계</th><th>보스</th><th>특징</th></tr>
<tr><td>초원</td><td>킹 슬라임</td><td>깎일수록 작아지고 빨라짐</td></tr>
<tr><td>카타콤</td><td>리치 왕</td><td>결계를 펼쳐 맵을 어둠에 잠금</td></tr>
<tr><td>불타는 지옥</td><td>지옥의 군주</td><td>결계 속에서 화염 공격</td></tr>
<tr><td>영혼의 심연</td><td>거울의 군주</td><td>플레이어의 스킬을 그대로 흉내 냄</td></tr>
<tr><td>불타는 사막 (무한)</td><td>개발자 · 엄마</td><td>끝없이 강해지는 적, 숨은 영웅 5명의 비밀</td></tr>
</table>
<h3>2-4 영웅 (14명, * 는 2.0에서 추가된 숨은 영웅)</h3>
<table>
<tr><th>영웅</th><th>특징</th></tr>
{hero_rows}
</table>
<h3>2-5 주요 규칙</h3>
<ol>
<li>레벨업마다 카드 3장 중 1장 선택, 처음 두 번은 이번 판의 「갈래」 선택.</li>
<li>무기 진화는 보스 처치 때마다 3택 1, 판당 2회, 되돌릴 수 없음.</li>
<li>스킬 진화: 재료 스킬을 모두 최대 레벨로 올리면 상위 스킬로 합쳐짐 (영웅별 고유 진화 포함).</li>
<li>보스의 빈틈은 직접 조준한 공격만 맞음 (패드 보조 조준 제외).</li>
<li>보스는 이번 싸움에서 가장 많이 맞은 공격을 기억해 막고, 다른 공격으로 몰아치면 기억이 깨짐.</li>
<li>밸런스 목표: 숙련자 기준 어떤 영웅이든 무한 모드 최종 보스까지 약 30분.</li>
</ol>

<h2 class="pb">3. 주요 기능 스크린샷과 설명</h2>
{features}

<h2 class="pb">4. 기술 구현</h2>
<h3>4-1 자동 플레이 봇을 이용한 밸런스 측정</h3>
<ol>
<li>사람 대신 이동 · 조준 · 공격 · 카드 선택을 하는 자동 봇(<code>tools/autoplay</code>)을 만들어 Unity 배치 모드에서 수십 판을 돌림.</li>
<li>영웅들이 비슷한 시간에 같은 목표를 달성하도록 공격력 · 성장 곡선을 조정하고, 회차별 결과를 <code>docs/balance</code>에 기록함.</li>
</ol>
<h3>4-2 플레이 데이터 수집 서버</h3>
<ol>
<li>플레이어 동의를 받은 경우에만 판 기록(영웅 · 시간 · 고른 카드 등)을 Cloudflare Workers + D1 서버로 보냄.</li>
<li>모은 데이터로 너무 강하거나 약한 영웅 · 카드를 찾아 다음 업데이트에 반영함.</li>
</ol>
<h3>4-3 자동 녹화 · 스크린샷 도구</h3>
<ol>
<li>실제 게임을 자동으로 연출해 트레일러 영상과 스팀 상점 스크린샷(4개 언어 × 18장)을 찍는 도구를 만듦.</li>
<li>업데이트로 화면이 바뀌어도 명령 한 줄로 홍보 자료를 다시 만들 수 있음.</li>
</ol>
<h3>4-4 현지화와 배포</h3>
<ol>
<li>모든 게임 안 문구를 표(LocTable)로 관리해 한국어 · 영어 · 일본어 · 중국어로 표시함.</li>
<li>빌드 → 스팀 업로드 → 패치노트를 스크립트로 자동화해 짧은 주기로 업데이트함.</li>
</ol>

<h2>5. AI 도구 활용</h2>
<p>공모전 공통 규정에 따라 출품 과정에서 사용한 AI 도구와 그 방법을 밝힌다. 모든 결과물은 개발자가 직접 검토하고 수정하였다.</p>
<table>
<tr><th>도구</th><th>사용한 곳과 방법</th></tr>
<tr><td>Claude Code (Anthropic)</td><td>기능 구현 보조, 버그 점검, 패치노트 · 상점 문구 초안, 자동 봇 측정 결과 정리. 설계와 최종 판단은 개발자가 함.</td></tr>
<tr><td>Google Gemini 이미지 생성</td><td>캐릭터 · 몹 · 아이콘 시안 생성 → 도트 변환 스크립트로 가공 → 한 장씩 검수 후 승인한 것만 적용.</td></tr>
<tr><td>AI 코딩 보조로 작성한 합성 스크립트</td><td>파이썬 합성 스크립트로 배경 음악 · 효과음을 직접 생성 (외부 음원 사용 안 함).</td></tr>
<tr><td>AI 번역 보조</td><td>일본어 · 중국어 번역 초안 작성 후 개발자가 검토.</td></tr>
</table>
<p>게임 안에서 실행 중에 외부 AI 서비스를 호출하는 기능은 없으며, 개발 과정에서 자동 플레이 봇과 AI 에이전트를 함께 써서 밸런스를 설계 · 측정 · 조정하였다.</p>

<h2 class="pb">6. 기대효과</h2>
<p class="prose">먼저 이 게임은 자동 공격이 기본인 서바이버 장르에 「직접 조준」이라는 손맛을 더하여, 같은 장르를 즐기던 플레이어에게 새로운 재미를 준다.
보스의 빈틈은 직접 조준한 공격만 맞고 보스는 가장 많이 맞은 공격을 기억해 막기 때문에, 플레이어는 상황을 읽고 공격 수단을 바꾸는 판단을 끊임없이 하게 된다.
한 판이 15~30분으로 짧고 판마다 카드 · 갈래 · 진화가 달라 반복해서 즐길 동기가 생기며, 영웅 14명이 저마다 다른 조작 방식을 가져 오래 즐길 수 있다.</p>
<p class="prose">또한 1인 개발로 기획 · 구현 · 그래픽 · 음악 · 4개 언어 현지화 · 스팀 배포 · 업데이트 운영까지 해낸 사례로서, AI 도구를 활용하면서도
자동 플레이 봇과 플레이 데이터 수집으로 결과를 검증하는 개발 방식을 보여 준다. 이는 작은 팀이나 학생 개발자도 짧은 기간에 완성도 있는 게임을 만들고
실제 시장에 내놓을 수 있다는 가능성을 보여 준다. 마지막으로, 일일 도전과 스팀 순위표를 이용하면 친구 · 동아리 단위로 기록을 겨루는 이벤트로도 활용할 수 있다.</p>

<h2>7. 추가 개발 계획</h2>
<ol>
<li>수집한 플레이 데이터를 바탕으로 영웅 · 카드 밸런스를 계속 조정함.</li>
<li>새 영웅 · 진화 갈래 · 세계를 추가함.</li>
<li>스팀 덱과 게임패드 조작성을 개선함.</li>
<li>순위표 · 일일 도전을 확장해 함께 경쟁하는 기능을 늘림.</li>
<li>모바일 등 다른 플랫폼 이식을 검토함.</li>
</ol>

<h2>8. 그 외 특장점</h2>
<ol>
<li>스팀에 상점 페이지를 연 실제 출시작으로, 무료 체험판도 제공함 (store.steampowered.com/app/5328770).</li>
<li>음악 · 효과음을 모두 직접 만들어 저작권 문제가 없음.</li>
<li>4개 언어와 풀 게임패드(스팀 덱 포함)를 지원함.</li>
<li>도전 과제 52개 · 스팀 순위표(무한 · 일일 도전) 연동.</li>
<li>심사용 제출 빌드는 스팀 없이 실행되며, 모든 영웅 · 스킨 · 모드가 열려 있고 포인트가 99,999,999로 설정되어 있어 바로 모든 기능을 확인할 수 있음.</li>
</ol>

<h2 class="pb">9. 제작과정</h2>
{tl}

<h2>10. 실행 방법</h2>
<ol>
<li>제출한 zip을 풀고 <code>3_실행파일/SoulSaver/Soul Saver Festival.exe</code>를 실행함 (Windows 10 64비트 · DirectX 11 · 메모리 4GB).</li>
<li>메인 메뉴의 「시작」으로 일반 판을, 「도전 모드」로 일일 도전 · 영혼 릴레이 · 역류를, 「영웅 · 스킨」으로 영웅과 스킨을 고름.</li>
</ol>
"""

css = """
@font-face { font-family: mg; src: url(malgun.ttf); }
@font-face { font-family: mg; font-weight: bold; src: url(malgunbd.ttf); }
body { font-family: mg; font-size: 10.5pt; line-height: 1.55; color: #111; }
.cover { text-align: center; margin-top: 70pt; }
.cover .t1 { font-size: 21pt; font-weight: bold; margin: 0; line-height: 1.5; }
.cover .t2 { font-size: 14pt; font-weight: bold; margin-top: 80pt; }
.cover hr { border: 0; border-top: 0.8pt solid #8fa8d8; }
.cover .t3 { font-size: 10pt; color: #333; }
.cover .t4 { font-size: 10.5pt; font-weight: bold; margin-top: 26pt; }
.cover .abs { text-align: justify; font-size: 11pt; margin-top: 24pt; line-height: 1.7; }
.toc { font-size: 9pt; color: #3a63b8; font-weight: bold; }
h2 { font-size: 14pt; font-weight: bold; text-align: center; margin-top: 18pt; }
h3 { font-size: 10.5pt; font-weight: bold; text-align: center; margin: 12pt 0 4pt 0; }
.pb { page-break-before: always; }
.sec { page-break-inside: avoid; }
.pic { text-align: center; margin: 2pt 0; }
ol { margin-left: 18pt; }
li { margin-bottom: 3pt; }
table { border-collapse: collapse; width: 100%; margin: 4pt 0; }
th, td { border: 0.6pt solid #999; padding: 3pt 5pt; text-align: left; vertical-align: top; font-size: 9.5pt; }
th { font-weight: bold; text-align: center; }
td.c { text-align: center; white-space: nowrap; }
.prose { text-align: justify; text-indent: 10pt; line-height: 1.8; }
.tl { margin: 5pt 0; }
code { font-size: 9pt; }
"""

arch = fitz.Archive()
arch.add(r"C:\Windows\Fonts")
arch.add(tmp)
story = fitz.Story(html=html, user_css=css, archive=arch)
media = fitz.paper_rect("a4")
where = media + (60, 60, -60, -60)
buf = os.path.join(tmp, "body.pdf")
writer = fitz.DocumentWriter(buf)
more = 1
while more:
    dev = writer.begin_page(media)
    more, _ = story.place(where)
    story.draw(dev)
    writer.end_page()
writer.close()

# ---------------- 목차 (2쪽): 제목이 실제로 놓인 쪽을 찾아 점선으로 채움 · 쪽 번호는 표지 다음 쪽이 1
doc = fitz.open(buf)
heads = re.findall(r"<h([23])[^>]*>(.*?)</h\1>", html)
font = fitz.Font(fontfile=FONT)
toc = []
for lvl, text in heads:
    text = re.sub(r"<[^>]+>", "", text)
    page = next((i for i in range(2, doc.page_count) if doc[i].search_for(text)), None)
    if page is not None:
        toc.append((int(lvl), text, page))     # page: 0부터 → 표시 번호 = page (표지 0 · 목차 1)
p = doc[1]
p.insert_font(fontname="mg", fontfile=FONT)
y = 92
L, R = 70, media.width - 70
for lvl, text, page in toc:
    size = 9.5 if lvl == 2 else 9
    x = L + (0 if lvl == 2 else 12)
    num = str(page)
    tw, nw = font.text_length(text, size), font.text_length(num, size)
    dots = "." * max(3, int((R - nw - 6 - (x + tw + 6)) / font.text_length(".", size)))
    p.insert_text((x, y), text + " " + dots, fontname="mg", fontsize=size)
    p.insert_text((R - nw, y), num, fontname="mg", fontsize=size)
    y += 17.5
    if y > media.height - 60:
        break
# 쪽 번호 (아래 가운데, 표지 제외)
for i in range(1, doc.page_count):
    pg = doc[i]
    pg.insert_font(fontname="mg", fontfile=FONT)
    s = str(i)
    pg.insert_text((media.width / 2 - font.text_length(s, 8) / 2, media.height - 30), s, fontname="mg", fontsize=8)
doc.save(out, garbage=4, deflate=True)
print("OK", out, doc.page_count, "pages", "목차", len(toc), "칸")
