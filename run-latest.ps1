#Requires -Version 5.1
<#
.SYNOPSIS
    Check prerequisites, build, and launch Mac MD for Windows.
    Does NOT install anything — lists what's missing and exits if incomplete.
#>

$ErrorActionPreference = 'Stop'

function Write-Step { param([string]$msg) Write-Host "  >> $msg" -ForegroundColor Cyan }
function Write-Ok   { param([string]$msg) Write-Host "  OK $msg" -ForegroundColor Green }
function Write-Fail { param([string]$msg) Write-Host "FAIL $msg" -ForegroundColor Red }

# ---------------------------------------------------------------------------
# Detection
# ---------------------------------------------------------------------------
function Find-MSBuild {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { return $null }
    $path = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
        -find "MSBuild\**\Bin\amd64\MSBuild.exe" 2>$null | Select-Object -First 1
    if ($path -and (Test-Path $path)) { return $path }
    $path = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
        -find "MSBuild\**\Bin\MSBuild.exe" 2>$null | Select-Object -First 1
    if ($path -and (Test-Path $path)) { return $path }
    return $null
}

function Test-MSVC {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { return $false }
    $result = & $vswhere -latest -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -property installationPath 2>$null | Select-Object -First 1
    return ($null -ne $result -and $result.Trim() -ne '')
}

function Test-DotNet8 {
    foreach ($cmd in @('dotnet', 'C:\Program Files\dotnet\dotnet.exe')) {
        try { if ((& $cmd --list-sdks 2>$null) -match '^8\.') { return $true } } catch {}
    }
    return $false
}

function Test-WebView2 {
    return (Test-Path 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}')
}

# ---------------------------------------------------------------------------
# Check
# ---------------------------------------------------------------------------
Write-Host "`nMac MD for Windows — Bootstrap" -ForegroundColor White
Write-Host "================================`n"

$msbuild     = Find-MSBuild
$hasMsvc     = Test-MSVC
$hasDotNet8  = Test-DotNet8
$hasWebView2 = Test-WebView2

$allGood = $msbuild -and $hasMsvc -and $hasDotNet8 -and $hasWebView2

if (-not $allGood) {
    Write-Host "Prerequisites" -ForegroundColor Yellow
    Write-Host "-------------"
    $ok  = "  [OK]     "
    $mis = "  [MISSING]"

    Write-Host ($(if ($msbuild)    { $ok } else { $mis }) + "MSBuild (VS 2022 Build Tools — any edition)")
    Write-Host ($(if ($hasMsvc)    { $ok } else { $mis }) + "C++ MSVC tools  (workload: 'C++ build tools' in VS Installer)")
    Write-Host ($(if ($hasDotNet8) { $ok } else { $mis }) + ".NET 8 SDK      https://dotnet.microsoft.com/download/dotnet/8.0")
    Write-Host ($(if ($hasWebView2){ $ok } else { $mis }) + "WebView2 Runtime  https://developer.microsoft.com/microsoft-edge/webview2/")

    # Disk space check — VS Build Tools + VCTools workload needs ~8 GB; warn if under 50 GB.
    $freeGB = [math]::Round((Get-PSDrive C).Free / 1GB, 1)
    $minGB  = 50
    Write-Host ""
    if ($freeGB -lt $minGB) {
        Write-Host "  [WARNING] Only ${freeGB} GB free on C: — at least ${minGB} GB recommended to install VS workloads." -ForegroundColor Red
    } else {
        Write-Host "  [OK]      ${freeGB} GB free on C: (${minGB} GB minimum recommended)" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Install everything via VS Installer + winget, then re-run this script." -ForegroundColor Yellow
    Write-Host "  VS 2022 Build Tools : https://aka.ms/vs/17/release/vs_buildtools.exe"
    Write-Host "    Required workloads: 'MSBuild Tools'  +  'C++ build tools'"
    Write-Host "  .NET 8 SDK          : winget install Microsoft.DotNet.SDK.8"
    Write-Host "  WebView2            : winget install Microsoft.EdgeWebView2Runtime"
    exit 1
}

Write-Ok "All prerequisites present"

# ---------------------------------------------------------------------------
# Ensure dotnet is on PATH for MSBuild's SDK resolver
# ---------------------------------------------------------------------------
$dotnetDir = 'C:\Program Files\dotnet'
if ((Test-Path $dotnetDir) -and ($env:PATH -notlike "*$dotnetDir*")) {
    $env:PATH = "$dotnetDir;$env:PATH"
}

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
Write-Host ""
Write-Step "Building Mac MD (x64)..."
& $msbuild "$PSScriptRoot\MacMD.sln" -restore -p:Platform=x64 -verbosity:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Fail "Build failed (exit code $LASTEXITCODE)."
    exit 1
}
Write-Ok "Build succeeded"

# ---------------------------------------------------------------------------
# Launch
# ---------------------------------------------------------------------------
$exe = "$PSScriptRoot\src\MacMD.Win\bin\x64\Debug\net8.0-windows10.0.19041.0\MacMD.Win.exe"
if (-not (Test-Path $exe)) {
    Write-Fail "Executable not found: $exe"
    Write-Host "Check that the build output path matches your project configuration."
    exit 1
}

Write-Step "Launching Mac MD..."
Start-Process $exe
Write-Ok "Done`n"
