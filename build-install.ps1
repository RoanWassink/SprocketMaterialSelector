param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Sprocket"
)

$ErrorActionPreference = "Stop"
$Project = Join-Path $PSScriptRoot "SprocketMaterialSelector.csproj"

Write-Host "Building Sprocket Material Selector v0.4.0 (BepInEx)..." -ForegroundColor Cyan
dotnet build $Project -c Release -nologo "-p:GameDir=$GameDir"
if ($LASTEXITCODE) { throw "Build failed." }

$dll = Join-Path $PSScriptRoot "bin\Release\net6.0\SprocketMaterialSelector.dll"
$plugins = Join-Path $GameDir "BepInEx\plugins"

if (!(Test-Path $plugins)) {
    New-Item -ItemType Directory -Path $plugins -Force | Out-Null
}

while (Get-Process Sprocket -ErrorAction SilentlyContinue) {
    Write-Host "Close Sprocket before installing..." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
}

Copy-Item $dll $plugins -Force

Write-Host ""
Write-Host "Installed:" -ForegroundColor Green
Write-Host "  $plugins\SprocketMaterialSelector.dll"
Write-Host ""
Write-Host "Start Sprocket and check BepInEx\LogOutput.log for:"
Write-Host "  Sprocket Material Selector loaded."
