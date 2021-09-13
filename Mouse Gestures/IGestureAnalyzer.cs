using System;
using WMG.Core;

namespace WMG.Gestures
{
    public delegate void UnfinishedGestureHandler(UnfinishedGesture currentGesture);
    public delegate void GestureHandler(Gesture gesture);
    public delegate void ClickThroughHandler(POINT mousePosition);

    /*
     * A type capable of analyzing detected mouse / keyboard events, converting them into mouse gestures.
     * The return value of all methods in this interface indicates whether the corresponding event was part of a gesture.
     */
    public interface IGestureAnalyzer
    {
        /*
         * Notify the analyzer that the right mouse button was pressed.
         */
        bool RmbDown(POINT position, Modifiers modifiers);

        /*
         * Notify the analyzer that the right mouse button was released.
         * If this event signifies the completion of a gesture, the cleanup callback should be called in a separate thread and before OnGestureComplete is triggered.
         */
        bool RmbUp(POINT position, Action<Gesture> cleanup);

        /*
         * Notify the analyzer that the mouse was moved.
         */
        bool MouseMovement(POINT newPosition);

        /*
         * Notify the analyzer that the mouse wheel was moved.
         */
        bool MouseWheelMovement(Direction direction);

        /*
         * Notify the analyzer that the given modifiers were pressed.
         */
        bool ModifiersDown(Modifiers pressedModifiers);

        /*
         * Notify the analyzer that the given modifiers were released.
         */
        bool ModifiersUp(Modifiers releasedModifiers);

        /*
         * Notify the analyzer that the modifier status has changed.
         */
        bool ModifiersUpdate(Modifiers newModifiers);

        /*
         * The gesture currently being recorded (or null if there is none).
         */
        UnfinishedGesture CurrentGesture { get; }

        void Reset();

        event ClickThroughHandler OnClickThrough;
        event GestureHandler OnGestureComplete;
        event UnfinishedGestureHandler OnGestureStart;
        event UnfinishedGestureHandler OnGestureUpdate;
    }
}