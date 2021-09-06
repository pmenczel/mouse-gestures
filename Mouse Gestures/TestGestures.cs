using System;
using System.Windows.Forms;
using WMG.Core;
using WMG.Gestures;

namespace WMG
{
    public class TestGestures : ApplicationContext
    {
        private static readonly Gesture CLOSE_GESTURE = new Gesture(Modifiers.NONE, new WMG.Gestures.Action[] { new MouseMovementAction(Direction.DOWN), new MouseMovementAction(Direction.RIGHT) });

        public static void Main(string[] args)
        {
            var analyzer = new GestureAnalyzer(Settings.WiggleRoomSq);
            Settings.OnChange += (() => analyzer.WiggleRoomSq = Settings.WiggleRoomSq);

            analyzer.OnGestureStart += (_ => Console.WriteLine("Mouse gesture started"));
            analyzer.OnGestureComplete += (gesture => Console.WriteLine($"Mouse gesture detected: {gesture.PrettyPrint()}\n"));
            analyzer.OnClickThrough += (_ => Console.WriteLine("Click-Through\n"));

            var hook = new NativeHook(analyzer);
            TestGestures app = new TestGestures();

            analyzer.OnGestureComplete += gesture =>
            {
                if (gesture.Equals(CLOSE_GESTURE))
                {
                    hook.Dispose();
                    app.ExitThread();
                }
            };

            Application.Run(app);
        }
    }
}
