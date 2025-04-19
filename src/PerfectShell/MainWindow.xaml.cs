
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PerfectShell.Core;
using Vanara.PInvoke;

namespace PerfectShell;

public partial class MainWindow : Window
{
    private readonly LuaHost _luaHost;

    public MainWindow()
    {
        InitializeComponent();

        // Make window full‑screen and bottom‑most shell window
        Loaded += (_, __) =>
        {
            WindowState   = WindowState.Maximized;
            Topmost       = false;
            var hwnd      = new WindowInteropHelper(this).Handle;
            User32.SetWindowPos(hwnd, HWND.HWND_BOTTOM, 0, 0, 0, 0,
                User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
        };

        // Bridge & Lua
        var uiBridge = new UiBridge(RootCanvas);
        _luaHost = new LuaHost(AppDomain.CurrentDomain.BaseDirectory + "Config", uiBridge);
    }
}
