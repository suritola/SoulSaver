# 자동 플레이 봇으로 무한 모드를 배치 모드에서 끝까지 돌려 영웅 · 갈래별 클리어 시간을 잼 (tools/autoplay)
# 프로젝트를 %LOCALAPPDATA%\SoulSaverBuild\Autoplay 로 복사하고 (빌드 · QA 복사본과 따로), 봇 코드는 그 복사본에만 넣음
#   복사본에서만 고치는 줄: 초월 카드 · 궁수 숨 고르기의 Auto 생략을 되살림 · 쓰러질 때 AutoplayHooks 를 부름 (원본 Assets 는 그대로)
#
# 사용법:
#   pwsh tools/autoplay/run.ps1 -Hero Gunner -Path1 0 -Path2 single -Runs 2
#   pwsh tools/autoplay/run.ps1 -Hero Ghost -Path1 Frost -Path2 Phase -Skill 0.5
#   pwsh tools/autoplay/run.ps1 -Hero all -Path1 all -Path2 random -Runs 1 -OutName 2026-10-19-1차    (9명 × 갈래 5 = 45판)
#   pwsh tools/autoplay/run.ps1 -Hero Archer,Rogue -Path1 2 -NoDeath                                   (쓰러져도 계속 · 사망 횟수만 셈)
# 인자:
#   -Hero      영웅 이름 (Gunner Swordsman Rogue Archer Alchemist Cartographer Clockmaker Postman Ghost · 쉼표 · all)
#   -Path1     첫 갈래: 0~4 · 갈래 키(g.snipe) · 상수 이름(Snipe) · random · all(0~4 모두)
#   -Path2     두 번째 갈래: 0~4 · 키 · 이름 · single(외길) · random · all(첫 갈래와 다른 넷 + 외길)  /  -Single 은 -Path2 single 과 같음
#   -Runs      조합마다 판 수 · -Seed 첫 시드 (판마다 +1)
#   -Skill     봇 실력 0~1 (기본 0.5 = 보통 실력 플레이어: 반응 약 0.28초 · 새 위협 15% 못 봄 · 조준 흔들림 약 9°)
#   -TimeScale 게임 한 프레임 길이 배수 (1 = 1/60초 · 기본 3 = 1/20초). Time.captureDeltaTime 으로 고정 → 실제 속도는 기계가 낼 수 있는 최대 (판정 · 물리는 매 프레임 같은 간격)
#   -MaxMinutes 판 상한 (게임 분, 기본 45) · -Beyond 엄마 처치 뒤 한계 돌파에서 더 버틸 게임 초 (기본 0 = 바로 끝)
#   -NoDeath   쓰러져도 체력을 채우고 계속 (클리어 시간 · 사망 횟수를 함께 잼)
#   -OutName   회차 이름 (결과 폴더 · docs/balance/runs/<이름>/ 에 복사, 기본 날짜시각)
#   -Commit    (선택) 그 git 커밋의 Assets · Packages · ProjectSettings 로 복사본을 만듦 (기본: 지금 작업 폴더)
#   -NoCopyToDocs  docs/balance/runs 로 복사하지 않음 · -Trace 1초마다 봇 상태 줄 (progress.log, 조정용)
#   -Render    화면을 그림 (기본은 카메라가 아무것도 그리지 않게 해 빠르게 · 판정 · 조준에는 영향 없음)
# 결과: %LOCALAPPDATA%\SoulSaverBuild\AutoplayOut\<OutName>\ results.csv · errors.txt(예외 · 오류 전체) · progress.log · unity_errors.txt · unity.log
param(
    [string[]]$Hero = @("Gunner"),
    [string[]]$Path1 = @("random"),
    [string[]]$Path2 = @("random"),
    [switch]$Single,
    [int]$Runs = 1,
    [int]$Seed = 1000,
    [double]$Skill = 0.5,
    [double]$TimeScale = 3,
    [double]$MaxMinutes = 45,
    [double]$Beyond = 0,
    [switch]$NoDeath,
    [string]$OutName = "",
    [string]$Commit = "",
    [switch]$NoCopyToDocs,
    [switch]$Trace,
    [switch]$Render,
    [int]$HangSeconds = 150,
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\2022.3.28f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$inv = [Globalization.CultureInfo]::InvariantCulture
$Project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work "Autoplay"
if (-not $OutName) { $OutName = Get-Date -Format "yyyy-MM-dd_HHmm" }
$Out = Join-Path (Join-Path $Work "AutoplayOut") $OutName
New-Item -ItemType Directory -Force $Out | Out-Null

# ---------------------------------------------------------------- 계획 (영웅 × 갈래1 × 갈래2 × 판)
$AllHeroes = "Gunner", "Swordsman", "Rogue", "Archer", "Alchemist", "Cartographer", "Clockmaker", "Postman", "Ghost"
$heroes = @($Hero -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($heroes -contains "all") { $heroes = $AllHeroes }
foreach ($h in $heroes) { if ($h -notin $AllHeroes) { throw "모르는 영웅: $h" } }
$p1s = @($Path1 -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })
if ($p1s -contains "all") { $p1s = "0", "1", "2", "3", "4" }
if ($Single) { $Path2 = @("single") }
$p2In = @($Path2 -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$plan = New-Object System.Collections.Generic.List[string]
$s = $Seed
foreach ($h in $heroes) {
    foreach ($a in $p1s) {
        $p2s = $p2In
        if ($p2In -contains "all") { $p2s = @("0", "1", "2", "3", "4" | Where-Object { $_ -ne $a }) + "single" }
        foreach ($b in $p2s) {
            for ($r = 0; $r -lt $Runs; $r++) { $plan.Add("$h|$a|$b|$s"); $s++ }
        }
    }
}
Write-Host ("계획 {0}판 · 실력 {1} · 프레임 {2}/60초 · 상한 {3}분 · 결과 {4}" -f $plan.Count, $Skill, $TimeScale, $MaxMinutes, $Out)

# ---------------------------------------------------------------- 복사본 만들기
$Src = $Project
if ($Commit) {
    $Src = Join-Path $Work "AutoplaySrc"
    if (Test-Path $Src) { Remove-Item -Recurse -Force $Src }
    New-Item -ItemType Directory -Force $Src | Out-Null
    $tar = Join-Path $Work "autoplay_src.tar"
    git -C $Project archive --format=tar -o $tar $Commit Assets Packages ProjectSettings
    if ($LASTEXITCODE -ne 0) { throw "git archive 실패 ($Commit)" }
    tar -xf $tar -C $Src
    Remove-Item $tar
    Write-Host "커밋 $Commit 으로 복사본을 만듦"
}
New-Item -ItemType Directory -Force $CopyDir | Out-Null
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Src $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP /MT:8 | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$scripts = Join-Path $CopyDir "Assets\Scripts"
$botDir = Join-Path $scripts "Autoplay"
New-Item -ItemType Directory -Force $botDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "AutoplayRunner.cs"), (Join-Path $PSScriptRoot "AutoplayBot.cs"), (Join-Path $PSScriptRoot "AutoplayBot.Windows.cs"), (Join-Path $PSScriptRoot "AutoplayBot.World.cs") $botDir
$testDir = Join-Path $CopyDir "Assets\AutoplayTest"
New-Item -ItemType Directory -Force $testDir | Out-Null
Copy-Item (Join-Path $PSScriptRoot "AutoplayTest.cs"), (Join-Path $PSScriptRoot "AutoplayTest.asmdef") $testDir

function Patch($file, $from, $to) {
    $p = Join-Path $scripts $file
    $t = [IO.File]::ReadAllText($p)
    if (-not $t.Contains($from)) { Write-Warning "$file : 고칠 줄을 찾지 못함 (게임 코드가 바뀌었는지 확인) — $from"; return }
    [IO.File]::WriteAllText($p, $t.Replace($from, $to), (New-Object Text.UTF8Encoding($true)))
}
# 초월 카드: Auto · 배치에서 꺼지던 것을 봇 판에서는 실제처럼
Patch "LevelShop.Transcend.cs" "if (!TranscendLeft || TutorialRun.Active || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) return;" "if (!TranscendLeft || TutorialRun.Active || ((GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode) && !AutoplayFlags.Real)) return;"
Patch "LevelShop.TranscendPips.cs" "&& !TutorialRun.Active && !GameInput.Auto;" "&& !TutorialRun.Active && (!GameInput.Auto || AutoplayFlags.Real);"
# 궁수 숨 고르기: Auto 에서 숨이 닳지 않던 것을 실제처럼
Patch "CharacterKit.cs" "if (!GameInput.Auto && !GameInput.TrailerRunning) ConsumeBreath();" "if (AutoplayFlags.Real || (!GameInput.Auto && !GameInput.TrailerRunning)) ConsumeBreath();"
Patch "CharacterKit.Archer.cs" "if (Paths.Has(ArcherPaths.Wind) && !GameInput.Auto && !GameInput.TrailerRunning) ConsumeBreath();" "if (Paths.Has(ArcherPaths.Wind) && (AutoplayFlags.Real || (!GameInput.Auto && !GameInput.TrailerRunning))) ConsumeBreath();"
# 쓰러짐: 사망 횟수를 세고, -NoDeath 면 체력을 채우고 계속
Patch "PlayerController.cs" "if (PlayerHealth <= 0) StartCoroutine(DeathSequence());" "if (PlayerHealth <= 0 && !AutoplayHooks.KeepAlive(this)) StartCoroutine(DeathSequence());"
# 저장 파일 분리 (복사본 제품 이름 → %LOCALAPPDATA%\Soul Saver Autoplay_Editor\save.json)
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
(Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver Autoplay" | Set-Content $settings -NoNewline

# ---------------------------------------------------------------- 실행 (멈추면 끄고 남은 판부터 다시)
$env:AP_OUT = $Out
$env:AP_PLAN = ($plan -join ";")
$env:AP_SKILL = $Skill.ToString($inv)
$env:AP_STEP = ($TimeScale / 60.0).ToString($inv)
$env:AP_MAXMIN = $MaxMinutes.ToString($inv)
$env:AP_BEYOND = $Beyond.ToString($inv)
$env:AP_NODEATH = $(if ($NoDeath) { "1" } else { "0" })
$env:AP_TRACE = $(if ($Trace) { "1" } else { "0" })
$env:AP_RENDER = $(if ($Render) { "1" } else { "0" })
$csv = Join-Path $Out "results.csv"
$startRows = if (Test-Path $csv) { (Get-Content $csv | Measure-Object -Line).Lines - 1 } else { 0 }
if ($startRows -lt 0) { $startRows = 0 }
$unityLog = Join-Path $Out "unity.log"
$restarts = 0
$compileRetry = 0
while ($true) {
    $done = if (Test-Path $csv) { [Math]::Max(0, (Get-Content $csv | Measure-Object -Line).Lines - 1) - $startRows } else { 0 }
    if ($done -ge $plan.Count) { break }
    $env:AP_SKIP = "$done"
    $log = Join-Path $Out ("unity_{0}.log" -f $restarts)
    Write-Host ("Unity 실행 ({0}/{1}판부터 · 로그 {2})" -f ($done + 1), $plan.Count, $log)
    $p = Start-Process $Unity -PassThru -NoNewWindow -ArgumentList @(
        "-batchmode", "-projectPath", "`"$CopyDir`"",
        "-runTests", "-testPlatform", "PlayMode", "-testFilter", "AutoplayTest",
        "-testResults", "`"$(Join-Path $Out 'test-results.xml')`"",
        "-logFile", "`"$log`"")
    $lastSize = -1; $lastChange = Get-Date; $hung = $false; $botStarted = $false
    while (-not $p.HasExited) {
        Start-Sleep -Seconds 5
        $size = if (Test-Path $log) { (Get-Item $log).Length } else { 0 }
        # 봇이 시작하기 전(가져오기 · 컴파일 · 셰이더)은 로그 없이 오래 걸릴 수 있어 15분까지 기다림 · 봇이 돌면 20초마다 [AP] 줄
        if (-not $botStarted -and $size -gt 0) { $botStarted = [bool](Select-String -Path $log -Pattern "\[AP\]" -SimpleMatch:$false -Quiet) }
        $limit = if ($botStarted) { $HangSeconds } else { 900 }
        if ($size -ne $lastSize) { $lastSize = $size; $lastChange = Get-Date }
        elseif (((Get-Date) - $lastChange).TotalSeconds -gt $limit) {
            Write-Warning "Unity 가 $limit 초 동안 로그 없이 멈춤 → 끄고 다시"
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
            $hung = $true
            break
        }
    }
    if (Test-Path $log) {
        $compile = Select-String -Path $log -Pattern "error CS\d+" | Select-Object -First 10
        if ($compile) {
            # 새로 넣은 봇 파일이 Unity 의 첫 컴파일 목록(지난 Library)에 아직 없어서 나는 오류는 한 번 더 켜면 사라짐
            if ($compileRetry -lt 1) { $compileRetry++; $restarts++; Write-Host "첫 컴파일이 옛 파일 목록으로 돌아 실패 → 한 번 더 켬"; continue }
            $compile | ForEach-Object { Write-Host $_.Line }; throw "봇 코드 컴파일 실패 (로그 $log)"
        }
    }
    $doneNow = if (Test-Path $csv) { [Math]::Max(0, (Get-Content $csv | Measure-Object -Line).Lines - 1) - $startRows } else { 0 }
    if ($doneNow -ge $plan.Count) { break }
    if ($doneNow -eq $done) {
        # 한 판도 못 끝냄: 그 판을 「Unity멈춤」으로 적고 다음 판으로
        $c = $plan[$done].Split("|")
        $fresh = -not (Test-Path $csv)
        if ($fresh) { Set-Content $csv "time,hero,path1,path2,skill,seed,result" -Encoding utf8 }
        Add-Content $csv ("{0},{1},{2},{3},{4},{5},{6}" -f (Get-Date -Format "yyyy-MM-dd HH:mm"), $c[0], $c[1], $c[2], $Skill.ToString($inv), $c[3], $(if ($hung) { "Unity멈춤" } else { "Unity종료" })) -Encoding utf8
    }
    $restarts++
    if ($restarts -gt $plan.Count + 3) { Write-Warning "다시 켜기가 너무 많아 멈춤"; break }
}

# ---------------------------------------------------------------- 정리 · 기록 복사
Get-ChildItem $Out -Filter "unity_*.log" | ForEach-Object {
    Select-String -Path $_.FullName -Pattern "Exception|^.*Error.*:|error CS" -Context 0, 4 | ForEach-Object { $_.Line; $_.Context.PostContext }
} | Set-Content (Join-Path $Out "unity_errors.txt") -Encoding utf8
Write-Host "결과:"
if (Test-Path $csv) { Import-Csv $csv | Select-Object -Last $plan.Count | Format-Table hero, path1, path2, result, end_time, four_bosses, dev_kill, mom_kill, level, deaths, exceptions -AutoSize | Out-String | Write-Host }
if (-not $NoCopyToDocs) {
    $docs = Join-Path $Project "docs\balance\runs\$OutName"
    New-Item -ItemType Directory -Force $docs | Out-Null
    foreach ($f in "results.csv", "errors.txt", "progress.log", "unity_errors.txt") {
        $src = Join-Path $Out $f
        if (Test-Path $src) { Copy-Item $src $docs -Force }
    }
    @("# 자동 플레이 회차 $OutName", "", "- 시각: $(Get-Date -Format 'yyyy-MM-dd HH:mm')", "- 게임 커밋: $(if ($Commit) { $Commit } else { (git -C $Project rev-parse --short HEAD) + ' (+ 작업 폴더 변경)' })",
      "- 영웅: $($heroes -join ', ') · 갈래1: $($p1s -join ', ') · 갈래2: $($p2In -join ', ') · 판: $Runs · 시드 $Seed~",
      "- 실력 $Skill · 프레임 $TimeScale/60초 · 상한 $MaxMinutes 분 · 불사 $([bool]$NoDeath) · 한계 돌파 $Beyond 초",
      "- 명령: pwsh tools/autoplay/run.ps1 $($MyInvocation.Line -replace '^.*?run\.ps1\s*', '')") | Set-Content (Join-Path $docs "README.md") -Encoding utf8
    Write-Host "기록 복사: $docs"
}
