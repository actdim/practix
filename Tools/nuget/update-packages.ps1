[CmdletBinding()]
param(
    [string]$SolutionPath = ""
)

$ErrorActionPreference = "Stop"

if (-not $SolutionPath) {
    $SolutionPath = Join-Path $PSScriptRoot "..\..\ActDim.Practix.sln"
}

Write-Host "Checking dotnet-outdated tool installation..." -ForegroundColor Cyan

# Install dotnet-outdated-tool globally if not present
try {
    dotnet tool install -g dotnet-outdated-tool --ignore-failed-sources 2>$null
}
catch {
    # Ignore error if tool is already installed
}

Write-Host "Running dotnet-outdated to update central package versions in Directory.Packages.props..." -ForegroundColor Cyan

dotnet outdated -u -v Minor $SolutionPath

Write-Host "Central Package versions successfully updated in Directory.Packages.props!" -ForegroundColor Green
