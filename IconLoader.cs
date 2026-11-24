using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MisterShot;

internal static class IconLoader
{
    private const string ResourceName = "MisterShot.app.ico";

    public static Icon Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
        if (stream is not null)
        {
            return new Icon(stream, SystemInformation.SmallIconSize);
        }

        return SystemIcons.Application;
    }
}
