# 스팀용 윈도우 빌드를 만들어 SteamPipe(steamcmd)로 올린다.
# 에디터가 켜져 있어도 되도록 release.ps1 과 같은 프로젝트 복사본에서 빌드한다.
#
# 준비: tools/steam/steam-config.json 에 appId · depotId · steamUser · steamcmd 경로를 채우고,
#       Assets/Scripts/SteamManager.cs 의 AppId 도 같은 값으로 맞춘다. (docs/steam/README.md)
#
# 사용법: pwsh tools/steam-upload.ps1 -Version v1.7.1             (빌드 + 업로드)
#         pwsh tools/steam-upload.ps1 -Version v1.7.1 -Preview    (업로드 없이 steamcmd 미리보기만)
#         pwsh tools/steam-upload.ps1 -Version v1.7.1 -SkipBuild  (마지막 빌드를 다시 올림)
#         pwsh tools/steam-upload.ps1 -Version v1.9.2 -Demo       (체험판: steam/steam-demo-config.json 의 앱 · 디포로)
param(
    [Parameter(Mandatory)][string]$Version,
    [switch]$Preview,
    [switch]$SkipBuild,
    [switch]$Demo,
    [string]$Unity = ""          # 비우면 ProjectVersion.txt 의 버전 (tools/unity-env.ps1)
)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "unity-env.ps1")
if (-not $Unity) { $Unity = $UnityExe }
[Console]::OutputEncoding = [Text.Encoding]::UTF8     # 한글 메시지가 깨지지 않게

$Project = Split-Path $PSScriptRoot -Parent
$Config = Get-Content (Join-Path $PSScriptRoot $(if ($Demo) { "steam\steam-demo-config.json" } else { "steam\steam-config.json" })) -Raw | ConvertFrom-Json
$Work = Join-Path $env:LOCALAPPDATA "SoulSaverBuild"
$CopyDir = Join-Path $Work $UnityCopyName
$OutDir = Join-Path $Work $(if ($Demo) { "SteamDemoOut" } else { "SteamOut" })
$ScriptDir = Join-Path $Work $(if ($Demo) { "SteamDemoScripts" } else { "SteamScripts" })

# ---------------------------------------------------------------- 설정 확인
if ($Config.appId -eq 0 -or $Config.depotId -eq 0) { throw "tools/steam/steam-config.json 에 appId 와 depotId 를 채우세요" }
if (-not $Config.steamUser) { throw "tools/steam/steam-config.json 에 steamUser (Steamworks 빌드 계정) 를 채우세요" }
$code = Get-Content (Join-Path $Project "Assets\Scripts\SteamManager.cs") -Raw
$idName = if ($Demo) { "DemoAppId" } else { "FullAppId" }
if ($code -notmatch "public const uint $idName = (\d+);" -or [uint32]$Matches[1] -ne [uint32]$Config.appId) {
    throw "SteamManager.cs 의 $idName 가 설정 파일의 appId ($($Config.appId)) 와 다릅니다"
}
if (-not (Test-Path $Config.steamcmd)) { throw "steamcmd 를 찾을 수 없습니다: $($Config.steamcmd) (https://developer.valvesoftware.com/wiki/SteamCMD 에서 받기)" }
$commit = (git -C $Project rev-parse --short HEAD).Trim()
Write-Host "Soul Saver $(if ($Demo) { '체험판 ' })$Version (커밋 $commit) → App $($Config.appId) / Depot $($Config.depotId)"

# ---------------------------------------------------------------- 빌드 (스팀 연동 켬)
if (-not $SkipBuild) {
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
    if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
    $log = Join-Path $Work $(if ($Demo) { "steam-demo-build.log" } else { "steam-build.log" })
    Write-Host "Unity 빌드 중... (로그: $log)"
    $p = Start-Process $Unity -Wait -PassThru -NoNewWindow -ArgumentList @(
        "-batchmode", "-quit", "-projectPath", "`"$CopyDir`"",
        "-executeMethod", "BuildScript.BuildWindows", "-steam", $(if ($Demo) { "-demo" } else { "-steam" }),
        "-buildOutput", "`"$OutDir`"", "-buildVersion", $Version.TrimStart("v"),
        "-logFile", "`"$log`"")
    if ($p.ExitCode -ne 0 -or -not (Get-ChildItem $OutDir -Filter *.exe -ErrorAction SilentlyContinue)) {
        Get-Content $log -Tail 40
        throw "빌드 실패 (종료 코드 $($p.ExitCode))"
    }
    Get-ChildItem $OutDir -Directory -Filter "*DoNotShip*" | Remove-Item -Recurse -Force
}
if (-not (Get-ChildItem $OutDir -Filter *.exe -ErrorAction SilentlyContinue)) { throw "올릴 빌드가 없습니다: $OutDir" }

# ---------------------------------------------------------------- SteamPipe 스크립트
New-Item -ItemType Directory -Force $ScriptDir | Out-Null
$depotVdf = Join-Path $ScriptDir "depot_build_$($Config.depotId).vdf"
$appVdf = Join-Path $ScriptDir "app_build_$($Config.appId).vdf"
@"
"DepotBuild"
{
    "DepotID" "$($Config.depotId)"
    "ContentRoot" "$OutDir"
    "FileMapping"
    {
        "LocalPath" "*"
        "DepotPath" "."
        "Recursive" "1"
    }
    "FileExclusion" "*.pdb"
    "FileExclusion" "*DoNotShip*"
}
"@ | Set-Content $depotVdf -Encoding utf8
@"
"AppBuild"
{
    "AppID" "$($Config.appId)"
    "Desc" "Soul Saver $(if ($Demo) { 'Demo ' })$Version ($commit)"
    "Preview" "$(if ($Preview) { 1 } else { 0 })"
    "SetLive" "$($Config.betaBranch)"
    "ContentRoot" "$OutDir"
    "BuildOutput" "$(Join-Path $Work 'SteamLogs')"
    "Depots"
    {
        "$($Config.depotId)" "$depotVdf"
    }
}
"@ | Set-Content $appVdf -Encoding utf8

# ---------------------------------------------------------------- 업로드 (Steam Guard 코드를 물어볼 수 있음)
Write-Host "steamcmd 로 업로드합니다. 처음에는 비밀번호와 Steam Guard 코드를 물어봅니다."
& $Config.steamcmd +login $Config.steamUser +run_app_build $appVdf +quit
if ($LASTEXITCODE -ne 0) { throw "steamcmd 업로드 실패 (종료 코드 $LASTEXITCODE)" }
if ($Preview) { Write-Host "미리보기 완료 (실제로 올리지 않음)" }
else { Write-Host "완료: Steamworks → 앱 관리 → SteamPipe → 빌드 에서 기본(default) 브랜치로 설정하세요" }
