// 번역표 (2.1~): 캐릭터 고유 레벨업 스킬 50종 · 스킬 진화 37종 · 진화 화면 · 진화 도감
// LocTable.Entries 에 그대로 더함 (이미 있는 키는 건드리지 않음)
public static partial class LocTable
{
    static LocTable()
    {
        void A(string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(ko)) Entries[ko] = L(en, ja, zh); }
        // 1.1.9 문구 다듬기: 키(코드 속 한국어)는 그대로 두고 화면에 보일 한국어만 바꿈
        void K(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }

        // ================================================================= 화면 · 공통
        A("재료", "Ingredients", "素材", "素材");
        // ---------------- 진화 강화 · 재료 스킬 설명 · 진화 순간 (1.1.2~)
        A("{0} 피해 ×{1}", "{0} damage ×{1}", "{0}のダメージ×{1}", "{0}伤害×{1}");
        A("진화 강화", "Evolution boost", "進化強化", "进化强化");
        A("재료 스킬", "Ingredient skills", "素材スキル", "素材技能");
        A("진화 뒤에도 효과가 그대로 이어집니다.", "Their effects carry on after evolving.", "進化後も効果はそのまま続きます。", "进化后效果依然保留。");
        A("이어짐", "Kept", "継続", "保留");
        A("진화로 강화되어 이어짐", "Kept · boosted by evolution", "進化で強化されて継続", "保留 · 进化强化");
        K("재료 스킬 효과는 그대로 이어집니다", "재료 스킬 효과는 그대로 이어집니다.", "Ingredient skill effects carry on.", "素材スキルの効果はそのまま続きます。", "素材技能效果依然保留。");
        A("진화까지 1단계!", "1 rank to evolve!", "進化まであと1段階！", "距离进化仅差1级！");
        // ---------------- 카드 설명 고침 (2.0.7~): 검사 밸런스 · 숫자 보강 · 진화 무기 필살기에도 적용
        A("적에게 맞은 뒤 1.5초 동안 주는 모든 피해가 늘어납니다.", "For 1.5s after being hit, all damage you deal is increased.", "敵に被弾してから1.5秒間、与えるすべてのダメージが増えます。", "受到敌人攻击后1.5秒内，造成的所有伤害提升。");
        A("휘두른 자리에 1초 동안 칼날 잔상이 남아 닿는 적을 0.25초마다 벱니다.", "Swings leave a blade afterimage for 1s that cuts enemies inside every 0.25s.", "振った場所に1秒間刃の残像が残り、触れた敵を0.25秒ごとに斬ります。", "挥砍处留下1秒的刀刃残影，每0.25秒斩击其中的敌人。");
        K("벤 적 하나당 최대 체력의 0.6 ~ 1.5%만큼 보호막이 쌓여 받는 피해를 먼저 막습니다. (한 번 휘두를 때 최대 3마리, 보호막이 맞은 뒤 1.5초는 쌓이지 않음 · 몸이 붉게 빛남)",
          "벤 적 하나당 최대 체력의 0.6~1.5%만큼 보호막이 쌓여 받는 피해를 먼저 막습니다. (한 번 휘두를 때 최대 3마리, 보호막이 맞은 뒤 1.5초는 쌓이지 않음 · 몸이 붉게 빛남)", "Each enemy you cut builds a shield worth 0.6–1.5% of max health that absorbs damage first. (Up to 3 enemies per swing; stops building for 1.5s after the shield is hit; your body glows red)", "斬った敵1体ごとに最大体力の0.6〜1.5%のシールドがたまり、受けるダメージを先に防ぎます。（1回の振りで最大3体、シールドが攻撃を受けた後1.5秒はたまらない・体が赤く光る）", "每斩中一名敌人，积累相当于最大生命0.6~1.5%的护盾，优先吸收伤害。（每次挥砍最多3名，护盾受击后1.5秒内不再积累 · 身体发出红光）");
        K("필살기를 쓴 뒤 1.5 ~ 4초 동안 휘두르기가 빨라집니다. 오래 모았을수록 더 오래 이어집니다.", "필살기를 쓴 뒤 1.5~4초 동안 휘두르기가 빨라집니다. 오래 모았을수록 더 오래 이어집니다.",
          "After your Ultimate, you swing faster for 1.5–4s. The longer you charged, the longer it lasts.", "必殺技の後1.5〜4秒間、振りが速くなります。長くためるほど長く続きます。", "必杀技后1.5~4秒内挥砍加快。蓄力越久，持续越长。");
        A("필살기를 모으는 동안 범위 안의 적을 초당 2.5칸씩 끌어당깁니다. (보스 제외)", "While charging your Ultimate, pull enemies in range toward you at 2.5 tiles/s. (Excludes bosses)", "必殺技をためている間、範囲内の敵を毎秒2.5マス引き寄せます。（ボス除く）", "蓄力必杀技时，以每秒2.5格的速度拉近范围内的敌人。（首领除外）");
        A("적을 벤 휘두르기마다 1.5초 동안 이동 속도가 오릅니다. (최대 5중첩, 계속 베면 유지)", "Each swing that cuts an enemy raises move speed for 1.5s. (Up to 5 stacks, kept while you keep cutting)", "敵を斬った振りごとに1.5秒間移動速度が上がります。（最大5重、斬り続ければ維持）", "每次斩中敌人的挥砍提升1.5秒移动速度。（最多5层，持续斩击即可保持）");
        A("평타로 같은 적을 세 번 벨 때마다 균열이 터져 그 적에게 추가 피해를 줍니다.", "Every third basic-attack cut on the same enemy shatters it for bonus damage.", "通常攻撃で同じ敵を3回斬るたびに亀裂が弾け、その敵に追加ダメージを与えます。", "普通攻击每斩同一敌人三次，裂痕爆开对其造成额外伤害。");
        A("필살기 0.45초 뒤 범위 85%의 잔향 베기가 한 번 더 일어납니다.", "An echo spin at 85% radius strikes again 0.45s after your Ultimate.", "必殺技の0.45秒後、範囲85%の残響斬りがもう一度起こります。", "必杀技0.45秒后，再发动一次范围85%的余响斩。");
        A("필살기가 쏟아지는 동안 그 안의 적이 받는 피해가 늘어납니다.", "While your Ultimate rains down, enemies inside it take more damage.", "必殺技が降る間、その中の敵が受けるダメージが増えます。", "必杀技落下期间，其中的敌人受到的伤害提升。");
        A("필살기가 끝나면 가운데에 거대한 유성 화살이 떨어집니다.", "When your Ultimate ends, a giant meteor arrow strikes its center.", "必殺技が終わると中央に巨大な流星の矢が落ちます。", "必杀技结束时，一支巨大的流星箭落在中央。");
        K("플라스크로 쓰러뜨린 적 수만큼 다음 필살기의 피해가 증가합니다. (최대 20)", "플라스크로 쓰러뜨린 적 수만큼 다음 필살기의 피해가 늘어납니다. (최대 20)", "Each flask kill strengthens your next Great Blast Flask. (Up to 20)", "フラスコで倒した敵の数だけ次の必殺技が強くなります。（最大20）", "烧瓶每击杀一名敌人，下一次必杀技就更强。（最多20）");
        A("필살기를 모으는 동안 주변 적에게 작은 플라스크가 떨어져 터집니다. (공격력 50%)", "While charging the Great Blast Flask, small flasks rain on nearby enemies. (50% attack)", "必殺技をためている間、周りの敵に小さなフラスコが落ちて爆発します。（攻撃力50%）", "蓄力必杀技时，小烧瓶落向周围敌人并爆炸。（攻击力50%）");
        A("평타로 벤 적 하나당 체력을 회복합니다. (한 번 휘두를 때 최대 3마리까지)", "Heal for each enemy your basic attack cuts. (Up to 3 per swing)", "通常攻撃で斬った敵1体ごとに体力を回復します。（1回の振りで最大3体まで）", "普通攻击每斩中一名敌人恢复生命。（每次挥砍最多3名）");
        A("평타를 휘두르면 베기 범위 안으로 날아오는 적 투사체를 모두 베어 없앱니다.", "Your basic swing cuts down every enemy projectile within its arc.", "通常攻撃を振ると、斬撃範囲に飛んでくる敵の投射物をすべて斬り消します。", "普通攻击挥砍时，斩灭挥砍范围内飞来的所有敌方投射物。");
        A("몇 번 휘두를 때마다 한 번은 사거리 1.4배 · 피해 1.6배의 강한 일격이 됩니다. (3단계: 강한 일격에 주변 충격파)", "Every few swings, one becomes a heavy strike with 1.4× reach and 1.6× damage. (Rank 3: the heavy strike also sends out a shockwave)", "何回か振るごとに1回は射程1.4倍・ダメージ1.6倍の強打になります。（3段階：強打に周囲の衝撃波）", "每挥砍几次，就有一次变为射程1.4倍、伤害1.6倍的强力一击。（3级：强力一击附带周围冲击波）");
        A("필살기 뒤 칼바람이 몸을 감싸고 돌며 주변을 벱니다. (0.3초마다 공격력 35%)", "After your Ultimate, a storm of blades circles you, cutting nearby foes. (35% attack every 0.3s)", "必殺技の後、刃の風が体を包んで回り周りを斬ります。（0.3秒ごとに攻撃力35%）", "必杀技后，刀风环绕身体旋转斩击周围。（每0.3秒攻击力35%）");

        // ---------------- 플레이 튜토리얼 (2.1.9~, TutorialRun · 이미 있는 키는 그대로)
        A("이동", "Move", "移動", "移动");
        A("조준 · 공격", "Aim · Attack", "照準・攻撃", "瞄准 · 攻击");
        A("재장전", "Reload", "リロード", "装填");
        A("필살기", "Ultimate", "必殺技", "必杀技");
        A("레벨업", "Level Up", "レベルアップ", "升级");
        A("영혼 트리", "Soul Tree", "魂のツリー", "灵魂树");
        A("무기 진화", "Weapon Evolution", "武器進化", "武器进化");
        A("마무리", "Finale", "仕上げ", "收尾");
        A("{MOVE}로 이리저리 움직여 보세요.", "Move around with {MOVE}.", "{MOVE}であちこち動いてみましょう。", "用{MOVE}四处移动看看。");
        A("조준하고 [{ATTACK}]로 공격합니다. 누르고 있으면 계속 쏩니다. 다가오는 적을 모두 쓰러뜨리세요.", "Aim and press [{ATTACK}] to attack. Hold it to keep firing. Defeat every approaching enemy.", "狙いを定めて[{ATTACK}]で攻撃します。押し続けると撃ち続けます。近づく敵をすべて倒しましょう。", "瞄准后按[{ATTACK}]攻击。按住即可连续射击。击败所有靠近的敌人。");     // 1.1~ 좌클릭 고정 → 지금 키 · 패드 버튼
        A("총알은 탄창만큼만 쏠 수 있습니다. [{RELOAD}]를 눌러 재장전하세요. (다 쏘면 저절로 재장전)", "You can only fire as many shots as your magazine holds. Press [{RELOAD}] to reload. (It reloads automatically when empty)", "弾は弾倉の分しか撃てません。[{RELOAD}]でリロードしましょう。（撃ち切ると自動でリロード）", "子弹只能打完一个弹匣。按[{RELOAD}]装填。（打空后会自动装填）");
        A("필살기 게이지가 가득 찼습니다! [{ULT}]를 누르고 있으면 시간이 느려지며 적을 조준하고, 떼면 조준한 적 모두에게 쏩니다.", "Your Ultimate gauge is full! Hold [{ULT}] to slow time and lock onto enemies, then release to fire at every target.", "必殺技ゲージが満タンになりました！[{ULT}]を押し続けると時間が遅くなって敵を狙い、離すと狙った敵すべてに撃ちます。", "必杀技槽已满！按住[{ULT}]时间会变慢并锁定敌人，松开即可向所有锁定的敌人射击。");     // 1.1~ 우클릭 고정 → 지금 키 · 패드 버튼
        A("경험치가 차면 레벨업 포인트가 쌓입니다. [{INTERACT}]를 눌러 창을 열고 카드를 고르세요. (클릭 → [{INTERACT}], 더블클릭, 숫자 1 · 2 · 3)", "When your EXP bar fills, you earn a level-up point. Press [{INTERACT}] to open the window and pick a card. (Click → [{INTERACT}], double-click, or 1 · 2 · 3)", "経験値がたまるとレベルアップポイントがたまります。[{INTERACT}]でウィンドウを開いてカードを選びましょう。（クリック → [{INTERACT}]、ダブルクリック、数字1・2・3）", "经验值满后会累积升级点数。按[{INTERACT}]打开窗口并选择卡牌。（点击 → [{INTERACT}]、双击或数字1 · 2 · 3）");
        A("적이 떨어뜨린 영혼 조각으로 영혼 트리의 칸을 배웁니다. [{UPGRADE}]로 트리를 열고 빛나는 칸을 하나 배운 뒤 닫으세요.", "Spend the Soul Shards enemies drop to learn Soul Tree nodes. Open the tree with [{UPGRADE}], learn one glowing node, then close it.", "敵が落とす魂のかけらで魂のツリーのマスを習得します。[{UPGRADE}]でツリーを開き、光るマスを1つ習得して閉じましょう。", "用敌人掉落的灵魂碎片学习灵魂树的节点。按[{UPGRADE}]打开灵魂树，学习一个发光的节点后关闭。");
        A("보스를 쓰러뜨리면 무기가 진화합니다. 세 갈래 중 하나를 고르세요. (판마다 두 번, 되돌릴 수 없음)", "Defeating a boss evolves your weapon. Choose one of three paths. (Twice per run, and there's no going back)", "ボスを倒すと武器が進化します。3つの道から1つを選びましょう。（1回のプレイで2回、やり直し不可）", "击败首领后武器会进化。从三条路线中选择一条。（每局两次，无法反悔）");
        A("진화한 무기로 남은 적을 모두 쓰러뜨리세요!", "Defeat the remaining enemies with your evolved weapon!", "進化した武器で残りの敵をすべて倒しましょう！", "用进化后的武器击败剩下的敌人！");
        A("튜토리얼 완료!", "Tutorial complete!", "チュートリアル完了！", "教程完成！");
        A("기본 조작을 모두 익혔습니다. 곧 메인 메뉴로 돌아갑니다.", "You've learned all the basics. Returning to the main menu shortly.", "基本操作をすべて覚えました。まもなくメインメニューに戻ります。", "你已掌握所有基本操作。即将返回主菜单。");
        A("완료!", "Done!", "完了！", "完成！");
        A("튜토리얼 그만두기", "Quit Tutorial", "チュートリアルをやめる", "退出教程");
        A("튜토리얼", "Tutorial", "チュートリアル", "教程");

        // ---------------- 설정 (2.2.0~)
        A("UI 크기", "UI Size", "UIサイズ", "界面大小");
        A("창이 비활성일 때 소리 끄기", "Mute When Window Is Inactive", "ウィンドウが非アクティブ時に消音", "窗口未激活时静音");

        // ---------------- 도감 (2.1~)
        K("각 스테이지의 마지막 적 · 체력이 절반 아래면 특수 스킬, 필살기 게이지가 차면 결계를 펼칩니다", "각 장의 마지막 적입니다. 체력이 절반 아래면 특수 스킬을 쓰고, 필살기 게이지가 차면 결계를 펼칩니다.",
          "The final foe of each chapter. Below half health, it uses special skills, and when its Ultimate gauge fills, it raises a barrier.", "各章の最後の敵です。体力が半分以下になると特殊スキルを使い、必殺技ゲージが満タンになると結界を展開します。", "每章的最终敌人。生命低于一半时施展特殊技能，必杀技槽满时展开结界。");
        A("레벨이 오를 때 세 장 중 하나를 고르거나 건너뜁니다 · 최대 레벨 50 · 재료를 모두 올리면 스킬 진화", "Pick one of three cards or skip on each level-up · Max level 50 · Max all ingredients to evolve a skill", "レベルアップごとに3枚から1枚を選ぶかスキップ・最大レベル50・素材をすべて上げるとスキル進化", "每次升级从三张中选一张或跳过 · 等级上限50 · 素材全部升满即可技能进化");
        K("필살기 「망자의 묘역」 — 15초 동안 회전하는 영혼 광선 넷 · 틈이 있는 저주 고리 · 발밑에서 솟는 뼈 가시", "필살기 「망자의 묘역」 — 15초 동안 회전하는 영혼 광선 넷 · 틈이 있는 저주 고리 · 발밑에서 솟는 뼈 가시가 몰아칩니다.",
          "Ultimate \"Graveyard of the Dead\" — for 15s, four sweeping soul beams, cursed rings with a gap, and bone spikes from below assail you.", "必殺技「亡者の墓域」— 15秒間、回転する魂の光線4本・隙間のある呪いの輪・足元から突き出す骨の棘が襲いかかります。", "必杀技「亡者墓域」— 15秒内，四道旋转灵魂光束 · 带缺口的诅咒之环 · 脚下升起的骨刺接连袭来。");
        K("필살기 「연옥 낙화」 — 15초 동안 곳곳의 운석 비 · 내 자리의 거대 운석 · 틈이 있는 운석 행진을 번갈아", "필살기 「연옥 낙화」 — 15초 동안 곳곳의 운석 비 · 내 자리의 거대 운석 · 틈이 있는 운석 행진이 번갈아 쏟아집니다.",
          "Ultimate \"Purgatory's Fall\" — for 15s, a meteor shower, a giant meteor at your feet, and a marching meteor line with a gap rain down in turn.", "必殺技「煉獄落火」— 15秒間、隕石の雨・足元の巨大隕石・隙間のある隕石の行進が交互に降り注ぎます。", "必杀技「炼狱落火」— 15秒内，陨石雨 · 脚下的巨型陨石 · 留有缺口的陨石行进交替落下。");
        K("필살기 「산성 범람」 — 9초 동안 결계 벽에 튕기는 산성 덩어리 · 쏟아지는 산성 비", "필살기 「산성 범람」 — 9초 동안 결계 벽에 튕기는 산성 덩어리와 쏟아지는 산성 비가 덮칩니다.",
          "Ultimate \"Acid Deluge\" — for 9s, acid blobs bounce off the barrier walls and acid rain pours down.", "必殺技「酸の氾濫」— 9秒間、結界の壁で跳ね返る酸の塊と降り注ぐ酸の雨が襲いかかります。", "必杀技「酸性泛滥」— 9秒内，酸液团在结界壁上反弹，酸雨倾泻而下。");
        A("모든 보스의 필살기", "Every Boss's Ultimate", "すべてのボスの必殺技", "所有首领的必杀技");
        A("보스를 때릴 때마다 보스 체력바 아래 보라색 게이지가 찹니다. 약한 공격 여러 번보다 센 한 방이 훨씬 많이 채우고, 한동안 때리지 않으면 아주 서서히 줄어듭니다. 가득 차면 맵 전체가 어두워지며 보스 둘레에 거대한 결계가 펼쳐지고, 15초 동안 보스마다 다른 공격이 쏟아집니다. 공격 자리마다 느낌표가 먼저 뜹니다.",
          "Every hit on a boss fills the purple gauge under its health bar — one heavy blow fills far more than many weak ones, and it drains very slowly if you stop hitting. When full, the whole map darkens and a huge barrier rises around the boss, raining boss-specific attacks for 15s. An exclamation mark warns of every strike.",
          "ボスを攻撃するたびに体力バーの下の紫ゲージがたまります。弱い攻撃を何度も当てるより強い一撃のほうがずっと多くたまり、しばらく攻撃しないとごくゆっくり減ります。満タンになるとマップ全体が暗くなってボスの周りに巨大な結界が広がり、15秒間ボスごとに異なる攻撃が降り注ぎます。攻撃の場所には先に「！」マークが出ます。",
          "每次攻击首领，血条下方的紫色槽就会积累。一次重击比多次弱攻击积累得多得多，一段时间不攻击会极其缓慢地下降。槽满时整张地图变暗，首领周围展开巨大结界，15秒内倾泻首领专属攻击。每次攻击位置都会先出现感叹号。");
        A("꾹 눌러 조준", "Hold to Aim", "長押しで狙う", "长按瞄准");
        A("종", "", "種", "种");
        K("정해진 두세 장의 레벨업 카드를 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다. 최대 레벨은 50이라 모두 올릴 수 없으니 노릴 진화를 고르세요. 원하지 않는 카드는 건너뛸 수 있고, 레벨은 카드를 골라야 오릅니다. 조합은 메인 메뉴의 「스킬 진화」에서 볼 수 있습니다.",
          "정해진 두세 장의 레벨업 카드를 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다. 최대 레벨은 50이라 모두 올릴 수 없으니 노릴 진화를 고르세요. 원하지 않는 카드는 건너뛸 수 있고, 레벨은 카드를 골라야 오릅니다. 조합은 메인 메뉴의 「스킬 진화」에서 볼 수 있습니다.",
          "Max out two or three specific level-up cards and they fuse into one with a new power. Levels cap at 50 (80 in Endless), so pick which evolutions to chase. You can skip unwanted cards, and you only level up when you take a card. See every recipe under Skill Evolution on the main menu.",
          "決まった2〜3枚のレベルアップカードをすべて最大段階にすると一つに合わさり、新しい能力が生まれます。最大レベルは50なので全部は上げられません。狙う進化を選びましょう。不要なカードはスキップでき、レベルはカードを選んだときだけ上がります。組み合わせはメインメニューの「スキル進化」で見られます。",
          "将指定的两到三张升级卡全部升到满级后会合而为一，获得新能力。等级上限为50，无法全部升满，请选择要追求的进化。不想要的卡牌可以跳过，只有选择卡牌时才会升级。组合可在主菜单的「技能进化」中查看。");
        K("무적 상태로 마우스 방향으로 돌진해, 지나간 적에게 출혈 피해를 입힙니다 (누르고 있으면 경로가 보이고, 떼면 돌진). 영혼 트리에서 그림자 숙련을 배우면 돌진에 이동 속도 · 둔화 · 연속 돌진 등이 붙습니다",
          "무적 상태로 마우스 쪽으로 돌진해 지나간 적에게 출혈 피해를 입힙니다. 누르고 있으면 경로가 보이고, 떼면 돌진합니다. 영혼 트리에서 그림자 숙련을 배우면 돌진에 이동 속도 · 둔화 · 연속 돌진 등이 붙습니다.",
          "Dash toward the cursor while invincible, making every enemy you pass bleed. Hold to see the path, release to dash. Learning Shadow Mastery in the Soul Tree adds speed, slows, chained dashes and more to the dash.",
          "無敵状態でマウスの方へ突進し、通り過ぎた敵に出血ダメージを与えます。押している間は経路が見え、離すと突進します。魂のツリーで影の熟練を覚えると、突進に移動速度・鈍化・連続突進などが付きます。",
          "在无敌状态下朝鼠标方向突进，使途经的敌人流血。按住可看到路径，松开即突进。在灵魂树学习暗影精通后，突进会附带移速 · 减速 · 连续突进等效果。");
        A("진화 재료", "Evolution ingredient", "進化の素材", "进化素材");
        A("결계 침식", "Barrier Erosion", "結界侵食", "结界侵蚀");
        A("망자의 묘역", "Graveyard of the Dead", "亡者の墓域", "亡者墓域");
        A("이 안에서는, 죽은 자만이 걷는다.", "In here, only the dead may walk.", "この中を歩けるのは、死者だけだ。", "在这里，只有死者能行走。");
        A("연옥 낙화", "Purgatory's Fall", "煉獄落火", "炼狱落火");
        A("하늘이 무너져 내린다.", "The sky itself falls.", "天が、崩れ落ちる。", "天穹，崩落吧。");
        A("산성 범람", "Acid Deluge", "酸の氾濫", "酸性泛滥");
        A("전부, 녹아내려라.", "Melt. All of it.", "すべて、溶け落ちろ。", "全部，融化吧。");
        A("정말로 메인 메뉴로 가시겠습니까?", "Really return to the main menu?", "本当にメインメニューに戻りますか？", "确定要返回主菜单吗？");
        A("지금 진행 중인 판이 끝납니다.", "Your current run will end.", "今のプレイは終了します。", "当前这一局将会结束。");
        A("스킬 진화", "Skill Evolution", "スキル進化", "技能进化");
        A("스킬 진화 도감", "Skill Evolution Codex", "スキル進化図鑑", "技能进化图鉴");
        A("클릭하거나 [", "Click or press [", "クリックするか [", "点击或按 [");
        K("] 를 눌러 계속", "]를 눌러 계속", "] to continue", "] で続ける", "] 继续");
        A("빗나감", "Miss", "ミス", "落空");
        A("이번 레벨업에서는 아무것도 배우지 않습니다. 원하는 스킬 · 진화를 노릴 때 쓰세요.",
          "Learn nothing this level-up. Use it to keep your build on the skills and evolutions you want.",
          "このレベルアップでは何も覚えません。狙いのスキル・進化に絞るときに使います。",
          "本次升级不学习任何能力。想专注于目标技能与进化时使用。");
        A("필살기의 ", "Ultimate ", "必殺技の", "必杀技的");
        A("남은 출혈의 ", "Remaining bleed ", "残り出血の", "剩余流血的");
        A("필살기 충전", "Ultimate charge", "必殺技チャージ", "必杀技充能");
        A("호문쿨루스 필요", "Requires Homunculus", "ホムンクルスが必要", "需要侏儒助手");
        A("필요", "Requires", "必要", "需要");
        A("진화 조합", "Evolution recipes", "進化の組み合わせ", "进化组合");
        K("레벨업 카드 두세 장을 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다. 레벨은 50까지라 모두 올릴 수는 없습니다.",
          "레벨업 카드 두세 장을 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다. 레벨은 50까지라 모두 올릴 수는 없습니다.",
          "Max out two or three level-up cards and they merge into one, gaining a new power. Levels cap at 50 (80 in Endless), so you can't max everything.",
          "レベルアップカード2〜3枚をすべて最大段階にすると一つに合わさり、新しい能力が生まれます。レベルは50までなので全部は上げられません。",
          "将两到三张升级卡全部升到最高级后会合而为一，获得新能力。等级上限为50，无法全部升满。");
        K("모든 캐릭터", "모든 영웅", "All Heroes", "すべての英雄", "所有英雄");
        A("잠긴 캐릭터", "Locked hero", "ロック中のキャラクター", "未解锁角色");
        K("포인트로 잠금을 풀면 이 캐릭터의 진화 조합을 볼 수 있습니다.", "포인트로 잠금을 풀면 이 영웅의 진화 조합을 볼 수 있습니다.", "Unlock this hero with points to see their evolution recipes.", "ポイントでロックを解除すると、この英雄の進化の組み合わせが見られます。", "用点数解锁该英雄后即可查看其进化组合。");

        // ================================================================= 거너
        A("탄피 지뢰", "Shell Mines", "薬莢地雷", "弹壳地雷");
        A("장전을 시작하면 발밑에 탄피 지뢰를 흩뿌립니다. 적이 밟으면 터집니다. (공격력 80%)", "Starting a reload scatters shell-casing mines at your feet. They blow up when stepped on. (80% attack)", "リロードを始めると足元に薬莢地雷をばらまきます。敵が踏むと爆発します。（攻撃力80%）", "开始装填时在脚下撒出弹壳地雷，敌人踩到就会爆炸。（攻击力80%）");
        A("지뢰 수", "Mines", "地雷の数", "地雷数量");
        A("처치 파편", "Kill Shrapnel", "撃破破片", "击杀破片");
        A("적을 쓰러뜨리면 그 자리에서 파편이 사방으로 튑니다. (공격력 30%씩)", "Kills burst into shrapnel in every direction. (30% attack each)", "敵を倒すとその場で破片が四方に飛び散ります。（攻撃力30%ずつ）", "击杀敌人时在原地向四周迸出破片。（每片攻击力30%）");
        A("파편 수", "Shards", "破片の数", "破片数量");
        A("총열 과열", "Barrel Heat", "銃身過熱", "枪管过热");
        K("쉬지 않고 쏠수록 총열이 달아올라 총알의 피해가 증가합니다. 1.5초 쉬면 식습니다.", "쉬지 않고 쏠수록 총열이 달아올라 총알의 피해가 늘어납니다. 1.5초 쉬면 식습니다.","The longer you keep firing, the hotter the barrel and the harder your bullets hit. It cools after 1.5s of rest.", "撃ち続けるほど銃身が熱くなり、弾のダメージが増えます。1.5秒休むと冷めます。", "持续射击会让枪管升温，子弹伤害提升。停火1.5秒后冷却。");
        A("최대 피해", "Max damage", "最大ダメージ", "最大伤害");
        A("영혼 탄환", "Soul Rounds", "魂の弾丸", "灵魂弹");
        A("필살기 게이지가 가득 찬 동안과 필살기로 쏜 총알이 영혼탄이 되어 더 아프고 적을 하나 더 꿰뚫습니다. (총알이 아닌 필살기는 피해만 증가)", "While your Ultimate gauge is full, and for shots fired by your Ultimate, bullets become soul rounds that hit harder and pierce one more enemy. (Non-bullet Ultimates just deal more damage)", "必殺技ゲージが満タンの間と必殺技で撃った弾が魂弾になり、ダメージが上がって敵をもう1体貫きます。（弾でない必殺技はダメージのみ増加）", "必杀技槽满时以及必杀技射出的子弹会变为灵魂弹，伤害更高并多穿透一名敌人。（非子弹类必杀技仅提升伤害）");
        A("영혼탄 피해", "Soul round damage", "魂弾ダメージ", "灵魂弹伤害");
        A("섬광 장전", "Flash Reload", "閃光リロード", "闪光装填");
        A("장전을 마치는 순간 섬광이 터져 주변 적을 기절시킵니다.", "Finishing a reload sets off a flash that stuns nearby enemies.", "リロードを終えた瞬間に閃光が走り、周りの敵を気絶させます。", "装填完成的瞬间爆出闪光，击晕周围敌人。");
        A("기절 시간", "Stun time", "気絶時間", "眩晕时间");
        A("반격 사격", "Return Fire", "反撃射撃", "反击射击");
        A("맞으면 가까운 적들에게 자동으로 총알을 쏩니다. (공격력 100%씩)", "When hit, you automatically fire at nearby enemies. (100% attack each)", "被弾すると近くの敵に自動で弾を撃ちます。（攻撃力100%ずつ）", "受击时自动向附近敌人开枪。（每发攻击力100%）");
        A("반격 탄 수", "Return shots", "反撃弾数", "反击弹数");
        A("속사 장전", "Quick Draw", "速射リロード", "速射装填");
        A("장전을 마친 직후 몇 발은 쉬지 않고 연달아 나갑니다.", "Right after a reload, your next few shots fire back to back with no delay.", "リロード直後の数発が間を置かず連続で出ます。", "装填完成后的几发子弹可无间隔连射。");
        A("연사 탄 수", "Rapid shots", "連射弾数", "连射弹数");
        A("달리며 장전", "Running Reload", "走りながらリロード", "边跑边装填");
        A("장전하는 동안 이동 속도가 오릅니다.", "Move faster while reloading.", "リロード中は移動速度が上がります。", "装填期间移动速度提升。");
        A("장전 중 이동 속도", "Move speed while reloading", "リロード中の移動速度", "装填时移动速度");
        A("전리품 탄약", "Scavenged Ammo", "戦利品の弾薬", "战利品弹药");
        A("적을 쓰러뜨리면 확률로 탄창에 한 발이 돌아옵니다.", "Kills have a chance to put a round back in your magazine.", "敵を倒すと確率で弾倉に1発戻ります。", "击杀敌人时有几率向弹匣补回一发。");
        A("회수 확률", "Recover chance", "回収確率", "回收几率");
        A("위협 사격", "Intimidation", "威嚇射撃", "威吓射击");
        A("필살기를 쓰면 주변 적이 겁에 질려 4초 동안 느려지고 받는 피해가 늘어납니다.", "Using your Ultimate terrifies nearby enemies, slowing them and making them take more damage for 4s.", "必殺技を使うと周りの敵が怯え、4秒間遅くなり受けるダメージが増えます。", "使用必杀技时附近敌人陷入恐惧，4秒内减速且受到的伤害增加。");
        A("받는 피해", "Damage taken", "受けるダメージ", "受到伤害");

        // ================================================================= 검사
        A("역습", "Riposte", "逆襲", "反击");
        A("맞은 뒤 1.5초 안에 주는 피해가 늘어납니다.", "Deal more damage for 1.5s after being hit.", "被弾してから1.5秒間、与えるダメージが増えます。", "受击后1.5秒内造成的伤害提升。");
        A("역습 피해", "Riposte damage", "逆襲ダメージ", "反击伤害");
        A("검의 궤적", "Blade Trail", "剣の軌跡", "剑之轨迹");
        A("휘두른 자리에 잠깐 칼날 잔상이 남아 닿는 적을 계속 벱니다.", "Swings leave a brief afterimage that keeps cutting enemies inside it.", "振った場所に少しの間刃の残像が残り、触れた敵を斬り続けます。", "挥砍处短暂留下刀刃残影，持续斩击触碰到的敌人。");
        A("잔상 피해 (0.25초마다)", "Afterimage damage (every 0.25s)", "残像ダメージ（0.25秒ごと）", "残影伤害（每0.25秒）");
        A("혈갑", "Blood Guard", "血甲", "血甲");
        A("벤 적 하나당 보호막이 조금씩 쌓여 받는 피해를 대신 막습니다.", "Each enemy you cut builds a small shield that absorbs damage.", "斬った敵1体ごとにシールドが少しずつたまり、受けるダメージを代わりに防ぎます。", "每斩中一名敌人就积累少量护盾，替你吸收伤害。");
        A("보호막 최대", "Max shield", "シールド最大", "护盾上限");
        A("회전 가속", "Whirl Momentum", "回転加速", "回旋加速");
        A("필살기를 쓴 뒤 휘두르기가 빨라집니다. 오래 모았을수록 더 오래 이어집니다.", "After an Ultimate, you swing faster. The longer you charged, the longer it lasts.", "必殺技の後、振りが速くなります。長くためるほど長く続きます。", "必杀技后挥砍加快。蓄力越久，持续越长。");
        A("강철 의지", "Iron Will", "鋼の意志", "钢铁意志");
        A("필살기를 모으는 동안 주변 적을 천천히 끌어당깁니다.", "While charging Ultimate, slowly pull nearby enemies in.", "必殺技をためている間、周りの敵をゆっくり引き寄せます。", "蓄力必杀技时缓缓拉近周围敌人。");
        A("끌어당기는 범위", "Pull range", "引き寄せ範囲", "牵引范围");
        A("검무", "Sword Dance", "剣舞", "剑舞");
        A("적을 벨 때마다 잠깐 이동 속도가 오릅니다. (최대 5중첩)", "Each swing that cuts an enemy briefly raises move speed. (Up to 5 stacks)", "敵を斬るたびに少しの間移動速度が上がります。（最大5重）", "每次斩中敌人短暂提升移动速度。（最多5层）");
        A("중첩당 이동 속도", "Move speed per stack", "1重ごとの移動速度", "每层移动速度");
        A("균열", "Fracture", "亀裂", "裂痕");
        A("같은 적을 세 번 벨 때마다 균열이 터져 추가 피해를 줍니다.", "Every third cut on the same enemy shatters it for bonus damage.", "同じ敵を3回斬るたびに亀裂が弾けて追加ダメージを与えます。", "每斩同一敌人三次，裂痕爆开造成额外伤害。");
        A("균열 피해", "Fracture damage", "亀裂ダメージ", "裂痕伤害");
        A("거인 사냥꾼", "Giant Hunter", "巨人狩り", "巨人猎手");
        A("보스 · 중간 보스에게 주는 피해가 늘어납니다.", "Deal more damage to bosses and mid-bosses.", "ボス・中ボスへのダメージが増えます。", "对首领和中头目造成的伤害提升。");
        A("보스에게 주는 피해", "Damage to bosses", "ボスへのダメージ", "对首领伤害");
        A("잔향 베기", "Echo Slash", "残響斬り", "余响斩");
        A("필살기가 끝나고 잠시 뒤 더 작은 회전 베기가 한 번 더 일어납니다.", "A moment after your Ultimate ends, a smaller echo spin follows.", "必殺技が終わった少し後、もう一度小さな回転斬りが起こります。", "必杀技结束片刻后，再发动一次较小的回旋斩。");
        A("잔향 피해", "Echo damage", "残響ダメージ", "余响伤害");
        A("영혼 흡수", "Soul Drinker", "魂吸収", "灵魂吸收");
        K("적을 쓰러뜨릴 때마다 검에 영혼이 깃들어 다음 베기의 피해가 증가합니다. (최대 8, 벨 때 모두 씀)", "적을 쓰러뜨릴 때마다 검에 영혼이 깃들어 다음 베기의 피해가 늘어납니다. (최대 8, 벨 때 모두 씀)","Each kill stores a soul in your blade, empowering your next swing. (Up to 8, all spent on the swing)", "敵を倒すたびに剣に魂が宿り、次の斬撃が強くなります。（最大8、斬ると全て使う）", "每击杀一名敌人，剑中就寄宿一个灵魂，强化下一次挥砍。（最多8个，挥砍时全部消耗）");
        A("영혼 하나당 피해", "Damage per soul", "魂1つあたりのダメージ", "每个灵魂伤害");

        // ================================================================= 도적
        A("연막", "Smoke Screen", "煙幕", "烟幕");
        A("필살기를 시작한 자리에 연막이 퍼져 안의 적이 크게 느려집니다.", "Your Ultimate leaves a smoke cloud where it began, greatly slowing enemies inside.", "必殺技を始めた場所に煙幕が広がり、中の敵が大きく遅くなります。", "流血突进的起点散开烟幕，其中的敌人大幅减速。");
        A("연막 지속", "Smoke duration", "煙幕の持続", "烟幕持续");
        A("피의 향연", "Blood Feast", "血の饗宴", "血之盛宴");
        A("출혈 중인 적을 쓰러뜨리면 체력을 회복합니다.", "Killing a bleeding enemy restores health.", "出血中の敵を倒すと体力を回復します。", "击杀流血中的敌人时恢复生命。");
        A("회복", "Heal", "回復", "恢复");
        A("출혈 폭발", "Hemorrhage", "出血爆発", "出血爆发");
        A("출혈이 3초 넘게 이어진 적은 남은 출혈이 한꺼번에 터집니다.", "Enemies bleeding for over 3s take all remaining bleed at once.", "出血が3秒以上続いた敵は、残りの出血が一度に弾けます。", "流血超过3秒的敌人，剩余流血伤害一次性爆发。");
        A("폭발 피해", "Burst damage", "爆発ダメージ", "爆发伤害");
        A("급습 표창", "Ambush Stars", "急襲手裏剣", "突袭飞镖");
        K("필살기가 끝난 뒤 2초 동안 표창의 피해가 증가합니다.", "필살기가 끝난 뒤 2초 동안 표창의 피해가 늘어납니다.", "Your throwing stars hit harder for 2s after your Ultimate.", "必殺技が終わった後2秒間、手裏剣のダメージが増えます。", "流血突进结束后2秒内飞镖伤害提升。");
        A("급습 피해", "Ambush damage", "急襲ダメージ", "突袭伤害");
        A("회전 표창", "Boomerang Star", "回転手裏剣", "回旋飞镖");
        // 회전 표창: 새 표창이 나타나는 대신 던진 그 표창이 꺾여 돌아옴 (SignatureSkills.Rogue ReturningShuriken)
        Entries["던진 표창이 잠시 뒤 되돌아오며 한 번 더 벱니다."] = E("던진 표창이 멀리 날아가거나 적을 다 꿰뚫으면 그 자리에서 꺾여 나에게 돌아오며, 오는 길의 적을 한 번 더 벱니다.", "Once your throwing star flies far or runs out of pierce, it turns back toward you, cutting enemies on the way again.", "投げた手裏剣は遠くまで飛ぶか貫通し切ると、その場で曲がって戻り、帰り道の敵をもう一度斬ります。", "掷出的飞镖飞远或穿透次数用尽后，会就地折返回到你身边，沿途再斩一次敌人。");
        A("되돌아오는 표창 피해", "Returning star damage", "戻る手裏剣のダメージ", "折返飞镖伤害");
        A("궤적 칼날", "Blade Path", "軌跡の刃", "轨迹刀刃");
        A("필살기가 지나간 길에 칼날 자국이 남아 닿는 적을 벱니다.", "Your Ultimate leaves a trail of blades that cuts enemies touching it.", "必殺技の通り道に刃の跡が残り、触れた敵を斬ります。", "流血突进的路径上留下刀痕，斩击触碰的敌人。");
        A("자국 지속", "Trail duration", "跡の持続", "刀痕持续");
        A("던지기 연습", "Practiced Throw", "投てき練習", "投掷练习");
        A("같은 적을 연달아 맞힐수록 피해가 오릅니다. (최대 5중첩)", "Hitting the same enemy in a row deals more and more damage. (Up to 5 stacks)", "同じ敵に続けて当てるほどダメージが上がります。（最大5重）", "连续命中同一敌人时伤害递增。（最多5层）");
        A("중첩당 피해", "Damage per stack", "1重ごとのダメージ", "每层伤害");
        A("넘치는 기세", "Overflowing Momentum", "あふれる勢い", "满溢气势");
        K("필살기 게이지가 가득 차는 순간 몸 주위로 표창이 사방에 퍼집니다. (공격력 100%씩)", "필살기 게이지가 가득 차는 순간 몸 주변으로 표창이 사방에 퍼집니다. (공격력 100%씩)", "The moment your Ultimate gauge fills, throwing stars burst out in every direction. (100% attack each)", "必殺技ゲージが満タンになった瞬間、体の周りに手裏剣が四方へ広がります。（攻撃力100%ずつ）", "必杀技槽充满的瞬间，飞镖向四周散射。（每枚攻击力100%）");
        A("표창 수", "Stars", "手裏剣の数", "飞镖数量");
        A("그림자 매듭", "Shadow Knot", "影の結び目", "暗影之结");
        A("필살기로 벤 적들이 4초 동안 그림자 줄로 묶여, 하나가 받은 피해의 일부를 나머지도 받습니다.", "Enemies cut by your Ultimate are tied by shadow for 4s; damage to one is partly shared by the rest.", "必殺技で斬った敵が4秒間影の糸で結ばれ、1体が受けたダメージの一部を他も受けます。", "被流血突进斩中的敌人被暗影之线绑定4秒，其中一个受到的伤害会部分传给其余敌人。");
        A("나눠 받는 피해", "Shared damage", "分け合うダメージ", "分摊伤害");
        A("빈 주머니의 비", "Empty-Pouch Rain", "空の袋の雨", "空囊之雨");
        A("표창을 다 던져 주머니가 비는 순간, 하늘에서 표창이 주변 적들에게 쏟아집니다. (공격력 100%씩)", "When your pouch runs empty, throwing stars rain down on nearby enemies. (100% attack each)", "手裏剣を投げ切って袋が空になった瞬間、空から手裏剣が周りの敵に降り注ぎます。（攻撃力100%ずつ）", "飞镖投尽、囊中空空的瞬间，飞镖从天而降砸向周围敌人。（每枚攻击力100%）");
        A("떨어지는 표창", "Falling stars", "降る手裏剣", "落下飞镖");

        // ================================================================= 궁수
        A("바람 읽기", "Wind Reading", "風読み", "读风");
        A("가득 당긴 화살이 지나간 길에 3초 동안 바람길이 남아, 그 위에 서 있으면 빨라집니다.", "Fully drawn arrows leave a wind lane for 3s; standing on it makes you faster.", "引き絞った矢の通り道に3秒間風の道が残り、その上にいると速くなります。", "满弓之箭飞过的路径留下3秒风道，站在其上移动更快。");
        A("바람길 이동 속도", "Speed on wind lane", "風の道の移動速度", "风道移动速度");
        A("집중 호흡", "Focused Breath", "集中呼吸", "专注呼吸");
        K("가득 당긴 화살을 연달아 쏠수록 다음 화살의 피해가 증가합니다. (최대 5중첩, 덜 당겨 쏘면 사라짐)", "가득 당긴 화살을 연달아 쏠수록 다음 화살의 피해가 늘어납니다. (최대 5중첩, 덜 당겨 쏘면 사라짐)","Each consecutive full-draw shot empowers the next. (Up to 5 stacks, lost on a partial draw)", "引き絞った矢を続けて撃つほど次の矢が強くなります。（最大5重、引き切らずに撃つと消える）", "连续射出满弓之箭会强化下一箭。（最多5层，未拉满射击则清空）");
        A("가시 씨앗", "Thorn Seeds", "棘の種", "荆棘种子");
        A("화살이 맞힌 자리에 작은 가시 덤불이 자라 적을 느리게 하고 찌릅니다.", "Arrows plant a small thornbush where they hit, slowing and pricking enemies.", "矢が当たった場所に小さな茨が育ち、敵を遅くして刺します。", "箭矢命中处长出小荆棘丛，减速并刺伤敌人。");
        A("덤불 지속", "Bush duration", "茂みの持続", "荆棘持续");
        A("울림 화살촉", "Resonant Tip", "共鳴の矢じり", "共鸣箭头");
        A("화살이 적을 맞히면 잠시 뒤 같은 자리에 울림이 퍼져 주변에 피해를 줍니다.", "Arrow hits send out a resonance a moment later, damaging enemies around.", "矢が敵に当たると少し後に同じ場所で共鳴が広がり、周りにダメージを与えます。", "箭矢命中后片刻，原地扩散共鸣伤害周围敌人。");
        A("울림 피해", "Resonance damage", "共鳴ダメージ", "共鸣伤害");
        A("표식 전염", "Spreading Mark", "標的の伝染", "标记传染");
        A("가득 당긴 화살로 적을 쓰러뜨리면 가까운 적들에게 표식을 남깁니다. (4초 동안 받는 피해 +20%)", "Kills with a fully drawn arrow mark nearby enemies. (+20% damage taken for 4s)", "引き絞った矢で敵を倒すと近くの敵に標的を残します。（4秒間受けるダメージ+20%）", "满弓之箭击杀敌人时为附近敌人留下标记。（4秒内受到伤害+20%）");
        A("표식을 남기는 수", "Marks left", "残す標的の数", "标记数量");
        A("꿰뚫는 시선", "Piercing Gaze", "貫く視線", "穿透之视");
        A("화살이 적을 꿰뚫을 때마다 그다음 적에게 주는 피해가 오릅니다.", "Each enemy an arrow pierces makes it hit the next one harder.", "矢が敵を貫くたびに、次の敵へのダメージが上がります。", "箭矢每穿透一名敌人，对下一名敌人的伤害提升。");
        A("꿰뚫을 때마다", "Per pierce", "貫くたびに", "每次穿透");
        A("후퇴 사격", "Backstep Shot", "後退射撃", "后撤射击");
        A("가득 당긴 화살을 쏘면 반대쪽으로 짧게 뛰어 물러납니다.", "Firing a full-draw arrow hops you back a short distance.", "引き絞った矢を撃つと反対側へ短く飛び退きます。", "射出满弓之箭时向反方向短距跃退。");
        A("물러나는 거리", "Hop distance", "下がる距離", "后撤距离");
        A("매복", "Ambush Trap", "待ち伏せ", "埋伏");
        A("가득 당겨 쏘면 발밑에 덫이 깔려(2초에 한 번), 들어온 적을 묶고 공격력 80% 피해를 줍니다. 묶인 적은 받는 피해가 30% 늘어납니다.", "Fully drawn shots lay a snare at your feet (once per 2s) that roots enemies and deals 80% attack damage. Rooted enemies take 30% more damage.", "引き絞って撃つと足元に罠が敷かれ（2秒に1回）、入った敵を縛って攻撃力80%のダメージを与えます。縛られた敵は受けるダメージが30%増えます。", "满弓射击时在脚下布下陷阱（每2秒一次），定住进入的敌人并造成80%攻击力伤害。被定住的敌人受到的伤害增加30%。");
        A("묶는 시간", "Root time", "縛る時間", "定身时间");
        A("높은 자리", "High Ground", "高所", "居高临下");
        A("유성 화살", "Meteor Arrow", "流星の矢", "流星箭");
        A("유성 피해", "Meteor damage", "流星ダメージ", "流星伤害");

        // ================================================================= 연금술사
        A("인화성 기체", "Flammable Vapor", "引火性ガス", "易燃气体");
        A("화염 시약이 산성 웅덩이에 닿으면 웅덩이가 불길에 휩싸여 크게 터집니다.", "When a fire reagent touches an acid puddle, the puddle bursts into flames and explodes.", "火炎の試薬が酸の水たまりに触れると、水たまりが炎に包まれて大きく爆発します。", "火焰试剂碰到酸池时，酸池燃起大火剧烈爆炸。");
        A("불길 폭발", "Blaze damage", "炎の爆発", "火焰爆炸");
        A("서리 결정", "Frost Crystals", "霜の結晶", "霜晶");
        A("빙결 시약에 맞은 적이 쓰러지면 얼음 파편이 사방으로 튑니다. (공격력 50%씩)", "Enemies hit by frost reagent shatter into ice shards when they die. (50% attack each)", "氷結の試薬を受けた敵が倒れると氷の破片が四方に飛びます。（攻撃力50%ずつ）", "被冰冻试剂击中的敌人死亡时向四周迸出冰片。（每片攻击力50%）");
        A("촉매 반응", "Catalyst", "触媒反応", "催化反应");
        A("한 적이 화염 · 빙결 · 산성 시약에 모두 맞으면 원소 붕괴가 일어나 큰 피해를 줍니다.", "An enemy hit by fire, frost, and acid reagents undergoes elemental collapse for heavy damage.", "1体の敵が火炎・氷結・酸の試薬すべてを受けると元素崩壊が起き、大ダメージを与えます。", "同一敌人被火焰、冰冻、酸性试剂全部命中时引发元素崩坏，造成巨大伤害。");
        A("붕괴 피해", "Collapse damage", "崩壊ダメージ", "崩坏伤害");
        A("증폭 용액", "Amplifying Solution", "増幅溶液", "增幅溶液");
        A("적 하나당", "Per enemy", "敵1体ごと", "每名敌人");
        A("조수 개조", "Assistant Upgrade", "助手改造", "助手改造");
        A("호문쿨루스가 던지는 플라스크에 화염 · 빙결 · 산성 시약을 번갈아 채우고 더 세게 터뜨립니다. (호문쿨루스 필요)", "Your Homunculus loads fire, frost, and acid in turn and its flasks hit harder. (Requires Homunculus)", "ホムンクルスが投げるフラスコに火炎・氷結・酸の試薬を交互に詰め、より強く爆発させます。（ホムンクルスが必要）", "侏儒助手投掷的烧瓶轮流装入火焰、冰冻、酸性试剂，爆炸更强。（需要侏儒助手）");
        A("조수 플라스크 피해", "Assistant flask damage", "助手フラスコのダメージ", "助手烧瓶伤害");
        A("유리 비", "Glass Rain", "ガラスの雨", "玻璃雨");
        A("떨어지는 간격", "Drop interval", "落ちる間隔", "落下间隔");
        A("응급 연고", "First-Aid Salve", "応急軟膏", "急救药膏");
        A("산성 웅덩이 위에 서 있으면 체력이 회복됩니다.", "Standing on an acid puddle heals you.", "酸の水たまりの上に立つと体力が回復します。", "站在酸池上时恢复生命。");
        A("초당 회복", "Heal per second", "毎秒回復", "每秒恢复");
        A("폭발 정제", "Refined Blast", "爆発精製", "爆炸精炼");
        A("불안정한 플라스크가 터질 때마다 체력을 회복합니다.", "Heal whenever an unstable flask explodes.", "不安定なフラスコが爆発するたびに体力を回復します。", "不稳定烧瓶每次爆炸时恢复生命。");
        A("연소 흔적", "Burning Steps", "燃焼の跡", "燃烧足迹");
        A("걸어간 자리에 작은 불길이 남아 밟은 적을 태웁니다.", "You leave small flames where you walk that burn enemies stepping in.", "歩いた場所に小さな炎が残り、踏んだ敵を焼きます。", "走过之处留下小火焰，灼烧踩到的敌人。");
        A("불길 피해 (초당)", "Flame damage (per second)", "炎ダメージ（毎秒）", "火焰伤害（每秒）");
        A("시약 농축", "Concentrate", "試薬濃縮", "试剂浓缩");
        A("시약 세 가지를 한 바퀴 던질 때마다 다음 플라스크가 농축되어 더 크게 터집니다.", "After each full cycle of three reagents, your next flask is concentrated and blasts bigger.", "3種の試薬を一巡投げるたびに次のフラスコが濃縮され、より大きく爆発します。", "每投完一轮三种试剂，下一个烧瓶会被浓缩，爆炸更大。");
        A("농축 플라스크 피해 · 범위", "Concentrated flask damage · radius", "濃縮フラスコのダメージ・範囲", "浓缩烧瓶伤害 · 范围");

        // ================================================================= 진화
        A("천둥 장전", "Thunder Reload", "雷鳴リロード", "雷鸣装填");
        K("장전을 마칠 때 주변 적 여섯에게 번개가 떨어지고 (공격력 120%), 탄피 지뢰가 두 배로 깔립니다.", "장전을 마칠 때 주변 적 여섯에게 번개가 떨어지고(공격력 120%), 탄피 지뢰가 두 배로 깔립니다.",
"Finishing a reload strikes six nearby enemies with lightning (120% attack), and twice as many shell mines are laid.", "リロードを終えると周りの敵6体に雷が落ち（攻撃力120%）、薬莢地雷が2倍敷かれます。", "装填完成时向附近六名敌人降下雷击（攻击力120%），弹壳地雷数量翻倍。");
        A("용광로 탄두", "Furnace Rounds", "溶鉱炉弾頭", "熔炉弹头");
        A("총열이 가장 뜨거울 때 모든 총알이 맞은 자리에서 불꽃 폭발을 일으키고 적을 태웁니다.", "At maximum barrel heat, every bullet erupts in a fiery blast that burns enemies.", "銃身が最も熱いとき、すべての弾が当たった場所で炎の爆発を起こし敵を焼きます。", "枪管最热时，所有子弹在命中处引发火焰爆炸并灼烧敌人。");
        A("파편 폭풍", "Shrapnel Storm", "破片の嵐", "破片风暴");
        A("파편이 두 배로 튀고, 파편이 맞힌 적에게서 한 번 더 가까운 적에게 튕깁니다.", "Twice the shrapnel, and each shard ricochets once more to a nearby enemy.", "破片が2倍飛び、破片が当たった敵からもう一度近くの敵へ跳ねます。", "破片数量翻倍，且破片命中后会再弹向附近敌人一次。");
        A("영혼 추적자", "Soul Seeker", "魂の追跡者", "灵魂追猎者");
        A("영혼탄이 적을 쫓아가고, 맞힌 적에게서 영혼 구슬이 튀어나와 가까운 적을 덮칩니다.", "Soul rounds home in, and each hit releases a soul orb that hunts a nearby enemy.", "魂弾が敵を追いかけ、当たった敵から魂の玉が飛び出して近くの敵を襲います。", "灵魂弹会追踪敌人，命中时迸出灵魂珠扑向附近敌人。");
        A("뇌격 반격", "Thunder Counter", "雷撃反撃", "雷击反击");
        K("맞으면 반격과 함께 몸 주위에 번개 폭풍이 터져 주변 적에게 큰 피해를 주고 기절시킵니다. (공격력 200%)", "맞으면 반격과 함께 몸 주변에 번개 폭풍이 터져 주변 적에게 큰 피해를 주고 기절시킵니다. (공격력 200%)","When hit, a lightning storm bursts around you along with your return fire, heavily damaging and stunning nearby enemies. (200% attack)", "被弾すると反撃と共に体の周りで雷の嵐が弾け、周りの敵に大ダメージを与えて気絶させます。（攻撃力200%）", "受击时在反击的同时于身边爆发雷暴，重创并击晕周围敌人。（攻击力200%）");
        A("총잡이의 춤", "Gunslinger's Dance", "ガンマンの舞", "枪手之舞");
        A("장전을 마치면 가까운 적 여섯에게 자동으로 총알을 한 발씩 쏩니다.", "Finishing a reload automatically fires a shot at each of six nearby enemies.", "リロードを終えると近くの敵6体に自動で1発ずつ撃ちます。", "装填完成后自动向附近六名敌人各开一枪。");
        A("장전을 마치면 가까운 적 여섯에게 자동으로 총알을 한 발씩 쏩니다. (포탑 설치 갈래: 포탑도 함께 쏩니다)", "Finishing a reload automatically fires a shot at each of six nearby enemies. (Turret Deploy path: turrets fire too)", "リロードを終えると近くの敵6体に自動で1発ずつ撃ちます。（砲台設置の流派：砲台も一緒に撃ちます）", "装填完成后自动向附近六名敌人各开一枪。（架设炮塔流派：炮塔也会一起开火）");
        A("무법자", "Outlaw", "無法者", "亡命之徒");
        A("겁에 질린 적을 쓰러뜨리면 탄창이 가득 차고, 겁이 가까운 적들에게 번집니다.", "Killing a terrified enemy fully reloads your magazine and spreads the terror to nearby foes.", "怯えた敵を倒すと弾倉が満タンになり、恐怖が近くの敵に広がります。", "击杀恐惧中的敌人会装满弹匣，并让恐惧蔓延到附近敌人。");
        A("완벽한 반격", "Perfect Counter", "完璧な反撃", "完美反击");
        A("맞거나 투사체를 쳐내면 다음 베기가 앞으로 날아가는 거대한 반격 검기가 됩니다. (공격력 300%)", "Getting hit or parrying a projectile turns your next swing into a giant counter wave. (300% attack)", "被弾するか投射物を弾くと、次の斬撃が前へ飛ぶ巨大な反撃の剣気になります。（攻撃力300%）", "受击或弹开投射物后，下一次挥砍化为向前飞出的巨大反击剑气。（攻击力300%）");
        A("천검 궤적", "Thousand-Blade Trail", "千剣の軌跡", "千剑轨迹");
        A("칼날 잔상이 사라질 때 그 자리에서 검기가 사방으로 날아갑니다.", "When a blade afterimage fades, sword waves fly out from it in all directions.", "刃の残像が消えるとき、その場から剣気が四方へ飛びます。", "刀刃残影消失时，剑气从原地向四方飞出。");
        A("피의 성채", "Blood Bastion", "血の城砦", "血之堡垒");
        A("보호막 최대치가 1.5배가 되고, 보호막이 깨지는 순간 피의 파동이 터집니다. (공격력 250%)", "Max shield is multiplied by 1.5, and a blood wave bursts the moment it breaks. (250% attack)", "シールド最大値が1.5倍になり、シールドが割れた瞬間に血の波動が弾けます。（攻撃力250%）", "护盾上限变为1.5倍，护盾破碎的瞬间爆发血之波动。（攻击力250%）");
        A("폭풍의 눈", "Eye of the Storm", "嵐の目", "风暴之眼");
        A("필살기 뒤 3초 동안 소용돌이가 주변 적을 빨아들이며 계속 벱니다.", "After your Ultimate, a vortex pulls in and keeps cutting nearby enemies for 3s.", "必殺技の後3秒間、渦が周りの敵を吸い込みながら斬り続けます。", "必杀技后3秒内，漩涡吸入周围敌人并持续斩击。");
        A("광검무", "Frenzied Blade Dance", "狂剣舞", "狂剑舞");
        A("검무가 최대 중첩일 때 모든 베기가 강한 일격(두 배 피해 · 더 멀리)이 됩니다.", "At max Sword Dance stacks, every swing becomes a heavy strike (double damage, longer reach).", "剣舞が最大まで重なったとき、すべての斬撃が強打（2倍ダメージ・遠くまで）になります。", "剑舞叠满时，每次挥砍都成为强力一击（双倍伤害 · 更远）。");
        A("거인 처단", "Giant Slayer", "巨人斬り", "斩巨");
        A("보스는 두 번만 베어도 균열이 터지고, 균열이 주변 적에게도 퍼집니다.", "Bosses fracture after just two cuts, and fractures spread to nearby enemies.", "ボスは2回斬るだけで亀裂が弾け、亀裂が周りの敵にも広がります。", "首领只需斩两次就会裂痕爆开，裂痕也会波及周围敌人。");
        A("영혼 회오리", "Soul Vortex", "魂の渦", "灵魂漩涡");
        A("잔향 베기가 흡수한 영혼만큼 커지고 강해지며, 쓴 영혼의 절반을 되돌려 줍니다.", "Echo Slash grows bigger and stronger with your stored souls, and refunds half of them.", "残響斬りが吸収した魂の分だけ大きく強くなり、使った魂の半分を返します。", "余响斩随储存的灵魂变大变强，并返还一半灵魂。");
        A("환영 연막", "Phantom Smoke", "幻影の煙幕", "幻影烟幕");
        Entries["연막 안에 서 있는 동안 적의 공격이 모두 빗나갑니다."] = E("연막 안에 서 있는 동안 받는 피해가 90% 줄어듭니다.", "While you stand in your smoke, damage taken is reduced by 90%.", "煙幕の中にいる間、受けるダメージが90%減少します。", "站在烟幕中时，受到的伤害降低90%。");     // 1.2.5~ 빗나감 → 받는 피해 -90%
        A("피바다", "Sea of Blood", "血の海", "血海");
        A("출혈 폭발이 주변 적에게도 같은 피해를 주고, 터질 때마다 체력을 회복합니다.", "Hemorrhage also hits nearby enemies for the same damage, and heals you each time.", "出血爆発が周りの敵にも同じダメージを与え、弾けるたびに体力を回復します。", "出血爆发对周围敌人造成同等伤害，并在每次爆发时恢复生命。");
        A("처형자의 길", "Executioner's Path", "処刑人の道", "处刑者之路");
        A("급습 시간 동안 표창을 던질 때마다 모든 적을 꿰뚫는 그림자 표창이 함께 날아가고, 묶인 적이 쓰러지면 급습 시간이 다시 채워집니다.", "During Ambush, each throw also launches a shadow star that pierces everything, and killing a knotted enemy refreshes Ambush.", "急襲中は手裏剣を投げるたびにすべての敵を貫く影の手裏剣も飛び、結ばれた敵が倒れると急襲時間が再び満ちます。", "突袭期间每次投掷都会附带一枚穿透所有敌人的暗影飞镖，被绑定的敌人倒下时突袭时间重置。");
        A("귀환하는 칼날", "Returning Blades", "帰還する刃", "归还之刃");
        A("되돌아오는 표창이 두 개가 되고, 되돌아올 때마다 탄창에 한 발이 돌아옵니다.", "Two stars return instead of one, and each return refunds a star.", "戻る手裏剣が2つになり、戻るたびに弾倉に1発戻ります。", "折返飞镖变为两枚，每次折返时补回一枚飞镖。");
        A("칼날 폭풍길", "Bladestorm Road", "刃の嵐の道", "刃风之路");
        A("칼날 자국에서 표창이 계속 솟구쳐 가까운 적에게 날아갑니다.", "Throwing stars keep springing from the blade trail toward nearby enemies.", "刃の跡から手裏剣が次々と飛び出し、近くの敵へ飛びます。", "刀痕中不断弹出飞镖射向附近敌人。");
        A("칼날 곡예", "Blade Acrobatics", "刃の曲芸", "刀刃杂技");
        A("던지기 연습이 최대 중첩일 때 맞힌 적 둘레로 칼날 고리가 터집니다. (공격력 200%)", "At max Practiced Throw stacks, a ring of blades bursts around the enemy you hit. (200% attack)", "投てき練習が最大まで重なったとき、当てた敵の周りに刃の輪が弾けます。（攻撃力200%）", "投掷练习叠满时，命中的敌人周围爆发刀刃之环。（攻击力200%）");
        A("사냥의 절정", "Peak of the Hunt", "狩りの絶頂", "狩猎巅峰");
        A("필살기 게이지가 가득 찬 동안이나 필살기 중에 적을 쓰러뜨리면 그 자리에 표창 비가 내립니다.", "While your Ultimate gauge is full or during your Ultimate, kills bring a rain of throwing stars down on the spot.", "必殺技ゲージが満タンの間か必殺技中に敵を倒すと、その場に手裏剣の雨が降ります。", "必杀技槽满时或必杀技中击杀敌人，原地降下飞镖雨。");
        A("폭풍의 길", "Storm Road", "嵐の道", "风暴之路");
        A("바람길 위에서는 시위를 순식간에 가득 당기고, 물러난 자리에 돌풍이 터져 적을 밀어냅니다.", "On a wind lane you draw instantly to full, and a gust erupts where you hop back from, pushing enemies away.", "風の道の上では一瞬で弦を引き絞り、下がった場所で突風が弾けて敵を押し出します。", "在风道上瞬间拉满弓弦，后撤的原位爆发狂风击退敌人。");
        A("명궁의 호흡", "Master Archer's Breath", "名弓の呼吸", "名弓之息");
        A("중첩이 가득 차면 다음 화살이 모든 적을 꿰뚫고, 덫에 걸린 적은 두 배 피해를 받습니다.", "At full stacks your next arrow pierces every enemy, and snared enemies take double damage.", "最大まで重なると次の矢がすべての敵を貫き、罠にかかった敵は2倍のダメージを受けます。", "叠满后下一支箭穿透所有敌人，被陷阱定住的敌人受到双倍伤害。");
        A("가시 숲", "Thorn Forest", "茨の森", "荆棘之林");
        A("가시 덤불이 1.5배 크게 자라고, 덤불 안의 적은 받는 피해가 20% 늘어납니다.", "Thornbushes grow 1.5× larger, and enemies inside take 20% more damage.", "茨が1.5倍大きく育ち、茂みの中の敵は受けるダメージが20%増えます。", "荆棘丛长大1.5倍，丛中敌人受到的伤害提升20%。");
        A("천 개의 메아리", "Thousand Echoes", "千の木霊", "千重回响");
        A("울림이 한 번 더 퍼지고, 울림에 맞은 적에게서 작은 화살이 튀어나갑니다.", "The resonance pulses once more, and small arrows burst out of the enemies it hits.", "共鳴がもう一度広がり、共鳴を受けた敵から小さな矢が飛び出します。", "共鸣再扩散一次，被共鸣击中的敌人身上会迸出小箭。");
        A("사냥꾼의 낙인", "Hunter's Brand", "狩人の烙印", "猎人烙印");
        A("표식이 붙은 적이 쓰러지면 폭발하고, 표식 효과가 두 배가 됩니다.", "Marked enemies explode on death, and the mark's effect doubles.", "標的が付いた敵が倒れると爆発し、標的の効果が2倍になります。", "被标记的敌人死亡时爆炸，标记效果翻倍。");
        A("만궁", "Full Draw Mastery", "満弓", "满弓");
        K("다섯 번째 적을 꿰뚫는 화살이 폭발하고, 분열한 화살도 꿰뚫을수록 피해가 증가합니다.", "다섯 번째 적을 꿰뚫는 화살이 폭발하고, 분열한 화살도 꿰뚫을수록 피해가 늘어납니다.","Arrows explode on their fifth pierce, and split arrows also grow stronger as they pierce.", "5体目を貫いた矢が爆発し、分裂した矢も貫くほど強くなります。", "穿透第五名敌人的箭会爆炸，分裂箭也会越穿越强。");
        A("천공의 사냥", "Hunt from the Heavens", "天空の狩り", "天穹狩猎");
        A("유성이 세 개 떨어지고, 떨어진 자리에 별빛이 3초 동안 남아 적을 태웁니다.", "Three meteors fall, each leaving starlight that burns enemies for 3s.", "流星が3つ落ち、落ちた場所に星の光が3秒間残って敵を焼きます。", "落下三颗流星，落点留下3秒星光灼烧敌人。");
        A("타오르는 늪", "Burning Swamp", "燃える沼", "燃烧沼泽");
        A("불붙은 웅덩이가 4초 동안 타오르며 그 위의 적을 계속 태웁니다.", "Ignited puddles keep burning for 4s, scorching enemies on them.", "火のついた水たまりが4秒間燃え続け、上の敵を焼き続けます。", "被点燃的酸池持续燃烧4秒，不断灼烧其上的敌人。");
        A("영구 동토", "Permafrost", "永久凍土", "永冻");
        A("얼음 파편이 맞힌 적도 얼어붙고, 얼어붙은 적은 받는 피해가 30% 늘어납니다.", "Ice shards freeze what they hit, and frozen enemies take 30% more damage.", "氷の破片が当たった敵も凍りつき、凍った敵は受けるダメージが30%増えます。", "冰片命中的敌人也会冻结，被冻结的敌人受到的伤害提升30%。");
        A("삼원소 붕괴", "Trielemental Collapse", "三元素崩壊", "三元素崩坏");
        A("원소 붕괴가 두 배 넓게 번지고, 휘말린 적에게는 원소 두 가지가 묻습니다.", "Elemental collapse spreads twice as wide and coats caught enemies with two elements.", "元素崩壊が2倍広く広がり、巻き込まれた敵には元素が2種付きます。", "元素崩坏范围扩大一倍，波及的敌人沾上两种元素。");
        A("연쇄 대폭발", "Chain Cataclysm", "連鎖大爆発", "连锁大爆炸");
        K("연쇄 폭발로 쓰러진 적도 다시 터지고 (최대 3번), 연쇄될 때마다 체력을 조금 회복합니다.", "연쇄 폭발로 쓰러진 적도 다시 터지고(최대 3번), 연쇄될 때마다 체력을 조금 회복합니다.",
"Enemies killed by chain explosions explode again (up to 3 times), healing you a little each time.", "連鎖爆発で倒れた敵もまた爆発し（最大3回）、連鎖するたびに体力を少し回復します。", "被连锁爆炸击杀的敌人也会再次爆炸（最多3次），每次连锁恢复少量生命。");
        A("쌍둥이 조수", "Twin Assistants", "双子の助手", "双子助手");
        A("조수가 하나 더 생겨 함께 플라스크를 던집니다.", "A second assistant joins in, throwing flasks alongside the first.", "助手がもう1体増え、一緒にフラスコを投げます。", "再多一名助手，一同投掷烧瓶。");
        A("유리 폭풍", "Glass Storm", "ガラスの嵐", "玻璃风暴");
        A("필살기가 터진 뒤 3초 동안 주변에 유리 비가 계속 쏟아집니다.", "After the Great Blast Flask bursts, glass keeps raining around it for 3s.", "必殺技が爆発した後3秒間、周りにガラスの雨が降り続きます。", "必杀技爆炸后3秒内，周围持续落下玻璃雨。");
        A("불꽃 행진", "Flame March", "炎の行進", "火焰行军");
        A("농축 플라스크가 터진 자리에 불길 고리가 퍼지고, 연소 흔적이 두 배로 커집니다.", "Concentrated flasks leave a ring of fire, and Burning Steps grow twice as large.", "濃縮フラスコが爆発した場所に炎の輪が広がり、燃焼の跡が2倍大きくなります。", "浓缩烧瓶爆炸处扩散火焰之环，燃烧足迹扩大一倍。");
        A("황금 시대", "Golden Age", "黄金時代", "黄金时代");
        A("코인을 주울 때마다 경험치를 조금 얻고, 코인 40개를 모을 때마다 황금 파동이 터져 주변 적에게 피해를 줍니다. (공격력 300%)", "Picking up coins grants a little EXP, and every 40 coins releases a golden wave that damages nearby enemies. (300% attack)", "コインを拾うたびに経験値を少し得て、コイン40枚ごとに黄金の波動が弾けて周りの敵にダメージを与えます。（攻撃力300%）", "每拾取金币获得少量经验，每收集40枚金币爆发黄金波动伤害周围敌人。（攻击力300%）");
        A("불멸의 육체", "Undying Flesh", "不滅の肉体", "不灭之躯");
        A("체력이 가득 차 있으면 3초마다 심장 박동 파동이 퍼져 주변 적을 밀어내고 최대 체력에 비례한 피해를 줍니다.", "At full health, a heartbeat pulse spreads every 3s, knocking back nearby enemies and dealing damage based on max health.", "体力が満タンのとき、3秒ごとに鼓動の波動が広がり、周りの敵を押し返して最大体力に比例したダメージを与えます。", "生命全满时每3秒扩散心跳波动，击退周围敌人并造成与最大生命成比例的伤害。");
        // 1.0.4 요청 growth: 끝없는 성장 카드 (LevelShop.Growth) · 영혼 정련 (SoulTreeUI) — A() 는 이미 있는 키를 건너뛰어 다른 갈래와 겹쳐도 안전
        A("성장: 힘", "Growth: Might", "成長：力", "成长：力量");
        A("성장: 생명", "Growth: Vitality", "成長：生命", "成长：生命");
        A("성장: 손놀림", "Growth: Dexterity", "成長：手さばき", "成长：手速");
        A("성장: 발걸음", "Growth: Stride", "成長：足取り", "成长：步伐");
        A("모든 무기 피해가 2% 늘어납니다. (기본 공격력 기준)", "All weapon damage increases by 2%. (Based on base attack)", "すべての武器ダメージが2%増えます。（基本攻撃力基準）", "所有武器伤害提升2%。（以基础攻击力计）");
        A("최대 체력이 5 늘어나고 그만큼 회복합니다.", "Max health increases by 5, and you heal that much.", "最大体力が5増え、その分回復します。", "最大生命提升5，并回复等量生命。");
        A("공격 속도가 1% 빨라집니다.", "Attack speed increases by 1%.", "攻撃速度が1%速くなります。", "攻击速度提升1%。");
        A("이동 속도가 1% 빨라집니다. (이동 속도 상한은 그대로)", "Move speed increases by 1%. (The move speed cap still applies)", "移動速度が1%速くなります。（移動速度の上限はそのまま）", "移动速度提升1%。（移动速度上限不变）");
        K("상한 없이 계속 쌓입니다", "상한 없이 계속 쌓입니다.", "Stacks without limit.", "上限なく積み重なります。", "可无限叠加。");
        A("최대 레벨(50) 전에 배울 레벨업 카드를 모두 배우면, 레벨업마다 끝없이 쌓이는 「성장」 카드를 고릅니다.",
          "If you learn every level-up card before max level (50), each level-up lets you pick an endlessly stacking \"Growth\" card.",
          "最大レベル（50）の前に覚えるレベルアップカードをすべて覚えると、レベルアップごとに無限に積み重なる「成長」カードを選びます。",
          "在达到等级上限（50）前学会所有升级卡后，每次升级都可选择一张可无限叠加的「成长」卡。");
        A("성장 카드", "Growth Cards", "成長カード", "成长卡");
        A("레벨이 오를 때 세 장 중 하나를 고르거나 건너뜁니다 · 최대 레벨 50 (그 뒤로는 레벨업 없음) · 재료를 모두 올리면 스킬 진화",
          "Pick one of three cards or skip on each level-up · Max level 50 (no level-ups after that) · Max all ingredients to evolve a skill",
          "レベルアップごとに3枚から1枚を選ぶかスキップ・最大レベル50（その後はレベルアップなし）・素材をすべて上げるとスキル進化",
          "每次升级从三张中选一张或跳过 · 等级上限50（之后不再升级）· 素材全部升满即可技能进化");
        A("최대 레벨(50) 전에 고를 레벨업 카드를 모두 배우면, 그 뒤 레벨업마다 상한 없이 쌓이는 성장 카드 중 하나를 고릅니다. 힘: 모든 무기 피해 +2% · 생명: 최대 체력 +5 · 손놀림: 공격 속도 +1% · 발걸음: 이동 속도 +1%",
          "If you learn every level-up card before max level (50), each further level-up lets you pick one of the endlessly stacking Growth cards. Might: all weapon damage +2% · Vitality: max health +5 · Dexterity: attack speed +1% · Stride: move speed +1%",
          "最大レベル（50）の前に選べるレベルアップカードをすべて覚えると、その後のレベルアップごとに上限なく積み重なる成長カードから1枚を選びます。力：すべての武器ダメージ +2%・生命：最大体力 +5・手さばき：攻撃速度 +1%・足取り：移動速度 +1%",
          "在达到等级上限（50）前学会所有可选升级卡后，之后每次升级都可从可无限叠加的成长卡中选一张。力量：所有武器伤害 +2% · 生命：最大生命 +5 · 手速：攻击速度 +1% · 步伐：移动速度 +1%");
        A("영혼 정련", "Soul Refining", "魂の精錬", "灵魂精炼");
        A("모든 무기 피해 +2% · 최대 체력 +4", "All weapon damage +2% · Max health +4", "すべての武器ダメージ +2%・最大体力 +4", "所有武器伤害 +2% · 最大生命 +4");
        K("트리를 모두 배우면 열립니다", "트리를 모두 배우면 열립니다.", "Unlocks once the whole tree is learned.", "ツリーをすべて習得すると解放されます。", "学满整棵树后解锁。");
        A("눌러서 정련하기", "Click to Refine", "クリックで精錬", "点击精炼");
        A("영혼 트리를 모두 배운 뒤 남는 영혼 조각으로 작은 능력치를 끝없이 올립니다. 할 때마다 비용이 5배로 오릅니다.",
          "Spend the Soul Shards left over after learning the whole Soul Tree to raise your stats a little at a time, endlessly. The cost rises 5× each time.",
          "魂のツリーをすべて習得した後に残った魂のかけらで、小さな能力値を無限に上げます。行うたびに費用が5倍に上がります。",
          "学满灵魂树后，用剩余的灵魂碎片无限提升少量属性。每次精炼费用变为原来的5倍。");
        A("정련 횟수", "Times refined", "精錬回数", "精炼次数");
        K("모든 칸을 배웠습니다", "모든 칸을 배웠습니다.", "You've learned every node.", "すべてのマスを習得しました。", "已学会所有节点。");
        AddHeroes();        // 1.0.2 새 캐릭터 (LocTable.Heroes)
        AddTranscend();     // 1.1~ 3장 각성 · 초월 카드 (LocTable.Transcend)
        AddTerrain();       // 1.2.3~ 무한 모드 지형 변화 (LocTable.Terrain)
        AddPaths();         // 2026-10-19~ 빌드 갈래 공통 문구 + 영웅 아홉 (LocTable.Paths · Paths/LocTable.Paths.<영웅>)
    }
}
