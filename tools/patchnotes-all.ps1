# 게임 안 패치노트 모음 (메인 메뉴 「패치노트」 버튼 · PatchNotesUI)
# docs/patch-notes/v<주>.<부>.<수>.md 를 최신 버전부터 한 파일로 묶음. 버전마다 "@@@ v2.1.2" 줄로 나눔
# 게임 안에는 플레이어용 짧은 노트 (docs/patch-notes/game/v*.md) 가 있으면 그것을 씀 — 개발 과정은 docs 쪽 자세한 노트에만
# 사용법: pwsh tools/patchnotes-all.ps1 [-Out <경로>]   (기본: Assets/Resources/PatchNotesAll.txt)
# steam-upload.ps1 · release.ps1 이 빌드할 때 복사본에도 새로 만들어 넣음
param([string]$Out)
$ErrorActionPreference = "Stop"
$Project = Split-Path $PSScriptRoot -Parent
if (-not $Out) { $Out = Join-Path $Project "Assets/Resources/PatchNotesAll.txt" }

$notes = Get-ChildItem (Join-Path $Project "docs/patch-notes") -Filter "v*.md" |
    Where-Object { $_.BaseName -match '^v\d+\.\d+\.\d+$' } |
    Sort-Object { [version]($_.BaseName.Substring(1)) } -Descending

$sb = [System.Text.StringBuilder]::new()
foreach ($n in $notes) {
    [void]$sb.Append("@@@ ").Append($n.BaseName).Append("`n")
    $src = Join-Path (Join-Path $n.DirectoryName "game") $n.Name
    if (-not (Test-Path $src)) { $src = $n.FullName }
    [void]$sb.Append((Get-Content $src -Raw -Encoding utf8).Replace("`r", "").TrimEnd()).Append("`n")
}
[System.IO.File]::WriteAllText($Out, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host ("패치노트 {0}개를 묶었습니다: {1}" -f $notes.Count, $Out)
