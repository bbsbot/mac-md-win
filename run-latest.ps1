#Requires -Version 5.1
<#
.SYNOPSIS
    Build and launch Mac MD for Windows.
    Detects native arch (ARM64 or x64), builds with dotnet msbuild, and launches.

.NOTES
    Prerequisites must be installed before running — see README.md "Building From Source".

    Build uses `dotnet msbuild` (NOT VS Build Tools msbuild.exe).
    Three MSBuild flags are required for WinUI 3 without VS installed:
      -p:EnableCoreMrtTooling=false    — disables old MRT PRI path (VS-only DLL)
      -p:EnablePriGenTooling=false     — disables old PRI gen path (VS-only DLL)
      -p:AppxGeneratePriEnabled=true   — activates NuGet-based PRI generation

    On ARM64 Windows, C:\Program Files\dotnet is ARM64-native. Always build
    ARM64 on ARM64 machines, x64 on x64 machines.
#>

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Write-Step { param([string]$msg) Write-Host "  >> $msg" -ForegroundColor Cyan }
function Write-Ok   { param([string]$msg) Write-Host "  OK $msg" -ForegroundColor Green }
function Write-Fail { param([string]$msg) Write-Host "FAIL $msg" -ForegroundColor Red }

# ---------------------------------------------------------------------------
# Architecture detection
# ---------------------------------------------------------------------------
# $env:PROCESSOR_ARCHITEW6432 is set to the NATIVE arch when PowerShell is
# running under x64 emulation on ARM64 Windows. It is empty when the shell
# itself is native. $env:PROCESSOR_ARCHITECTURE reflects the shell's arch,
# not the machine's, so it can mislead on ARM64 with an x64 shell.
$nativeArch = $env:PROCESSOR_ARCHITEW6432
if (-not $nativeArch) { $nativeArch = $env:PROCESSOR_ARCHITECTURE }
$platform = if ($nativeArch -eq 'ARM64') { 'ARM64' } else { 'x64' }

# ---------------------------------------------------------------------------
# Header
# ---------------------------------------------------------------------------
Write-Host "`nMac MD for Windows — Bootstrap" -ForegroundColor White
Write-Host "================================`n"
Write-Host "  Native arch : $nativeArch  →  building $platform"
Write-Host ""
Write-Host "  Prerequisites: see README.md — 'Building From Source'" -ForegroundColor Yellow
Write-Host ""

# ---------------------------------------------------------------------------
# Locate dotnet
# ---------------------------------------------------------------------------
$dotnet = $null
foreach ($candidate in @('dotnet', 'C:\Program Files\dotnet\dotnet.exe')) {
    try {
        $ver = & $candidate --version 2>$null
        if ($ver) { $dotnet = $candidate; break }
    } catch {}
}

if (-not $dotnet) {
    Write-Fail ".NET SDK not found on PATH or at C:\Program Files\dotnet\dotnet.exe"
    exit 1
}

# Ensure dotnet dir is on PATH (needed by dotnet msbuild's SDK resolver)
$dotnetDir = Split-Path (& where.exe $dotnet 2>$null | Select-Object -First 1) -ErrorAction SilentlyContinue
if (-not $dotnetDir) { $dotnetDir = 'C:\Program Files\dotnet' }
if ((Test-Path $dotnetDir) -and ($env:PATH -notlike "*$dotnetDir*")) {
    $env:PATH = "$dotnetDir;$env:PATH"
}

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
Write-Step "Building Mac MD ($platform) via dotnet msbuild..."

& $dotnet msbuild "$PSScriptRoot\MacMD.sln" `
    -restore `
    -p:Platform=$platform `
    -verbosity:minimal `
    -p:EnableCoreMrtTooling=false `
    -p:EnablePriGenTooling=false `
    -p:AppxGeneratePriEnabled=true

if ($LASTEXITCODE -ne 0) {
    Write-Fail "Build failed (exit code $LASTEXITCODE)."
    exit 1
}
Write-Ok "Build succeeded"

# ---------------------------------------------------------------------------
# Launch
# ---------------------------------------------------------------------------
$exe = "$PSScriptRoot\src\MacMD.Win\bin\$platform\Debug\net8.0-windows10.0.19041.0\MacMD.Win.exe"
if (-not (Test-Path $exe)) {
    Write-Fail "Executable not found: $exe"
    Write-Host "Check that the build output path matches your project configuration."
    exit 1
}

Write-Step "Launching Mac MD..."
$proc = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 2

if ($proc.HasExited) {
    Write-Fail "App exited immediately (exit code $($proc.ExitCode) / 0x$($proc.ExitCode.ToString('X8')))."
    Write-Host "  Check the Windows Event Log (Application) for crash details."
    Write-Host "  Common causes:"
    Write-Host "    0x80008082 — wrong-arch dotnet (x64 app on ARM64 machine)"
    Write-Host "    0xC000007B — STATUS_INVALID_IMAGE_FORMAT (wrong-arch native DLL)"
    Write-Host "    0xC000027B — STATUS_STOWED_EXCEPTION (WinUI crash, e.g. missing resources.pri)"
    exit 1
}

Write-Ok "Mac MD is running (PID $($proc.Id))`n"
