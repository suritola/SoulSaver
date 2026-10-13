# 스팀 상점 스크린샷(1920×1080 PNG)을 배치 모드로 찍는다 (tools/steam/store-art/StoreShots.cs)
# 메인 프로젝트 · 빌드용 복사본(%LOCALAPPDATA%\SoulSaverBuild\Project)을 건드리지 않도록 따로 복사본을 만들어 쓴다.
# 복사본은 제품 이름을 「Soul Saver StoreShots」로 바꿔서 촬영용 저장값(영웅 해금 등)이 실제 저장과 섞이지 않게 한다.
#
# 사용법: pwsh tools/steam/store-art/shoot.ps1                         (4개 언어 · 모든 장면)
#         pwsh tools/steam/store-art/shoot.ps1 -Lang en -Only 01,06      (영어 · 01 · 06 장면만, 빨리 확인)
#         pwsh tools/steam/store-art/shoot.ps1 -Publish                  (찍은 뒤 docs/steam/art 로 옮김: 정식판 + 체험판)
# 결과:   <Work>\Shots\<언어>\*.png
param(
    [string[]]$Lang = @("en", "ko", "ja", "zh"),
    [string]$Only = "",
    [string]$Work = (Join-Path $env:LOCALAPPDATA "SoulSaverStoreShots"),
    [switch]$Publish,
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$Lang = $Lang -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ }

$Project = Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent
$CopyDir = Join-Path $Work "Project"
$ShotDir = Join-Path $Work "Shots"
if ($CopyDir -like "*SoulSaverBuild\Project*") { throw "빌드용 복사본은 쓰지 않습니다: $CopyDir" }

New-Item -ItemType Directory -Force $CopyDir | Out-Null
$dirs = @("Assets", "Packages", "ProjectSettings")
if (-not (Test-Path (Join-Path $CopyDir "Library"))) { $dirs += "Library" }     # 처음 한 번만 (가져오기 시간을 줄임)
foreach ($d in $dirs) {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP /XD "Assets\StoreShots" | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$capDir = Join-Path $CopyDir "Assets\StoreShots"
New-Item -ItemType Directory -Force $capDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "StoreShots.cs"), (Join-Path $PSScriptRoot "StoreShots.asmdef") $capDir -Force
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
(Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver StoreShots" | Set-Content $settings -NoNewline

if (Test-Path $ShotDir) { Remove-Item -Recurse -Force $ShotDir }
$env:SHOTS_OUT = $ShotDir
$env:SHOTS_LANGS = ($Lang -join ",")
$env:SHOTS_ONLY = $Only
$log = Join-Path $Work "shots.log"
Write-Host "촬영 중: $($Lang -join ', ') (로그: $log)"
$p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-projectPath", "`"$CopyDir`"",
    "-runTests", "-testPlatform", "PlayMode", "-testFilter", "StoreShots.Shoot",
    "-testResults", "`"$(Join-Path $Work 'shots-results.xml')`"",
    "-logFile", "`"$log`"")
$n = (Get-ChildItem $ShotDir -Recurse -Filter *.png -ErrorAction SilentlyContinue).Count
if ($n -eq 0) { Get-Content $log -Tail 60; throw "스크린샷이 없습니다 (Unity 종료 코드 $($p.ExitCode))" }
Write-Host "완료: $n 장 → $ShotDir"

if ($Publish) {
    # 영어가 기본 파일 이름, 나머지는 _koreana · _japanese · _schinese (스팀 언어 이름)
    $suffix = @{ en = ""; ko = "_koreana"; ja = "_japanese"; zh = "_schinese" }
    $full = Join-Path $Project "docs\steam\art\screenshots"
    $demo = Join-Path $Project "docs\steam\art\demo\screenshots"
    New-Item -ItemType Directory -Force $full, $demo | Out-Null
    # 체험판 범위(1장 지하 묘역 · 거너 · 검사)에 맞는 정식판 장면 + 체험판 전용 장면
    $demoFromFull = @("03_weapon_evolution", "04_skill_evolution", "05_soul_tree")
    foreach ($l in $Lang) {
        foreach ($f in Get-ChildItem (Join-Path $ShotDir $l) -Filter *.png) {
            $base = $f.BaseName
            if ($base -like "demo_*") {
                Copy-Item $f.FullName (Join-Path $demo ($base.Substring(5) + $suffix[$l] + ".png")) -Force
            } else {
                Copy-Item $f.FullName (Join-Path $full ($base + $suffix[$l] + ".png")) -Force
                if ($demoFromFull -contains $base) { Copy-Item $f.FullName (Join-Path $demo ($base + $suffix[$l] + ".png")) -Force }
            }
        }
    }
    Write-Host "옮김: $full , $demo"
}
