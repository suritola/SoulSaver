# 실제 게임 플레이 트레일러(MP4)와 스팀 스크린샷(PNG)을 배치 모드로 찍는다 (tools/trailer/GameplayCapture.cs)
# 에디터가 켜져 있어도 되도록 release.ps1 · steam-upload.ps1 과 같은 프로젝트 복사본에서 실행한다.
# 복사본은 제품 이름을 바꿔서, 스킨 장착 같은 촬영용 저장값이 원래 에디터 저장 데이터와 섞이지 않게 한다.
#
# 사용법: pwsh tools/trailer/record.ps1                            (영어 · 30fps · Medium 화질)
#         pwsh tools/trailer/record.ps1 -Lang en,ko,ja,zh -Quality High   (언어마다 한 번씩, 게임 언어 · 자막이 바뀜)
#         pwsh tools/trailer/record.ps1 -Mode highlight -Quality High     (2.2.1~ 자막 없는 하이라이트: 화려한 장면만 · 긴박한 음악 · 펀치 줌)
#         pwsh tools/trailer/record.ps1 -Mode reddit -Quality High        (레딧 홍보 15초: 거리를 두고 싸우며 필살기 · 보스 궁극기만)
# 결과:   %LOCALAPPDATA%\SoulSaverBuild\Capture\<언어>\SoulSaver_Trailer_<언어>.mp4 (하이라이트는 SoulSaver_Highlight.mp4), screenshots\*.png
param(
    [ValidateSet("trailer", "highlight", "reddit")][string]$Mode = "trailer",
    [string[]]$Lang = @("en"),              # en · ko · ja · zh (쉼표로 여러 개)
    [int]$Fps = 30,
    [ValidateSet("Low", "Medium", "High")][string]$Quality = "Medium",
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$Lang = $Lang -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ }
foreach ($l in $Lang) { if ($l -notin "en", "ko", "ja", "zh") { throw "지원하지 않는 언어: $l (en · ko · ja · zh)" } }

$Project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work "Project"
$CaptureRoot = Join-Path $Work "Capture"

New-Item -ItemType Directory -Force $CopyDir | Out-Null
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$capDir = Join-Path $CopyDir "Assets\Capture"
New-Item -ItemType Directory -Force $capDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "GameplayCapture.cs"), (Join-Path $PSScriptRoot "GameplayCapture.asmdef") $capDir
# 하이라이트 음악: 직접 합성한 웅장한 트레일러 음악 (make_trailer_music.py → Assets/Capture/Resources/TrailerMusic.wav, 복사본에만)
if ($Mode -ne "trailer") {
    $musicDir = Join-Path $capDir "Resources"
    New-Item -ItemType Directory -Force $musicDir | Out-Null
    & py -X utf8 (Join-Path $PSScriptRoot "make_trailer_music.py") (Join-Path $musicDir "TrailerMusic.wav")
    if ($LASTEXITCODE -ne 0) { throw "트레일러 음악 만들기 실패" }
}
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
(Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver Capture" | Set-Content $settings -NoNewline

$env:CAPTURE_FPS = "$Fps"
$env:CAPTURE_QUALITY = $Quality
foreach ($l in $Lang) {
    $Out = Join-Path $CaptureRoot $l
    if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
    New-Item -ItemType Directory -Force $Out | Out-Null
    $env:CAPTURE_OUT = $Out
    $env:CAPTURE_LANG = $l
    $log = Join-Path $Work "capture.log"
    Write-Host "촬영 중: $l ($Fps fps · $Quality, 로그: $log)"
    $p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
        "-batchmode", "-projectPath", "`"$CopyDir`"",
        "-runTests", "-testPlatform", "PlayMode", "-testFilter", $(if ($Mode -eq "highlight") { "GameplayCapture.HighlightReel" } elseif ($Mode -eq "reddit") { "GameplayCapture.RedditClip" } else { "GameplayCapture.Capture" }),
        "-testResults", "`"$(Join-Path $Work 'capture-results.xml')`"",
        "-logFile", "`"$log`"")
    Select-String -Path $log -Pattern "\[CAP\] done" | ForEach-Object { $_.Line }
    $mp4 = Join-Path $Out $(if ($Mode -eq "highlight") { "SoulSaver_Highlight.mp4" } elseif ($Mode -eq "reddit") { "SoulSaver_Reddit15.mp4" } else { "SoulSaver_Trailer_$l.mp4" })
    if (-not (Test-Path $mp4)) { Get-Content $log -Tail 40; throw "$l 영상이 만들어지지 않았습니다 (Unity 종료 코드 $($p.ExitCode))" }
    Write-Host ("완료: {0} ({1:N1} MB), 스크린샷 {2}장" -f $mp4, ((Get-Item $mp4).Length / 1MB), (Get-ChildItem (Join-Path $Out "screenshots") -ErrorAction SilentlyContinue).Count)
}

# 복사본을 원래대로 (다음 빌드에 촬영 코드가 섞이지 않게)
Remove-Item -Recurse -Force $capDir, "$capDir.meta" -ErrorAction SilentlyContinue
robocopy (Join-Path $Project "ProjectSettings") (Join-Path $CopyDir "ProjectSettings") /MIR /NFL /NDL /NJH /NJS /NP | Out-Null

