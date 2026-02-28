# Install-Prerequisites.ps1
# Automates the installation of dependencies for Mac MD for Windows on ARM64

Write-Host "Starting installation of prerequisites..." -ForegroundColor Cyan

# 1. Install .NET 8 SDK using Winget
Write-Host "Installing .NET 8 SDK..." -ForegroundColor Yellow
winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements --silent

# 2. Install WebView2 Runtime using Winget
Write-Host "Installing Microsoft Edge WebView2 Runtime..." -ForegroundColor Yellow
winget install Microsoft.EdgeWebView2Runtime --accept-package-agreements --accept-source-agreements --silent

# 3. Download and Install Visual Studio 2022 Build Tools (C++ and MSBuild)
Write-Host "Downloading Visual Studio 2022 Build Tools..." -ForegroundColor Yellow
$vsBuildToolsUrl = "https://aka.ms/vs/17/release/vs_buildtools.exe"
$installerPath = "$env:TEMP\vs_buildtools.exe"
Invoke-WebRequest -Uri $vsBuildToolsUrl -OutFile $installerPath

Write-Host "Installing VS Build Tools (MSBuild & C++ Workloads). This may take several minutes..." -ForegroundColor Yellow
# Running the installer silently with the required workloads
$process = Start-Process -FilePath $installerPath -ArgumentList "--quiet --wait --norestart --nocache --add Microsoft.VisualStudio.Workload.MSBuildTools --add Microsoft.VisualStudio.Workload.VCTools" -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "Visual Studio Build Tools installed successfully!" -ForegroundColor Green
} else {
    Write-Host "Visual Studio Build Tools installation finished with exit code: $($process.ExitCode). A reboot might be required." -ForegroundColor Yellow
}

# Cleanup
Remove-Item -Path $installerPath -Force

Write-Host "All installations completed. Please restart your terminal before re-running the Bootstrap script." -ForegroundColor Cyan