# 윈도우 빌드를 만들어 비공개 저장소(myFirst2DGame-builds)의 Release로 올린다.
# 에디터가 켜져 있어도 되도록 프로젝트 복사본에서 빌드한다.
#
# 사용법: pwsh tools/release.ps1            (버전 자동 증가: v1.0 -> v1.1 ...)
#         pwsh tools/release.ps1 -Version v1.7.7   (버전은 AGENT.md 규칙 4, 빼면 지난 버전 +1 로 자동)
#         pwsh tools/release.ps1 -NotesFile docs/patch-notes/v1.0.md
param(
    [string]$Version,
    [string]$NotesFile,     # 패치노트 파일 (없으면 커밋 목록으로 자동 작성)
    [string]$Repo = "suritola/myFirst2DGame-builds",
    [string]$Unity = ""          # 비우면 ProjectVersion.txt 의 버전 (tools/unity-env.ps1)
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "unity-env.ps1")
if (-not $Unity) { $Unity = $UnityExe }

$Project = Split-Path $PSScriptRoot -Parent
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work $UnityCopyName
$OutDir = Join-Path $Work "Out"
$Gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
if (-not $Gh) { $Gh = "C:\Program Files\GitHub CLI\gh.exe" }

# ---------------------------------------------------------------- 버전과 변경 내역
$commit = (git -C $Project rev-parse --short HEAD).Trim()
$prevTag = $null
$prevCommit = $null
$latest = & $Gh release list -R $Repo --limit 1 --json tagName --jq ".[0].tagName" 2>$null
if ($LASTEXITCODE -eq 0 -and $latest) {
    $prevTag = $latest.Trim()
    # 여러 줄 설명은 줄 배열로 오므로 하나로 합침
    $body = (& $Gh release view $prevTag -R $Repo --json body --jq ".body") -join "`n"
    if ($body -match "소스 커밋: ([0-9a-f]+)") { $prevCommit = $Matches[1] }
}
if (-not $Version) {
    # 버전: 주.부.수 — 지난 릴리즈 +1, 끝자리가 9를 넘으면 부 +1, 부가 9를 넘으면 주 +1 (AGENT.md 규칙 4)
    $major = 1; $minor = 7; $patch = 5
    if ($prevTag -match "^v(\d+)\.(\d+)\.(\d+)$") { $major = [int]$Matches[1]; $minor = [int]$Matches[2]; $patch = [int]$Matches[3] }
    $patch++
    if ($patch -gt 9) { $patch = 0; $minor++ }
    if ($minor -gt 9) { $minor = 0; $major++ }
    $Version = "v$major.$minor.$patch"
}
if ($prevCommit) { $changes = git -C $Project log --pretty="- %s" "$prevCommit..HEAD" }
else { $changes = git -C $Project log --pretty="- %s" -n 15 }
if ($NotesFile) { $notes = "소스 커밋: $commit`n`n" + (Get-Content $NotesFile -Raw -Encoding utf8) }
else { $notes = "소스 커밋: $commit`n`n## 변경 내역`n" + ($changes -join "`n") }
Write-Host "버전 $Version (커밋 $commit)"

# ---------------------------------------------------------------- 프로젝트 복사 (Library는 재사용해서 빠르게)
New-Item -ItemType Directory -Force $CopyDir | Out-Null
foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy (Join-Path $Project $d) (Join-Path $CopyDir $d) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "$d 복사 실패 (robocopy $LASTEXITCODE)" }
}
# 게임 안 패치노트 (메인 메뉴에서 켤 때마다 띄움): 이 버전의 패치노트를 넣음
$patchNotes = Join-Path $Project "docs/patch-notes/$Version.md"
# 게임 안 패치노트는 플레이어용 짧은 노트 (docs/patch-notes/game) — 없으면 자세한 노트
$gameNotes = Join-Path $Project "docs/patch-notes/game/$Version.md"
if (Test-Path $gameNotes) { $patchNotes = $gameNotes }
if (Test-Path $patchNotes) { & py (Join-Path $PSScriptRoot "patchnotes_steam.py") strip $patchNotes (Join-Path $CopyDir "Assets/Resources/PatchNotes.txt"); if ($LASTEXITCODE -ne 0) { throw "패치노트 복사 실패" } }     # 게임 글꼴에 없는 이모지 빼고
# 메인 메뉴 「패치노트」 버튼: 지난 버전까지 모두 (tools/patchnotes-all.ps1)
& (Join-Path $PSScriptRoot "patchnotes-all.ps1") -Out (Join-Path $CopyDir "Assets/Resources/PatchNotesAll.txt")

# ---------------------------------------------------------------- 빌드
if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
$log = Join-Path $Work "build.log"
Write-Host "Unity 빌드 중... (로그: $log)"
$p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
    "-batchmode", "-quit", "-projectPath", "`"$CopyDir`"",
    "-executeMethod", "BuildScript.BuildWindows",
    "-buildOutput", "`"$OutDir`"", "-buildVersion", $Version.TrimStart("v"),
    "-logFile", "`"$log`"")
if ($p.ExitCode -ne 0 -or -not (Get-ChildItem $OutDir -Filter *.exe -ErrorAction SilentlyContinue)) {
    Get-Content $log -Tail 40
    throw "빌드 실패 (종료 코드 $($p.ExitCode))"
}

# ---------------------------------------------------------------- 압축 (디버그 파일 제외)
Get-ChildItem $OutDir -Directory -Filter "*DoNotShip*" | Remove-Item -Recurse -Force
$zip = Join-Path $Work "SoulSaver-$Version-Windows.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path (Join-Path $OutDir "*") -DestinationPath $zip
Write-Host ("압축 완료: {0:N1} MB" -f ((Get-Item $zip).Length / 1MB))

# ---------------------------------------------------------------- 업로드
& $Gh release create $Version $zip -R $Repo --title "Soul Saver $Version" --notes $notes
if ($LASTEXITCODE -ne 0) { throw "Release 업로드 실패" }
Write-Host "완료: https://github.com/$Repo/releases/tag/$Version"
