using System;
using WMG.Core;
using WMG.Gestures;

namespace WMG.Reactions
{
    public enum ReactionTarget : int
    {
        ACTIVE_WINDOW,
        WINDOW_AT_GESTURE_START,
        WINDOW_AT_POINTER
    }

    public static class ReactionTargetUtils
    {
        public static IntPtr FindTargetWindow(Gesture gesture, ReactionTarget target)
        {
            POINT? position = null;
            switch (target)
            {
                case ReactionTarget.ACTIVE_WINDOW:
                    return WinAPI.GetForegroundWindow();
                case ReactionTarget.WINDOW_AT_GESTURE_START:
                    position = gesture.RawData?.InitialMousePosition();
                    goto default;
                case ReactionTarget.WINDOW_AT_POINTER:
                    position = gesture.RawData?.CurrentMousePosition();
                    goto default;
                default:
                    if (position.HasValue)
                    {
                        return WinAPI.RootWindowFromPoint(position.Value);
                    }
                    else
                    {
                        return IntPtr.Zero;
                    }
            }
        }
    }

    public sealed class CloseWindowReaction : Reaction
    {
        public override ReactionType RType => CloseWindowType.INSTANCE;

        public ReactionTarget Target { get; }

        public CloseWindowReaction(ReactionTarget target)
        {
            this.Target = target;
        }

        public override void Perform(Gesture gesture, IContext context)
        {
            IntPtr window = ReactionTargetUtils.FindTargetWindow(gesture, Target);
            if (window != IntPtr.Zero)
            {
                // WinAPI.PostMessage(window, (uint)WinAPI.Messages.WM_CLOSE, UIntPtr.Zero, IntPtr.Zero);
                WinAPI.PostMessage(window,
                    (uint)WinAPI.Messages.WM_SYSCOMMAND,
                    new UIntPtr((uint)WinAPI.Messages.SC_CLOSE),
                    IntPtr.Zero);
            }
        }
    }

    public sealed class CloseWindowType : ReactionType
    {
        private CloseWindowType() { }

        private static readonly string IDENTIFIER = "CloseWindow";

        public static readonly CloseWindowType INSTANCE = new CloseWindowType();

        public override string StoreString(Reaction r)
        {
            if (r is CloseWindowReaction c)
            {
                return IDENTIFIER + (int)c.Target;
            }
            return null;
        }

        public override Reaction LoadString(string str)
        {
            if (str.StartsWith(IDENTIFIER))
            {
                string targetString = str.Substring(IDENTIFIER.Length);
                if (Enum.TryParse<ReactionTarget>(targetString, out ReactionTarget target))
                {
                    return new CloseWindowReaction(target);
                }
            }
            return null;

        }
    }

    public sealed class MinimizeWindowReaction : Reaction
    {
        public override ReactionType RType => MinimizeWindowType.INSTANCE;

        public ReactionTarget Target { get; }

        public bool RestoreIfMaximized { get; }

        public MinimizeWindowReaction(ReactionTarget target, bool restoreIfMaximized)
        {
            this.Target = target;
            this.RestoreIfMaximized = restoreIfMaximized;
        }

        public override void Perform(Gesture gesture, IContext context)
        {
            IntPtr window = ReactionTargetUtils.FindTargetWindow(gesture, Target);
            if (window != IntPtr.Zero)
            {
                if (RestoreIfMaximized && IsMaximized(window))
                {
                    WinAPI.PostMessage(window,
                        (uint)WinAPI.Messages.WM_SYSCOMMAND,
                        new UIntPtr((uint)WinAPI.Messages.SC_RESTORE),
                        IntPtr.Zero);
                }
                else
                {
                    WinAPI.PostMessage(window,
                        (uint)WinAPI.Messages.WM_SYSCOMMAND,
                        new UIntPtr((uint)WinAPI.Messages.SC_MINIMIZE),
                        IntPtr.Zero);
                }
            }
        }

        private static bool IsMaximized(IntPtr hWnd)
        {
            WinAPI.WINDOWPLACEMENT placement = WinAPI.WINDOWPLACEMENT.Default;
            if (WinAPI.GetWindowPlacement(hWnd, ref placement))
            {
                return placement.ShowCmd == (int)WinAPI.ShowCommands.SW_SHOWMAXIMIZED;
            }
            return false;
        }
    }

    public sealed class MinimizeWindowType : ReactionType
    {
        private MinimizeWindowType() { }

        private static readonly string IDENTIFIER = "MinimizeWindow";

        public static readonly MinimizeWindowType INSTANCE = new MinimizeWindowType();

        public override string StoreString(Reaction r)
        {
            if (r is MinimizeWindowReaction m)
            {
                return IDENTIFIER + (int)m.Target + ';' + m.RestoreIfMaximized;
            }
            return null;
        }

        public override Reaction LoadString(string str)
        {
            if (str.StartsWith(IDENTIFIER))
            {
                string remainder = str.Substring(IDENTIFIER.Length);
                string[] parts = remainder.Split(';');
                if (parts.Length != 2)
                    return null;
                if (Enum.TryParse<ReactionTarget>(parts[0], out ReactionTarget target)
                    && bool.TryParse(parts[1], out bool rim))
                {
                    return new MinimizeWindowReaction(target, rim);
                }
            }
            return null;
        }
    }

    public sealed class MaximizeWindowReaction : Reaction
    {
        public override ReactionType RType => MaximizeWindowType.INSTANCE;

        public ReactionTarget Target { get; }

        public MaximizeWindowReaction(ReactionTarget target)
        {
            this.Target = target;
        }

        public override void Perform(Gesture gesture, IContext context)
        {
            IntPtr window = ReactionTargetUtils.FindTargetWindow(gesture, Target);
            if (window != IntPtr.Zero)
            {
                WinAPI.PostMessage(window,
                    (uint)WinAPI.Messages.WM_SYSCOMMAND,
                    new UIntPtr((uint)WinAPI.Messages.SC_MAXIMIZE),
                    IntPtr.Zero);
            }
        }
    }

    public sealed class MaximizeWindowType : ReactionType
    {
        private MaximizeWindowType() { }

        private static readonly string IDENTIFIER = "MaximizeWindow";

        public static readonly MaximizeWindowType INSTANCE = new MaximizeWindowType();

        public override string StoreString(Reaction r)
        {
            if (r is MaximizeWindowReaction m)
            {
                return IDENTIFIER + (int)m.Target;
            }
            return null;
        }

        public override Reaction LoadString(string str)
        {
            if (str.StartsWith(IDENTIFIER))
            {
                string targetString = str.Substring(IDENTIFIER.Length);
                if (Enum.TryParse<ReactionTarget>(targetString, out ReactionTarget target))
                {
                    return new MaximizeWindowReaction(target);
                }
            }
            return null;
        }
    }
}