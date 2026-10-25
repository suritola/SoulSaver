using UnityEngine;

// 번역표 (1.4.4~): 플레이 데이터 보내기 (Telemetry) — 동의창 · 설정 > 일반 · 개인정보 처리방침 전문 (문서 버전 1 · 시행일 2026-10-20)
// 전문을 고치면 Telemetry.ConsentVersion 을 +1 (다시 동의를 받음) · docs/privacy/ 의 처리방침도 같이
// {contact} 는 Telemetry.ContactEmail (비어 있으면 telemetry.contact.fallback) 로 바뀜
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddTelemetryBoot() { if (!SceneFlow.DeferStartup(5, AddTelemetry)) AddTelemetry(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddTelemetry()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 동의창
        C("telemetry.title", "플레이 데이터 보내기 동의", "Share Play Data?", "プレイデータ送信への同意", "是否同意发送游戏数据");
        C("telemetry.summary",
          "판이 끝날 때마다 영웅 · 카드 · 결과 같은 플레이 기록을 익명으로 보내 게임 밸런스를 다듬는 데 씁니다. 이름 · SteamID · 이메일은 보내지 않습니다. 동의하지 않아도 게임은 똑같이 즐길 수 있고, 설정 > 일반에서 언제든 바꿀 수 있습니다.",
          "After each run, anonymous play records such as your hero, cards and result are sent to help balance the game. Your name, SteamID and email are never sent. The game plays exactly the same if you decline, and you can change this anytime in Settings > General.",
          "各プレイの終了時に、英雄・カード・結果などのプレイ記録を匿名で送信し、ゲームバランスの調整に使います。名前・SteamID・メールアドレスは送信しません。同意しなくてもゲームは同じように遊べ、設定 > 一般でいつでも変更できます。",
          "每局结束时，将匿名发送英雄、卡牌、结果等游戏记录，用于调整游戏平衡。不会发送姓名、SteamID 或电子邮箱。即使不同意，游戏体验也完全相同，并可随时在 设置 > 常规 中更改。");
        C("telemetry.agree", "동의", "Agree", "同意する", "同意");
        C("telemetry.disagree", "동의하지 않음", "Decline", "同意しない", "不同意");
        C("telemetry.contact.fallback", "스팀 상점 페이지의 개발자 문의", "the developer contact on the Steam store page", "Steamストアページの開発者お問い合わせ", "Steam 商店页面上的开发者联系方式");

        // ================================================================= 설정 > 일반
        C("telemetry.setting", "플레이 데이터 보내기", "Share Play Data", "プレイデータ送信", "发送游戏数据");
        C("telemetry.setting.tip",
          "켜면 판이 끝날 때마다 익명 플레이 기록을 보내 밸런스 조정에 씁니다(동의). 끄면 보내지 않고, 아직 보내지 못한 기록도 지웁니다. 자세한 내용은 「전문 보기」에 있습니다.",
          "When on, an anonymous play record is sent after each run to help with balancing (consent). When off, nothing is sent and unsent records are discarded. See 'Read Full Text' for details.",
          "オンにすると、各プレイの終了時に匿名のプレイ記録を送信し、バランス調整に使います(同意)。オフにすると送信せず、未送信の記録も削除します。詳しくは「全文を読む」をご覧ください。",
          "开启后，每局结束时会发送匿名游戏记录用于平衡调整（同意）。关闭后不再发送，并删除尚未发送的记录。详情请见「查看全文」。");
        C("telemetry.view", "전문 보기", "Read Full Text", "全文を読む", "查看全文");
        C("telemetry.id", "익명 ID", "Anonymous ID", "匿名ID", "匿名 ID");
        C("telemetry.delete", "내 데이터 삭제 요청", "Request Deletion of My Data", "自分のデータの削除を依頼", "申请删除我的数据");
        C("telemetry.delete.confirm", "한 번 더 누르면 삭제를 요청합니다", "Press again to request deletion", "もう一度押すと削除を依頼します", "再按一次即申请删除");
        C("telemetry.delete.sending", "요청을 보내는 중...", "Sending request...", "依頼を送信中...", "正在发送申请...");
        C("telemetry.delete.done", "삭제했습니다 (새 익명 ID)", "Deleted (new anonymous ID)", "削除しました(新しい匿名ID)", "已删除（新的匿名 ID）");
        C("telemetry.delete.failed", "보내지 못했습니다. 잠시 뒤 다시 해 주세요", "Could not send. Please try again later", "送信できませんでした。しばらくしてからお試しください", "发送失败，请稍后再试");

        // ================================================================= 전문 (개인정보 처리방침)
        C("telemetry.policy", PolicyKo, PolicyEn, PolicyJa, PolicyZh);
    }

    const string PolicyKo = @"<b>Soul Saver 플레이 데이터 수집 · 이용 안내 (개인정보 처리방침)</b>
문서 버전 1 · 시행일 2026-10-20

Soul Saver 개발자(이하 「개발자」)는 게임 밸런스를 다듬기 위해, 동의한 이용자에 한해 판이 끝날 때마다 플레이 기록 한 줄을 익명으로 모읍니다.

<b>1. 수집하는 항목</b>
게임 버전, 정식판 · 체험판 구분, 플랫폼(운영체제 종류), 게임 언어, 영웅, 난이도, 모드, 판 결과, 플레이 시간, 레벨, 처치 수, 고른 카드, 무기, 스킬, 갈래, 진화, 장별 시간, 피해 통계, 쓰러진 원인, 영혼 조각, 필살기, 도달한 장, 영혼 무게 · 각인 · 유물, 스킬 진화, 갈래 단계 · 교차, 계약 · 업적 진행, 재도전 방식, 판 사이 메뉴에 머문 시간, 약점 적중 통계, 그 밖의 게임 진행 수치(점수, 전령 · 기억 이벤트 발생 여부 등), 동의문 버전, 날짜(일 단위), 무작위로 만든 익명 ID.
이름, SteamID, 이메일, IP 주소는 저장하지 않습니다. 전송 과정에서 통신을 위해 IP 주소가 일시적으로 처리되지만 저장하거나 기록하지 않습니다.

<b>2. 이용 목적</b>
게임 밸런스 조정, 버그 수정, 성능 개선에만 씁니다. 광고, 판매, 프로파일링에 쓰지 않으며, 제3자에게 판매하거나 제공하지 않습니다.

<b>3. 처리의 법적 근거</b>
이용자의 동의 (GDPR 제6조 제1항 a호).

<b>4. 보유 기간</b>
수집일로부터 2년이 지나면 자동으로 삭제합니다. 삭제를 요청하거나 서비스를 종료하면 지체 없이 삭제합니다.

<b>5. 처리 위탁 및 국외 이전</b>
· 이전받는 자: Cloudflare, Inc. (미국, privacyquestions@cloudflare.com)
· 이전 국가: 미국 등 Cloudflare 데이터센터가 있는 국가
· 이전 일시 및 방법: 판이 끝날 때마다 인터넷(HTTPS 암호화)으로 전송
· 이전 항목: 1번의 항목
· 이용 목적: 데이터 저장 및 호스팅
· 보유 기간: 4번과 같음
· 거부 방법 및 결과: 「동의하지 않음」을 고르거나 설정에서 끄면 이전되지 않습니다. 거부해도 게임 이용에 불이익이 없습니다.
유럽 경제 지역(EEA) 등에서의 국외 이전은 Cloudflare 의 표준계약조항(SCC)과 EU-US Data Privacy Framework 에 따릅니다.

<b>6. 이용자의 권리</b>
· 동의를 거부할 수 있고, 동의한 뒤에도 언제든 설정 > 일반에서 철회할 수 있습니다.
· 설정 > 일반의 「내 데이터 삭제 요청」으로 보낸 기록 전부의 삭제를 요청할 수 있습니다.
· 열람, 정정, 처리 정지는 아래 연락처로 요청할 수 있습니다.
· 한국 개인정보보호위원회에 민원을 낼 수 있으며, EU 거주자는 거주국 감독기관에 민원을 낼 수 있습니다.
기록은 익명 ID 로만 구분되므로, 요청할 때는 설정 > 일반에 표시된 익명 ID 를 함께 알려 주세요.

<b>7. 아동</b>
만 14세 미만(미국은 만 13세 미만)은 보호자의 동의 없이 동의하지 말고 「동의하지 않음」을 골라 주세요.

<b>8. 안전성 확보 조치</b>
모든 전송은 HTTPS 로 암호화하며, 저장된 데이터에는 개발자만 접근할 수 있습니다.

<b>9. 개인정보 처리자 및 문의</b>
Soul Saver 개발자 · 연락처: {contact}

<b>10. 변경</b>
이 안내가 바뀌면 게임 안에서 다시 동의를 받습니다.
미국 캘리포니아 거주자(CCPA): 개발자는 개인정보를 판매하거나 공유하지 않습니다.";

    const string PolicyEn = @"<b>Soul Saver Play Data Notice (Privacy Policy)</b>
Document version 1 · Effective 2026-10-20

To fine-tune game balance, the Soul Saver developer (the 'Developer') collects one anonymous play record at the end of each run, only from players who have agreed.

<b>1. What we collect</b>
Game version, full or demo build, platform (operating system type), game language, hero, difficulty, mode, run result, play time, level, kill count, cards picked, weapons, skills, paths, evolutions, time per chapter, damage statistics, cause of defeat, soul shards, ultimate, chapter reached, soul weight, brands and relics, skill evolutions, path tiers and crossovers, contract and milestone progress, retry type, time spent in menus between runs, weak-point hit statistics, other gameplay counters (score, whether herald or memory events occurred, etc.), consent document version, date (day only), and a randomly generated anonymous ID.
We do not store your name, SteamID, email address or IP address. Your IP address is processed briefly during transmission so the connection can work, but it is never stored or logged.

<b>2. Why we use it</b>
Only for game balancing, bug fixing and performance improvement. It is not used for advertising, sale or profiling, and it is never sold or provided to third parties.

<b>3. Legal basis</b>
Your consent (GDPR Article 6(1)(a)).

<b>4. Retention</b>
Records are deleted automatically 2 years after collection, or without delay when you request deletion or when the service ends.

<b>5. Processors and international transfer</b>
· Recipient: Cloudflare, Inc. (United States, privacyquestions@cloudflare.com)
· Countries: the United States and other countries where Cloudflare data centers are located
· When and how: sent over the internet (HTTPS encrypted) at the end of each run
· Items transferred: the items in section 1
· Purpose: data storage and hosting
· Retention: as in section 4
· How to refuse and consequences: choose 'Decline' or turn it off in Settings and nothing is transferred. Refusing has no effect on your use of the game.
Transfers from the European Economic Area and similar regions rely on Cloudflare's Standard Contractual Clauses (SCCs) and the EU-US Data Privacy Framework.

<b>6. Your rights</b>
· You may decline, and you may withdraw consent at any time in Settings > General.
· You may request deletion of all records you have sent with 'Request Deletion of My Data' in Settings > General.
· You may request access, correction or restriction of processing via the contact below.
· You may lodge a complaint with the Personal Information Protection Commission of Korea, or, if you live in the EU, with the supervisory authority of your country.
Records are identified only by the anonymous ID, so please include the anonymous ID shown in Settings > General with any request.

<b>7. Children</b>
If you are under 14 (under 13 in the United States), do not agree without a parent or guardian's consent; please choose 'Decline'.

<b>8. Security</b>
All transmissions are encrypted with HTTPS, and only the Developer can access the stored data.

<b>9. Controller and contact</b>
The Soul Saver developer · Contact: {contact}

<b>10. Changes</b>
If this notice changes, we will ask for your consent again in the game.
California residents (CCPA): the Developer does not sell or share personal information.";

    const string PolicyJa = @"<b>Soul Saver プレイデータの収集・利用について(プライバシーポリシー)</b>
文書バージョン1・施行日 2026-10-20

Soul Saver の開発者(以下「開発者」)は、ゲームバランスを調整するため、同意したユーザーに限り、各プレイの終了時にプレイ記録を1件、匿名で収集します。

<b>1. 収集する項目</b>
ゲームのバージョン、製品版・体験版の区別、プラットフォーム(OSの種類)、ゲームの言語、英雄、難易度、モード、プレイ結果、プレイ時間、レベル、撃破数、選んだカード、武器、スキル、系統、進化、章ごとの時間、ダメージ統計、倒れた原因、魂のかけら、必殺技、到達した章、魂の重み・刻印・遺物、スキル進化、系統の段階・交差、契約・実績の進行、リトライの方法、プレイの合間にメニューにいた時間、弱点ヒットの統計、その他のゲーム進行の数値(スコア、使者・記憶イベントの発生有無など)、同意文書のバージョン、日付(日単位)、ランダムに生成した匿名ID。
名前、SteamID、メールアドレス、IPアドレスは保存しません。送信の際、通信のためにIPアドレスが一時的に処理されますが、保存・記録はしません。

<b>2. 利用目的</b>
ゲームバランスの調整、バグ修正、パフォーマンス改善にのみ使います。広告、販売、プロファイリングには使わず、第三者への販売・提供も行いません。

<b>3. 処理の法的根拠</b>
ユーザーの同意(GDPR 第6条第1項(a))。

<b>4. 保存期間</b>
収集日から2年が経過すると自動的に削除します。削除の依頼があった場合やサービス終了時には、遅滞なく削除します。

<b>5. 処理の委託および国外移転</b>
・移転先: Cloudflare, Inc.(米国、privacyquestions@cloudflare.com)
・移転先の国: 米国など Cloudflare のデータセンターがある国
・移転の時期および方法: 各プレイの終了時にインターネット(HTTPS暗号化)で送信
・移転する項目: 第1項の項目
・利用目的: データの保存およびホスティング
・保存期間: 第4項と同じ
・拒否の方法および結果: 「同意しない」を選ぶか、設定でオフにすると移転されません。拒否してもゲームの利用に不利益はありません。
欧州経済領域(EEA)などからの国外移転は、Cloudflare の標準契約条項(SCC)および EU-US Data Privacy Framework に基づきます。

<b>6. ユーザーの権利</b>
・同意を拒否でき、同意した後でも設定 > 一般でいつでも撤回できます。
・設定 > 一般の「自分のデータの削除を依頼」で、送信したすべての記録の削除を依頼できます。
・閲覧、訂正、処理の停止は下記の連絡先で依頼できます。
・韓国の個人情報保護委員会に苦情を申し立てることができ、EU在住の方は居住国の監督機関に苦情を申し立てることができます。
記録は匿名IDでのみ区別されるため、ご依頼の際は設定 > 一般に表示される匿名IDもお知らせください。

<b>7. 子ども</b>
14歳未満(米国では13歳未満)の方は、保護者の同意なしに同意せず、「同意しない」を選んでください。

<b>8. 安全管理措置</b>
すべての送信は HTTPS で暗号化され、保存されたデータには開発者のみがアクセスできます。

<b>9. 個人情報の管理者およびお問い合わせ</b>
Soul Saver 開発者・連絡先: {contact}

<b>10. 変更</b>
この案内が変更された場合は、ゲーム内で改めて同意をお願いします。
米国カリフォルニア州の居住者(CCPA): 開発者は個人情報を販売・共有しません。";

    const string PolicyZh = @"<b>Soul Saver 游戏数据收集与使用说明（隐私政策）</b>
文档版本 1 · 生效日期 2026-10-20

为调整游戏平衡，Soul Saver 开发者（以下简称「开发者」）仅在玩家同意的情况下，于每局结束时匿名收集一条游戏记录。

<b>1. 收集的项目</b>
游戏版本、正式版或试玩版、平台（操作系统类型）、游戏语言、英雄、难度、模式、对局结果、游戏时长、等级、击杀数、选择的卡牌、武器、技能、流派、进化、各章节用时、伤害统计、倒下原因、灵魂碎片、必杀技、到达的章节、灵魂之重、烙印、遗物、技能进化、流派阶段与交叉、契约与成就进度、重试方式、两局之间停留在菜单的时间、弱点命中统计、其他游戏进度数值(分数、是否触发使者或记忆事件等)、同意书版本、日期（精确到日）、随机生成的匿名 ID。
不保存姓名、SteamID、电子邮箱或 IP 地址。传输过程中为完成通信会临时处理 IP 地址，但不会保存或记录。

<b>2. 使用目的</b>
仅用于游戏平衡调整、错误修复和性能改进。不用于广告、销售或用户画像，也不会出售或提供给第三方。

<b>3. 处理的法律依据</b>
玩家的同意（GDPR 第 6 条第 1 款 (a) 项）。

<b>4. 保存期限</b>
自收集之日起满 2 年后自动删除。玩家申请删除或服务终止时，将立即删除。

<b>5. 委托处理及跨境传输</b>
· 接收方：Cloudflare, Inc.（美国，privacyquestions@cloudflare.com）
· 传输国家：美国等 Cloudflare 数据中心所在国家
· 传输时间及方式：每局结束时通过互联网（HTTPS 加密）发送
· 传输项目：第 1 条所列项目
· 使用目的：数据存储与托管
· 保存期限：同第 4 条
· 拒绝方式及后果：选择「不同意」或在设置中关闭即不会传输。拒绝不会影响游戏的使用。
自欧洲经济区（EEA）等地区的跨境传输，依据 Cloudflare 的标准合同条款（SCC）及 EU-US Data Privacy Framework。

<b>6. 玩家的权利</b>
· 可以拒绝同意，同意后也可随时在 设置 > 常规 中撤回。
· 可通过 设置 > 常规 中的「申请删除我的数据」申请删除已发送的全部记录。
· 可通过下方联系方式申请查阅、更正或停止处理。
· 可向韩国个人信息保护委员会投诉；欧盟居民可向所在国监管机构投诉。
记录仅以匿名 ID 区分，提出申请时请一并提供 设置 > 常规 中显示的匿名 ID。

<b>7. 儿童</b>
未满 14 周岁（美国为未满 13 周岁）者，未经监护人同意请勿同意，请选择「不同意」。

<b>8. 安全保障措施</b>
所有传输均通过 HTTPS 加密，仅开发者可访问已保存的数据。

<b>9. 个人信息处理者及联系方式</b>
Soul Saver 开发者 · 联系方式：{contact}

<b>10. 变更</b>
本说明如有变更，将在游戏内重新征求同意。
美国加利福尼亚州居民（CCPA）：开发者不会出售或共享个人信息。";
}
