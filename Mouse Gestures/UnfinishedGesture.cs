using System;
using System.Collections.Generic;
using System.Linq;
using WMG.Core;

namespace WMG.Gestures
{
    /*
     * Representation of a mouse gesture that is currently being performed.
     * We record the following data:
     * - the modifiers at the beginning of the gesture
     * - a list of actions that have been completed already
     * - the timestamp of each completed action
     * - the position of the mouse at the beginning of the action
     * - the complete mouse movement data since the last action was completed (necessary in order to detect direction changes)
     */
    public class UnfinishedGesture
    {
        public Modifiers InitialModifiers { get; }

        private readonly List<AnnotatedAction> completedActions = new();
        public IEnumerable<AnnotatedAction> CompletedActions => new List<AnnotatedAction>(completedActions);

        private readonly List<Point> movementData = new();
        public IEnumerable<Point> MovementData => new List<Point>(movementData);

        public UnfinishedGesture(Modifiers initialModifiers, Point initialMousePosition)
        {
            InitialModifiers = initialModifiers;
            movementData.Add(initialMousePosition);
        }

        /*
         * Find the current modifiers, according to the latest ModifierChangeAction in the gesture.
         */
        public Modifiers CurrentModifiers
        {
            get
            {
                var lastChangeAction = completedActions.FindLast(a => a.Action is ModifierChangeAction);
                if (lastChangeAction == null)
                    return InitialModifiers;
                else
                    return (lastChangeAction.Action as ModifierChangeAction)!.NewModifiers;
            }
        }

        /*
         * Find the current mouse position, according to the recorded mouse movement data
         */
        public Point CurrentMousePosition => movementData.Last();

        /*
         * Find the mouse position at which the gesture was started
         */
        public Point InitialMousePosition
        {
            get
            {
                AnnotatedAction? firstAction = completedActions.FirstOrDefault();
                if (firstAction == null)
                    return movementData.First();
                else
                    return firstAction.Position;
            }
        }

        /*
         * Find the latest completed action in this gesture (or null, if there is none).
         */
        public AnnotatedAction? LatestAction => completedActions.LastOrDefault();

        /*
         * Add a new action to this unfinished gesture (mutating the object).
         * initialMousePosition is the mouse position at the start of the action.
         */
        internal void RecordAction(WMGAction action, Point initialMousePosition)
        {
            completedActions.Add(new AnnotatedAction(action, DateTime.Now, initialMousePosition));
        }

        /* 
         * For instantaneous events (scroll, modifiers): uses the current mouse position as the initial mouse position.
         */
        internal void RecordAction(WMGAction action)
        {
            RecordAction(action, CurrentMousePosition);
        }

        /*
         * Add new movement data to this unfinished gesture (mutating the object).
         */
        internal void RecordMovement(Point mousePosition)
        {
            movementData.Add(mousePosition);
        }

        /*
         * Clears the recorded movement data, adding the given point as the single new entry (mutating the object).
         */
        internal void ClearMovementData(Point newInitialPosition)
        {
            movementData.Clear();
            movementData.Add(newInitialPosition);
        }

        /*
         * Clears the recorded movement data except for its last entry, which becomes the new initial position.
         */
        internal void ClearMovementData()
        {
            ClearMovementData(CurrentMousePosition);
        }

        public Gesture Complete(Point mousePosition)
        {
            ClearMovementData(mousePosition);
            return new Gesture(InitialModifiers,
                               from a in completedActions select a.Action,
                               this);
        }
    }

    public class AnnotatedAction
    {
        public WMGAction Action { get; }
        public DateTime Timestamp { get; }
        public Point Position { get; }

        public AnnotatedAction(WMGAction action, DateTime timestamp, Point position)
        {
            Action = action;
            Timestamp = timestamp;
            Position = position;
        }

        public override string ToString() => $"{Action.PrettyPrint()} at {Position}, {Timestamp.ToLongTimeString()}";
    }
}
