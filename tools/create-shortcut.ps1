$ws = New-Object -ComObject WScript.Shell
$desktop = [Environment]::GetFolderPath('Desktop')
$shortcut = $ws.CreateShortcut("$desktop\Mac MD.lnk")
$shortcut.TargetPath = "C:\Users\Admin\Documents\projects\mac-md-win\src\MacMD.Win\bin\x64\Debug\net8.0-windows10.0.19041.0\MacMD.Win.exe"
$shortcut.WorkingDirectory = "C:\Users\Admin\Documents\projects\mac-md-win\src\MacMD.Win\bin\x64\Debug\net8.0-windows10.0.19041.0"
$shortcut.Description = "Mac MD for Windows (Debug)"
$shortcut.Save()
Write-Host "Shortcut created at: $desktop\Mac MD.lnk"
