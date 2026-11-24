using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static MisterShot.Native;

namespace MisterShot;

/// <summary>
/// Tray-only application. Owns a hidden message window that receives the
/// global hotkey and tray-icon callbacks, plus the Win32 message loop.
/// </summary>
internal sealed unsafe class App
{
    private const string ClassName = "MisterShot.HiddenWindow";
    private const uint TrayCallbackMessage = WM_APP + 1;
    private const int HotkeyId = 0xB001;
    private const uint TrayIconId = 1;

    private const uint MenuCapture = 1;
    private const uint MenuExit = 2;

    private const string Tip = "MisterShot — скриншот активного окна (Ctrl+Alt+S)";

    internal static App? Current;

    private readonly TrayIcon _tray = new(TrayIconId);
    private IntPtr _hwnd;
    private IntPtr _hicon;
    private bool _hotkeyRegistered;

    public int Run()
    {
        Current = this;

        IntPtr hInstance = GetModuleHandleW(null);
        _hicon = IconLoader.Load();

        ushort atom = RegisterWindowClass(hInstance);
        if (atom == 0)
        {
            return 1;
        }

        _hwnd = CreateWindowExW(
            WS_EX_TOOLWINDOW, ClassName, "MisterShot", 0,
            0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
        {
            return 2;
        }

        _tray.Add(_hwnd, _hicon, TrayCallbackMessage, Tip);

        _hotkeyRegistered = RegisterHotKey(_hwnd, HotkeyId, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_S);
        if (!_hotkeyRegistered)
        {
            _tray.Balloon("MisterShot",
                "Не удалось занять Ctrl+Alt+S — сочетание уже используется другой программой.\n" +
                "Скриншот по-прежнему доступен по клику на значке в трее.",
                NIIF_WARNING);
        }

        MSG msg;
        while (GetMessageW(out msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }

        Cleanup();
        return 0;
    }

    private ushort RegisterWindowClass(IntPtr hInstance)
    {
        fixed (char* className = ClassName)
        {
            var wc = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                style = 0,
                lpfnWndProc = (IntPtr)(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, IntPtr>)&WndProc,
                hInstance = hInstance,
                hIcon = _hicon,
                hIconSm = _hicon,
                lpszClassName = (IntPtr)className,
            };

            return RegisterClassExW(ref wc);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        App? app = Current;
        if (app is not null)
        {
            switch (msg)
            {
                case WM_HOTKEY when wParam.ToInt32() == HotkeyId:
                    app.Capture();
                    return IntPtr.Zero;

                case TrayCallbackMessage:
                    switch ((uint)(lParam.ToInt64() & 0xFFFF))
                    {
                        case WM_LBUTTONUP:
                            app.Capture();
                            return IntPtr.Zero;
                        case WM_RBUTTONUP:
                            app.ShowMenu();
                            return IntPtr.Zero;
                    }
                    break;

                case WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
            }
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void Capture()
    {
        if (ScreenCapture.TryCaptureActiveWindow(out byte[] dib))
        {
            if (!ClipboardService.TrySetDib(_hwnd, dib))
            {
                _tray.Balloon("MisterShot",
                    "Буфер обмена занят другим приложением. Попробуйте ещё раз.",
                    NIIF_WARNING);
            }
        }
        else
        {
            _tray.Balloon("MisterShot", "Не удалось определить активное окно.", NIIF_WARNING);
        }
    }

    private void ShowMenu()
    {
        IntPtr menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        AppendMenuW(menu, MF_STRING, (IntPtr)MenuCapture, "Скриншот активного окна\tCtrl+Alt+S");
        AppendMenuW(menu, MF_SEPARATOR, IntPtr.Zero, null);
        AppendMenuW(menu, MF_STRING, (IntPtr)MenuExit, "Выход");

        GetCursorPos(out POINT pt);
        SetForegroundWindow(_hwnd);
        uint command = (uint)TrackPopupMenu(
            menu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.x, pt.y, 0, _hwnd, IntPtr.Zero);
        DestroyMenu(menu);
        PostMessageW(_hwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);

        switch (command)
        {
            case MenuCapture:
                Capture();
                break;
            case MenuExit:
                DestroyWindow(_hwnd);
                break;
        }
    }

    private void Cleanup()
    {
        if (_hotkeyRegistered)
        {
            UnregisterHotKey(_hwnd, HotkeyId);
            _hotkeyRegistered = false;
        }

        _tray.Remove();

        if (_hicon != IntPtr.Zero)
        {
            DestroyIcon(_hicon);
            _hicon = IntPtr.Zero;
        }
    }

    internal static void CopyString(char* destination, int capacity, string value)
    {
        int length = Math.Min(value.Length, capacity - 1);
        for (int i = 0; i < length; i++)
        {
            destination[i] = value[i];
        }

        destination[length] = '\0';
    }
}
