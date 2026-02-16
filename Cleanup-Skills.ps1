# Cleanup-Skills.ps1
# Removes unneeded skills directories and updates project structure
# Run from project root

param(
    [switch]$DryRun,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$skillsRoot = Join-Path (Join-Path $PSScriptRoot ".claude") "skills"

# Validate we're in the right place
if (-not (Test-Path $skillsRoot)) {
    Write-Error "Skills directory not found at: $skillsRoot"
    Write-Error "Run this script from the project root."
    exit 1
}

Write-Host "=== Skills Cleanup Patch ===" -ForegroundColor Cyan
Write-Host "Skills root: $skillsRoot" -ForegroundColor Gray
Write-Host ""

# --- 1. Remove 'official' directory entirely ---
$officialDir = Join-Path $skillsRoot "official"
if (Test-Path $officialDir) {
    $fileCount = (Get-ChildItem -Path $officialDir -Recurse -File).Count
    if ($DryRun) {
        Write-Host "[DRY RUN] Would delete: official/ ($fileCount files)" -ForegroundColor Yellow
    } else {
        Remove-Item -Path $officialDir -Recurse -Force
        Write-Host "[DELETED] official/ ($fileCount files removed)" -ForegroundColor Green
    }
} else {
    Write-Host "[SKIP] official/ already removed" -ForegroundColor DarkGray
}

# --- 2. Verify keepers exist ---
$keepers = @("dotnet", "workflow", "community")
foreach ($dir in $keepers) {
    $path = Join-Path $skillsRoot $dir
    if (Test-Path $path) {
        $count = (Get-ChildItem -Path $path -Recurse -File).Count
        Write-Host "[KEPT] $dir/ ($count files)" -ForegroundColor Cyan
    } else {
        Write-Host "[WARN] $dir/ not found!" -ForegroundColor Red
    }
}

# --- 3. Remove any stray top-level files that aren't needed ---
# (e.g., leftover READMEs or metadata at the skills root level)
$strayFiles = Get-ChildItem -Path $skillsRoot -File -ErrorAction SilentlyContinue
if ($strayFiles) {
    foreach ($file in $strayFiles) {
        if ($file.Name -eq "MANIFEST.md") {
            Write-Host "[KEPT] MANIFEST.md (reference document)" -ForegroundColor Cyan
            continue
        }
        if ($DryRun) {
            Write-Host "[DRY RUN] Would delete stray file: $($file.Name)" -ForegroundColor Yellow
        } else {
            Remove-Item -Path $file.FullName -Force
            Write-Host "[DELETED] stray file: $($file.Name)" -ForegroundColor Green
        }
    }
}

# --- 4. Summary ---
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
$remaining = (Get-ChildItem -Path $skillsRoot -Recurse -File).Count
Write-Host "Total files remaining in skills/: $remaining" -ForegroundColor White

if ($DryRun) {
    Write-Host ""
    Write-Host "This was a dry run. Re-run without -DryRun to apply changes." -ForegroundColor Yellow
}