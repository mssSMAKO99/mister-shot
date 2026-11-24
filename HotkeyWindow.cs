using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MisterShot;

/// <summary>
/// A hidden message-only window used as the receiver for WM_HOTKEY
/// notifications (NotifyIcon has no HWND of its own).
/// </summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0xB001;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_S = 0x53;

    private bool _registered;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public event Action? HotkeyPressed;

    /// <summary>Registers Ctrl+Alt+S. Returns false if the combo is taken.</summary>
    public bool Register()
    {
        _registered = RegisterHotKey(Handle, HotkeyId, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_S);
        return _registered;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = false;
        }

        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
