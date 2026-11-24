using System.Reflection;
using static MisterShot.Native;

namespace MisterShot;

internal static unsafe class IconLoader
{
    private const string ResourceName = "MisterShot.app.ico";

    /// <summary>
    /// Loads the tray icon from the embedded .ico, choosing the image whose size
    /// best matches the small-icon metric. Falls back to the stock app icon.
    /// </summary>
    public static IntPtr Load()
    {
        int desired = GetSystemMetrics(SM_CXSMICON);
        if (desired <= 0)
        {
            desired = 16;
        }

        byte[]? ico = ReadEmbeddedIcon();
        if (ico is not null)
        {
            IntPtr icon = CreateFromIco(ico, desired);
            if (icon != IntPtr.Zero)
            {
                return icon;
            }
        }

        // IDI_APPLICATION == 32512.
        return LoadImageW(IntPtr.Zero, (IntPtr)32512, IMAGE_ICON, desired, desired, LR_DEFAULTCOLOR);
    }

    private static byte[]? ReadEmbeddedIcon()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>Picks the best-size image from an ICO container and builds an HICON.</summary>
    private static IntPtr CreateFromIco(byte[] ico, int desired)
    {
        const int DirSize = 6;
        const int EntrySize = 16;

        if (ico.Length < DirSize + EntrySize)
        {
            return IntPtr.Zero;
        }

        int count = ico[4] | (ico[5] << 8);
        if (count <= 0)
        {
            return IntPtr.Zero;
        }

        int bestOffset = -1;
        int bestSize = -1;
        int bestScore = int.MaxValue;

        for (int i = 0; i < count; i++)
        {
            int entry = DirSize + i * EntrySize;
            if (entry + EntrySize > ico.Length)
            {
                break;
            }

            int entryWidth = ico[entry] == 0 ? 256 : ico[entry];
            int bytesInRes = BitConverter.ToInt32(ico, entry + 8);
            int imageOffset = BitConverter.ToInt32(ico, entry + 12);
            if (bytesInRes <= 0 || imageOffset <= 0 || imageOffset + bytesInRes > ico.Length)
            {
                continue;
            }

            int score = Math.Abs(entryWidth - desired);
            if (score < bestScore)
            {
                bestScore = score;
                bestOffset = imageOffset;
                bestSize = bytesInRes;
                if (score == 0)
                {
                    break;
                }
            }
        }

        if (bestOffset < 0)
        {
            return IntPtr.Zero;
        }

        fixed (byte* data = ico)
        {
            return CreateIconFromResourceEx(
                data + bestOffset, (uint)bestSize, fIcon: true, dwVer: 0x00030000,
                cxDesired: desired, cyDesired: desired, flags: LR_DEFAULTCOLOR);
        }
    }
}
