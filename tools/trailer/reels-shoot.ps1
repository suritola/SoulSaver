# 릴스 · 쇼츠용 세로 컷을 테이크 단위로 찍는다 (tools/trailer/GameplayCapture.Reels.cs · 프로젝트 복사본에서 배치 실행)
#   영상 · 소리를 섞지 않고 컷마다 <컷>_s<씨앗>.mkv · .cues.jsonl · .trace.csv · .meta.json 만 남긴다. 편집은 tools/trailer/post/build.py
#
# 사용법: pwsh tools/trailer/reels-shoot.ps1 -Cuts "aim2:1,aim2:2,horde_meadow:1"            (컷:씨앗, 쉼표로 여러 개)
#         pwsh tools/trailer/reels-shoot.ps1 -Cuts "aim2:1" -Scale 0.5 -Out C:\Temp\ss_test   (절반 크기로 먼저 보기)
# 결과:   %LOCALAPPDATA%\SoulSaverBuild\Reels\takes\  (-Out 으로 바꿈)
# 이 PC(i7-8565U · 내장 그래픽)에서는 다른 무거운 작업(ffmpeg 인코딩 · Unity)과 같이 돌리지 않는다.
param(
    [Parameter(Mandatory = $true)][string]$Cuts,
    [double]$Scale = 1.0,
    [string]$Out = "",
    [string]$Lang = "ko",
    [string]$Unity = ""
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "..\unity-env.ps1")
if (-not $Unity) { $Unity = $UnityExe }
[Console]::OutputEncoding = [Text.Encoding]::UTF8

$Project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work $UnityCopyName
if (-not $Out) { $Out = Join-Path $Work "Reels\takes" }
New-Item -ItemType Directory -Force $Out, $CopyDir | Out-Null

$ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ffmpeg) { $ffmpeg = (Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Gyan.FFmpeg*\*\bin\ffmpeg.exe" | Select-Object -Last 1).FullName }
if (-not $ffmpeg) { throw "ffmpeg 를 찾지 못했습니다 (winget install Gyan.FFmpeg)" }

foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$capDir = Join-Path $CopyDir "Assets\Capture"
New-Item -ItemType Directory -Force $capDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "GameplayCapture.cs"), (Join-Path $PSScriptRoot "GameplayCapture.Reels.cs"), (Join-Path $PSScriptRoot "GameplayCapture.asmdef") $capDir
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
(Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver Capture" | Set-Content $settings -NoNewline

$env:CAPTURE_FPS = "60"
$env:CAPTURE_OUT = $Out
$env:CAPTURE_LANG = $Lang
$env:CAPTURE_CUTS = $Cuts
$env:CAPTURE_SCALE = $Scale.ToString([Globalization.CultureInfo]::InvariantCulture)
$env:CAPTURE_FFMPEG = $ffmpeg
$log = Join-Path $Work "reels.log"
Write-Host "촬영 중: $Cuts (배율 $Scale, 로그: $log)"
$p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-projectPath", "`"$CopyDir`"",
    "-runTests", "-testPlatform", "PlayMode", "-testFilter", "GameplayCapture.ReelsShoot",
    "-testResults", "`"$(Join-Path $Work 'reels-results.xml')`"",
    "-logFile", "`"$log`"")
Select-String -Path $log -Pattern "\[CAP\] (cut|컷 실패|모르는|done)|error CS" | ForEach-Object { $_.Line }

# 복사본을 원래대로 (다음 빌드에 촬영 코드가 섞이지 않게)
Remove-Item -Recurse -Force $capDir, "$capDir.meta" -ErrorAction SilentlyContinue
robocopy (Join-Path $Project "ProjectSettings") (Join-Path $CopyDir "ProjectSettings") /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
Write-Host "끝 (Unity 종료 코드 $($p.ExitCode)) → $Out"
