using System;
using System.Collections.Generic;

namespace WMG.Gestures
{
    /* There are three types of Action: mouse movements, mouse wheel movements and changes of modifier keys.
     * The former two are specified by a direction (up, down, left or right), the latter is specified by the new set of pressed modifier keys (shift, control, alt, left mouse button).
     */
    public abstract class WMGAction // named "WMGAction" instead of just "Action" because of System.Action
    {
        /* Do not allow for additional child classes. */
        internal WMGAction() { }

        /* Child classes must implement ToString. */
        public abstract override string ToString();

        public string PrettyPrint() => $"[{ToString()}]";

        public static WMGAction FromString(string str)
        {
            switch (str)
            {
                case "UP": return new MouseMovementAction(Direction.UP);
                case "DOWN": return new MouseMovementAction(Direction.DOWN);
                case "LEFT": return new MouseMovementAction(Direction.LEFT);
                case "RIGHT": return new MouseMovementAction(Direction.RIGHT);

                case "WHEEL UP": return new MouseWheelAction(Direction.UP);
                case "WHEEL DOWN": return new MouseWheelAction(Direction.DOWN);
                case "WHEEL LEFT": return new MouseWheelAction(Direction.LEFT);
                case "WHEEL RIGHT": return new MouseWheelAction(Direction.RIGHT);

                default: return new ModifierChangeAction(Modifiers.FromString(str));
            }
        }
    }

    public sealed class MouseMovementAction : WMGAction, IEquatable<MouseMovementAction>
    {
        public readonly Direction direction;

        public MouseMovementAction(Direction direction)
        {
            this.direction = direction;
        }

        public override string ToString()
        {
            switch (direction)
            {
                case Direction.UP: return "UP";
                case Direction.DOWN: return "DOWN";
                case Direction.LEFT: return "LEFT";
                case Direction.RIGHT: return "RIGHT";
                default: return "";
            }
        }

        // --- generated code ---

        public override bool Equals(object obj) => Equals(obj as MouseMovementAction);

        public bool Equals(MouseMovementAction other)
        {
            return other != null && direction == other.direction;
        }

        public override int GetHashCode() => -1006820870 + direction.GetHashCode();
    }

    public sealed class MouseWheelAction : WMGAction, IEquatable<MouseWheelAction>
    {
        public readonly Direction direction;

        public MouseWheelAction(Direction direction)
        {
            this.direction = direction;
        }

        public override string ToString()
        {
            switch (direction)
            {
                case Direction.UP: return "WHEEL UP";
                case Direction.DOWN: return "WHEEL DOWN";
                case Direction.LEFT: return "WHEEL LEFT";
                case Direction.RIGHT: return "WHEEL RIGHT";
                default: return "";
            }
        }

        // --- generated code ---

        public override bool Equals(object obj) => Equals(obj as MouseWheelAction);

        public bool Equals(MouseWheelAction other)
        {
            return other != null && direction == other.direction;
        }

        public override int GetHashCode() => -1006820870 + direction.GetHashCode();
    }

    public sealed class ModifierChangeAction : WMGAction, IEquatable<ModifierChangeAction>
    {
        public readonly Modifiers newModifiers;

        public ModifierChangeAction(Modifiers newModifiers)
        {
            this.newModifiers = newModifiers;
        }

        public override string ToString() => newModifiers.ToString();

        // --- generated code ---

        public override bool Equals(object obj)
        {
            return Equals(obj as ModifierChangeAction);
        }

        public bool Equals(ModifierChangeAction other)
        {
            return other != null &&
                   EqualityComparer<Modifiers>.Default.Equals(newModifiers, other.newModifiers);
        }

        public override int GetHashCode()
        {
            return 578441165 + EqualityComparer<Modifiers>.Default.GetHashCode(newModifiers);
        }
    }

    public enum Direction
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }
}
