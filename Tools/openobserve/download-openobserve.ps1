[CmdletBinding()]
param(
    [string]$DownloadUrl = "https://downloads.openobserve.ai/releases/openobserve/v0.92.2/openobserve-v0.92.2-windows-amd64.zip",
    [string]$TargetDir = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

if (-not $TargetDir) {
    $TargetDir = Get-Location
}

Write-Host "Downloading OpenObserve from: $DownloadUrl ..." -ForegroundColor Cyan

$zipPath = Join-Path $TargetDir "openobserve.zip"

try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $zipPath -UserAgent "Mozilla/5.0"
    Write-Host "Extracting archive to: $TargetDir ..." -ForegroundColor Cyan
    Expand-Archive -Path $zipPath -DestinationPath $TargetDir -Force
    Remove-Item -Path $zipPath -Force
    Write-Host "OpenObserve successfully downloaded and extracted into $TargetDir!" -ForegroundColor Green
}
catch {
    Write-Error "Failed to download or extract OpenObserve: $_"
}
