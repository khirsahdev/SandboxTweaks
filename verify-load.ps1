# Smoke-test: launches the game with the active Thunderstore profile's BepInEx,
# waits for the main menu, then checks LogOutput.log for a clean load.
# It does NOT test gameplay (that needs you to click a save slot) — it only
# confirms the DLL loads and all Harmony patches resolve without error.
#
# Usage:  pwsh -ExecutionPolicy Bypass -File verify-load.ps1
$ErrorActionPreference = 'Stop'

$game    = "C:\Program Files (x86)\Steam\steamapps\common\Gamble With Your Friends"
$profile = "C:\Users\Khirsah\AppData\Roaming\Thunderstore Mod Manager\DataFolder\GambleWithYourFriends\profiles\Default"
$exe     = "$game\Gamble With Your Friends.exe"
$log     = "$profile\BepInEx\LogOutput.log"

if (Test-Path $log) { Remove-Item $log -Force }

# Doorstop env-var override → load the profile's BepInEx preloader.
$env:DOORSTOP_ENABLED         = "1"
$env:DOORSTOP_TARGET_ASSEMBLY = "$profile\BepInEx\core\BepInEx.Preloader.dll"

Write-Host "Launching game (will close automatically in ~40s)..." -ForegroundColor Cyan
$proc = Start-Process -FilePath $exe -WorkingDirectory $game -PassThru
Start-Sleep -Seconds 40
if (!$proc.HasExited) { $proc.Kill() }

if (!(Test-Path $log)) {
    Write-Host "FAIL: no LogOutput.log produced — BepInEx did not load." -ForegroundColor Red
    exit 1
}

Write-Host "`n--- SandboxTweaks log lines ---" -ForegroundColor Cyan
Select-String -Path $log -Pattern "SandboxTweaks|Sandbox Tweaks|com.khirsah" -SimpleMatch |
    ForEach-Object { $_.Line }

Write-Host "`n--- errors / patch failures ---" -ForegroundColor Cyan
Select-String -Path $log -Pattern "Error|Exception|Failed to patch" |
    ForEach-Object { $_.Line }
