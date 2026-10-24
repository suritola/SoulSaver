# QA 화면 확인을 배치 모드로 (tools/qa/QaCapture.cs) — record.ps1 과 같은 프로젝트 복사본에서 실행
# 복사본에만: QaFlags.cs 를 넣고 RunSave(저장) · StageManager(보스 보상 창)의 배치 모드 생략을 QaFlags 로 풀 수 있게 고침
# 사용법: pwsh tools/qa/qa.ps1                       (모든 장면)
#         pwsh tools/qa/qa.ps1 -Only mom_domain,ui_menu
#         pwsh tools/qa/qa.ps1 -Only readability       (가독성 명도 · 밀도 → py -3.14 -X utf8 tools/qa/luma_ab.py "%LOCALAPPDATA%\SoulSaverBuild\Qa")
# 결과:   %LOCALAPPDATA%\SoulSaverBuild\Qa\qa_log.txt · *.png
param(
    [string]$Only = "",
    [string]$Unity = ""          # 비우면 ProjectVersion.txt 의 버전 (tools/unity-env.ps1)
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "..\unity-env.ps1")
if (-not $Unity) { $Unity = $UnityExe }
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$Project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work $UnityCopyName
$Out = Join-Path $Work "Qa"

New-Item -ItemType Directory -Force $CopyDir | Out-Null
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$capDir = Join-Path $CopyDir "Assets\QaCapture"
New-Item -ItemType Directory -Force $capDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "QaCapture.cs"), (Join-Path $PSScriptRoot "QaCapture.asmdef") $capDir
$scripts = Join-Path $CopyDir "Assets\Scripts"
Copy-Item (Join-Path $PSScriptRoot "QaFlags.cs.txt") (Join-Path $scripts "QaFlags.cs")
function Patch($file, $from, $to) {
    $p = Join-Path $scripts $file
    $s = [IO.File]::ReadAllText($p)
    if (-not $s.Contains($from)) { throw "$file 고칠 줄을 찾지 못함" }
    [IO.File]::WriteAllText($p, $s.Replace($from, $to), (New-Object Text.UTF8Encoding($false)))
}
Patch "RunSave.cs" "&& !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode" "&& (QaFlags.AllowSave || (!GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode))"
Patch "StageManager.cs" "if (TutorialRun.Active || Demo.On || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) { RunSave.NeedBossReward = false; yield break; }" "if (!QaFlags.AllowReward && (TutorialRun.Active || Demo.On || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode)) { RunSave.NeedBossReward = false; yield break; }"
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
(Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver QA" | Set-Content $settings -NoNewline

if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
New-Item -ItemType Directory -Force $Out | Out-Null
$env:QA_OUT = $Out
$env:QA_ONLY = $Only
$log = Join-Path $Work "qa.log"
Write-Host "QA 실행 중 (로그: $log · 결과: $Out)"
try {
    $p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
        "-batchmode", "-projectPath", "`"$CopyDir`"",
        "-runTests", "-testPlatform", "PlayMode", "-testFilter", "QaCapture",
        "-testResults", "`"$(Join-Path $Work 'qa-results.xml')`"",
        "-logFile", "`"$log`"")
    Write-Host ("끝 (Unity 종료 코드 {0}) · 스크린샷 {1}장" -f $p.ExitCode, (Get-ChildItem $Out -Filter *.png).Count)
}
finally {
    # 복사본을 원래대로 (다음 빌드에 QA 코드 · 고친 줄이 섞이지 않게)
    Remove-Item -Recurse -Force $capDir, "$capDir.meta", (Join-Path $scripts "QaFlags.cs"), (Join-Path $scripts "QaFlags.cs.meta") -ErrorAction SilentlyContinue
    robocopy (Join-Path $Project "Assets\Scripts") $scripts /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    robocopy (Join-Path $Project "ProjectSettings") (Join-Path $CopyDir "ProjectSettings") /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
}
