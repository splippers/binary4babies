using System.Collections;
using System.Text;
using UnityEngine;

namespace NibBall
{
    /// <summary>
    /// Run with a desktop player started with -batchmode -nographics -nibball-selftest. Throws balls at the bins, a window, a
    /// wall and back at the wall gently, then logs SELFTEST PASS or SELFTEST FAIL with reasons and quits.
    /// </summary>
    public sealed class SelfTest : MonoBehaviour
    {
        public NibBallApp app;
        readonly StringBuilder fails = new StringBuilder();

        void Check(bool ok, string what) { Debug.Log("SELFTEST " + (ok ? "ok   " : "FAIL ") + what); if (!ok) fails.AppendLine(what); }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);
            app.Arena.SetSatellites(false);   // the other checks throw across the room
            yield return Bins();
            yield return Window();
            yield return Wall();
            yield return Gentle();
            yield return Toysies();
            yield return Satellites();
            Debug.Log(fails.Length == 0 ? "SELFTEST PASS" : "SELFTEST FAIL\n" + fails);
            Application.Quit(fails.Length == 0 ? 0 : 1);
        }

        static Vector3 Lob(Vector3 from, Vector3 to, float seconds) => (to - from - 0.5f * Physics.gravity * seconds * seconds) / seconds;

        IEnumerator Bins()
        {
            var a = app.Arena;
            Check(a.layout.againstWall, "arena fitted against a wall (depth " + a.layout.depthAvailable.ToString("0.0") + " m, bins " + a.layout.binWidth.ToString("0.00") + " m)");
            int target = a.target;
            Debug.Log("SELFTEST target " + target);
            var from = a.StandHere + Vector3.up * 1.4f;
            for (int i = 0; i < 4; i++)
            {
                if ((target & Nibble.Weights[i]) == 0) continue;
                var rim = a.transform.TransformPoint(new Vector3((i - 1.5f) * (a.layout.binWidth + ArenaPlacer.Gap), 0.45f + 0.1f, -(a.layout.binWidth * 0.5f + 0.12f)));
                var b = Ball.Create(from);
                b.Release(Lob(from, rim, 0.8f));
                yield return new WaitForSeconds(2.0f);
            }
            float waited = 0f;
            while (a.score < 1 && waited < 5f) { waited += Time.deltaTime; yield return null; }
            Check(a.score == 1, "threw " + target + " into the bins and scored (score " + a.score + ", showing " + a.nibble.Binary + ")");
            yield return new WaitForSeconds(3.2f);
            Check(a.nibble.Value == 0 && a.target != 0, "new round started with cleared bits");
        }

        IEnumerator Toysies()
        {
            foreach (var kind in Toys.All)
            {
                var from = new Vector3(0f, 1.5f, 0f);
                var b = Ball.Create(from, kind);
                Check(b.col != null && b.rb != null && b.kind == kind, kind + " builds with a collider and a body");
                b.Release(new Vector3(0.2f, 0f, 0.6f));
                yield return new WaitForSeconds(1.5f);
                Check(b != null && b.transform.position.y < 1.5f && b.transform.position.y > -0.5f, kind + " falls and lands on the floor (y " + (b != null ? b.transform.position.y.ToString("0.00") : "gone") + ")");
                if (b != null) Destroy(b.gameObject);
            }
        }

        IEnumerator Satellites()
        {
            var a = app.Arena;
            a.SetSatellites(true);
            a.nibble.Clear();
            int hits = 0;
            for (int bit = 0; bit < 4; bit++)
            {
                var sat = a.SatelliteFor(bit);
                Check(sat != null, "satellite for bit " + Nibble.Weights[bit] + " exists at " + (sat != null ? sat.transform.position.ToString("0.0") : "-"));
                if (sat == null) continue;
                var target = sat.transform.position;
                var from = target + (a.StandHere - target).normalized * 2f; from.y = target.y + 0.1f;
                bool wasOn = a.nibble.IsOn(bit);
                var b = Ball.Create(from);
                b.Release((target - from).normalized * 6f);
                yield return new WaitForSeconds(0.9f);
                if (a.nibble.IsOn(bit) != wasOn) hits++;
                Check(a.nibble.IsOn(bit) != wasOn || a.celebrating || a.score > 0, "a ball thrown at the " + Nibble.Weights[bit] + " bullseye flipped bit " + Nibble.Weights[bit]);
                if (b != null) Destroy(b.gameObject);
                yield return new WaitForSeconds(0.2f);
            }
            a.SetSatellites(false);
        }

        IEnumerator Window()
        {
            int before = app.Arena.windowsBroken;
            var from = new Vector3(0f, 1.4f, 0f);
            var to = new Vector3(SyntheticRoom.HalfX, 1.5f, 0.3f);
            var b = Ball.Create(from);
            b.Release((to - from).normalized * 8f);
            yield return new WaitForSeconds(1.2f);
            Check(app.Arena.windowsBroken == before + 1, "fast ball broke the window");
            Check(b != null && b.transform.position.x > SyntheticRoom.HalfX, "ball went through the window");

            int broken = app.Arena.windowsBroken;
            var b2 = Ball.Create(from);
            b2.Release(Lob(from, to, 0.7f));   // slow enough to be gentle, fast enough that an intact pane would break
            yield return new WaitForSeconds(1.2f);
            Check(app.Arena.windowsBroken == broken, "a second ball passes the broken pane without new damage");
            Check(b2 != null && b2.transform.position.x > SyntheticRoom.HalfX, "second ball went through the open window");
        }

        IEnumerator Wall()
        {
            int before = app.Arena.holesPunched;
            var from = new Vector3(0f, 1.2f, 0f);
            var to = new Vector3(1.2f, 1.0f, -SyntheticRoom.HalfZ);
            var b = Ball.Create(from);
            b.Release((to - from).normalized * 9f);
            yield return new WaitForSeconds(1.2f);
            Check(app.Arena.holesPunched == before + 1, "fast ball punched a hole in the wall");
            Check(b != null && b.transform.position.z < -SyntheticRoom.HalfZ, "ball went through the wall");
        }

        IEnumerator Gentle()
        {
            int w = app.Arena.windowsBroken, h = app.Arena.holesPunched;
            var from = new Vector3(0f, 1.0f, 0f);
            var b = Ball.Create(from);
            b.Release(new Vector3(0.3f, 0f, 1.4f));
            yield return new WaitForSeconds(3f);
            Check(app.Arena.windowsBroken == w && app.Arena.holesPunched == h, "a gentle roll against the wall breaks nothing");
            Check(b != null && b.transform.position.z < SyntheticRoom.HalfZ, "gentle ball stayed in the room");
        }
    }
}
