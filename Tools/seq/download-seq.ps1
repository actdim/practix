[CmdletBinding()]
param(
    [string]$DownloadUrl = "https://github.com/datalust/seq-cli/releases/download/v2024.3.1118/seq-cli-2024.3.1118-win-x64.zip",
    [string]$TargetDir = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

if (-not $TargetDir) {
    $TargetDir = Get-Location
}

Write-Host "Downloading Seq CLI from: $DownloadUrl ..." -ForegroundColor Cyan

$zipPath = Join-Path $TargetDir "seq-cli.zip"

try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $zipPath -UserAgent "Mozilla/5.0"
    Write-Host "Extracting archive to: $TargetDir ..." -ForegroundColor Cyan
    Expand-Archive -Path $zipPath -DestinationPath $TargetDir -Force
    Remove-Item -Path $zipPath -Force
    Write-Host "Seq CLI successfully downloaded and extracted into $TargetDir!" -ForegroundColor Green
}
catch {
    Write-Error "Failed to download or extract Seq CLI: $_"
}
