using System.Threading;

namespace MisterShot;

internal static class Program
{
    private const string MutexName = "MisterShot.SingleInstance.v1";

    [STAThread]
    private static int Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isNew);
        if (!isNew)
        {
            // Another copy already lives in the tray.
            return 0;
        }

        return new App().Run();
    }
}
