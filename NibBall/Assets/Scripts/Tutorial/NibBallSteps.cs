using UnityEngine;

namespace NibBall
{
    /// <summary>The NibBall tutorial: the stock steps for the Quest, then the game's own (bin a toy, make the target, hit a bullseye, mend damage).</summary>
    public static class NibBallSteps
    {
        const string Done = "NibBall.TutorialDone";

        public static bool AlreadySeen { get { try { return PlayerPrefs.GetInt(Done, 0) == 1; } catch { return false; } } }

        public static TutorialRunner Start(NibBallApp app, Hands hands, Camera cam)
        {
            var stock = new StockSteps(hands);
            var run = TutorialRunner.Create(cam);
            int startValue = 0, startScore = 0;
            run.steps.Add(new TutorialStep { id = "floor", prompt = "Welcome to NibBall.  Look around your room", hint = "Look at the floor and walls so the headset can see them", done = stock.FloorFound(), timeout = 10f });
            run.steps.Add(new TutorialStep { id = "scan", prompt = "Press B (or hold both fists) to scan your room", hint = "Walk the room and look at every wall, window and door", done = stock.RoomScanned(), timeout = 30f });
            run.steps.Add(new TutorialStep { id = "input", prompt = "Hold up your hands or pick up your controllers", hint = "Hands need Settings > Movement tracking > Hand tracking on", done = stock.InputSeen(), timeout = 20f });
            run.steps.Add(new TutorialStep { id = "grab", prompt = "Squeeze grip or trigger (or pinch) to grab a toy off the rack", hint = "Reach for the toys beside the yellow ring", done = stock.Grab(), onStart = stock.ResetGrab });
            run.steps.Add(new TutorialStep { id = "throw", prompt = "Swing and let go to throw it", hint = "Release at the top of your swing", done = stock.Throw(), onStart = stock.ResetThrow });
            run.steps.Add(new TutorialStep
            {
                id = "bin", prompt = "Throw a toy into a bin to flip its bit", hint = "The bins are on the far wall: 8 4 2 1",
                done = () => app.Arena != null && (app.Arena.nibble.Value != startValue || app.Arena.score != startScore),
                onStart = () => { if (app.Arena != null) { startValue = app.Arena.nibble.Value; startScore = app.Arena.score; } }, timeout = 60f,
            });
            run.steps.Add(new TutorialStep { id = "poke", prompt = "Touch a panel button: try MOVE TARGETS", hint = "The panel is left of the yellow ring", done = stock.Poke(), onStart = stock.ResetPoke });
            run.Finished = () => { try { PlayerPrefs.SetInt(Done, 1); PlayerPrefs.Save(); } catch { } Debug.Log("NibBall: tutorial finished"); };
            run.Begin();
            return run;
        }
    }
}
