using System;
using System.Collections.Generic;

namespace WMG.Gestures
{
    /* Represents any combination of the modifiers "shift", "control", "alt" and "left mouse button" (lmb).
     * Usage example:
     *     Modifiers m = Modifiers.CTRL + Modifiers.LMB
     */
    public sealed class Modifiers : IEquatable<Modifiers>
    {
        private Modifiers() { }

        public bool Shift { get; private set; } = false;
        public bool Control { get; private set; } = false;
        public bool Alt { get; private set; } = false;
        public bool Lmb { get; private set; } = false;

        public static readonly Modifiers NONE = new();
        public static readonly Modifiers SHIFT = new() { Shift = true };
        public static readonly Modifiers CONTROL = new() { Control = true };
        public static readonly Modifiers ALT = new() { Alt = true };
        public static readonly Modifiers LMB = new() { Lmb = true };

        public static Modifiers operator +(Modifiers m1, Modifiers m2) => new()
        {
            Shift = m1.Shift || m2.Shift,
            Control = m1.Control || m2.Control,
            Alt = m1.Alt || m2.Alt,
            Lmb = m1.Lmb || m2.Lmb
        };

        public static Modifiers operator -(Modifiers m1, Modifiers m2) => new()
        {
            Shift = m1.Shift && !m2.Shift,
            Control = m1.Control && !m2.Control,
            Alt = m1.Alt && !m2.Alt,
            Lmb = m1.Lmb && !m2.Lmb
        };

        public override string ToString()
        {
            if (this.Equals(Modifiers.NONE)) return "NONE";
            else
            {
                var strings = new List<string>();
                if (Shift) strings.Add("SHIFT");
                if (Control) strings.Add("CONTROL");
                if (Alt) strings.Add("ALT");
                if (Lmb) strings.Add("LMB");
                return string.Join("|", strings);
            }
        }

        /*
         * Converts the ToString representation back to a Modifiers object (with some leniency).
         */
        public static Modifiers FromString(string str)
        {
            str = str.ToUpper();
            Modifiers result = Modifiers.NONE;

            if (str.Contains("SHIFT")) result += Modifiers.SHIFT;
            if (str.Contains("CONTROL")) result += Modifiers.CONTROL;
            if (str.Contains("ALT")) result += Modifiers.ALT;
            if (str.Contains("LMB")) result += Modifiers.LMB;

            return result;
        }

        // --- generated code ---

        public override bool Equals(object? obj)
        {
            return Equals(obj as Modifiers);
        }

        public bool Equals(Modifiers? other)
        {
            return other != null &&
                   Shift == other.Shift &&
                   Control == other.Control &&
                   Alt == other.Alt &&
                   Lmb == other.Lmb;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Shift, Control, Alt, Lmb);
        }
    }
}
