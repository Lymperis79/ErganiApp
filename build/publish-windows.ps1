# publish-windows.ps1
# Builds a self-contained single-executable for Windows x64.
# Run from the repository root: .\build\publish-windows.ps1
#
# Output: build\publish\win-x64\ErganiManager.exe

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot   = Split-Path -Parent $PSScriptRoot
$Project    = Join-Path $RepoRoot "src\ErganiManager.UI\ErganiManager.UI.csproj"
$OutputDir  = Join-Path $RepoRoot "build\publish\win-x64"

Write-Host "Building ErganiManager $Version for Windows x64..." -ForegroundColor Cyan

dotnet publish $Project `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $OutputDir `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -p:Version=$Version `
    -p:AssemblyVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit 1
}

Write-Host ""
Write-Host "✅ Published to: $OutputDir" -ForegroundColor Green
Write-Host "   Executable:   ErganiManager.exe"
Write-Host ""
Write-Host "To zip for distribution:"
Write-Host "   Compress-Archive -Path '$OutputDir\*' -DestinationPath 'ErganiManager-$Version-win-x64.zip'"
