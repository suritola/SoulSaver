# 게임 안 패치노트 모음 (메인 메뉴 「패치노트」 버튼 · PatchNotesUI)
# 2026-10-18~ 스팀 게시판 공지와 똑같이: docs/steam/announcements 의 공지(tools/patchnotes_steam.py 의 POSTS)를 최신부터 한 파일로 묶음
#   (예전엔 docs/patch-notes/v*.md 빌드마다 하나씩이라 스팀 게시판과 단위 · 내용이 달랐음)
# 사용법: pwsh tools/patchnotes-all.ps1 [-Out <경로>]   (기본: Assets/Resources/PatchNotesAll.txt)
# steam-upload.ps1 · release.ps1 이 빌드할 때 복사본에도 새로 만들어 넣음
param([string]$Out)
$ErrorActionPreference = "Stop"
$Project = Split-Path $PSScriptRoot -Parent
if (-not $Out) { $Out = Join-Path $Project "Assets/Resources/PatchNotesAll.txt" }
& py (Join-Path $PSScriptRoot "patchnotes_steam.py") all $Out
if ($LASTEXITCODE -ne 0) { throw "패치노트 묶기 실패 (patchnotes_steam.py)" }
