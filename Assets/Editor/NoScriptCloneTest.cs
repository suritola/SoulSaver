using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 재발 방지 (1.2.0 렉 원인): 실행 중에 Instantiate 로 복제하는 씬 오브젝트 아래에 게임 스크립트(MonoBehaviour)가 붙어 있으면
//   복제마다 스크립트가 따라붙어 Start 가 또 복제하는 연쇄가 생길 수 있음 (PlayerHP 가 자기 자신을 프레임마다 복제했던 일)
// 검사:
//   1) GameScene 의 bossbar.backBar · StageManager.hellMap 아래에 게임 스크립트(Assembly-CSharp)가 0개 (UI 그림 · TMP 는 괜찮음)
//   2) Assets/Scripts 안에 자기 자신을 복제하는 코드(Instantiate(gameObject) · Instantiate(this) · Instantiate(transform))가 없음
// 메뉴: Soul Saver/검증/스크립트 복제 금지 검사 · 명령줄: -executeMethod NoScriptCloneTest.RunBatch (실패하면 종료 코드 1)
public static class NoScriptCloneTest
{
    const string ScenePath = "Assets/Prefabs/Scenes/GameScene.unity";

    [MenuItem("Soul Saver/검증/스크립트 복제 금지 검사")]
    static void RunMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        List<string> problems = Run();
        if (problems.Count == 0) EditorUtility.DisplayDialog("스크립트 복제 금지 검사", "통과", "확인");
        else EditorUtility.DisplayDialog("스크립트 복제 금지 검사", "실패\n\n" + string.Join("\n", problems), "확인");
    }

    // 빌드 전에 부름 (tools/*.ps1 에서 -executeMethod 로)
    public static void RunBatch()
    {
        List<string> problems = Run();
        foreach (string p in problems) Debug.LogError("[스크립트 복제 금지 검사] " + p);
        if (problems.Count > 0) EditorApplication.Exit(1);
        Debug.Log("[스크립트 복제 금지 검사] 통과");
    }

    public static List<string> Run()
    {
        List<string> problems = new List<string>();
        CheckScene(problems);
        CheckSources(problems);
        return problems;
    }

    static void CheckScene(List<string> problems)
    {
        if (!File.Exists(ScenePath)) { problems.Add("씬을 찾지 못함: " + ScenePath); return; }
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (bossbar b in root.GetComponentsInChildren<bossbar>(true))
                CheckSubtree(b.backBar, "bossbar.backBar", problems);
            foreach (StageManager sm in root.GetComponentsInChildren<StageManager>(true))
                CheckSubtree(FieldObject(sm, "hellMap"), "StageManager.hellMap", problems);
        }
    }

    static GameObject FieldObject(Object owner, string field)
    {
        FieldInfo f = owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return f != null ? f.GetValue(owner) as GameObject : null;
    }

    static void CheckSubtree(GameObject go, string label, List<string> problems)
    {
        if (go == null) return;
        foreach (MonoBehaviour m in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (m == null) continue;
            string asm = m.GetType().Assembly.GetName().Name;
            if (asm != "Assembly-CSharp") continue;          // UI 그림 · TMP 등 엔진 · 패키지 컴포넌트는 괜찮음
            problems.Add(label + " 아래 " + m.gameObject.name + " 에 " + m.GetType().Name + " (복제마다 따라붙음)");
        }
    }

    static readonly Regex SelfClone = new Regex(@"Instantiate\s*\(\s*(gameObject|this|transform)\s*[,)]");

    static void CheckSources(List<string> problems)
    {
        string root = Path.Combine(Application.dataPath, "Scripts");
        if (!Directory.Exists(root)) return;
        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int comment = line.IndexOf("//", System.StringComparison.Ordinal);
                string code = comment >= 0 ? line.Substring(0, comment) : line;
                if (SelfClone.IsMatch(code))
                    problems.Add("자기 자신 복제: " + file.Substring(Application.dataPath.Length - 6).Replace('\\', '/') + ":" + (i + 1));
            }
        }
    }
}
