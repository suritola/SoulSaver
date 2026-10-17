using UnityEngine;

// 한 페이즈의 적 생성 규칙
[System.Serializable]
public class SpawnPhase
{
    public int killsToEnter = 0;        // 스테이지에서 이만큼 처치하면 이 페이즈로
    public float spawnInterval = 1.5f;  // 적 생성 간격(초)
    public int maxAlive = 10;           // 동시에 살아 있을 수 있는 최대 적 수
    public float[] weights;             // 적 종류별 등장 비율 (stage.enemies 순서)
    [System.NonSerialized] public float minSeconds;   // 무한 모드: 이 시각(초) 전에는 이 페이즈로 올라가지 않음 (EndlessMode 가 넣음 · 이야기 모드 0)
}

// 한 스테이지(맵)의 적 구성
[System.Serializable]
public class StageConfig
{
    public string stageName;
    public GameObject[] enemies;        // 약한 적부터 강한 적 순서
    public SpawnPhase[] phases;
    public int bossKills = 60;          // 이만큼 처치하면 보스 등장
    public GameObject bossPrefab;
    public float[] minionWeights;       // 보스가 부르는 부하 비율
    [System.NonSerialized] public float[] finalWeights;   // 무한 모드 (1.2.3~): 개발자 · 엄마와 싸우는 동안 나오는 약한 적 비율 (EndlessMode.WithEndlessStage 가 넣음 · 이야기 모드 null)
}
