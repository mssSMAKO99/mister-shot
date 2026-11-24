using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MisterShot;

internal static class ClipboardHelper
{
    /// <summary>
    /// Copies an image to the clipboard, retrying briefly if another process
    /// currently holds the clipboard open.
    /// </summary>
    public static bool TrySetImage(Image image, int attempts = 5, int delayMs = 120)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                // copy: true so the image survives after this app exits.
                Clipboard.SetDataObject(image, copy: true);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(delayMs);
            }
        }

        return false;
    }
}
