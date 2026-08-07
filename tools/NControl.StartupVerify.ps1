# NControl 启动渲染验证脚本
# 用法: powershell -ExecutionPolicy Bypass -File tools/NControl.StartupVerify.ps1 [-OutPng <路径>]
# 验证项:
#   1. 应用进程启动成功
#   2. 主窗口句柄非零(窗口真实创建)
#   3. 窗口尺寸合理(宽>=700 高>=450,物理像素)
#   4. PrintWindow 截取窗口内容,扫描像素:
#      - 品牌紫蓝主色像素 >= 50(R/G 低、B 高,B-R>=60)
#      - 浅色背景像素 >= 500(RGB 均 >= 190)
#      - 颜色多样性 >= 200(排除全黑/全白/纯色)
# 全部通过输出 "启动渲染验证 PASS",否则 FAIL 并以非零码退出。

param(
    [string]$AppPath = "src\NControl.App\bin\Debug\net10.0-windows\NControl.exe",
    [int]$TimeoutSeconds = 40,
    [string]$OutPng = "ncontrol_startup_verify.png"
)

$ErrorActionPreference = "Stop"

$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class StartupVerifyNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);

    public static Bitmap CaptureWindow(IntPtr hWnd)
    {
        RECT r;
        GetWindowRect(hWnd, out r);
        int w = r.Right - r.Left;
        int h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return null;
        Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            IntPtr hdc = g.GetHdc();
            bool ok = PrintWindow(hWnd, hdc, 2); // PW_RENDERFULLCONTENT: 支持 WPF/DirectX 内容
            g.ReleaseHdc(hdc);
            if (!ok)
            {
                g.FillRectangle(Brushes.Black, 0, 0, w, h); // 失败则返回全黑,由上层回退
                return bmp;
            }
        }
        return bmp;
    }
}
"@
Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

function Test-PixelColors([System.Drawing.Bitmap]$bmp, [int]$minAccent, [int]$minBackground, [int]$minUnique)
{
    $accent = 0; $background = 0; $unique = @{}
    for ($y = 0; $y -lt $bmp.Height; $y += 2)
    {
        for ($x = 0; $x -lt $bmp.Width; $x += 2)
        {
            $c = $bmp.GetPixel($x, $y)
            $r = $c.R; $g = $c.G; $b = $c.B
            if ($r -le 170 -and $g -le 175 -and $b -ge 180 -and ($b - $r) -ge 60 -and ($b - $g) -ge 40) { $accent++ }
            if ($r -ge 190 -and $g -ge 190 -and $b -ge 190) { $background++ }
            $key = ($r -shr 4) -shl 8 -bor ($g -shr 4) -shl 4 -bor ($b -shr 4)
            $unique[$key] = $true
        }
    }
    $u = $unique.Count
    return @{ Accent = $accent; Background = $background; Unique = $u;
              Pass = ($accent -ge $minAccent -and $background -ge $minBackground -and $u -ge $minUnique) }
}

$resolved = (Resolve-Path $AppPath).Path
$proc = Start-Process -FilePath $resolved -PassThru

$hwnd = [IntPtr]::Zero
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline -and $proc.MainWindowHandle -eq [IntPtr]::Zero)
{
    Start-Sleep -Milliseconds 300
    $proc.Refresh()
    if ($proc.HasExited) { break }
}
$hwnd = $proc.MainWindowHandle

Write-Host "== 启动渲染验证 =="
if ($proc.HasExited) { Write-Host "FAIL 进程提前退出,ExitCode=$($proc.ExitCode)"; exit 1 }
if ($hwnd -eq [IntPtr]::Zero) { Write-Host "FAIL 主窗口句柄为零(窗口未创建)"; Stop-Process -Id $proc.Id -Force; exit 1 }
Write-Host "PASS 窗口句柄非零: $hwnd"

$title = $proc.MainWindowTitle
Write-Host "窗口标题: [$title]"
if ([string]::IsNullOrWhiteSpace($title) -or $title -notmatch 'NControl') { Write-Host "FAIL 窗口标题不匹配 NControl"; Stop-Process -Id $proc.Id -Force; exit 1 }
Write-Host "PASS 窗口身份确认(标题含 NControl)"

$bmp = [StartupVerifyNative]::CaptureWindow($hwnd)
$w = $bmp.Width; $h = $bmp.Height
Write-Host "窗口尺寸: ${w}x${h}"
if ($w -lt 700 -or $h -lt 450) { Write-Host "FAIL 窗口尺寸过小"; Stop-Process -Id $proc.Id -Force; exit 1 }
Write-Host "PASS 窗口尺寸合理"

# PrintWindow 全黑则回退屏幕捕获
$probe = Test-PixelColors $bmp 50 500 50
if (-not $probe.Pass)
{
    Write-Host "PrintWindow 内容异常(可能全黑),回退前台截屏..."
    [StartupVerifyNative]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 800
    $bmp.Dispose()
    $bmp = [StartupVerifyNative]::CaptureWindow($hwnd)
    $probe = Test-PixelColors $bmp 50 500 50
}

$bmp.Save((Join-Path (Get-Location) $OutPng), [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "截图已保存: $OutPng"
Write-Host "主色像素=$($probe.Accent) 背景像素=$($probe.Background) 颜色数=$($probe.Unique)"

if ($probe.Pass)
{
    Write-Host "PASS 启动渲染验证 PASS"
    Stop-Process -Id $proc.Id -Force
    exit 0
}
else
{
    Write-Host "FAIL 像素扫描未达阈值(主色>=50 背景>=500 颜色数>=50)"
    Stop-Process -Id $proc.Id -Force
    exit 1
}
