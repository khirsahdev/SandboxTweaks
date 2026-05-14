# Builds SandboxTweaks.dll and assembles the Thunderstore package zip.
# Usage:  pwsh -ExecutionPolicy Bypass -File build-package.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# 1. compile (also deploys to the active Thunderstore profile via the csproj target)
dotnet build "$root\SandboxTweaks.csproj" -c Release -v m
if ($LASTEXITCODE -ne 0) { throw "build failed" }

# 2. read version from manifest
$manifest = Get-Content "$root\thunderstore\manifest.json" -Raw | ConvertFrom-Json
$version  = $manifest.version_number
$name     = $manifest.name

# 3. stage the package layout
$stage = "$root\dist\_stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$plugDir = "$stage\BepInEx\plugins\SandboxTweaks"
New-Item -ItemType Directory -Force -Path $plugDir | Out-Null

Copy-Item "$root\thunderstore\manifest.json"  "$stage\manifest.json"
Copy-Item "$root\thunderstore\icon.png"       "$stage\icon.png"
Copy-Item "$root\thunderstore\README.md"      "$stage\README.md"
Copy-Item "$root\thunderstore\CHANGELOG.md"   "$stage\CHANGELOG.md"
Copy-Item "$root\LICENSE"                     "$stage\LICENSE"
Copy-Item "$root\bin\Release\SandboxTweaks.dll" "$plugDir\SandboxTweaks.dll"

# 4. zip it
$zip = "$root\dist\$name-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$stage\*" -DestinationPath $zip
Remove-Item $stage -Recurse -Force

Write-Host "Package: $zip" -ForegroundColor Green
