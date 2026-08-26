# Builds src/Coach.Web/wwwroot's generated assets (Tailwind CSS + vendored
# Alpine.js) locally, so `dotnet run` works without going through Docker.
# The Dockerfile does the equivalent for the actual deployed image -- this
# script exists purely for local dev convenience and isn't part of the
# deploy path.
#
# Usage (from repo root): ./scripts/build-web-assets.ps1

$ErrorActionPreference = "Stop"

$tailwindVersion = "v4.3.3"
$alpineJsVersion = "3.16.3"

$webDir = Join-Path $PSScriptRoot "../src/Coach.Web"
$cssDir = Join-Path $webDir "wwwroot/css"
$jsDir = Join-Path $webDir "wwwroot/js"
New-Item -ItemType Directory -Force -Path $cssDir, $jsDir | Out-Null

$tailwindExe = Join-Path $env:TEMP "tailwindcss-windows-x64.exe"
if (-not (Test-Path $tailwindExe)) {
    Write-Host "==> Downloading Tailwind CLI $tailwindVersion"
    Invoke-WebRequest -Uri "https://github.com/tailwindlabs/tailwindcss/releases/download/$tailwindVersion/tailwindcss-windows-x64.exe" -OutFile $tailwindExe
}

Write-Host "==> Building Tailwind CSS"
& $tailwindExe -i (Join-Path $webDir "Styles/tailwind.css") -o (Join-Path $cssDir "app.css") --minify --cwd $webDir

Write-Host "==> Fetching Alpine.js $alpineJsVersion"
Invoke-WebRequest -Uri "https://cdn.jsdelivr.net/npm/alpinejs@$alpineJsVersion/dist/cdn.min.js" -OutFile (Join-Path $jsDir "alpine.min.js")

Write-Host "Done. `dotnet run` from src/Coach.Web will now find wwwroot/css/app.css and wwwroot/js/alpine.min.js."
