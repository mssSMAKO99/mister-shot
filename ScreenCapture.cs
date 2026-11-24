using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MisterShot;

/// <summary>Captures the currently focused window exactly as it appears on screen.</summary>
internal static class ScreenCapture
{
    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    public static Bitmap? CaptureActiveWindow()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || IsIconic(hwnd))
        {
            return null;
        }

        if (!TryGetFrameBounds(hwnd, out RECT rect) && !GetWindowRect(hwnd, out rect))
        {
            return null;
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    /// <summary>
    /// DWM extended frame bounds exclude the invisible resize border / drop shadow
    /// that <see cref="GetWindowRect"/> includes on Windows 10/11.
    /// </summary>
    private static bool TryGetFrameBounds(IntPtr hwnd, out RECT rect)
    {
        rect = default;
        try
        {
            int hr = DwmGetWindowAttribute(
                hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT nativeRect, Marshal.SizeOf<RECT>());

            if (hr == 0)
            {
                rect = nativeRect;
                return true;
            }
        }
        catch (DllNotFoundException)
        {
            // dwmapi unavailable (very old Windows) — fall back to GetWindowRect.
        }

        return false;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);
}
