using System;
using System.Windows.Forms;
using WMG.Core;
using WMG.Gestures;
using WMG.Reactions;

namespace WMG
{
    public class TestGestures : ApplicationContext
    {
        private static readonly Gesture EXIT_GESTURE = new Gesture(Modifiers.NONE, new WMG.Gestures.WMGAction[] { new MouseMovementAction(Direction.RIGHT), new MouseMovementAction(Direction.LEFT), new MouseMovementAction(Direction.RIGHT) });
        private static readonly Reaction EXIT_REACTION = new ExitReaction();

        private static readonly Gesture CLOSE_GESTURE = new Gesture(Modifiers.NONE, new WMG.Gestures.WMGAction[] { new MouseMovementAction(Direction.DOWN), new MouseMovementAction(Direction.RIGHT) });
        private static readonly Reaction CLOSE_REACTION = new CloseWindowReaction(ReactionTarget.WINDOW_AT_GESTURE_START);

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
                if (gesture.Equals(CLOSE_GESTURE))
                {
                    CLOSE_REACTION.Perform(gesture, context);
                }
                else if (gesture.Equals(EXIT_GESTURE))
                {
                    EXIT_REACTION.Perform(gesture, context);
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
