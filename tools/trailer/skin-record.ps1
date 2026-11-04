# 스킨 상점 미리보기 무대 녹화 (tools/trailer/SkinCapture.cs) — 영웅 하나의 기본 · 캐릭터 · 무기 스킨을 차례로, 소리 포함 MP4
# 사용법: pwsh tools/trailer/skin-record.ps1 -Hero Gunner [-Secs 13.5]
# 결과:   %LOCALAPPDATA%\SoulSaverBuild\Capture\skins\skins_<영웅>_라벨.mp4 (스킨 이름 자막) · skins_<영웅>.txt (구간표)
# compile-check.ps1 과 같은 복사본을 쓰므로 같은 잠금 파일을 기다림
param([string]$Hero = "Gunner", [double]$Secs = 13.5)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "..\unity-env.ps1")
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$Project = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work $UnityCopyName
$Out = Join-Path $Work "Capture\skins"
New-Item -ItemType Directory -Force $CopyDir, $Out | Out-Null
$lock = Join-Path $Work "compile.lock"
while ($true) {
    try { $fs = [System.IO.File]::Open($lock, "CreateNew", "Write", "None"); $fs.Close(); break }
    catch {
        $age = (Get-Date) - (Get-Item $lock -ErrorAction SilentlyContinue).LastWriteTime
        if ($age.TotalMinutes -gt 40) { Remove-Item -Force $lock -ErrorAction SilentlyContinue; continue }
        Write-Host "다른 Unity 작업이 끝나기를 기다리는 중..."
        Start-Sleep -Seconds 20
    }
}
$capDir = Join-Path $CopyDir "Assets\SkinCapture"
$settings = Join-Path $CopyDir "ProjectSettings\ProjectSettings.asset"
try {
    foreach ($d in "Assets", "Packages", "ProjectSettings") {
        robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
    }
    New-Item -ItemType Directory -Force $capDir | Out-Null
    Copy-Item (Join-Path $PSScriptRoot "SkinCapture.cs"), (Join-Path $PSScriptRoot "SkinCapture.asmdef") $capDir
    # 촬영용 저장값이 원래 저장 데이터와 섞이지 않게 제품 이름을 바꿈
    (Get-Content $settings -Raw) -replace "(?m)^  productName: .*$", "  productName: Soul Saver Capture" | Set-Content $settings -NoNewline
    $env:SKINCAP_OUT = $Out; $env:SKINCAP_HERO = $Hero; $env:SKINCAP_SECS = "$Secs"
    $log = Join-Path $Work "skincap.log"
    Write-Host "녹화 중: $Hero (로그: $log)"
    $p = Start-Process $UnityExe -Wait -PassThru -NoNewWindow -ArgumentList @(
        "-batchmode", "-projectPath", "`"$CopyDir`"",
        "-runTests", "-testPlatform", "PlayMode", "-testFilter", "SkinCapture.Reel",
        "-testResults", "`"$(Join-Path $Work 'skincap-results.xml')`"",
        "-logFile", "`"$log`"")
} finally {
    Remove-Item -Recurse -Force $capDir, "$capDir.meta" -ErrorAction SilentlyContinue
    robocopy (Join-Path $Project "ProjectSettings") (Join-Path $CopyDir "ProjectSettings") /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    Remove-Item -Force $lock -ErrorAction SilentlyContinue
}
$mp4 = Join-Path $Out "skins_$Hero.mp4"
if (-not (Test-Path $mp4)) { Get-Content $log -Tail 40; throw "영상이 만들어지지 않았습니다 (Unity 종료 코드 $($p.ExitCode))" }

# 스킨 이름 자막 (구간표 → ffmpeg drawtext, 맑은 고딕)
$ff = (Get-ChildItem "$env:LOCALAPPDATA\Programs\Python" -Recurse -Filter "ffmpeg-win*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
$marks = Get-Content (Join-Path $Out "skins_$Hero.txt") -Encoding UTF8
$filters = @()
for ($i = 0; $i -lt $marks.Count; $i++) {
    $a, $label = $marks[$i] -split "`t", 2
    $b = if ($i + 1 -lt $marks.Count) { ($marks[$i + 1] -split "`t")[0] } else { "9999" }
    $txt = Join-Path $Out "label_$i.txt"
    [IO.File]::WriteAllText($txt, $label, (New-Object Text.UTF8Encoding $false))
    $tp = ($txt -replace "\\", "/") -replace ":", "\:"
    $filters += "drawtext=fontfile='C\:/Windows/Fonts/malgunbd.ttf':textfile='$tp':fontsize=46:fontcolor=white:box=1:boxcolor=black@0.6:boxborderw=14:x=40:y=40:enable='between(t,$a,$b)'"
}
$final = Join-Path $Out "skins_${Hero}_라벨.mp4"
& $ff -y -loglevel error -i $mp4 -vf ($filters -join ",") -c:v libx264 -crf 23 -preset medium -c:a aac -b:a 160k $final
if ($LASTEXITCODE -ne 0) { throw "자막 넣기 실패" }
Write-Host ("완료: {0} ({1:N1} MB)" -f $final, ((Get-Item $final).Length / 1MB))
