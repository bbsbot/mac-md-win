param(
    [string]$ProcessName = "MacMD.Win",
    [string]$OutputPath = "C:\Users\Admin\Documents\projects\mac-md-win\src\MacMD.Tests\M1_acceptance.png"
)

$proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "APP_RUNNING: PID $($proc.Id)"

    Add-Type -TypeDefinition @"
    using System;
    using System.Runtime.InteropServices;
    public class WinApi {
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
"@
    $hwnd = $proc.MainWindowHandle
    [WinApi]::ShowWindow($hwnd, 9) | Out-Null
    [WinApi]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Seconds 2

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing
    $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bitmap = New-Object System.Drawing.Bitmap($screen.Width, $screen.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($screen.Location, [System.Drawing.Point]::Empty, $screen.Size)
    $bitmap.Save($OutputPath)
    $graphics.Dispose()
    $bitmap.Dispose()
    Write-Host "SCREENSHOT_SAVED: $OutputPath"

    Stop-Process -Name $ProcessName -Force
    Write-Host "APP_STOPPED"
} else {
    Write-Host "APP_NOT_RUNNING: $ProcessName process not found"
}
