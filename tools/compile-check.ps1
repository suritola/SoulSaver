# 컴파일만 확인 (빌드 없이) — 빌드 복사본(release.ps1 와 같은 폴더 · Library 재사용)에 Assets 등을 맞춰 넣고 Unity 를 배치 모드로 열었다 닫음
#   에디터를 켜지 않고도 C# 오류를 잡음 · 오류가 있으면 「error CS」 줄을 보여 주고 종료 코드 1
#   실행: pwsh tools/compile-check.ps1 [-TimeoutMin 25]
param([int]$TimeoutMin = 25)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "unity-env.ps1")
$Project = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work $UnityCopyName
New-Item -ItemType Directory -Force $CopyDir | Out-Null
# 여러 작업자가 동시에 돌려도 한 번에 하나만 (같은 복사본을 Unity 둘이 열면 충돌) — 잠금 파일을 기다림, 40분 넘은 잠금은 버려진 것으로 봄
$lock = Join-Path $Work "compile.lock"
while ($true) {
    try { $fs = [System.IO.File]::Open($lock, "CreateNew", "Write", "None"); $fs.Close(); break }
    catch {
        $age = (Get-Date) - (Get-Item $lock -ErrorAction SilentlyContinue).LastWriteTime
        if ($age.TotalMinutes -gt 40) { Remove-Item -Force $lock -ErrorAction SilentlyContinue; continue }
        Write-Host "다른 컴파일 확인이 끝나기를 기다리는 중..."
        Start-Sleep -Seconds 20
    }
}
try {
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
$log = Join-Path $Work "compile.log"
if (Test-Path $log) { Remove-Item -Force $log }
Write-Host "컴파일 확인 중... (로그: $log)"
$p = Start-Process $UnityExe -PassThru -NoNewWindow -ArgumentList @("-batchmode", "-quit", "-nographics", "-projectPath", "`"$CopyDir`"", "-logFile", "`"$log`"")
if (-not $p.WaitForExit($TimeoutMin * 60 * 1000)) {
    Stop-Process -Id $p.Id -Force
    Write-Host "시간 초과 ($TimeoutMin 분) — Unity 를 끊음"
    exit 2
}
$errors = Select-String -Path $log -Pattern "error CS\d+" | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
if ($errors) {
    Write-Host "컴파일 오류 $($errors.Count)개:"
    $errors | ForEach-Object { Write-Host "  $_" }
    exit 1
}
if ($p.ExitCode -ne 0) { Get-Content $log -Tail 30; Write-Host "Unity 종료 코드 $($p.ExitCode)"; exit $p.ExitCode }
Write-Host "컴파일 통과"
} finally { Remove-Item -Force $lock -ErrorAction SilentlyContinue }
