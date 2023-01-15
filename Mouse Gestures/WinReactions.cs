using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
            Point? position = null;
            switch (target)
            {
                case ReactionTarget.ACTIVE_WINDOW:
                    return WinAPI.GetForegroundWindow();
                case ReactionTarget.WINDOW_AT_GESTURE_START:
                    position = gesture.RawData?.InitialMousePosition();
                    break;
                case ReactionTarget.WINDOW_AT_POINTER:
                    position = gesture.RawData?.CurrentMousePosition();
                    break;
            }

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

        public override string StoreString(Reaction r, ISerializationContext context)
        {
            if (r is CloseWindowReaction c)
            {
                return IDENTIFIER + (int)c.Target;
            }
            return null;
        }

        public override Reaction LoadString(string str, ISerializationContext context)
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

        public override string StoreString(Reaction r, ISerializationContext context)
        {
            if (r is MinimizeWindowReaction m)
            {
                return IDENTIFIER + (int)m.Target + ';' + m.RestoreIfMaximized;
            }
            return null;
        }

        public override Reaction LoadString(string str, ISerializationContext context)
        {
            if (str.StartsWith(IDENTIFIER))
            {
                string remainder = str.Substring(IDENTIFIER.Length);
                string[] parts = remainder.Split(';');

                if (parts.Length == 2 &&
                    Enum.TryParse<ReactionTarget>(parts[0], out ReactionTarget target) &&
                    bool.TryParse(parts[1], out bool rim))
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

        public override string StoreString(Reaction r, ISerializationContext context)
        {
            if (r is MaximizeWindowReaction m)
            {
                return IDENTIFIER + (int)m.Target;
            }
            return null;
        }

        public override Reaction LoadString(string str, ISerializationContext context)
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

    public sealed class MoveWindowReaction : Reaction
    {
        public override ReactionType RType => MoveWindowType.INSTANCE;

        public ReactionTarget Target { get; }

        public Rect Location { get; }

        public MoveWindowReaction(ReactionTarget target, Rect location)
        {
            Target = target;
            Location = location;
        }

        public override void Perform(Gesture gesture, IContext context)
        {
            IntPtr window = ReactionTargetUtils.FindTargetWindow(gesture, Target);
            if (window != IntPtr.Zero)
            {
                WinAPI.MoveWindow(window, Location.Left, Location.Top, Location.Width, Location.Height, true);
            }
        }
    }

    public sealed class MoveWindowType : ReactionType
    {
        private MoveWindowType() { }

        private static readonly string IDENTIFIER = "MoveWindow";

        public static readonly MoveWindowType INSTANCE = new MoveWindowType();

        public override string StoreString(Reaction r, ISerializationContext context)
        {
            if (r is MoveWindowReaction m)
            {
                return IDENTIFIER + (int)m.Target + ';' +
                    m.Location.Left + ";" + m.Location.Top + ";" +
                    m.Location.Width + ";" + m.Location.Height;
            }
            return null;
        }

        public override Reaction LoadString(string str, ISerializationContext context)
        {
            if (str.StartsWith(IDENTIFIER))
            {
                string remainder = str.Substring(IDENTIFIER.Length);
                string[] parts = remainder.Split(';');

                if (parts.Length == 5 &&
                    Enum.TryParse<ReactionTarget>(parts[0], out ReactionTarget target)
                    && int.TryParse(parts[1], out int l)
                    && int.TryParse(parts[2], out int t)
                    && int.TryParse(parts[3], out int w)
                    && int.TryParse(parts[4], out int h))
                {
                    return new MoveWindowReaction(target, Rect.FromDimensions(l, t, w, h));
                }
            }
            return null;
        }
    }

}