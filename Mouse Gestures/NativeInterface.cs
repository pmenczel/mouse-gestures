using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WMG.Gestures;
using static WMG.Core.WinAPI;

namespace WMG.Core
{
    /*
     * Sets up low level keyboard and mouse hooks, and passes the events on to a gesture analyzer.
     * 
     * While a gesture is being performed, all events are consumed, making the following steps necessary:
     * 1) If the right mouse button is clicked without any gesturing (click-through), the mouse click must be emulated afterwards.
     * 2) When the gesture is finished, we must emulate the pressing / releasing of modifier keys so that the operating system doesn't get confused about their state.
     */
    public class NativeInterface : IDisposable
    {
        // values of IntPtr.Zero indicate that there is no hook installed currently
        private IntPtr mouseHook = IntPtr.Zero;
        private IntPtr keyHook = IntPtr.Zero;

        private readonly IGestureAnalyzer analyzer;

        internal bool forceDisabled = false; // can be set to true temporarily for simulating events without notifying the analyzer

        public NativeInterface(IGestureAnalyzer analyzer)
        {
            this.analyzer = analyzer;
            analyzer.OnClickThrough += ClickThrough;

            CheckHook();
            Settings.OnChange += CheckHook;
        }

        private void CheckHook()
        {
            if (Settings.Disabled)
                Unhook();
            else
                Hook();
        }

        private void Hook()
        {
            if (disposed) return;
            if (mouseHook != IntPtr.Zero && keyHook != IntPtr.Zero) return; // nothing to do

            analyzer.Reset();

            // retrieve module handle of our process, which is required for low level hooks
            ProcessModule currentModule = Process.GetCurrentProcess().MainModule;
            IntPtr moduleHandle = WinAPI.GetModuleHandle(currentModule.ModuleName);

            if (mouseHook == IntPtr.Zero) // Try setting the mouse hook
            {
                mouseHook = WinAPI.SetWindowsHookEx(HookType.WH_MOUSE_LL, MouseHookProc, moduleHandle, 0);
                // A return value of IntPtr.Zero indicates that something went wrong. (The error code is retrieved by HookException.)
                if (mouseHook == IntPtr.Zero) throw new HookException();
            }

            if (keyHook == IntPtr.Zero) // Try setting the keyboard hook
            {
                keyHook = WinAPI.SetWindowsHookEx(HookType.WH_KEYBOARD_LL, KeyHookProc, moduleHandle, 0);
                if (keyHook == IntPtr.Zero) // Something went wring, let's try removing the mouse hook before throwing an Exception
                {
                    var ex = new HookException();
                    try
                    {
                        Unhook();
                        throw ex;
                    }
                    catch (HookException ex2)
                    {
                        throw new AggregateException("There was an error uninstalling low level hooks after another error.", ex, ex2);
                    }
                }
            }
        }

        private void Unhook()
        {
            analyzer.Reset();

            Exception ex = null; // we only throw possible exceptions after trying to uninstall both hooks
            if (mouseHook != IntPtr.Zero)
            {
                bool success = WinAPI.UnhookWindowsHookEx(mouseHook);
                if (!success) ex = new HookException();
            }
            mouseHook = IntPtr.Zero;

            if (keyHook != IntPtr.Zero)
            {
                bool success = WinAPI.UnhookWindowsHookEx(keyHook);
                if (!success) ex = new HookException();
            }
            keyHook = IntPtr.Zero;

            if (ex != null) throw ex;
        }

        /*
         * Handles a low level mouse event.
         * 
         * nCode: If this is less than zero, we must ignore the event and call CallNextHookEx.
         * wParam: Actually a *MouseInput*.
         * lParam: Actually a pointer to an *MsLLHookStruct*.
         */
        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || forceDisabled)
                return WinAPI.CallNextHookEx(mouseHook, nCode, wParam, lParam);
            MsLLHookStruct mouseInfo = Marshal.PtrToStructure<MsLLHookStruct>(lParam);
            Point currentMouseLocation = mouseInfo.pt;

            // whether the event should be consumed (i.e., whether CallNextHookEx should be called at the end of the method)
            bool consume = false;

            // Let's update the mouse location first, and then check what actually happened.
            analyzer.MouseMovement(currentMouseLocation); // we don't consume because of only mouse movement

            switch ((MouseInput)wParam.ToInt32())
            {
                // Left mouse button was pressed / released -> update modifiers.
                case MouseInput.WM_LBUTTONDOWN:
                    consume |= analyzer.ModifiersDown(Modifiers.LMB);
                    break;

                case MouseInput.WM_LBUTTONUP:
                    consume |= analyzer.ModifiersUp(Modifiers.LMB);
                    break;

                // Mouse wheel was scrolled (vertically)
                // -> find out by how much, and call MouseWheelMovement that amount of times.
                case MouseInput.WM_MOUSEWHEEL:
                    int amount = (mouseInfo.mouseData >> 16) / WinAPI.WHEEL_DELTA;
                    var direction = (amount >= 0) ? Direction.UP : Direction.DOWN;
                    if (amount < 0) amount = -amount;
                    for (int i = 0; i < amount; i++)
                        consume |= analyzer.MouseWheelMovement(direction);
                    break;

                // Mouse wheel was scrolled (horizontally).
                case MouseInput.WM_MOUSEHWHEEL:
                    amount = (mouseInfo.mouseData >> 16) / WinAPI.WHEEL_DELTA;
                    direction = (amount >= 0) ? Direction.RIGHT : Direction.LEFT;
                    if (amount < 0) amount = -amount;
                    for (int i = 0; i < amount; i++)
                        consume |= analyzer.MouseWheelMovement(direction);
                    break;

                // Start a new gesture! (maybe)
                case MouseInput.WM_RBUTTONDOWN:
                    if (CanStartGestureAt(currentMouseLocation))
                        consume |= analyzer.RmbDown(currentMouseLocation, CurrentlyPressedModifiers());
                    break;

                // End of the gesture -> finish up.
                case MouseInput.WM_RBUTTONUP:
                    consume |= analyzer.RmbUp(currentMouseLocation, CleanupModifiers);
                    break;
            }

            if (consume)
                // The documentation https://docs.microsoft.com/en-us/windows/desktop/api/winuser/nc-winuser-hookproc says
                //    "If the hook procedure does not call CallNextHookEx, the return value should be zero"
                // but returning IntPtr.Zero here does not consume the event! Returning 1 seems to be the correct thing, don't know why...
                return (IntPtr)1;
            else
                return WinAPI.CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        /*
         * Handles a low level keyboard event.
         * 
         * nCode: If this is less than zero, we must ignore the event and call CallNextHookEx.
         * wParam: Actually a *KeyInput*.
         * lParam: Actually a pointer to a *KbdLLHookStruct*.
         */
        private IntPtr KeyHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0 || forceDisabled)
                return WinAPI.CallNextHookEx(keyHook, nCode, wParam, lParam);
            KbdLLHookStruct kbdStruct = Marshal.PtrToStructure<KbdLLHookStruct>(lParam);

            Modifiers keyInfo = KeysToModifiers((Keys)kbdStruct.vkCode);
            if (keyInfo == Modifiers.NONE) // no relevant key pressed
                return WinAPI.CallNextHookEx(keyHook, nCode, wParam, lParam);

            bool consume = false;

            switch ((KeyInput)wParam)
            {
                case KeyInput.WM_KEYDOWN:
                case KeyInput.WM_SYSKEYDOWN: // not sure what's the difference, let's catch both
                    consume |= analyzer.ModifiersDown(keyInfo);
                    break;
                case KeyInput.WM_KEYUP:
                case KeyInput.WM_SYSKEYUP:
                    consume |= analyzer.ModifiersUp(keyInfo);
                    break;
            }

            if (consume)
                return (IntPtr)1;
            else
                return WinAPI.CallNextHookEx(keyHook, nCode, wParam, lParam);
        }

        /*
         * Determines whether a gesture can be started right now by a right mouse click at the specified location.
         * TODO: the idea is that we should not register mouse gestures in applications such as Opera, which use mouse gestures themselves
         */
        private bool CanStartGestureAt(Point mouseLocation)
        {
            return true;
        }

        // Helper method for KeyHookProc
        private Modifiers KeysToModifiers(Keys keys)
        {
            switch (keys)
            {
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.ShiftKey:         // still don't understand 100% when this is used, and when LShiftKey / RShiftKey are used.
                    return Modifiers.SHIFT; // let's just be on the safe side and check for all possibilities
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.ControlKey:
                    return Modifiers.CONTROL;
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.Menu:
                    return Modifiers.ALT;
                case Keys.LButton: // not going to happen if we come from KeyHookProc, but let's include it anyways
                    return Modifiers.LMB;
                default:
                    return Modifiers.NONE;
            }
        }

        /*
         * Helper method to find out which modifier keys (shift, ctrl, alt, lmb) are currently pressed.
         * Note that this might not work properly any more as soon as low level events have been consumed.
         */
        private Modifiers CurrentlyPressedModifiers()
        {
            Modifiers result = Modifiers.NONE;
            if (WinAPI.GetKeyState(Keys.ShiftKey) < 0) // GetKeyState sets the high-order bit, i.e. the sign bit, if the key is pressed
                result += Modifiers.SHIFT;     // Keys.ShiftKey works for both left and right shift key
            if (WinAPI.GetKeyState(Keys.ControlKey) < 0)
                result += Modifiers.CONTROL;
            if (WinAPI.GetKeyState(Keys.Menu) < 0)
                result += Modifiers.ALT;
            if (WinAPI.GetKeyState(Keys.LButton) < 0)
                result += Modifiers.LMB;
            return result;
        }

        /*
         * This will be called after a gesture is completed, before event subscribers are notified.
         * Here, we deal with the situation where a modifier was already pressed at the beginning of the gesture, but released during the gesture.
         * Since the release event was consumed, the key would still appear pressed to other programs and the operating system.
         * We must therefore simulate release events for all modifiers that were initially pressed and are not pressed any more.
         */
        private void CleanupModifiers(Gesture gesture)
        {
            var initialModifiers = gesture.InitialModifiers;
            var lastModifierChange = gesture.Actions.Where(a => a is ModifierChangeAction).LastOrDefault();
            if (lastModifierChange == null)
                return;
            var currentModifiers = (lastModifierChange as ModifierChangeAction).newModifiers;

            forceDisabled = true;

            if (initialModifiers.Shift && !currentModifiers.Shift)
            {
                // There are several shift keys, and we have to specify which one we want to release.
                // We therefore ask the operating system, which shift keys it _thinks_ to be still pressed.
                // (Same for Control and Alt.)
                if (WinAPI.GetKeyState(Keys.LShiftKey) < 0) // GetKeyState sets the high-order bit, i.e. the sign bit, if the key is pressed
                    WinAPI.keybd_event((byte)Keys.LShiftKey, 0, 2, UIntPtr.Zero); // 2 = KEYEVENTF_KEYUP
                if (WinAPI.GetKeyState(Keys.RShiftKey) < 0)
                    WinAPI.keybd_event((byte)Keys.RShiftKey, 0, 2, UIntPtr.Zero);
            }
            if (initialModifiers.Control && !currentModifiers.Control)
            {
                if (WinAPI.GetKeyState(Keys.LControlKey) < 0)
                    WinAPI.keybd_event((byte)Keys.LControlKey, 0, 2, UIntPtr.Zero);
                if (WinAPI.GetKeyState(Keys.RControlKey) < 0)
                    WinAPI.keybd_event((byte)Keys.RControlKey, 0, 2, UIntPtr.Zero);
            }
            if (initialModifiers.Alt && !currentModifiers.Alt)
            {
                if (WinAPI.GetKeyState(Keys.LMenu) < 0)
                    WinAPI.keybd_event((byte)Keys.LMenu, 0, 2, UIntPtr.Zero);
                if (WinAPI.GetKeyState(Keys.RMenu) < 0)
                    WinAPI.keybd_event((byte)Keys.RMenu, 0, 2, UIntPtr.Zero);
            }
            if (initialModifiers.Lmb && !currentModifiers.Lmb)
            {
                bool succ = WinAPI.GetCursorPos(out Point currentMousePos); // it's probably not too important where we release the mouse, but let's try to use the current mouse position
                WinAPI.mouse_event(MouseEventFlags.ABSOLUTE | MouseEventFlags.LEFTUP, succ ? currentMousePos.x : 0, succ ? currentMousePos.y : 0, 0, UIntPtr.Zero);
            }

            forceDisabled = false;
        }

        /*
         * This will be called after a click-through event. Here, we must simulate the right mouse button click that we already, "accidentally", consumed.
         */
        private void ClickThrough(Point position)
        {
            forceDisabled = true;
            WinAPI.mouse_event(MouseEventFlags.ABSOLUTE | MouseEventFlags.RIGHTDOWN, position.x, position.y, 0, UIntPtr.Zero);
            WinAPI.mouse_event(MouseEventFlags.ABSOLUTE | MouseEventFlags.RIGHTUP, position.x, position.y, 0, UIntPtr.Zero);
            forceDisabled = false;
        }

        #region IDisposable Support
        private bool disposed = false;

        public void Dispose()
        {
            if (!disposed)
            {
                Unhook();
                Settings.OnChange -= CheckHook;
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~NativeInterface()
        {
            Dispose();
        }
        #endregion
    }

    [Serializable]
    internal class HookException : Exception
    {
        public HookException() : base(CreateMessage()) { }

        private static string CreateMessage() => $"There was an error while installing / uninstalling a low level hook. Error code: {Marshal.GetLastWin32Error()}";
    }
}
