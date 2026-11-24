using System.Runtime.InteropServices;
using static MisterShot.Native;

namespace MisterShot;

/// <summary>Captures the focused window to a clipboard-ready CF_DIB byte block.</summary>
internal static unsafe class ScreenCapture
{
    private const int HeaderSize = 40; // sizeof(BITMAPINFOHEADER)

    public static bool TryCaptureActiveWindow(out byte[] dib)
    {
        dib = [];

        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || IsIconic(hwnd))
        {
            return false;
        }

        if (!TryGetFrameBounds(hwnd, out RECT rect) && !GetWindowRect(hwnd, out rect))
        {
            return false;
        }

        int width = rect.right - rect.left;
        int height = rect.bottom - rect.top;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        IntPtr hdcScreen = GetDC(IntPtr.Zero);
        if (hdcScreen == IntPtr.Zero)
        {
            return false;
        }

        IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
        IntPtr hbm = CreateCompatibleBitmap(hdcScreen, width, height);
        if (hdcMem == IntPtr.Zero || hbm == IntPtr.Zero)
        {
            if (hbm != IntPtr.Zero) DeleteObject(hbm);
            if (hdcMem != IntPtr.Zero) DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
            return false;
        }

        IntPtr old = SelectObject(hdcMem, hbm);
        bool captured = BitBlt(hdcMem, 0, 0, width, height, hdcScreen, rect.left, rect.top, SRCCOPY | CAPTUREBLT);
        // GetDIBits requires the bitmap NOT to be selected into a DC.
        SelectObject(hdcMem, old);

        if (!captured)
        {
            DeleteObject(hbm);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
            return false;
        }

        int imageSize = width * 4 * height;
        IntPtr pixels = Marshal.AllocHGlobal(imageSize);

        var header = new BITMAPINFOHEADER
        {
            biSize = (uint)HeaderSize,
            biWidth = width,
            biHeight = height, // bottom-up
            biPlanes = 1,
            biBitCount = 32,
            biCompression = BI_RGB,
            biSizeImage = (uint)imageSize,
        };

        int scanned = GetDIBits(hdcMem, hbm, 0, (uint)height, pixels, ref header, DIB_RGB_COLORS);

        DeleteObject(hbm);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        if (scanned == 0)
        {
            Marshal.FreeHGlobal(pixels);
            return false;
        }

        // GDI leaves the alpha byte undefined in BI_RGB bitmaps; force opaque so
        // consumers that read alpha don't render a transparent image.
        byte* source = (byte*)pixels;
        for (int i = 3; i < imageSize; i += 4)
        {
            source[i] = 255;
        }

        dib = new byte[HeaderSize + imageSize];
        fixed (byte* destination = dib)
        {
            *(BITMAPINFOHEADER*)destination = header;
            Buffer.MemoryCopy(source, destination + HeaderSize, imageSize, imageSize);
        }

        Marshal.FreeHGlobal(pixels);
        return true;
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
                hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT nativeRect, sizeof(RECT));
            if (hr == 0)
            {
                rect = nativeRect;
                return true;
            }
        }
        catch (DllNotFoundException)
        {
            // dwmapi unavailable — fall back to GetWindowRect.
        }

        return false;
    }
}
