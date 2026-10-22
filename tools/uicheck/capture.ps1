# UI 배치 확인용 스크린샷을 배치 모드로 찍는다 (tools/uicheck/UiCapture.cs)
# record.ps1 과 같은 프로젝트 복사본을 써서 에디터가 켜져 있어도 되고 원래 저장 데이터와 섞이지 않는다.
#
# 사용법: pwsh tools/uicheck/capture.ps1                          (16:9 1920x1080 · 4:3 1440x1080)
#         pwsh tools/uicheck/capture.ps1 -Sizes 1920x1080,1920x1200
# 결과:   %LOCALAPPDATA%\SoulSaverBuild\UiCapture\<가로x세로>\*.png
param(
    [string[]]$Sizes = @("1920x1080", "1440x1080"),
    [switch]$UiV2,                  # UI 개편 새 모습으로 찍기 (실행 인자 -uiv2 → UiRules.Legacy = false)
    [string]$Unity = ""          # 비우면 ProjectVersion.txt 의 버전 (tools/unity-env.ps1)
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "..\unity-env.ps1")
if (-not $Unity) { $Unity = $UnityExe }
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$Sizes = $Sizes -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ }

$Project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work $UnityCopyName
$OutRoot = Join-Path $Work "UiCapture"

New-Item -ItemType Directory -Force $CopyDir | Out-Null
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$capDir = Join-Path $CopyDir "Assets\UiCapture"
New-Item -ItemType Directory -Force $capDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "UiCapture.cs"), (Join-Path $PSScriptRoot "UiCapture.asmdef") $capDir
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
(Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver Capture" | Set-Content $settings -NoNewline

try {
    foreach ($s in $Sizes) {
        $w, $h = $s -split "x"
        $Out = Join-Path $OutRoot $s
        if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
        New-Item -ItemType Directory -Force $Out | Out-Null
        $env:CAPTURE_OUT = $Out
        $env:CAPTURE_W = $w
        $env:CAPTURE_H = $h
        $log = Join-Path $Work "uicapture.log"
        Write-Host "촬영 중: $s (로그: $log)"
        $p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList (@(
            "-batchmode", "-projectPath", "`"$CopyDir`"",
            "-runTests", "-testPlatform", "PlayMode", "-testFilter", "UiCapture",
            "-testResults", "`"$(Join-Path $Work 'uicapture-results.xml')`"",
            "-logFile", "`"$log`"") + $(if ($UiV2) { @("-uiv2") } else { @() }))
        $n = (Get-ChildItem $Out -Filter *.png).Count
        if ($n -eq 0) { Get-Content $log -Tail 40; throw "$s 스크린샷이 없습니다 (Unity 종료 코드 $($p.ExitCode))" }
        Write-Host "완료: $s 스크린샷 $n 장 → $Out"
    }
}
finally {
    # 복사본을 원래대로 (다음 빌드에 촬영 코드가 섞이지 않게)
    Remove-Item -Recurse -Force $capDir, "$capDir.meta" -ErrorAction SilentlyContinue
    robocopy (Join-Path $Project "ProjectSettings") (Join-Path $CopyDir "ProjectSettings") /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
}
