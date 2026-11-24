using System.Drawing;
using System.Windows.Forms;

namespace MisterShot;

/// <summary>
/// Tray-only application: no main window, just a notification icon with a
/// context menu and a global Ctrl+Alt+S hotkey.
/// </summary>
internal sealed class TrayApp : ApplicationContext
{
    private readonly Icon _icon;
    private readonly NotifyIcon _tray;
    private readonly HotkeyWindow _hotkey;
    private bool _disposed;

    public TrayApp()
    {
        _icon = IconLoader.Load();

        var menu = new ContextMenuStrip();
        var shotItem = new ToolStripMenuItem("Скриншот активного окна", null, (_, _) => Capture())
        {
            ShortcutKeyDisplayString = "Ctrl+Alt+S",
        };
        shotItem.Font = new Font(shotItem.Font, FontStyle.Bold);
        menu.Items.Add(shotItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Выход", null, (_, _) => ExitThread()));

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "MisterShot — скриншот активного окна (Ctrl+Alt+S)",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                Capture();
            }
        };

        _hotkey = new HotkeyWindow();
        _hotkey.HotkeyPressed += Capture;
        if (!_hotkey.Register())
        {
            Notify("MisterShot",
                "Не удалось занять Ctrl+Alt+S — сочетание уже используется другой программой.\n" +
                "Скриншот по-прежнему доступен по клику на значке в трее.",
                ToolTipIcon.Warning);
        }
    }

    private void Capture()
    {
        try
        {
            using var bitmap = ScreenCapture.CaptureActiveWindow();
            if (bitmap is null)
            {
                Notify("MisterShot", "Не удалось определить активное окно.", ToolTipIcon.Warning);
                return;
            }

            if (!ClipboardHelper.TrySetImage(bitmap))
            {
                Notify("MisterShot",
                    "Буфер обмена занят другим приложением. Попробуйте ещё раз.",
                    ToolTipIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Notify("MisterShot", $"Ошибка скриншота: {ex.Message}", ToolTipIcon.Error);
        }
    }

    private void Notify(string title, string text, ToolTipIcon icon)
    {
        _tray.BalloonTipTitle = title;
        _tray.BalloonTipText = text;
        _tray.BalloonTipIcon = icon;
        _tray.ShowBalloonTip(4000);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _hotkey.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _icon.Dispose();
        }

        base.Dispose(disposing);
    }
}
