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
                WinAPI.SendMessage(window, (uint)WinAPI.Messages.WM_CLOSE, UIntPtr.Zero, IntPtr.Zero);
        }
    }

    public sealed class CloseWindowType : ReactionType
    {
        private CloseWindowType() { }

        private static readonly string IDENTIFIER = "CloseWindowType";

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
}