using System.Threading;
using System.Windows.Forms;

namespace MisterShot;

internal static class Program
{
    private const string MutexName = "MisterShot.SingleInstance.v1";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isNew);
        if (!isNew)
        {
            // Another instance already lives in the tray.
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
