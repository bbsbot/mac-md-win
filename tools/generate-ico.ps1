param(
    [string]$SrcPath = "reference\apple\mac_md.png",
    [string]$OutPath = "src\MacMD.Win\Assets\app.ico"
)

Add-Type -AssemblyName System.Drawing

$assetsDir = Split-Path $OutPath
if (-not (Test-Path $assetsDir)) { New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null }

$src = [System.Drawing.Image]::FromFile((Resolve-Path $SrcPath))
Write-Host "Source image: $($src.Width)x$($src.Height)"

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$ms = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($ms)

# ICO Header
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$sizes.Count)

# Generate PNG data for each size
$pngDataList = @()
foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.DrawImage($src, 0, 0, $size, $size)
    $g.Dispose()

    $pngMs = New-Object System.IO.MemoryStream
    $bmp.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngDataList += ,$pngMs.ToArray()
    $pngMs.Dispose()
    $bmp.Dispose()
}

# Write directory entries
$headerSize = 6
$dirSize = 16 * $sizes.Count
$offset = $headerSize + $dirSize

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $size = $sizes[$i]
    $data = $pngDataList[$i]
    $w = if ($size -eq 256) { 0 } else { $size }
    $h = if ($size -eq 256) { 0 } else { $size }

    $writer.Write([byte]$w)
    $writer.Write([byte]$h)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$data.Length)
    $writer.Write([UInt32]$offset)
    $offset += $data.Length
}

# Write image data
foreach ($data in $pngDataList) {
    $writer.Write($data)
}

$writer.Flush()
[System.IO.File]::WriteAllBytes($OutPath, $ms.ToArray())
$writer.Dispose()
$ms.Dispose()
$src.Dispose()

$fileInfo = Get-Item $OutPath
Write-Host "ICO created: $OutPath ($([math]::Round($fileInfo.Length / 1KB, 1)) KB, $($sizes.Count) sizes: $($sizes -join ', '))"
