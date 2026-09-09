<#
.SYNOPSIS
    Unlists (deprecates from search/indexes) older versions of solution packages on NuGet.
.DESCRIPTION
    1. Loads NUGET_API_KEY and NUGET_SOURCE from .env file (if present) or environment.
    2. Discovers packable projects in the solution.
    3. Queries published versions on NuGet via the V3 FlatContainer API.
    4. Unlists matching versions using 'dotnet nuget delete --non-interactive'.
.EXAMPLE
    .\deprecate.cmd 1.0.13
    .\deprecate.cmd -UpToVersion 1.0.13
    .\deprecate.cmd -ExactVersion 1.0.12
    .\deprecate.cmd -Versions "1.0.10", "1.0.11", "1.0.12"
    .\deprecate.cmd 1.0.13 -DryRun
#>

param(
    [Parameter(Position = 0)]
    [string]$UpToVersion,

    [string]$ExactVersion,
    [string[]]$Versions,
    [string[]]$PackageIds,
    [string]$ApiKey,
    [string]$Source = "https://api.nuget.org/v3/index.json",
    [switch]$ExcludeUpToVersion,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$envFile = Join-Path $scriptDir ".env"
$solutionFile = Join-Path $scriptDir "ActDim.Practix.sln"

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

# Validate parameters
if (-not $UpToVersion -and -not $ExactVersion -and (-not $Versions -or $Versions.Count -eq 0)) {
    Write-Host "Usage: .\deprecate.cmd <UpToVersion> [-DryRun]" -ForegroundColor Yellow
    Write-Host "Examples:"
    Write-Host "  .\deprecate.cmd 1.0.13"
    Write-Host "  .\deprecate.cmd -UpToVersion 1.0.13"
    Write-Host "  .\deprecate.cmd -ExactVersion 1.0.12"
    Write-Host "  .\deprecate.cmd -Versions 1.0.10, 1.0.11, 1.0.12"
    Write-Host "  .\deprecate.cmd 1.0.13 -DryRun"
    exit 1
}

if (-not $DryRun -and [string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Error "NUGET_API_KEY is not set in .env or environment. Provide -ApiKey or set it in .env"
    exit 1
}

# 3. Discover package IDs if not specified
if (-not $PackageIds -or $PackageIds.Count -eq 0) {
    $discovered = @()
    $csprojFiles = Get-ChildItem -Path $scriptDir -Filter "*.csproj" -Recurse | Where-Object { $_.FullName -notmatch "[\\/]Tests[\\/]" }
    
    foreach ($file in $csprojFiles) {
        $content = Get-Content $file.FullName -Raw
        if ($content -match "<IsPackable>false</IsPackable>") {
            continue
        }
        
        $pkgName = $file.BaseName
        if ($content -match "<PackageId>([^<]+)</PackageId>") {
            $pkgName = $matches[1].Trim()
        }
        $discovered += $pkgName
    }
    
    $PackageIds = $discovered | Select-Object -Unique | Sort-Object
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "-> NuGet Package Deprecation / Unlist Engine" -ForegroundColor Cyan
Write-Host "   Target Source: $Source"
if ($UpToVersion) {
    $mode = if ($ExcludeUpToVersion) { "< $UpToVersion" } else { "<= $UpToVersion" }
    Write-Host "   Target Filter: Versions $mode"
}
elseif ($ExactVersion) {
    Write-Host "   Target Filter: Exact Version $ExactVersion"
}
elseif ($Versions) {
    Write-Host "   Target Filter: Explicit Versions ($($Versions -join ', '))"
}
if ($DryRun) {
    Write-Host "   Mode:          DRY-RUN (no changes will be applied)" -ForegroundColor Yellow
}
Write-Host "   Packages:      $($PackageIds.Count) package(s)"
Write-Host "==================================================" -ForegroundColor Cyan

function Compare-SemVer([string]$v1, [string]$v2) {
    $p1 = ($v1.Split('-')[0]).Split('.') | ForEach-Object { [int]$_ }
    $p2 = ($v2.Split('-')[0]).Split('.') | ForEach-Object { [int]$_ }
    
    for ($i = 0; $i -lt 3; $i++) {
        $num1 = if ($i -lt $p1.Length) { $p1[$i] } else { 0 }
        $num2 = if ($i -lt $p2.Length) { $p2[$i] } else { 0 }
        if ($num1 -lt $num2) { return -1 }
        if ($num1 -gt $num2) { return 1 }
    }
    return 0
}

$totalUnlisted = 0
$totalSkipped = 0

foreach ($pkgId in $PackageIds) {
    Write-Host "`nChecking $pkgId..." -ForegroundColor White
    $flatContainerUrl = "https://api.nuget.org/v3-flatcontainer/$($pkgId.ToLowerInvariant())/index.json"
    
    $publishedVersions = @()
    try {
        $response = Invoke-RestMethod -Uri $flatContainerUrl -Method Get -TimeoutSec 10 -ErrorAction Stop
        if ($response -and $response.versions) {
            $publishedVersions = $response.versions
        }
    }
    catch {
        Write-Host "  -> No published versions found on NuGet or package does not exist." -ForegroundColor DarkGray
        continue
    }

    if ($publishedVersions.Count -eq 0) {
        Write-Host "  -> No published versions." -ForegroundColor DarkGray
        continue
    }

    $targetVersions = @()
    foreach ($ver in $publishedVersions) {
        $match = $false
        if ($UpToVersion) {
            $cmp = Compare-SemVer $ver $UpToVersion
            if ($ExcludeUpToVersion -and $cmp -lt 0) { $match = $true }
            elseif (-not $ExcludeUpToVersion -and $cmp -le 0) { $match = $true }
        }
        elseif ($ExactVersion -and $ver -eq $ExactVersion) {
            $match = $true
        }
        elseif ($Versions -and ($Versions -contains $ver)) {
            $match = $true
        }

        if ($match) {
            $targetVersions += $ver
        }
    }

    if ($targetVersions.Count -eq 0) {
        Write-Host "  -> No matching versions to unlist among ($($publishedVersions -join ', '))." -ForegroundColor DarkGray
        continue
    }

    foreach ($ver in $targetVersions) {
        if ($DryRun) {
            Write-Host "  [DRY-RUN] Would unlist $pkgId v$ver" -ForegroundColor Yellow
            $totalUnlisted++
        }
        else {
            Write-Host "  Unlisting $pkgId v$ver..." -ForegroundColor Magenta
            try {
                $output = dotnet nuget delete $pkgId $ver --api-key $ApiKey --source $Source --non-interactive 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "    [OK] Successfully unlisted $pkgId v$ver" -ForegroundColor Green
                    $totalUnlisted++
                }
                else {
                    Write-Warning "    [WARN] dotnet nuget delete returned code $LASTEXITCODE : $output"
                    $totalSkipped++
                }
            }
            catch {
                Write-Warning "    [ERROR] Failed to unlist $pkgId v$ver : $_"
                $totalSkipped++
            }
        }
    }
}

Write-Host "`n==================================================" -ForegroundColor Cyan
if ($DryRun) {
    Write-Host "Dry-Run Complete: $totalUnlisted version(s) identified for unlisting." -ForegroundColor Yellow
}
else {
    Write-Host "Deprecation / Unlist Complete: $totalUnlisted version(s) unlisted, $totalSkipped skipped/failed." -ForegroundColor Green
}
Write-Host "==================================================" -ForegroundColor Cyan

