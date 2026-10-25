using UnityEngine;

// 번역표 (1.1.9~): 설정 「화면」 탭 프레임 설정 — 최대 프레임 · 수직 동기화 설명 · 프레임 표시 (GameSettings · FpsCounter)
// 이미 있는 키는 건드리지 않음 (키 중복 크래시 방지)
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddPerfBoot() { if (!SceneFlow.DeferStartup(5, AddPerf)) AddPerf(); }      // 1.6.1~ 켤 때는 로딩 화면이 뜬 뒤로 (큰 함수는 불릴 때 통째로 JIT 되므로 작은 함수에서 미룸 · SceneFlow)

    static void AddPerf()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        void S(string ko, string en, string ja, string zh) => C(ko, ko, en, ja, zh);

        S("최대 프레임", "Max Frame Rate", "最大フレームレート", "最大帧率");
        S("제한 없음", "Unlimited", "無制限", "无限制");
        S("1초에 그리는 화면 수의 상한입니다. 낮추면 컴퓨터 부담과 발열이 줄어듭니다. 수직 동기화를 켜면 모니터 주사율을 따릅니다.",
          "The maximum number of frames drawn per second. Lowering it reduces load and heat on your computer. With VSync on, the monitor's refresh rate is used instead.",
          "1秒間に描画する画面数の上限です。下げるとパソコンの負荷と発熱が減ります。垂直同期をオンにすると、モニターのリフレッシュレートに従います。",
          "每秒绘制画面数的上限。调低可减轻电脑负担与发热。开启垂直同步后将跟随显示器刷新率。");
        S("켜면 모니터 주사율에 맞춰 그려 화면 찢김이 사라집니다. 대신 입력이 조금 늦어질 수 있습니다.",
          "When on, frames are synced to your monitor's refresh rate to prevent screen tearing. Input may feel slightly delayed.",
          "オンにするとモニターのリフレッシュレートに合わせて描画し、画面のちらつき（ティアリング）がなくなります。その代わり、入力が少し遅れることがあります。",
          "开启后将按显示器刷新率绘制，消除画面撕裂。但输入可能会略有延迟。");
        S("프레임 표시", "Show FPS", "フレームレート表示", "显示帧率");
        S("화면 오른쪽 위 구석에 초당 프레임 수(FPS)를 작게 표시합니다.",
          "Shows the frames per second (FPS) in small text in the top-right corner of the screen.",
          "画面右上の隅に、1秒あたりのフレーム数（FPS）を小さく表示します。",
          "在屏幕右上角以小字显示每秒帧数（FPS）。");
    }
}
