using System.Runtime.InteropServices;
using static MisterShot.Native;

namespace MisterShot;

internal static class ClipboardService
{
    /// <summary>
    /// Copies a CF_DIB block to the clipboard, retrying briefly if another
    /// process currently holds the clipboard open.
    /// </summary>
    public static bool TrySetDib(IntPtr hwnd, byte[] dib, int attempts = 5, int delayMs = 120)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            if (OpenClipboard(hwnd))
            {
                try
                {
                    if (!EmptyClipboard())
                    {
                        continue;
                    }

                    IntPtr handle = GlobalAlloc(GMEM_MOVEABLE, (nuint)dib.Length);
                    if (handle == IntPtr.Zero)
                    {
                        return false;
                    }

                    IntPtr target = GlobalLock(handle);
                    if (target == IntPtr.Zero)
                    {
                        GlobalFree(handle);
                        return false;
                    }

                    Marshal.Copy(dib, 0, target, dib.Length);
                    GlobalUnlock(handle);

                    // Ownership transfers to the clipboard on success.
                    if (SetClipboardData(CF_DIB, handle) == IntPtr.Zero)
                    {
                        GlobalFree(handle);
                        return false;
                    }

                    return true;
                }
                finally
                {
                    CloseClipboard();
                }
            }

            Thread.Sleep(delayMs);
        }

        return false;
    }
}
