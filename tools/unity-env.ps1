# 프로젝트가 쓰는 Unity 버전(ProjectSettings/ProjectVersion.txt)에 맞는 에디터 경로 · 빌드 복사본 폴더 — 2026-10-23~ Unity 6 교체
#   빌드 · 캡처 · QA 스크립트가 점 소싱(. tools/unity-env.ps1)으로 씀: $UnityVersion · $UnityExe · $UnityCopyName
#   복사본은 엔진 버전마다 따로 (2022 = 「Project」 그대로 · 6000 = 「Project-6000」) — 두 엔진의 Library 가 섞이면 깨짐
$__root = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$__line = Get-Content (Join-Path $__root "ProjectSettings/ProjectVersion.txt") | Where-Object { $_ -match "^m_EditorVersion: (.+)$" } | Select-Object -First 1
$UnityVersion = ($__line -replace "^m_EditorVersion: ", "").Trim()
$UnityExe = "C:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
$UnityCopyName = if ($UnityVersion -like "2022.*") { "Project" } else { "Project-" + $UnityVersion.Split(".")[0] }
if (-not (Test-Path $UnityExe)) { throw "Unity $UnityVersion 가 설치되어 있지 않습니다: $UnityExe (Unity Hub 에서 설치)" }
