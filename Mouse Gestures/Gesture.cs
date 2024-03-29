﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WMG.Gestures
{
    /*
     * Representation of an abstract mouse gesture as a list of actions. The possible actions are:
     * - mouse movement
     * - mouse scrolling
     * - pressing or releasing modifier keys (shift, ctrl, alt, left mouse button).
     * Note that a change in modifier keys is recorded separately from mouse movements. Pressing, for example, shift while moving the mouse to the right thus corresponds to a gesture with three events: mouse right, shift, mouse right.
     * 
     * A gesture is always initiated by pressing the right mouse button and ended by releasing the right mouse button.
     * We must store the modifier keys that are already pressed at the initial time, and the list of actions following afterwards.
     * 
     * Possible future improvements: allow other modifier keys, allow gestures starting with something other than "right mouse button"
     */
    public class Gesture : System.IEquatable<Gesture>
    {
        public Modifiers InitialModifiers { get; }
        public IEnumerable<WMGAction> Actions { get; }
        public UnfinishedGesture? RawData { get; }

        public Gesture(Modifiers initialModifiers, IEnumerable<WMGAction> actions, UnfinishedGesture? rawData)
        {
            InitialModifiers = initialModifiers;
            Actions = actions;
            RawData = rawData;
        }

        public Gesture(Modifiers initialModifiers, IEnumerable<WMGAction> actions) : this(initialModifiers, actions, null) { }

        public override string ToString()
        {
            var actionStrings = from a in Actions select a.ToString();
            return InitialModifiers + ";" + string.Join(";", actionStrings);
        }

        public static Gesture FromString(string str)
        {
            var strings = str.Split(';');

            var initialModifiers = Modifiers.FromString(strings[0]);
            var actions = new WMGAction[strings.Length - 1];

            for (int i = 1; i < strings.Length; i++)
            {
                actions[i - 1] = WMGAction.FromString(strings[i]);
            }
            return new Gesture(initialModifiers, actions);
        }

        public string PrettyPrint()
        {
            string result = $"[BEGIN {InitialModifiers}]";
            foreach (WMGAction action in Actions)
            {
                result += $" {action.PrettyPrint()}";
            }
            return result;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Gesture);
        }

        public bool Equals(Gesture? other)
        {
            return other != null && InitialModifiers.Equals(other.InitialModifiers) && Actions.SequenceEqual(other.Actions);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(InitialModifiers);
            foreach (WMGAction a in Actions)
            {
                hash.Add(EqualityComparer<WMGAction>.Default.GetHashCode(a));
            }
            return hash.ToHashCode();
        }
    }
}
