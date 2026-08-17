<#
.SYNOPSIS
    Builds, packs, and publishes all packable NuGet packages in the solution.
.DESCRIPTION
    1. Loads NUGET_API_KEY and NUGET_SOURCE from .env file (if present) or environment.
    2. Explicitly builds the solution in Release configuration.
    3. Packs all NuGet packages into ./nupkgs.
    4. Publishes packages to NuGet with --skip-duplicate.
#>

param(
    [string]$ApiKey,
    [string]$Source,
    [switch]$PackOnly,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$envFile = Join-Path $scriptDir ".env"
$solutionFile = Join-Path $scriptDir "ActDim.Practix.sln"
$outputDir = Join-Path $scriptDir "nupkgs"

# 1. Load from .env if present
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#") -and $line.Contains("=")) {
            $parts = $line.Split("=", 2)
            $name = $parts[0].Trim()
            $value = $parts[1].Trim()
            if (-not [string]::IsNullOrWhiteSpace($value)) {
                [Environment]::SetEnvironmentVariable($name, $value, "Process")
            }
        }
    }
}

# 2. Resolve ApiKey and Source
if (-not $ApiKey) {
    $ApiKey = $env:NUGET_API_KEY
}

if (-not $Source) {
    $Source = if ($env:NUGET_SOURCE) { $env:NUGET_SOURCE } else { "https://api.nuget.org/v3/index.json" }
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# 3. Clean old nupkg files in ./nupkgs
Get-ChildItem -Path $outputDir -Filter "*.nupkg" | Remove-Item -Force -ErrorAction SilentlyContinue
Get-ChildItem -Path $outputDir -Filter "*.snupkg" | Remove-Item -Force -ErrorAction SilentlyContinue

# 4. Explicit Release Build
if (-not $NoBuild) {
    Write-Host "`n=== 1. Building Solution in Release Configuration ===" -ForegroundColor Cyan
    dotnet build $solutionFile -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }
}

# 5. Pack packages
Write-Host "`n=== 2. Packing NuGet Packages into ./nupkgs ===" -ForegroundColor Cyan
dotnet pack $solutionFile -c Release --no-build -o $outputDir
if ($LASTEXITCODE -ne 0) {
    Write-Error "Packing failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

if ($PackOnly) {
    Write-Host "`nPackOnly switch specified. All packages successfully created in: $outputDir" -ForegroundColor Green
    exit 0
}

# 6. Publish packages
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Warning "NUGET_API_KEY is not set in .env or environment. Packages were built and packed to ./nupkgs, but not published."
    Write-Host "To publish, set NUGET_API_KEY in .env or pass -ApiKey <YOUR_KEY>" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=== 3. Publishing Packages to $Source ===" -ForegroundColor Cyan
$packages = Get-ChildItem -Path $outputDir -Filter "*.nupkg"

foreach ($pkg in $packages) {
    Write-Host "Pushing $($pkg.Name)..." -ForegroundColor Magenta
    dotnet nuget push $pkg.FullName --api-key $ApiKey --source $Source --skip-duplicate
}

Write-Host "`n=== All packages published successfully! ===" -ForegroundColor Green
