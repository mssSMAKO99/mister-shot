# Generates app.ico (multi-size, PNG-based) for MisterShot.
# Run: pwsh -File tools/generate-icon.ps1
param(
    [string]$Output = (Join-Path $PSScriptRoot '..\app.ico')
)

Add-Type -AssemblyName System.Drawing

function New-RoundedRect {
    param([double]$X, [double]$Y, [double]$W, [double]$H, [double]$R)
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $R * 2
    $p.AddArc([float]$X, [float]$Y, [float]$d, [float]$d, 180, 90)
    $p.AddArc([float]($X + $W - $d), [float]$Y, [float]$d, [float]$d, 270, 90)
    $p.AddArc([float]($X + $W - $d), [float]($Y + $H - $d), [float]$d, [float]$d, 0, 90)
    $p.AddArc([float]$X, [float]($Y + $H - $d), [float]$d, [float]$d, 90, 90)
    $p.CloseFigure()
    return $p
}

function New-IconBitmap {
    param([int]$Size)
    $s = [double]$Size
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    # Rounded-square background with vertical gradient.
    $pad = $s * 0.055
    $bg = New-RoundedRect -X $pad -Y $pad -W ($s - 2 * $pad) -H ($s - 2 * $pad) -R ($s * 0.20)
    $c1 = [System.Drawing.Color]::FromArgb(255, 99, 102, 241)
    $c2 = [System.Drawing.Color]::FromArgb(255, 67, 56, 202)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.Point(0, 0)),
        (New-Object System.Drawing.Point(0, $Size)),
        $c1, $c2)
    $g.FillPath($bgBrush, $bg)

    # Camera body (white rounded rect).
    $bodyX = $s * 0.20
    $bodyY = $s * 0.36
    $bodyW = $s * 0.60
    $bodyH = $s * 0.34
    $body = New-RoundedRect -X $bodyX -Y $bodyY -W $bodyW -H $bodyH -R ($s * 0.07)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillPath($white, $body)

    # Viewfinder bump.
    $bump = New-RoundedRect -X ($s * 0.33) -Y ($s * 0.28) -W ($s * 0.18) -H ($s * 0.14) -R ($s * 0.04)
    $g.FillPath($white, $bump)

    # Lens (indigo circle inside the white body).
    $lensD = $s * 0.20
    $lensBrush = New-Object System.Drawing.SolidBrush($c2)
    $g.FillEllipse($lensBrush, [float]($s * 0.5 - $lensD / 2), [float]($s * 0.53 - $lensD / 2), [float]$lensD, [float]$lensD)
    $g.FillEllipse($white, [float]($s * 0.5 - $lensD / 4), [float]($s * 0.53 - $lensD / 4), [float]($lensD / 2), [float]($lensD / 2))

    # Small flash dot.
    $g.FillEllipse($lensBrush, [float]($s * 0.68), [float]($s * 0.60), [float]($s * 0.05), [float]($s * 0.05))

    $bgBrush.Dispose(); $white.Dispose(); $lensBrush.Dispose()
    $bg.Dispose(); $body.Dispose(); $bump.Dispose(); $g.Dispose()
    return $bmp
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = @()
foreach ($sz in $sizes) {
    $b = New-IconBitmap -Size $sz
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += , @{ Size = $sz; Bytes = $ms.ToArray() }
    $ms.Dispose(); $b.Dispose()
}

$fs = [System.IO.File]::Create($Output)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)            # reserved
$bw.Write([uint16]1)            # type = icon
$bw.Write([uint16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $dim = if ($img.Size -ge 256) { 0 } else { $img.Size }
    $bw.Write([byte]$dim)       # width
    $bw.Write([byte]$dim)       # height
    $bw.Write([byte]0)          # color count
    $bw.Write([byte]0)          # reserved
    $bw.Write([uint16]1)        # planes
    $bw.Write([uint16]32)       # bit count
    $bw.Write([uint32]$img.Bytes.Length)
    $bw.Write([uint32]$offset)
    $offset += $img.Bytes.Length
}
foreach ($img in $images) { $bw.Write($img.Bytes) }
$bw.Flush(); $bw.Dispose(); $fs.Dispose()

Write-Host "Wrote $Output ($($images.Count) sizes)."
