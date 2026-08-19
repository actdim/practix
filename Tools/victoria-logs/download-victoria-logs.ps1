[CmdletBinding()]
param(
    [string]$DownloadUrl = "https://github.com/VictoriaMetrics/VictoriaLogs/releases/download/v1.51.1/victoria-logs-windows-amd64-v1.51.1.zip",
    [string]$TargetDir = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

if (-not $TargetDir) {
    $TargetDir = Get-Location
}

Write-Host "Downloading VictoriaLogs from: $DownloadUrl ..." -ForegroundColor Cyan

$zipPath = Join-Path $TargetDir "victoria-logs.zip"

try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $zipPath -UserAgent "Mozilla/5.0"
    Write-Host "Extracting archive to: $TargetDir ..." -ForegroundColor Cyan
    Expand-Archive -Path $zipPath -DestinationPath $TargetDir -Force
    Remove-Item -Path $zipPath -Force
    Write-Host "VictoriaLogs successfully downloaded and extracted into $TargetDir!" -ForegroundColor Green
}
catch {
    Write-Error "Failed to download or extract VictoriaLogs: $_"
}
