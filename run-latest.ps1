$exe = "$PSScriptRoot\src\MacMD.Win\bin\x64\Debug\net8.0-windows10.0.19041.0\MacMD.Win.exe"

if (-not (Test-Path $exe)) {
    Write-Host "No build found. Building..." -ForegroundColor Yellow
    & "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe" `
        "$PSScriptRoot\MacMD.sln" -restore -p:Platform=x64 -verbosity:minimal
    if ($LASTEXITCODE -ne 0) { Write-Host "Build failed." -ForegroundColor Red; exit 1 }
}

Write-Host "Launching Mac MD..." -ForegroundColor Green
Start-Process $exe
