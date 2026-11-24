using static MisterShot.Native;

namespace MisterShot;

/// <summary>Thin wrapper over <c>Shell_NotifyIconW</c> for a single tray icon.</summary>
internal sealed unsafe class TrayIcon
{
    private readonly uint _id;
    private IntPtr _hwnd;
    private IntPtr _hicon;
    private bool _added;

    public TrayIcon(uint id)
    {
        _id = id;
    }

    public bool Add(IntPtr hwnd, IntPtr hicon, uint callbackMessage, string tip)
    {
        _hwnd = hwnd;
        _hicon = hicon;

        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = hwnd,
            uID = _id,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = callbackMessage,
            hIcon = hicon,
        };

        App.CopyString(data.szTip, 128, tip);

        _added = Shell_NotifyIconW(NIM_ADD, ref data);
        return _added;
    }

    public void Balloon(string title, string text, uint flags)
    {
        if (!_added)
        {
            return;
        }

        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = _hwnd,
            uID = _id,
            uFlags = NIF_INFO,
            hIcon = _hicon,
            uTimeoutOrVersion = 5000,
            dwInfoFlags = flags,
        };

        App.CopyString(data.szInfoTitle, 64, title);
        App.CopyString(data.szInfo, 256, text);

        Shell_NotifyIconW(NIM_MODIFY, ref data);
    }

    public void Remove()
    {
        if (!_added)
        {
            return;
        }

        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            hWnd = _hwnd,
            uID = _id,
        };

        Shell_NotifyIconW(NIM_DELETE, ref data);
        _added = false;
    }
}
