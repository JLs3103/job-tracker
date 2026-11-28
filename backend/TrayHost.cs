using System.Diagnostics;
using System.Runtime.InteropServices;

namespace backend;

internal static partial class TrayHost
{
    private const int WmUser = 0x0400;
    private const int WmTrayIcon = WmUser + 1;
    private const int WmCommand = 0x0111;
    private const int WmDestroy = 0x0002;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonUp = 0x0205;

    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;

    private const uint MfString = 0x00000000;
    private const uint MfSeparator = 0x00000800;
    private const uint TpmBottomAlign = 0x0020;
    private const uint TpmLeftAlign = 0x0000;

    private const int CmdOpen = 1001;
    private const int CmdExit = 1002;

    private static WndProcDelegate? _wndProc;
    private static string _appUrl = "http://localhost:5173";
    private static IHostApplicationLifetime? _lifetime;
    private static nint _hwnd;

    public static void Start(string appUrl, IHostApplicationLifetime lifetime)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        _appUrl = appUrl;
        _lifetime = lifetime;

        var thread = new Thread(RunMessageLoop)
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    public static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore browser launch errors
        }
    }

    private static void RunMessageLoop()
    {
        const string className = "JobTrackerTrayWindow";
        _wndProc = WindowProc;

        var wndClass = new WndClassEx
        {
            cbSize = (uint)Marshal.SizeOf<WndClassEx>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            lpszClassName = className
        };

        RegisterClassEx(ref wndClass);
        _hwnd = CreateWindowEx(0, className, "Kerja IT", 0, 0, 0, 0, 0, 0, 0, 0, 0);
        if (_hwnd == 0)
        {
            return;
        }

        var iconHandle = LoadAppIcon();
        var notifyData = new NotifyIconData
        {
            cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NifMessage | NifIcon | NifTip,
            uCallbackMessage = WmTrayIcon,
            hIcon = iconHandle,
            szTip = "Kerja IT — Job Tracker (localhost:5173)"
        };

        Shell_NotifyIcon(NimAdd, ref notifyData);

        _lifetime?.ApplicationStopping.Register(() =>
        {
            if (_hwnd != 0)
            {
                PostMessage(_hwnd, WmDestroy, 0, 0);
            }
        });

        while (GetMessage(out var msg, 0, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        Shell_NotifyIcon(NimDelete, ref notifyData);
        if (iconHandle != 0)
        {
            DestroyIcon(iconHandle);
        }
    }

    private static nint WindowProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WmTrayIcon:
                var mouseMsg = (int)(lParam & 0xFFFF);
                if (mouseMsg == WmLButtonDblClk)
                {
                    OpenBrowser(_appUrl);
                }
                else if (mouseMsg == WmRButtonUp)
                {
                    ShowContextMenu(hwnd);
                }
                return 0;

            case WmCommand:
                var cmd = (int)(wParam & 0xFFFF);
                if (cmd == CmdOpen)
                {
                    OpenBrowser(_appUrl);
                }
                else if (cmd == CmdExit)
                {
                    _lifetime?.StopApplication();
                    PostQuitMessage(0);
                }
                return 0;

            case WmDestroy:
                PostQuitMessage(0);
                return 0;
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private static void ShowContextMenu(nint hwnd)
    {
        var menu = CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            AppendMenu(menu, MfString, CmdOpen, "Buka Job Tracker (localhost:5173)");
            AppendMenu(menu, MfSeparator, 0, string.Empty);
            AppendMenu(menu, MfString, CmdExit, "Keluar (Exit)");

            GetCursorPos(out var pt);
            SetForegroundWindow(hwnd);
            TrackPopupMenu(menu, TpmBottomAlign | TpmLeftAlign, pt.X, pt.Y, 0, hwnd, 0);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private static nint LoadAppIcon()
    {
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
        {
            var smallIcons = new nint[1];
            if (ExtractIconEx(exePath, 0, null, smallIcons, 1) > 0 && smallIcons[0] != 0)
            {
                return smallIcons[0];
            }
        }
        return 0;
    }

    private delegate nint WndProcDelegate(nint hwnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClassEx
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public Point pt;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WndClassEx lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(nint hMenu, uint uFlags, nuint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool TrackPopupMenu(
        nint hMenu, uint uFlags, int x, int y, int nReserved, nint hWnd, nint prcRect);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(
        string lpszFile, int nIconIndex, nint[]? phiconLarge, nint[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint hIcon);
}
