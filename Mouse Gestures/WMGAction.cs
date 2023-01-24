using System;
using System.Collections.Generic;

namespace WMG.Gestures
{
    /* 
     * There are three types of Action: mouse movements, mouse wheel movements and changes of modifier keys.
     * The former two are specified by a direction (up, down, left or right), the latter is specified by the new set of pressed modifier keys (shift, control, alt, left mouse button).
     */
    public abstract record WMGAction // named "WMGAction" instead of just "Action" because of System.Action
    {
        /* Do not allow for additional child classes. */
        internal WMGAction() { }

        /* Child classes must implement ToString. */
        public abstract override string ToString();

        public string PrettyPrint() => $"[{ToString()}]";

        public static WMGAction FromString(string str)
        {
            return str switch
            {
                "UP" => new MouseMovementAction(Direction.UP),
                "DOWN" => new MouseMovementAction(Direction.DOWN),
                "LEFT" => new MouseMovementAction(Direction.LEFT),
                "RIGHT" => new MouseMovementAction(Direction.RIGHT),
                "WHEEL UP" => new MouseWheelAction(Direction.UP),
                "WHEEL DOWN" => new MouseWheelAction(Direction.DOWN),
                "WHEEL LEFT" => new MouseWheelAction(Direction.LEFT),
                "WHEEL RIGHT" => new MouseWheelAction(Direction.RIGHT),
                _ => new ModifierChangeAction(Modifiers.FromString(str)),
            };
        }
    }

    public sealed record MouseMovementAction(Direction MovementDirection) : WMGAction
    {
        public override string ToString()
        {
            return MovementDirection switch
            {
                Direction.UP => "UP",
                Direction.DOWN => "DOWN",
                Direction.LEFT => "LEFT",
                Direction.RIGHT => "RIGHT",
                _ => "",
            };
        }
    }

    public sealed record MouseWheelAction(Direction WheelDirection) : WMGAction
    {
        public override string ToString()
        {
            return WheelDirection switch
            {
                Direction.UP => "WHEEL UP",
                Direction.DOWN => "WHEEL DOWN",
                Direction.LEFT => "WHEEL LEFT",
                Direction.RIGHT => "WHEEL RIGHT",
                _ => "",
            };
        }
    }

    public sealed record ModifierChangeAction(Modifiers NewModifiers) : WMGAction
    {
        public override string ToString()
        {
            return NewModifiers.ToString();
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
