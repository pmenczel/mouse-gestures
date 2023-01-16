using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;
using WMG.Core;
using WMG.Gestures;
using WMG.Reactions;

namespace WMG
{
    public class TestGestures : ApplicationContext
    {
        private static readonly Dictionary<Gesture, Reaction> GESTURES = new Dictionary<Gesture, Reaction>();

        static TestGestures()
        {
            GESTURES.Add(
                new Gesture(Modifiers.NONE, new WMG.Gestures.WMGAction[] { new MouseMovementAction(Direction.RIGHT),
                                                                           new MouseMovementAction(Direction.LEFT),
                                                                           new MouseMovementAction(Direction.RIGHT) }),
                new ExitReaction());
            GESTURES.Add(
                new Gesture(Modifiers.NONE, new WMG.Gestures.WMGAction[] { new MouseMovementAction(Direction.DOWN),
                                                                           new MouseMovementAction(Direction.RIGHT) }),
                new CloseWindowReaction(ReactionTarget.WINDOW_AT_GESTURE_START));
            GESTURES.Add(
                new Gesture(Modifiers.NONE, new WMG.Gestures.WMGAction[] { new MouseMovementAction(Direction.UP) }),
                new MaximizeWindowReaction(ReactionTarget.WINDOW_AT_GESTURE_START));
            GESTURES.Add(
                new Gesture(Modifiers.NONE, new WMG.Gestures.WMGAction[] { new MouseMovementAction(Direction.DOWN) }),
                new MinimizeWindowReaction(ReactionTarget.WINDOW_AT_GESTURE_START, true));
            GESTURES.Add(
                new Gesture(Modifiers.NONE, new WMG.Gestures.WMGAction[] { new MouseMovementAction(Direction.LEFT) }),
                new MoveWindowReaction(ReactionTarget.WINDOW_AT_GESTURE_START, Rect.FromDimensions(100, 100, 600, 400)));

            double QUARTER = 0.25;
            double THIRD = 1.0 / 3.0;
            double HALF = 0.5;
            Layout l1 = new Layout
            {
                Name = "Layout 1",
                Dimensions = {
                    new WindowDimensions(0, 0.0, 0.0, THIRD, 1.0),
                    new WindowDimensions(0, THIRD, 0.0, THIRD, 1.0),
                    new WindowDimensions(0, 2*THIRD, 0.0, THIRD, 1.0),
                    new WindowDimensions(1, 0.0, 0.0, 1.0, 1.0) }
            };
            Layout l2 = new Layout
            {
                Name = "Layout 2",
                Dimensions = {
                    new WindowDimensions(0, 0.0, 0.0, QUARTER, 1.0),
                    new WindowDimensions(0, QUARTER, 0.0, HALF, 1.0),
                    new WindowDimensions(0, 3*QUARTER, 0.0, QUARTER, 1.0),
                    new WindowDimensions(1, 0.0, 0.0, 1.0, 1.0) }
            };
            Layout l3 = new Layout
            {
                Name = "Layout 3",
                Dimensions = {
                    new WindowDimensions(0, 0.0, 0.0, HALF, 0.0),
                    new WindowDimensions(0, HALF, 0.0, HALF, 0.0),
                    new WindowDimensions(1, 0.0, 0.0, HALF, 0.0),
                    new WindowDimensions(1, HALF, 0.0, HALF, 0.0) }
            };
            LayoutManager man = new LayoutManager
            {
                Fullscreen = false,
                Layouts = { l1, l2, l3 }
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string test = JsonSerializer.Serialize(man, options);
            Console.WriteLine(test);
            Console.WriteLine();

            var test2 = JsonSerializer.Deserialize<LayoutManager>(test);
            Console.WriteLine(JsonSerializer.Serialize(test2, options));
        }

        public static void Main(string[] args)
        {
            var analyzer = new GestureAnalyzer(Settings.WiggleRoomSq);

            analyzer.OnGestureStart += (_ => Console.WriteLine("Mouse gesture started"));
            analyzer.OnGestureComplete += (gesture => Console.WriteLine($"Mouse gesture detected: {gesture.PrettyPrint()}\n"));
            analyzer.OnClickThrough += (_ => Console.WriteLine("Click-Through\n"));

            var hook = new NativeInterface(analyzer);
            TestGestures app = new TestGestures();
            var context = new TestContext(hook, app);

            analyzer.OnGestureComplete += gesture =>
            {
                if (GESTURES.ContainsKey(gesture))
                {
                    GESTURES[gesture].Perform(gesture, context);
                }
            };

            Application.Run(app);
        }
    }

    public class TestContext : IContext
    {
        private NativeInterface hook;
        private ApplicationContext app;

        public TestContext(NativeInterface hook, ApplicationContext app)
        {
            this.hook = hook;
            this.app = app;
        }

        public IDisposable DisableTemporarily()
        {
            hook.forceDisabled = true;
            return new ReactivateHookOnDispose(hook);
        }

        private class ReactivateHookOnDispose : IDisposable
        {
            private NativeInterface hook;

            public ReactivateHookOnDispose(NativeInterface hook)
            {
                this.hook = hook;
            }

            private bool disposed = false;

            public void Dispose()
            {
                if (!disposed)
                {
                    hook.forceDisabled = false;
                    disposed = true;
                }
                GC.SuppressFinalize(this);
            }

            ~ReactivateHookOnDispose()
            {
                Dispose();
            }
        }

        public void ExitApplication()
        {
            hook.Dispose();
            app.ExitThread();
        }
    }
}
