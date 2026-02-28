#Requires -Version 5.1
<#
.SYNOPSIS
    Check prerequisites, build, and launch Mac MD for Windows.
    Does NOT install anything — lists what's missing and exits if incomplete.

.NOTES
    Pass --skip-checks to bypass prerequisite detection (useful for debugging
    build/run issues on machines where prereqs are known to be present).

    Build uses `dotnet msbuild` (NOT VS Build Tools msbuild.exe).
    VS Build Tools MSBuild lacks the .NET SDK resolver plugin and will fail
    with "Microsoft.NET.Sdk not found". dotnet msbuild has it built in.

    Three MSBuild flags are required for WinUI 3 without VS installed:
      -p:EnableCoreMrtTooling=false    — disables old MRT PRI path (VS-only DLL)
      -p:EnablePriGenTooling=false     — disables old PRI gen path (VS-only DLL)
      -p:AppxGeneratePriEnabled=true   — activates NuGet-based PRI generation

    On ARM64 Windows, C:\Program Files\dotnet is ARM64-native. Building x64
    there causes ERROR_BAD_EXE_FORMAT when the app tries to load ARM64
    hostfxr.dll. This script detects native arch and builds accordingly.
#>

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Args
# ---------------------------------------------------------------------------
$skipChecks = $args -contains '--skip-checks'

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
# Detection
# ---------------------------------------------------------------------------
function Test-DotNet8Sdk {
    foreach ($cmd in @('dotnet', 'C:\Program Files\dotnet\dotnet.exe')) {
        try { if ((& $cmd --list-sdks 2>$null) -match '^8\.') { return $true } } catch {}
    }
    return $false
}

function Test-DotNet8Runtime {
    foreach ($cmd in @('dotnet', 'C:\Program Files\dotnet\dotnet.exe')) {
        try { if ((& $cmd --list-runtimes 2>$null) -match 'Microsoft\.WindowsDesktop\.App 8\.') { return $true } } catch {}
    }
    return $false
}

function Test-WebView2 {
    # Check both 64-bit and 32-bit registry locations
    return (
        (Test-Path 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}') -or
        (Test-Path 'HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}') -or
        (Test-Path 'HKCU:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}')
    )
}

# ---------------------------------------------------------------------------
# Header
# ---------------------------------------------------------------------------
Write-Host "`nMac MD for Windows — Bootstrap" -ForegroundColor White
Write-Host "================================`n"
Write-Host "  Native arch : $nativeArch  →  building $platform"

# ---------------------------------------------------------------------------
# Prerequisite check
# ---------------------------------------------------------------------------
if ($skipChecks) {
    Write-Host "  (--skip-checks: skipping prerequisite detection)`n" -ForegroundColor Yellow
} else {
    $hasSdk      = Test-DotNet8Sdk
    $hasRuntime  = Test-DotNet8Runtime
    $hasWebView2 = Test-WebView2

    # To BUILD you only need the .NET 8 SDK.
    # To RUN the built app you also need the Desktop Runtime + WebView2.
    # VS Build Tools / MSVC are NOT required.
    $canBuild = $hasSdk
    $canRun   = $hasRuntime -and $hasWebView2

    if (-not $canBuild -or -not $canRun) {
        Write-Host "Prerequisites" -ForegroundColor Yellow
        Write-Host "-------------"
        $ok  = "  [OK]     "
        $mis = "  [MISSING]"

        Write-Host ($(if ($hasSdk)     { $ok } else { $mis }) + ".NET 8 SDK              winget install Microsoft.DotNet.SDK.8")
        Write-Host ($(if ($hasRuntime) { $ok } else { $mis }) + ".NET 8 Desktop Runtime  winget install Microsoft.DotNet.DesktopRuntime.8")
        Write-Host ($(if ($hasWebView2){ $ok } else { $mis }) + "WebView2 Runtime        winget install Microsoft.EdgeWebView2Runtime")
        Write-Host ""
        Write-Host "Install missing items, then re-run this script." -ForegroundColor Yellow
        Write-Host "  Note: VS Build Tools and MSVC are NOT required."
        exit 1
    }

    Write-Ok "All prerequisites present`n"
}

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
