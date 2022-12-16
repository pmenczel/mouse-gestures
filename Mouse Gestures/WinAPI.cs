using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WMG.Core
{
    internal sealed class WinAPI
    {
        #region Mouse / keyboard hooks

        /*
         * Argument for SetWindowsHookEx.
         * http://pinvoke.net/default.aspx/Enums/HookType.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowshookexa
         * (we only include those constants that we need here)
         */
        internal enum HookType : int
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
        internal enum MouseInput : int
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
        internal enum KeyInput : int
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
        internal struct MsLLHookStruct
        {
            internal POINT pt;
            internal int mouseData;
            internal int flags;
            internal int time;
            internal UIntPtr dwExtraInfo;
        }

        /*
         * lParam argument to HookProc for low level keyboard events.
         * http://pinvoke.net/default.aspx/Structures/KBDLLHOOKSTRUCT.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/ns-winuser-tagkbdllhookstruct
         * (leaving KbdLLHookStructFlags as uint for simplicity)
         */
        [StructLayout(LayoutKind.Sequential)]
        internal class KbdLLHookStruct
        {
            internal uint vkCode;
            internal uint scanCode;
            internal uint flags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
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
        internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        /*
         * http://www.pinvoke.net/default.aspx/user32/SetWindowsHookEx.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-setwindowshookexa
         */
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        /*
         * http://www.pinvoke.net/default.aspx/user32/UnhookWindowsHookEx.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-unhookwindowshookex
         */
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /*
         * http://www.pinvoke.net/default.aspx/user32/CallNextHookEx.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-callnexthookex
         * We do not require the overloaded versions for now, since we treat LowLevelMouseProc and LowLevelKeyboardProc as HookProc.
         */
        [DllImport("user32.dll")]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Simulate mouse / keyboard events

        /*
         * Flags for mouse_event.
         * http://www.pinvoke.net/default.aspx/user32/mouse_event.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-mouse_event
         */
        [Flags]
        internal enum MouseEventFlags : uint
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
        internal const int WHEEL_DELTA = 120;

        /*
         * http://www.pinvoke.net/default.aspx/user32/mouse_event.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-mouse_event
         */
        [DllImport("user32.dll")]
        internal static extern void mouse_event(MouseEventFlags dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        /*
         * http://www.pinvoke.net/default.aspx/user32/keybd_event.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-keybd_event
         */
        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        #endregion

        #region Interact with other windows / processes

        /*
         * http://www.pinvoke.net/default.aspx/user32.GetForegroundWindow
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getforegroundwindow
         */
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        /*
         * http://www.pinvoke.net/default.aspx/user32.WindowFromPoint
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-windowfrompoint
         */
        [DllImport("user32.dll")]
        internal static extern IntPtr WindowFromPoint(POINT p);

        /*
         * Used by *GetAncestor*.
         * http://www.pinvoke.net/default.aspx/Enums/GetAncestor_Flags.html
         */
        internal enum GetAncestorFlags : uint
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
        internal static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

        /*
         * Helper method for a commonly used requirement.
         * This is - as far as I can tell - the top level window at the given point.
         */
        internal static IntPtr RootWindowFromPoint(POINT p)
        {
            IntPtr window = WindowFromPoint(p);
            if (window == IntPtr.Zero)
                return IntPtr.Zero;
            else
                return GetAncestor(window, GetAncestorFlags.GetRoot);
        }

        internal enum Messages : uint
        {
            // https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-size
            WM_SIZE = 0x0005,

            // https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-move
            WM_MOVE = 0x0003,

            // https://learn.microsoft.com/en-us/windows/win32/winmsg/wm-close
            WM_CLOSE = 0x0010,

            // https://learn.microsoft.com/en-us/windows/win32/menurc/wm-syscommand
            WM_SYSCOMMAND = 0x0112,
            SC_CLOSE = 0xF060
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
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /*
         * https://www.pinvoke.net/default.aspx/user32/GetWindowTextLength.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nf-winuser-getwindowtextlengthw
         */
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        /*
         * Helper method which converts the output of GetWindowText to a string.
         */
        internal static string GetWindowText(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            var stringBuilder = new StringBuilder(length + 1);
            GetWindowText(hWnd, stringBuilder, stringBuilder.Capacity);
            return stringBuilder.ToString();
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
        internal static extern short GetKeyState(System.Windows.Forms.Keys nVirtKey);

        /*
         * https://www.pinvoke.net/default.aspx/user32.getcursorpos
         * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorpos
         */
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        /*
         * http://www.pinvoke.net/default.aspx/kernel32/GetModuleHandle.html
         * https://docs.microsoft.com/en-us/windows/desktop/api/libloaderapi/nf-libloaderapi-getmodulehandlea
         */
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }

    /*
     * Part of MsLLHookStruct, added SquareDistance method and made public for convenience
     * http://pinvoke.net/default.aspx/Structures.POINT
     * https://docs.microsoft.com/en-us/windows/desktop/api/windef/ns-windef-point
     */
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct POINT
    {
        public readonly int x;
        public readonly int y;

        public double SquareDistance(POINT other) =>
            (x - other.x) * (x - other.x) + (y - other.y) * (y - other.y);

        public override string ToString() => $"({x}, {y})";
    }
}