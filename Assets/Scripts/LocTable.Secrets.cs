using UnityEngine;

// 번역표 (2.0.0): 사막 비밀 — 표지판 문구 · 비문 · 영웅 대사 · 알림 · 결과 화면 한 줄 · 도감 수수께끼 · 전령 · 로딩 팁 (DesertSecrets)
// 이미 있는 키는 건드리지 않음
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddDesertSecretLines()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 도감 「사막에서 본 것」 · 수수께끼
        K("codex.sec.desert", "사막에서 본 것", "Seen in the Desert", "砂漠で見たもの", "沙漠所见");
        K("secret.riddle.wrestler", "밀면 밀린다. 다만 한 번에 밀지 마라.", "Push, and it is pushed. Just don't push it all in one go.", "押せば押される。ただし、一度に押すな。", "推它就会被推动。但别一口气推到底。");
        K("secret.riddle.foreman", "끝나지 않는 공사 끝에는, 대답하는 표지판이 있다.", "At the end of the never-ending construction, there is a sign that answers back.", "終わらない工事の果てに、答えを返す看板がある。", "在没有尽头的施工尽头，有一块会回答的牌子。");
        K("secret.riddle.puppeteer", "보지 않을 때만 다가오는 것.", "Something that only approaches when you are not looking.", "見ていないときだけ近づいてくるもの。", "只在你不看的时候靠近的东西。");
        K("secret.riddle.undertaker", "사막에 네 이름이 새겨진 돌이 있다. 거기서 끝나면, 시작된다.", "A stone with your name carved on it stands in the desert. End there, and it begins.", "砂漠に、あなたの名前が刻まれた石がある。そこで終われば、始まる。", "沙漠里有一块刻着你名字的石头。在那里结束，就是开始。");

        // ================================================================= 공사 반장: 표지판 (때릴 때마다 글자가 바뀜)
        K("secret.sign.0", "공사 중", "Under construction", "工事中", "施工中");
        K("secret.sign.1", "추후 업데이트 예정", "Coming in a future update", "近日アップデート予定", "后续更新预定");
        K("secret.sign.2", "곧이요", "Soon", "もうすぐです", "快了");
        K("secret.sign.3", "진짜 곧이요", "Really soon", "本当にもうすぐです", "真的快了");
        K("secret.sign.4", "다음 패치 때요", "Next patch", "次のパッチです", "下个补丁");
        K("secret.sign.5", "때리지 마세요", "Please don't hit", "叩かないでください", "请别打");
        K("secret.sign.6", "아 진짜…", "Oh, come on...", "あぁもう…", "哎，真是的…");
        K("secret.sign.done", "공사 완료?", "Construction done?", "工事完了？", "施工完成？");
        K("secret.sign.again", "또요?", "Again?", "またですか？", "又来？");
        K("secret.sign.beta", "버그 제보 감사합니다", "Thanks for the bug report", "バグ報告ありがとうございます", "感谢反馈漏洞");

        // ================================================================= 영웅 대사 (말풍선 1.5초)
        K("secret.say.wrestler", "잠깐! 그건 내 소요!", "Wait! That's my bull!", "ちょっと待った！それは俺の牛だ！", "等等！那是我的牛！");
        K("secret.say.foreman.first", "언제요?", "When?", "いつですか？", "什么时候？");
        K("secret.say.foreman", "아 지금 하잖아요!", "I'm doing it right now!", "今やってるじゃないですか！", "我这不是正在做吗！");
        K("secret.say.puppeteer", "드디어 움직였네.", "Finally, you moved.", "やっと動いたね。", "总算动起来了。");
        K("secret.say.undertaker", "또 왔네. 이번엔 자네가 들어 봐.", "You again. This time, you're the one going in.", "また来たな。今度はお前が入ってみろ。", "又来了。这回轮到你躺进去了。");
        K("secret.say.undertaker.again", "또 왔어? 이번엔 꽃이라도 들고 오지.", "You again? At least bring flowers this time.", "また来たのか？せめて花くらい持ってこい。", "又来了？这回好歹带朵花来。");
        K("secret.say.undertaker.self", "동업자끼리 이러기야?", "Is this how business partners treat each other?", "同業者同士でこれか？", "同行之间就这样？");

        // ================================================================= 비문 (이름 = 스팀 이름 · 없으면 여행자)
        A("여행자", "Traveler", "旅人", "旅人");
        K("secret.grave.text", "여기 {0} 잠들다.\n벌써 {1}번째.", "Here lies {0}.\nAlready the {1}th time.", "ここに{0}眠る。\nもう{1}回目。", "{0}长眠于此。\n已经是第{1}次了。");
        K("secret.grave.empty", "여기 {0} 잠들 자리.", "A place for {0} to rest.", "{0}が眠る場所。", "{0}安息之地。");
        K("secret.grave.today", "여기 {0} 잠들다.", "Here lies {0}.", "ここに{0}眠る。", "{0}长眠于此。");
        K("secret.grave.daily", "오늘의 도전자", "today's challenger", "今日の挑戦者", "今日挑战者");

        // ================================================================= 알림 · 결과 화면 한 줄
        K("secret.toast.daily", "사막의 비밀 발견! 순위 보너스 +{0}", "Desert secret found! Ranking bonus +{0}", "砂漠の秘密を発見！ランキングボーナス +{0}", "发现沙漠秘密！排名加分 +{0}");
        K("secret.toast.again", "사막의 비밀 · 영혼 조각 +{0}", "Desert secret · Soul Shards +{0}", "砂漠の秘密・魂の欠片 +{0}", "沙漠秘密 · 灵魂碎片 +{0}");
        K("secret.toast.skin", "전설 스킨 「{0}」 해금!", "Legendary skin \"{0}\" unlocked!", "伝説スキン「{0}」解放！", "传说皮肤「{0}」已解锁！");
        K("secret.result.new", "사막의 비밀을 풀었습니다 — 새 영웅: {0}", "You solved a desert secret — new hero: {0}", "砂漠の秘密を解きました — 新しい英雄：{0}", "你解开了沙漠的秘密 — 新英雄：{0}");
        K("secret.result.again", "사막의 비밀을 다시 풀었습니다 · {0} (영혼 조각 보상)", "Desert secret solved again · {0} (Soul Shard reward)", "砂漠の秘密をもう一度解きました · {0}（魂の欠片報酬）", "再次解开沙漠秘密 · {0}（灵魂碎片奖励）");
        K("secret.result.skin", "전설 스킨 「{0}」 해금!", "Legendary skin \"{0}\" unlocked!", "伝説スキン「{0}」解放！", "传说皮肤「{0}」已解锁！");
        K("secret.result.daily", "사막의 비밀 발견! 순위 보너스 +{0}", "Desert secret found! Ranking bonus +{0}", "砂漠の秘密を発見！ランキングボーナス +{0}", "发现沙漠秘密！排名加分 +{0}");

        // ================================================================= 망자의 전령 힌트 (다른 영웅 · 사막 · 아직 잠김)
        K("herald.secret.wrestler", "사막에 소 발자국이 있던가.", "Were there hoofprints in the desert?", "砂漠に牛の足跡なんてあったかな。", "沙漠里有牛的蹄印吗？");
        K("herald.secret.foreman", "사막 동쪽에 주황 고깔이 줄지어 있더군. 누가 공사를 하다 만 모양이다.", "There was a line of orange cones in the east of the desert. Someone seems to have left a job half-done.", "砂漠の東にオレンジのコーンが並んでいた。誰かが工事を途中でやめたらしい。", "沙漠东边有一排橙色锥桶。看来有人施工到一半就走了。");
        K("herald.secret.puppeteer", "사막의 긴 그림자 하나는, 내가 안 볼 때 움직인다더라.", "They say one long shadow in the desert moves when I'm not looking.", "砂漠の長い影のひとつは、見ていないときに動くそうだ。", "听说沙漠里有一道长影，在我不看的时候会动。");
        K("herald.secret.undertaker", "네 묘비는 사막에 세워 두었다.", "Your gravestone has been set up in the desert.", "お前の墓石は砂漠に立ててある。", "你的墓碑已经立在沙漠里了。");
        K("herald.secret.grave", "네 묘비 앞에서 쓰러졌다. 묘지기가 삽을 내밀었다.", "You fell before your own gravestone. The gravekeeper held out a shovel.", "自分の墓石の前で倒れた。墓守がシャベルを差し出した。", "你倒在了自己的墓碑前。守墓人递来了一把铲子。");

        // ================================================================= 로딩 팁 (힌트 한 줄씩)
        A("모래 위의 발자국은 어디론가 이어진다.", "Footprints on the sand lead somewhere.", "砂の上の足跡は、どこかへ続いている。", "沙地上的脚印通向某处。");
        A("사막의 표지판은 대답한다.", "Signs in the desert answer back.", "砂漠の看板は答えを返す。", "沙漠里的牌子会回答。");
        A("사막의 그림자 중에는 주인이 움직이는 것이 있다.", "Some shadows in the desert move with their owners.", "砂漠の影の中には、持ち主が動くものがある。", "沙漠的影子里，有些是主人在动。");
        A("사막에 회색은 하나뿐이다.", "There is only one gray thing in the desert.", "砂漠に灰色はひとつだけだ。", "沙漠里只有一样东西是灰色的。");
        A("빈 삼각대는 북적이는 장면을 기다린다.", "The empty tripod is waiting for a crowded scene.", "空の三脚は、賑やかな場面を待っている。", "空三脚架在等待热闹的场面。");      // 2.0.1~ 사진사

        // ================================================================= 2.0.1~ 어디서 풀리는지 · 다시 풀었을 때 · 전설 스킨 잠금 이유
        K("secret.where", "무한 모드 사막에서만 나타납니다 (일일 도전에도 있지만 해금은 무한 모드에서만)",
          "Appears only in the Endless Mode desert (also in Daily Challenges, but heroes unlock only in Endless Mode)",
          "エンドレスモードの砂漠にだけ現れます（デイリーチャレンジにもありますが、解放はエンドレスモードのみ）",
          "仅在无尽模式的沙漠中出现（每日挑战中也有，但只能在无尽模式中解锁）");
        K("secret.toast.points", "사막의 비밀 · 포인트 +{0}", "Desert secret · Points +{0}", "砂漠の秘密・ポイント +{0}", "沙漠秘密 · 积分 +{0}");
        K("skin.lock.secret.short", "사막 비밀로 해금", "Unlocked via desert secret", "砂漠の秘密で解放", "通过沙漠秘密解锁");
        K("skin.lock.secret", "{0}(으)로 무한 모드 사막에서 그 영웅의 비밀을 다시 풀면 해금됩니다.",
          "Solve this hero's desert secret again in Endless Mode while playing as {0} to unlock.",
          "{0}でエンドレスモードの砂漠にあるこの英雄の秘密をもう一度解くと解放されます。",
          "使用{0}在无尽模式的沙漠中再次解开该英雄的秘密即可解锁。");
    }
}
