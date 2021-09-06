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

        public static readonly Modifiers NONE = new Modifiers();
        public static readonly Modifiers SHIFT = new Modifiers()
        {
            Shift = true
        };
        public static readonly Modifiers CONTROL = new Modifiers()
        {
            Control = true
        };
        public static readonly Modifiers ALT = new Modifiers()
        {
            Alt = true
        };
        public static readonly Modifiers LMB = new Modifiers()
        {
            Lmb = true
        };

        public static Modifiers operator +(Modifiers m1, Modifiers m2) => new Modifiers()
        {
            Shift = m1.Shift || m2.Shift,
            Control = m1.Control || m2.Control,
            Alt = m1.Alt || m2.Alt,
            Lmb = m1.Lmb || m2.Lmb
        };

        public static Modifiers operator -(Modifiers m1, Modifiers m2) => new Modifiers()
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

        public override bool Equals(object obj)
        {
            return Equals(obj as Modifiers);
        }

        public bool Equals(Modifiers other)
        {
            return other != null &&
                   Shift == other.Shift &&
                   Control == other.Control &&
                   Alt == other.Alt &&
                   Lmb == other.Lmb;
        }

        public override int GetHashCode()
        {
            int hashCode = 2090988377;
            hashCode = hashCode * -1521134295 + Shift.GetHashCode();
            hashCode = hashCode * -1521134295 + Control.GetHashCode();
            hashCode = hashCode * -1521134295 + Alt.GetHashCode();
            hashCode = hashCode * -1521134295 + Lmb.GetHashCode();
            return hashCode;
        }
    }
}
