using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using static WMG.Core.WinAPI;

namespace WMG.Core
{
    public sealed class WinAPI
    {
        #region Mouse / keyboard hooks

        /*
         * Argument for SetWindowsHookEx.
         * http://pinvoke.net/default.aspx/Enums/HookType.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowshookexa
         * (we only include those constants that we need here)
         */
        public enum HookType : int
        {
            WH_KEYBOARD = 2,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE = 7,
            WH_MOUSE_LL = 14
        }

        /*
         * WM constants which are supplied as wParam arguments to HookProc for low level mouse events.
         * http://www.pinvoke.net/default.aspx/Constants/WM.html
         */
        public enum MouseInput : int
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_MOUSEHWHEEL = 0x020E,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        /*
         * WM constants which are supplied as wParam arguments to HookProc for low level keyboard events.
         * http://www.pinvoke.net/default.aspx/Constants/WM.html
         */
        public enum KeyInput : int
        {
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105
        }

        /*
         * lParam argument to HookProc for low level mouse events.
         * http://pinvoke.net/default.aspx/Structures.MSLLHOOKSTRUCT
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/ns-winuser-msllhookstruct
         * (leaving MsLLHookStructFlags as int for simplicity; wondering if all of these should be uints actually?)
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct MsLLHookStruct
        {
            public Point pt;
            public int mouseData;
            public int flags;
            public int time;
            public UIntPtr dwExtraInfo;
        }

        /*
         * lParam argument to HookProc for low level keyboard events.
         * http://pinvoke.net/default.aspx/Structures/KBDLLHOOKSTRUCT.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/ns-winuser-tagkbdllhookstruct
         * (leaving KbdLLHookStructFlags as uint for simplicity)
         */
        [StructLayout(LayoutKind.Sequential)]
        public struct KbdLLHookStruct
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        /*
         * Callback from SetWindowsHookEx.
         * http://pinvoke.net/default.aspx/Delegates/HookProc.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nc-winuser-hookproc
         * 
         * For low-level hooks, this is actually a LowLevelMouseProc
         * http://www.pinvoke.net/default.aspx/Delegates/LowLevelMouseProc.html
         * https://msdn.microsoft.com/en-us/library/windows/desktop/ms644986.aspx
         * or a LowLevelKeyboardProc
         * http://www.pinvoke.net/default.aspx/Delegates/LowLevelKeyboardProc.html
         * https://msdn.microsoft.com/en-us/library/windows/desktop/ms644985.aspx
         * but we will use the same delegate and marshal wParam / lParam to the required types manually.
         */
        public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        /*
         * http://www.pinvoke.net/default.aspx/user32/SetWindowsHookEx.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowshookexa
         */
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        /*
         * http://www.pinvoke.net/default.aspx/user32/UnhookWindowsHookEx.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-unhookwindowshookex
         */
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /*
         * http://www.pinvoke.net/default.aspx/user32/CallNextHookEx.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-callnexthookex
         * We do not require the overloaded versions for now, since we treat LowLevelMouseProc and LowLevelKeyboardProc as HookProc.
         */
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Simulate mouse / keyboard events

        /*
         * Flags for mouse_event.
         * http://www.pinvoke.net/default.aspx/user32/mouse_event.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-mouse_event
         */
        [Flags]
        public enum MouseEventFlags : uint
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010,
            WHEEL = 0x00000800,
            XDOWN = 0x00000080,
            XUP = 0x00000100
        }

        /**
         * From https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-mouse_event:
         *    If dwFlags contains MOUSEEVENTF_WHEEL, then dwData specifies the amount of wheel movement.
         *    A positive value indicates that the wheel was rotated forward, away from the user; a negative value indicates that the wheel was rotated backward, toward the user.
         *    One wheel click is defined as WHEEL_DELTA, which is 120.
         */
        public const int WHEEL_DELTA = 120;

        /*
         * http://www.pinvoke.net/default.aspx/user32/mouse_event.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-mouse_event
         */
        [DllImport("user32.dll")]
        public static extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        /*
         * http://www.pinvoke.net/default.aspx/user32/keybd_event.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-keybd_event
         */
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        #endregion

        #region Interact with other windows / processes

        /*
         * http://www.pinvoke.net/default.aspx/user32.GetForegroundWindow
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getforegroundwindow
         */
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /*
         * http://www.pinvoke.net/default.aspx/user32.WindowFromPoint
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-windowfrompoint
         */
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point p);

        /*
         * Used by *GetAncestor*.
         * http://www.pinvoke.net/default.aspx/Enums/GetAncestor_Flags.html
         */
        public enum GetAncestorFlags : uint
        {
            GetParent = 1,
            GetRoot = 2,
            GetRootOwner = 3
        }

        /*
         * http://www.pinvoke.net/default.aspx/user32.GetAncestor
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getancestor
         */
        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        /*
         * Helper method for a commonly used requirement.
         * This is - as far as I can tell - the top level window at the given point.
         */
        public static IntPtr RootWindowFromPoint(Point p)
        {
            IntPtr window = WindowFromPoint(p);
            if (window == IntPtr.Zero)
                return IntPtr.Zero;
            else
                return GetAncestor(window, GetAncestorFlags.GetRoot);
        }

        /*
         * Used by SendMessage / PostMessage.
         */
        public enum Messages : uint
        {
            // https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-size
            WM_SIZE = 0x0005,

            // https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-move
            WM_MOVE = 0x0003,

            // https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-close
            WM_CLOSE = 0x0010,

            // https://learn.microsoft.com/en-us/windows/win32/menurc/wm-syscommand
            WM_SYSCOMMAND = 0x0112,
            SC_MINIMIZE = 0xF020,
            SC_MAXIMIZE = 0xF030,
            SC_CLOSE = 0xF060,
            SC_RESTORE = 0xF120
        }

        /*
         * https://pinvoke.net/default.aspx/user32/SendMessage.html
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendmessage
         */
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);

        /*
         * https://www.pinvoke.net/default.aspx/user32.postmessage
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-postmessagea
         */
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);

        /*
         * http://www.pinvoke.net/default.aspx/user32.GetWindowText
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getwindowtextw
         */
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /*
         * https://www.pinvoke.net/default.aspx/user32/GetWindowTextLength.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getwindowtextlengthw
         */
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /*
         * Helper method which converts the output of GetWindowText to a string.
         */
        public static string GetWindowText(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            var stringBuilder = new StringBuilder(length + 1);
            GetWindowText(hWnd, stringBuilder, stringBuilder.Capacity);
            return stringBuilder.ToString();
        }

        /*
         * Required for GetWindowPlacement function
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-windowplacement
         * https://pinvoke.net/default.aspx/Structures/WINDOWPLACEMENT.html
         */
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public uint Length;
            public uint Flags;
            public uint ShowCmd;
            public Point MinPosition;
            public Point MaxPosition;
            public Rect NormalPosition;

            public static WINDOWPLACEMENT Default
            {
                get
                {
                    WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                    result.Length = (uint)Marshal.SizeOf(result);
                    return result;
                }
            }
        }

        /*
         * https://www.pinvoke.net/default.aspx/user32.getwindowplacement
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowplacement
         */
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        /*
         * Used by ShowWindow
         */
        public enum ShowCommands : int
        {
            SW_SHOWNORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_MINIMIZE = 6,
            SW_RESTORE = 9
        }

        /*
         * https://www.pinvoke.net/default.aspx/user32.showwindow
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
         */
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /*
         * https://www.pinvoke.net/default.aspx/user32.movewindow
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-movewindow
         */
        [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool redraw);

        #endregion

        #region Monitor information

        /* 
         * Used in GetMonitorInfo.
         */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFO
        {
            public int Size;
            public Rect Monitor;
            public Rect WorkArea;
            public uint Flags;

            public const uint MONITORINFOF_PRIMARY = 1;

            public void Init()
            {
                this.Size = Marshal.SizeOf(typeof(MONITORINFO));
            }
        }

        /*
         * https://www.pinvoke.net/default.aspx/user32/GetMonitorInfo.html
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmonitorinfow
         */
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        /*
         * https://pinvoke.net/default.aspx/user32/EnumDisplayMonitors.html
         * https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors
         */
        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        // Somewhat more high-level version of the MONITORINFO struct
        public readonly struct MonitorInformation
        {
            // Area filled by a fullscreen window on this monitor
            public readonly Rect MonitorArea;
            // Area filled by a maximized window on this monitor
            public readonly Rect WorkArea;
            // Whether this is the primary monitor
            public readonly bool Primary;

            public MonitorInformation(Rect monitorArea, Rect workArea, bool primary)
            {
                this.MonitorArea = monitorArea;
                this.WorkArea = workArea;
                this.Primary = primary;
            }
        }

        /*
         * Helper method for common task: retrieve information about all available monitors.
         */
        public static List<MonitorInformation> ListMonitors()
        {
            List<MonitorInformation> result = new List<MonitorInformation>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
                {
                    MONITORINFO info = new MONITORINFO();
                    info.Init();
                    bool success = GetMonitorInfo(hMonitor, ref info);
                    if (success)
                    {
                        result.Add(new MonitorInformation(info.Monitor,
                            info.WorkArea,
                            (info.Flags & MONITORINFO.MONITORINFOF_PRIMARY) != 0));
                    }
                    return true;
                },
                IntPtr.Zero);
            return result;
        }

        #endregion

        #region Other stuff

        /*
         * http://www.pinvoke.net/default.aspx/user32/GetKeyState.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getkeystate
         * Note in particular (from pinvoke):
         *    The Enum Keys in System.Windows.Forms has the key code used for this function [...].
         *    If casting in this manner, beware that the Keys.Shift member does not behave in the way that you would expect;
         *    you should use one of Keys.Menu, Keys.LMenu, Keys.RMenu instead.
         *    Return value will be 0 if off and 1 if on as a toggle and -127 if key held down.
         */
        [DllImport("user32.dll")]
        public static extern short GetKeyState(System.Windows.Forms.Keys nVirtKey);

        /*
         * https://www.pinvoke.net/default.aspx/user32.getcursorpos
         * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorpos
         */
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Point lpPoint);

        /*
         * http://www.pinvoke.net/default.aspx/kernel32/GetModuleHandle.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/libloaderapi/nf-libloaderapi-getmodulehandlea
         */
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }

    /*
     * Part of MsLLHookStruct, added SquareDistance method and made public for convenience
     * http://pinvoke.net/default.aspx/Structures.POINT
     * https://docs.microsoft.com/en-us/windows/desktop/api/windef/ns-windef-point
     */
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Point : IEquatable<Point>
    {
        public readonly int x;
        public readonly int y;

        public override bool Equals(object obj)
        {
            return obj is Point point && Equals(point);
        }

        public bool Equals(Point other)
        {
            return x == other.x &&
                   y == other.y;
        }

        public override int GetHashCode()
        {
            int hashCode = 1502939027;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            return hashCode;
        }

        public double SquareDistance(Point other) =>
            (x - other.x) * (x - other.x) + (y - other.y) * (y - other.y);

        public override string ToString() => $"({x}, {y})";

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }
    }

    /* 
     * Part of WINDOWPLACEMENT and MONITORINFO structs, made public for convenience
     * https://pinvoke.net/default.aspx/Structures/RECT.html
     */
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Rect : IEquatable<Rect>
    {
        public readonly int Left, Top, Right, Bottom;

        public Rect(int l, int t, int r, int b)
        {
            this.Left = l;
            this.Top = t;
            this.Right = r;
            this.Bottom = b;
        }

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public static Rect FromDimensions(int left, int top, int width, int height) => new Rect(left, top, left + width, top + height);

        public override bool Equals(object obj)
        {
            return obj is Rect rect && Equals(rect);
        }

        public bool Equals(Rect other)
        {
            return Left == other.Left &&
                   Top == other.Top &&
                   Right == other.Right &&
                   Bottom == other.Bottom;
        }

        public override int GetHashCode()
        {
            int hashCode = -1819631549;
            hashCode = hashCode * -1521134295 + Left.GetHashCode();
            hashCode = hashCode * -1521134295 + Top.GetHashCode();
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            hashCode = hashCode * -1521134295 + Bottom.GetHashCode();
            return hashCode;
        }

        public override string ToString() => $"({Left}, {Top}, {Right}, {Bottom})";

        public static bool operator ==(Rect left, Rect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect left, Rect right)
        {
            return !(left == right);
        }
    }
}