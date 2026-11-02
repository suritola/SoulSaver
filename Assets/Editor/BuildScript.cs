using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 명령줄 빌드용 (tools/release.ps1, tools/steam-upload.ps1 에서 호출)
// Unity.exe -batchmode -quit -projectPath <경로> -executeMethod BuildScript.BuildWindows -buildOutput <폴더> -buildVersion <버전> [-steam]
// -steam 이 있으면 스팀 연동을 켜고, 없으면 DISABLESTEAMWORKS 로 꺼서 스팀 없이 실행되게 함
public static class BuildScript
{
    const string IconPath = "Assets/Art/Icon/GunSaverIcon.png";
    const string NoSteam = "DISABLESTEAMWORKS";
    const string DemoDefine = "SOULSAVER_DEMO";
    const string TestUnlockDefine = "SOULSAVER_TESTUNLOCK";

    public static void BuildWindows()
    {
        string output = GetArg("-buildOutput") ?? "Builds/Windows";
        string version = GetArg("-buildVersion");
        if (!string.IsNullOrEmpty(version)) PlayerSettings.bundleVersion = version;

        bool steam = Environment.GetCommandLineArgs().Contains("-steam");
        // -demo: 체험판 (Demo.cs). 저장 데이터 · 실행 파일이 정식판과 섞이지 않게 제품 이름도 바꿈
        bool demo = Environment.GetCommandLineArgs().Contains("-demo");
        // -testUnlock: 개발자 시험 빌드 — 모든 영웅을 열린 것으로 봄 (저장은 건드리지 않음 · 정식 빌드 스크립트는 이 인자를 넘기지 않음)
        bool testUnlock = Environment.GetCommandLineArgs().Contains("-testUnlock");
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone)
            .Split(';').Where(d => d.Length > 0 && d != NoSteam && d != DemoDefine && d != TestUnlockDefine).ToList();
        if (!steam) defines.Add(NoSteam);
        if (testUnlock) defines.Add(TestUnlockDefine);
        Debug.Log("시험용 전체 해금: " + (testUnlock ? "켬" : "끔"));
        if (demo)
        {
            defines.Add(DemoDefine);
            PlayerSettings.productName = "Soul Saver Demo";
        }
        Debug.Log("체험판: " + (demo ? "켬" : "끔"));
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", defines));
        Debug.Log("스팀 연동: " + (steam ? "켬" : "끔"));

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = System.IO.Path.Combine(output, PlayerSettings.productName + ".exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("빌드 결과: " + report.summary.result + ", 크기: " + report.summary.totalSize + " bytes");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
