using System;
using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    /// <summary>One thing to teach: say it, wait for the player to really do it, nudge after a while, give up gracefully.</summary>
    public sealed class TutorialStep
    {
        public string id, prompt, hint;
        public Func<bool> done;            // the completion test; true means the player did it
        public float hintAfter = 12f;      // seconds before the hint replaces the prompt
        public float timeout = 45f;        // seconds before moving on anyway (no scan, no hand tracking... must never trap the player)
        public Action onStart;             // reset any detector state when the step begins
    }

    /// <summary>
    /// Game-neutral interactive tutorial. Plays an ordered list of steps on a head-locked card; each waits for a completion
    /// test the game supplies, shows a hint after hintAfter, can be skipped, and moves on at timeout. Nothing here knows NibBall:
    /// the stock detectors and the NibBall steps live beside it (StockSteps.cs, NibBallSteps.cs).
    /// </summary>
    public sealed class TutorialRunner : MonoBehaviour
    {
        public readonly List<TutorialStep> steps = new List<TutorialStep>();
        public Action Finished;
        public bool Running { get; private set; }
        TextMesh card;
        int index;
        float stepStart, doneAt = -1f;

        public static TutorialRunner Create(Camera cam, string title = "TUTORIAL")
        {
            var go = new GameObject("Tutorial");
            var t = go.AddComponent<TutorialRunner>();
            t.card = Look.Label(cam.transform, "Tutorial Card", "", 0.045f, new Color(0.7f, 1f, 0.8f), new Vector3(0f, 0.2f, 1.3f));
            PanelButton.Build(cam.transform, "SKIP", new Vector3(0.28f, -0.22f, 0.7f), new Vector2(0.16f, 0.06f), t.Skip);
            return t;
        }

        public void Begin()
        {
            index = -1; Running = true; Next();
        }

        public void Skip()
        {
            if (!Running) return;
            if (index < steps.Count - 1 && doneAt < 0f) { Next(); return; }
            Finish();
        }

        void Next()
        {
            index++;
            doneAt = -1f;
            if (index >= steps.Count) { Finish(); return; }
            stepStart = Time.time;
            steps[index].onStart?.Invoke();
            Show();
        }

        void Show()
        {
            var s = steps[index];
            card.text = $"{index + 1}/{steps.Count}   {s.prompt}";
        }

        void Finish()
        {
            Running = false;
            card.text = "";
            Finished?.Invoke();
        }

        void Update()
        {
            if (!Running || index >= steps.Count) return;
            var s = steps[index];
            if (doneAt < 0f)
            {
                bool ok = false;
                try { ok = s.done != null && s.done(); } catch (Exception e) { Debug.LogError("NibBall: tutorial step " + s.id + ": " + e.Message); }
                float t = Time.time - stepStart;
                if (ok) { doneAt = Time.time; card.text = "Nice!"; Debug.Log("NibBall: tutorial step " + s.id + " done in " + t.ToString("0.0") + " s"); }
                else if (t > s.timeout) { Debug.Log("NibBall: tutorial step " + s.id + " timed out"); Next(); }
                else if (t > s.hintAfter && !string.IsNullOrEmpty(s.hint)) card.text = $"{index + 1}/{steps.Count}   {s.hint}";
            }
            else if (Time.time - doneAt > 1.2f) Next();
        }
    }
}
