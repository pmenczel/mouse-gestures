using System;
using System.Linq;
using System.Threading;
using WMG.Core;

namespace WMG.Gestures
{
    /*
     * A GestureAnalyzer interprets raw mouse / keyboard events, converting them into mouse gestures.
     * The GestureAnalyzer is notified about the raw events via the methods offered in the IGestureAnalyzer interface,
     *      which return a bool indicating whether the GestureAnalyzer considers them part of a mouse gesture.
     * While a gesture is being performed, we keep track of its progress as an UnfinishedGesture event.
     * 
     * The parameter "wiggle room" plays a key role in the analysis:
     * 1. Mouse movements smaller than the wiggle room do not create a mouse movement action.
     * 2. If there is an ongoing mouse movement action in one direction, a movement into another direction of less than the wiggle room will be ignored.
     * 
     * In detail, the detection algorithm works as follows:
     * When a modifier change or a mouse scroll event is detected:
     *      1. Any recorded mouse movement less than the wiggle room is discarded
     *      2. The respective MouseWheelAction or ModifierChangeAction is added
     * When mouse movement is detected:
     *      Case 1: The latest completed action A is not a MouseMovementAction:
     *          If the mouse has moved more than the wiggle room from the position of A, this adds a new MouseMovementAction
     *      Case 2: Otherwise (this is the tricky bit):
     *          1. We go back in the recorded movement until we are at least (wiggle room) from the current mouse position, call that point P
     *          2. We determine the direction from P to the current mouse position
     *          3. If it differs from the direction of the latest MouseMovementAction, this is a direction change. We adds a new MouseMovementAction, where the anchor point is P
     *          4. It the direction is still the same, we do nothing except adding the new point to the raw data.
     * 
     * The GestureAnalyzer fires events
     * - at the beginning of a gesture
     * - for every processed raw input while a gesture is being recorded
     * - at the completion of a gesture.
     * All events are launched asynchronously, since this might be called from the message queue (and event subscribers might simulate events).
     * 
     * "Click-through" events are treated specially: events where the right mouse button was pressed and released without any mouse movement (or any other events) in between.
     * (However, if the left mouse button is pressed as well, this is a rocker gesture and not a click-through.)
     * That signifies a "normal" mouse click which is not part of a gesture; however, we cannot know at the time of the first right mouse button press whether it will be part of a gesture or not.
     * If the initial mouse down event was consumed already, one should thus emulate a click event without notifying the gesture analyzer.
     */
    public class GestureAnalyzer : IGestureAnalyzer
    {
        // this is the squared wiggle room
        public int WiggleRoomSq { get; set; }

        public UnfinishedGesture CurrentGesture { get; private set; } = null;

        // wiggleRoomSq is the squared wiggle room
        public GestureAnalyzer(int wiggleRoomSq)
        {
            WiggleRoomSq = wiggleRoomSq;
        }

        public event UnfinishedGestureHandler OnGestureStart;
        public event UnfinishedGestureHandler OnGestureUpdate;
        public event GestureHandler OnGestureComplete;
        public event ClickThroughHandler OnClickThrough;

        private void RaiseGestureStart(UnfinishedGesture arg)
        {
            if (OnGestureStart == null) return;
            foreach (UnfinishedGestureHandler x in OnGestureStart.GetInvocationList())
            {
                // we ignore exceptions thrown by the clients (i.e., they end up in the uncaught-exception-handler)
                x.BeginInvoke(arg, x.EndInvoke, null);
            }
        }

        // the following three methods are basically copies of RaiseGestureStart, but not sure if it is possible to abstract away
        private void RaiseGestureUpdate(UnfinishedGesture arg)
        {
            if (OnGestureUpdate == null) return;
            foreach (UnfinishedGestureHandler x in OnGestureUpdate.GetInvocationList())
            {
                x.BeginInvoke(arg, x.EndInvoke, null);
            }
        }

        private void RaiseGestureComplete(Gesture arg)
        {
            if (OnGestureComplete == null) return;
            foreach (GestureHandler x in OnGestureComplete.GetInvocationList())
            {
                x.BeginInvoke(arg, x.EndInvoke, null);
            }
        }
        
        private void RaiseClickThrough(POINT arg)
        {
            if (OnClickThrough == null) return;
            foreach (ClickThroughHandler x in OnClickThrough.GetInvocationList())
            {
                x.BeginInvoke(arg, x.EndInvoke, null);
            }
        }

        /*
         * Notify the analyzer that the right mouse button was pressed.
         * Returns: true, since we must assume at this point that the event is the beginning of a gesture.
         */
        public bool RmbDown(POINT position, Modifiers modifiers)
        {
            // start new gesture
            CurrentGesture = new UnfinishedGesture(modifiers, position);
            RaiseGestureStart(CurrentGesture);
            return true;
        }

        /*
         * Notify the analyzer that the right mouse button was released.
         * 
         * In case this represents the end of a mouse gesture, the cleanup action will be invoked.
         * It is guaranteed to be invoked before the gesture complete event fires, and in a separate Thread.
         * 
         * Returns: whether this event is part of a gesture.
         * Note that this will return true in the case of a click-through, for consistency with the previous RmbDown event.
         */
        public bool RmbUp(POINT position, Action<Gesture> cleanup)
        {
            if (CurrentGesture == null)
                return false;

            // detect click through
            if (!CurrentGesture.CompletedActions.Any() && CurrentGesture.MovementData.Count() == 1 && !CurrentGesture.InitialModifiers.Lmb)
            {
                RaiseClickThrough(position);
            }
            else
            {
                var gesture = CurrentGesture.Complete(position);
                new Thread(() => { cleanup?.Invoke(gesture); RaiseGestureComplete(gesture); }).Start();
            }
            CurrentGesture = null;
            return true;
        }

        public void Reset()
        {
            if (CurrentGesture != null)
            {
                CurrentGesture = null;
                RaiseGestureUpdate(null);
            }
        }

        /*
         * Notify the analyzer that the mouse was moved.
         * Returns: whether this event is part of a gesture.
         */
        public bool MouseMovement(POINT newPosition)
        {
            if (CurrentGesture == null)
                return false;

            // If it's the same point again, ignore (important for click-through detection)
            if (newPosition.Equals(CurrentGesture.CurrentMousePosition()))
                return false;

            // First of all, record the movement
            CurrentGesture.RecordMovement(newPosition);

            // --- implement the algorithm outlined at the top ---
            AnnotatedAction latestAction = CurrentGesture.LatestAction();
            if (!(latestAction?.Action is MouseMovementAction)) // no action yet, mouse wheel action or modifier change action
            {
                // Compare the current mouse position to the starting point of the latest action
                // (or to the starting point of the gesture, if there is no action yet)
                // In any case, that point can be obtained as follows:
                var startingPosition = CurrentGesture.MovementData.First();
                var movementDirection = DetermineDirection(startingPosition, newPosition);
                // If the movement direction is null, we haven't left the wiggle room yet.
                // Otherwise, this is a new mouse movement action!
                if (movementDirection != null)
                {
                    var newAction = new MouseMovementAction(movementDirection.Value);
                    CurrentGesture.RecordAction(newAction, startingPosition);
                    // we do not clear the movement data; we keep everything from the beginning of the gesture
                }
            }
            else // i.e., latest action was mouse movement
            {
                var previousDirection = (latestAction.Action as MouseMovementAction).direction;
                // To determine the current direction, we go back in the recorded movement data until we find a point at least (wiggle room) away from the current position and compare that point to the current position.
                try
                {
                    var earlierPosition = CurrentGesture.MovementData.Last(point => point.SquareDistance(newPosition) >= WiggleRoomSq);
                    var currentDirection = DetermineDirection(earlierPosition, newPosition).Value;

                    // If the current direction and the previous direction differ, this is a new action. The initial mouse position of the new action is the "earlier position".
                    if (currentDirection != previousDirection)
                    {
                        var newAction = new MouseMovementAction(currentDirection);
                        CurrentGesture.RecordAction(newAction, earlierPosition);
                        // the unfinished gesture will still contain the recorded movement data of the previous mouse event, but there is no reason to clean it up at this point
                    }
                }
                catch (InvalidOperationException)
                {
                    // we were unable to find a suitable "earlier position" (can happen in rare cases)
                    // we are thus unable to determine the current direction of the mouse movement -> abort
                }
            }

            RaiseGestureUpdate(CurrentGesture);
            return true;
        }

        /*
         * Notify the analyzer that the modifier status has changed.
         * Returns: whether this event is part of a gesture.
         */
        public bool ModifiersUpdate(Modifiers newModifiers)
        {
            if (CurrentGesture == null)
                return false;
            if (CurrentGesture.CurrentModifiers().Equals(newModifiers))
                return false;

            var newAction = new ModifierChangeAction(newModifiers);
            CurrentGesture.RecordAction(newAction);
            CurrentGesture.ClearMovementData();
            RaiseGestureUpdate(CurrentGesture);
            return true;
        }

        /*
         * Notify the analyzer that the given modifiers were pressed.
         * Returns: whether this event is part of a gesture.
         */
        public bool ModifiersDown(Modifiers pressedModifiers)
        {
            if (CurrentGesture == null)
                return false;
            else
                return ModifiersUpdate(CurrentGesture.CurrentModifiers() + pressedModifiers);
        }

        /*
         * Notify the analyzer that the given modifiers were released.
         * Returns: whether this event is part of a gesture.
         */
        public bool ModifiersUp(Modifiers releasedModifiers)
        {
            if (CurrentGesture == null)
                return false;
            else
                return ModifiersUpdate(CurrentGesture.CurrentModifiers() - releasedModifiers);
        }

        /*
         * Notify the analyzer that the mouse wheel was moved.
         * Returns: whether this event is part of a gesture.
         */
        public bool MouseWheelMovement(Direction direction)
        {
            if (CurrentGesture == null)
                return false;

            var newAction = new MouseWheelAction(direction);
            CurrentGesture.RecordAction(newAction);
            CurrentGesture.ClearMovementData();
            RaiseGestureUpdate(CurrentGesture);
            return true;
        }

        /*
         * Returns the direction from p1 to p2.
         * Returns null if the squared distance between the points is smaller than Settings.WiggleRoom.
         */
        private Direction? DetermineDirection(POINT p1, POINT p2)
        {
            if (p1.SquareDistance(p2) < WiggleRoomSq)
            {
                return null;
            }
            else if (Math.Abs(p2.x - p1.x) >= Math.Abs(p2.y - p1.y))
            {
                return p2.x > p1.x ? Direction.RIGHT : Direction.LEFT;
            }
            else
            {
                return p2.y > p1.y ? Direction.DOWN : Direction.UP;
            }
        }
    }
}
