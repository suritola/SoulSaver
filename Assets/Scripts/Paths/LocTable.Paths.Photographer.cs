// 번역표: 사진사 갈래 문구 4개 언어 (LocTable.Paths 의 AddPaths 가 부름)
// 넣을 것: 갈래 이름 · 한 줄 성격(tagline) · 입문 카드 이름 · 바뀌는 것 · 대가 · 강점 · 약점 · 공명 I · II · 극의 · 새 교차 이름 · 설명
//   + 갈래 효과의 피해 출처 이름(결과 화면 · 판 기록) (사진사는 31번 새 카드가 없음)
//   (Paths/Paths.Photographer.cs 의 PathDef · CrossDef 에 쓴 한국어 문장 그대로가 키 — 문장을 고치면 여기 키도 같이)
// 형식: PA(한국어, 영어, 일본어, 중국어) · 화면에 보일 한국어만 바꿀 때 PK(키, 한국어, 영어, 일본어, 중국어)
// 문구 지침: docs/review/2026-10-16-문구-다듬기-지침.md (완결 문장 · 수치는 카드 글이 금색으로)
// 용어: 필름 film · フィルム · 胶卷 / 셔터 shutter · シャッター · 快门 / 틀 frame · フレーム · 取景框 / 초점 focus · ピント · 对焦 / 인화 print · 現像 · 冲印 / 사본 copy · コピー · 副本
public static partial class LocTable
{
    static void AddPathsPhotographer()
    {
        // ---------------- 연사
        PA("연사", "Burst", "連写", "连拍");
        PA("자주 찍고 걸으며 처리 · 무리 · 기동", "Shoot often, clear on the move · Crowds · Mobility", "頻繁に撮り、歩きながら処理 · 群れ · 機動", "频繁拍摄、边走边清 · 敌群 · 机动");
        PA("연속 촬영", "Continuous Shooting", "連続撮影", "连续拍摄");
        PA("필름이 6장에서 12장으로 늘고, 셔터 간격이 40% 짧아지고, 틀이 20% 작아지며, 종이가 되는 시간이 2.5초에서 1.5초로 줄어듭니다.",
           "Film rises from 6 to 12 shots, the shutter interval is 40% shorter, the frame is 20% smaller, and paper time drops from 2.5s to 1.5s.",
           "フィルムが6枚から12枚に増え、シャッター間隔が40%短くなり、フレームが20%小さくなり、紙になる時間が2.5秒から1.5秒に減ります。",
           "胶卷从6张增加到12张，快门间隔缩短40%，取景框缩小20%，变成纸的时间从2.5秒缩短到1.5秒。");
        PA("셔터 피해가 35% 줄어듭니다.", "Shutter damage is 35% lower.", "シャッターのダメージが35%減ります。", "快门伤害降低35%。");
        PA("무리 · 기동", "Crowds · Mobility", "群れ · 機動", "敌群 · 机动");
        PA("보스 · 감기 공백", "Bosses · Rewind gaps", "ボス · 巻き上げの空白", "首领 · 卷片空档");
        PA("1초 안에 같은 적을 세 번 찍으면 「필름 끊김」으로 공격력 100%를 한 번 더 주고 종이 시간을 새로 시작합니다.",
           "Shooting the same foe three times within 1s triggers a Film Snap: 100% Attack once more, and its paper time restarts.",
           "1秒以内に同じ敵を3回撮ると「フィルム切れ」が起き、攻撃力100%をもう一度与えて紙の時間を最初からやり直します。",
           "1秒内对同一敌人拍三次会触发「断片」，再造成一次攻击力100%的伤害，并重新开始纸化时间。");
        PA("장노출 틀이 겹친 자리는 보스에게도 피해가 2배가 됩니다.",
           "Where long-exposure frames overlap, damage doubles, bosses included.",
           "長時間露光のフレームが重なった場所では、ボスにもダメージが2倍になります。",
           "长曝光取景框重叠的位置，伤害翻倍，对首领同样有效。");
        PA("필름 감기가 1.5초에서 0.6초로 줄고, 감는 동안에도 셔터를 누를 수 있습니다. (피해 50%)",
           "Rewinding drops from 1.5s to 0.6s, and you can still press the shutter while rewinding. (50% damage)",
           "フィルムの巻き上げが1.5秒から0.6秒に減り、巻いている間もシャッターを押せます。（ダメージ50%）",
           "卷片时间从1.5秒缩短到0.6秒，卷片期间也能按快门。（伤害50%）");

        // ---------------- 망원
        PA("망원", "Telephoto", "望遠", "长焦");
        PA("멀리서 초점 한 장 · 보스 · 원거리", "One focused shot from afar · Bosses · Long range", "遠くからピント一枚 · ボス · 遠距離", "远处对焦一张 · 首领 · 远程");
        PA("망원 렌즈", "Telephoto Lens", "望遠レンズ", "长焦镜头");
        PA("사거리가 11칸에서 18칸으로 늘고, 틀이 35% 작아지며, 초점 원이 0.7칸에서 1.2칸으로, 초점 피해가 1.6배에서 2.2배로 커집니다.",
           "Range rises from 11 to 18 tiles, the frame is 35% smaller, the focus circle grows from 0.7 to 1.2 tiles, and focus damage rises from x1.6 to x2.2.",
           "射程が11マスから18マスに伸び、フレームが35%小さくなり、ピントの円が0.7マスから1.2マスに、ピントのダメージが1.6倍から2.2倍に大きくなります。",
           "射程从11格提高到18格，取景框缩小35%，对焦圈从0.7格扩大到1.2格，对焦伤害从1.6倍提高到2.2倍。");
        PA("나에게서 6칸 안의 적은 초점이 맞지 않아 피해가 50% 줄고 종이가 되지 않습니다.",
           "Foes within 6 tiles of you are out of focus: they take 50% less damage and do not turn to paper.",
           "自分から6マス以内の敵はピントが合わず、ダメージが50%減り、紙になりません。",
           "离我6格内的敌人对不上焦，伤害降低50%，也不会变成纸。");
        PA("보스 · 원거리", "Bosses · Long range", "ボス · 遠距離", "首领 · 远程");
        PA("근접 포위", "Being surrounded", "接近戦での包囲", "被近身包围");
        PA("초점이 맞은 적은 종이 시간이 2배가 되고, 보스는 초점 표식을 3개 얻습니다.",
           "Focused foes stay paper twice as long, and a focused boss gains 3 focus marks.",
           "ピントが合った敵は紙の時間が2倍になり、ボスはピント印を3つ得ます。",
           "对上焦的敌人纸化时间翻倍，首领一次获得3个对焦标记。");
        PA("표식 5개가 쌓인 보스를 초점으로 찍으면 「클로즈업」으로 표식을 모두 써서 공격력 400%를 줍니다.",
           "Focusing a boss with 5 marks triggers a Close-up: all marks are spent for 400% Attack.",
           "印が5つ溜まったボスをピントで撮ると「クローズアップ」になり、印をすべて使って攻撃力400%を与えます。",
           "对集满5个标记的首领对焦拍摄会触发「特写」，耗尽所有标记造成攻击力400%的伤害。");
        PA("한 장에 초점이 둘이 됩니다.", "Each shot has two focus points.", "1枚にピントが2つになります。", "每张照片有两个对焦点。");

        // ---------------- 암실
        PA("암실", "Darkroom", "暗室", "暗房");
        PA("사본으로 홀리고 끌어당김 · 생존 · 제어", "Lure and pull with copies · Survival · Control", "コピーで惑わし引き寄せる · 生存 · 制御", "用副本诱引拉拢 · 生存 · 控制");
        PA("인화가 두 장(최고 · 두 번째)이 되고, 미끼가 3초에서 4.5초로 늘며, 사본이 찢어질 때 피해가 60% 늘어납니다.",
           "You print two photos (best and second best), the lure lasts 4.5s instead of 3s, and copies deal 60% more damage when they tear.",
           "現像が2枚（最高と2番目）になり、おとりが3秒から4.5秒に伸び、コピーが破れるときのダメージが60%増えます。",
           "冲印变为两张（最佳和第二佳），诱饵从3秒延长到4.5秒，副本撕裂时伤害提高60%。");
        PA("필름이 6장에서 4장으로 줄고 필름 감기가 0.3초 느려집니다.",
           "Film drops from 6 to 4 shots and rewinding is 0.3s slower.",
           "フィルムが6枚から4枚に減り、巻き上げが0.3秒遅くなります。",
           "胶卷从6张减少到4张，卷片慢0.3秒。");
        PA("생존 · 제어", "Survival · Control", "生存 · 制御", "生存 · 控制");
        PA("순간 화력", "Burst damage", "瞬間火力", "瞬间输出");
        PA("사본이 찢어진 자리에 0.8초 동안 암실 커튼이 서서 반지름 1.4칸 안의 적 투사체를 지웁니다.",
           "Where a copy tears, a darkroom curtain stands for 0.8s and erases enemy projectiles within 1.4 tiles.",
           "コピーが破れた場所に0.8秒間暗室のカーテンが立ち、半径1.4マス以内の敵の飛び道具を消します。",
           "副本撕裂处会立起暗房帘幕0.8秒，清除半径1.4格内的敌方投射物。");
        PA("사본 3칸 안의 보스는 받는 피해가 15% 늘어납니다. (보스의 움직임은 그대로)",
           "A boss within 3 tiles of a copy takes 15% more damage. (Its movement is unchanged)",
           "コピーから3マス以内のボスが受けるダメージが15%増えます。（ボスの動きは変わりません）",
           "副本3格内的首领受到的伤害提高15%。（首领的移动不变）");
        PA("한 번에 인화하는 사본 상한이 6장에서 10장이 되고, 인화가 세 장이 됩니다.",
           "The copy cap per print rises from 6 to 10, and you print three photos.",
           "1回の現像で出るコピーの上限が6枚から10枚になり、現像が3枚になります。",
           "每次冲印的副本上限从6张提高到10张，并且冲印三张。");

        // ---------------- 종이 공작
        PA("종이 공작", "Paper Craft", "ペーパークラフト", "纸工艺");
        PA("종이를 퍼뜨려 연쇄 · 무리", "Spread the paper into chains · Crowds", "紙を広げて連鎖 · 群れ", "扩散纸片引发连锁 · 敌群");
        PA("종이가 되는 시간이 2.5초에서 4초로 늘고, 종이가 된 적이 쓰러지면 종잇조각 2장이 기본으로 날아갑니다. (현상액이 더 늘립니다)",
           "Paper time rises from 2.5s to 4s, and a paper foe that dies sends out 2 paper scraps by default. (Developer adds more)",
           "紙になる時間が2.5秒から4秒に伸び、紙になった敵が倒れると紙片2枚が基本で飛びます。（現像液でさらに増えます）",
           "纸化时间从2.5秒延长到4秒，纸化的敌人倒下时默认飞出2片纸屑。（显影液可再增加）");
        PA("셔터 피해가 30% 줄어듭니다.", "Shutter damage is 30% lower.", "シャッターのダメージが30%減ります。", "快门伤害降低30%。");
        PA("무리 연쇄", "Crowd chains", "群れの連鎖", "敌群连锁");
        PA("보스 · 빠른 적", "Bosses · Fast foes", "ボス · 素早い敵", "首领 · 快速敌人");
        PA("종잇조각에 맞은 적의 종이 시간이 0.8초에서 1.5초로 늘어납니다.",
           "Foes hit by paper scraps stay paper for 1.5s instead of 0.8s.",
           "紙片に当たった敵の紙の時間が0.8秒から1.5秒に伸びます。",
           "被纸屑击中的敌人纸化时间从0.8秒延长到1.5秒。");
        PA("종이 적 다섯 이상이 3칸 안에 모이면 「종이 더미」로 접혀 터집니다. (공격력 200% · 둘레 4칸 안 보스에게는 그 50%)",
           "When 5 or more paper foes gather within 3 tiles, they fold into a Paper Pile and burst. (200% Attack · bosses within 4 tiles take 50% of it)",
           "紙の敵が5体以上3マス以内に集まると「紙の山」に折りたたまれて爆発します。（攻撃力200% · 周囲4マス以内のボスにはその50%）",
           "纸化敌人有五个以上聚在3格内时会折成「纸堆」并爆开。（攻击力200% · 周围4格内的首领受到其50%）");
        PA("종잇조각 수가 2배가 되고, 조각이 맞은 적에게서 한 번 더 튑니다.",
           "Paper scraps double in number, and they bounce once more off the foe they hit.",
           "紙片の数が2倍になり、紙片が当たった敵からもう一度跳ねます。",
           "纸屑数量翻倍，击中敌人后会再弹一次。");

        // ---------------- 플래시
        PA("플래시", "Flash", "フラッシュ", "闪光");
        PA("번쩍임으로 멈추고 지움 · 생존 · 탄막", "Freeze and erase with light · Survival · Bullet hell", "閃光で止めて消す · 生存 · 弾幕", "用闪光定住并清除 · 生存 · 弹幕");
        PA("스트로보", "Strobe", "ストロボ", "频闪");
        PA("셔터 순간 틀 바깥 1.5칸 테두리까지 번쩍여 그 안의 일반 적이 0.5초 멈추고(같은 적은 3초에 한 번), 틀 안의 적 투사체가 기본으로 지워집니다.",
           "The shutter flash reaches 1.5 tiles beyond the frame, stopping regular foes there for 0.5s (the same foe once per 3s), and enemy projectiles in the frame are erased by default.",
           "シャッターの瞬間にフレームの外1.5マスまで光り、その中の一般の敵が0.5秒止まり（同じ敵は3秒に1回）、フレーム内の敵の飛び道具が基本で消えます。",
           "快门瞬间闪光覆盖取景框外1.5格，范围内的普通敌人停住0.5秒（同一敌人每3秒一次），取景框内的敌方投射物默认被清除。");
        PA("셔터 간격이 25% 늘어납니다.", "The shutter interval is 25% longer.", "シャッター間隔が25%長くなります。", "快门间隔延长25%。");
        PA("생존 · 탄막 보스", "Survival · Bullet-hell bosses", "生存 · 弾幕ボス", "生存 · 弹幕首领");
        PA("화력", "Firepower", "火力", "输出");
        PA("틀 안에 내 몸을 넣고 찍으면 0.4초 동안 무적이 됩니다. (2초에 한 번)",
           "Shooting with yourself inside the frame makes you invulnerable for 0.4s. (Once per 2s)",
           "フレームの中に自分の体を入れて撮ると、0.4秒間無敵になります。（2秒に1回）",
           "把自己放进取景框里拍摄，会获得0.4秒无敌。（每2秒一次）");
        PA("플래시로 지운 투사체마다 파노라마 게이지가 1% 찹니다. (셔터 한 장에 최대 5%)",
           "Each projectile erased by the flash adds 1% to the Panorama gauge. (Up to 5% per shot)",
           "フラッシュで消した飛び道具1つにつきパノラマゲージが1%溜まります。（シャッター1回につき最大5%）",
           "每清除一个投射物，全景槽增加1%。（每次快门最多5%）");
        PA("멈춤이 1초가 되고, 테두리가 3칸으로 넓어집니다.",
           "The freeze lasts 1s and the flash border widens to 3 tiles.",
           "停止が1秒になり、縁が3マスに広がります。",
           "定住时间变为1秒，闪光边缘扩大到3格。");

        // ---------------- 새 교차 둘
        PA("종이 인형극", "Paper Puppet Show", "紙人形劇", "纸偶剧");
        PA("인화 사본이 종이 적처럼 펄럭이며 가까운 적에게 초당 1칸씩 다가가고, 닿으면 찢어집니다.",
           "Printed copies flutter like paper foes and creep toward nearby foes at 1 tile per second, tearing when they touch.",
           "現像コピーが紙の敵のようにひらひらしながら、近くの敵へ毎秒1マスずつ近づき、触れると破れます。",
           "冲印副本像纸化敌人一样飘动，每秒向附近敌人靠近1格，碰到就会撕裂。");
        PA("스트로보 연사", "Strobe Burst", "ストロボ連写", "频闪连拍");
        PA("세 번째 셔터마다 스트로보 테두리가 두 배 넓게 터집니다.",
           "Every third shutter, the strobe border bursts twice as wide.",
           "3回目のシャッターごとに、ストロボの縁が2倍の広さで炸裂します。",
           "每第三次快门，频闪边缘会以两倍宽度炸开。");

        // ---------------- 피해 출처 이름 (결과 화면 · 판 기록)
        PA("클로즈업", "Close-up", "クローズアップ", "特写");
        PA("종이 더미", "Paper Pile", "紙の山", "纸堆");
        PA("필름 끊김", "Film Snap", "フィルム切れ", "断片");
    }
}
