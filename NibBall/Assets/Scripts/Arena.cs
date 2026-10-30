using System.Collections;
using UnityEngine;

namespace NibBall
{
    /// <summary>
    /// The four bins, the "stand here" ring and the scoreboard, laid out from an ArenaLayout, plus the rules: a ball in a bin
    /// flips that bit; make the target number and you score. Bins are the bits, left to right 8 4 2 1.
    /// </summary>
    public sealed class Arena : MonoBehaviour
    {
        public ArenaLayout layout;
        public readonly Nibble nibble = new Nibble();
        public int target, score, windowsBroken, holesPunched;
        public bool celebrating;
        public System.Action Changed;

        const float BinHeight = 0.45f;
        static readonly Color Off = new Color(0.22f, 0.27f, 0.38f), On = new Color(1f, 0.8f, 0.15f), Good = new Color(0.25f, 0.9f, 0.45f);

        Bin[] bins = new Bin[4];
        Transform board;
        TextMesh headline, readout, damage, hint;
        readonly System.Random rng = new System.Random();

        public static Arena Build(ArenaLayout layout)
        {
            var go = new GameObject("Arena");
            var a = go.AddComponent<Arena>();
            a.layout = layout;
            go.transform.position = layout.baseCenter;
            go.transform.rotation = Quaternion.LookRotation(-layout.forward, Vector3.up);   // local +Z points at the wall
            a.Construct();
            a.NewTarget();
            return a;
        }

        public Vector3 StandHere => layout.baseCenter + layout.forward * layout.throwDistance;

        void Construct()
        {
            float w = layout.binWidth, pitch = w + ArenaPlacer.Gap;
            var wallMat = Look.Lit_(Off);
            for (int i = 0; i < 4; i++)
            {
                var bin = new GameObject("Bin " + Nibble.Weights[i]).AddComponent<Bin>();
                bin.transform.SetParent(transform, false);
                bin.transform.localPosition = new Vector3((i - 1.5f) * pitch, 0f, -(w * 0.5f + 0.12f));   // a hand's width off the wall
                bin.Build(this, i, w, BinHeight);
                bins[i] = bin;
            }

            // stand-here ring on the floor, with a little arrow of dots pointing at the bins
            var ring = new GameObject("Stand Here");
            ring.transform.SetParent(transform, false);
            ring.transform.position = StandHere + Vector3.up * 0.01f;
            var glow = Look.Glow(new Color(1f, 0.8f, 0.15f));
            const int dots = 28;
            for (int i = 0; i < dots; i++)
            {
                float a = i * Mathf.PI * 2f / dots;
                Look.Box(ring.transform, "dot", new Vector3(Mathf.Cos(a) * 0.3f, 0f, Mathf.Sin(a) * 0.3f), new Vector3(0.03f, 0.004f, 0.03f), glow, false);
            }

            // scoreboard above the row, turned to face the player
            board = new GameObject("Board").transform;
            board.SetParent(transform, false);
            board.localPosition = new Vector3(0f, BinHeight + 0.75f, -(w * 0.5f + 0.12f));
            board.gameObject.AddComponent<Billboard>();
            headline = Look.Label(board, "Headline", "", 0.16f, new Color(1f, 0.85f, 0.2f), new Vector3(0f, 0.2f, 0f));
            readout = Look.Label(board, "Readout", "", 0.1f, Color.white, new Vector3(0f, 0.04f, 0f));
            damage = Look.Label(board, "Damage", "", 0.06f, new Color(1f, 0.55f, 0.45f), new Vector3(0f, -0.1f, 0f));
            hint = Look.Label(board, "Hint", "Throw a ball into a bin to flip its bit", 0.05f, new Color(0.8f, 0.9f, 1f), new Vector3(0f, -0.2f, 0f));

            RoomSurface.Damaged += OnDamaged;
            Refresh();
        }

        void OnDestroy() { RoomSurface.Damaged -= OnDamaged; }

        void OnDamaged(SurfaceKind kind, Vector3 at)
        {
            if (kind == SurfaceKind.Window) windowsBroken++; else holesPunched++;
            Refresh();
        }

        public void NewTarget()
        {
            nibble.Clear();
            target = nibble.NextTarget(rng);
            celebrating = false;
            for (int i = 0; i < 4; i++) bins[i].SetLit(false, Off);
            Refresh();
        }

        public void OnBin(int index, Ball ball)
        {
            if (celebrating) return;
            bool on = nibble.Toggle(index);
            bins[index].SetLit(on, On);
            if (on) Sfx.BitOn(bins[index].transform.position); else Sfx.BitOff(bins[index].transform.position);
            if (nibble.Value == target) StartCoroutine(Celebrate());
            Refresh();
        }

        IEnumerator Celebrate()
        {
            celebrating = true;
            score++;
            for (int i = 0; i < 4; i++) bins[i].SetLit(nibble.IsOn(i), Good);
            Sfx.Cheer(board.position);
            Debris.Confetti(board.position + Vector3.down * 0.2f, 80);
            Refresh();
            yield return new WaitForSeconds(2.8f);
            NewTarget();
        }

        void Refresh()
        {
            if (headline == null) return;
            headline.text = celebrating ? "YES!  " + target + " = " + nibble.Binary : "Make  " + target;
            readout.text = nibble.Binary + "  =  " + nibble.Value + "      score " + score;
            damage.text = windowsBroken + holesPunched == 0 ? "" : "Oops!  windows " + windowsBroken + "   wall holes " + holesPunched;
            Changed?.Invoke();
        }

        public void SetHint(string text) { if (hint != null) hint.text = text; }
    }

    /// <summary>One open-topped box standing for one bit.</summary>
    public sealed class Bin : MonoBehaviour
    {
        Arena arena;
        int index;
        Renderer lamp;
        TextMesh digit;
        BoxCollider zone;
        float inner;

        public void Build(Arena a, int i, float w, float h)
        {
            arena = a; index = i;
            const float t = 0.025f;
            var body = Look.Lit_(new Color(0.22f, 0.27f, 0.38f));
            Look.Box(transform, "floor", new Vector3(0, t / 2, 0), new Vector3(w, t, w), body);
            Look.Box(transform, "left", new Vector3(-(w - t) / 2, h / 2, 0), new Vector3(t, h, w), body);
            Look.Box(transform, "right", new Vector3((w - t) / 2, h / 2, 0), new Vector3(t, h, w), body);
            Look.Box(transform, "back", new Vector3(0, h / 2, (w - t) / 2), new Vector3(w, h, t), body);
            Look.Box(transform, "front", new Vector3(0, h / 2, -(w - t) / 2), new Vector3(w, h, t), body);

            // the lamp: a bar along the front rim (the front faces the player, local -Z), lit when the bit is on; and the weight, big, on the front face
            lamp = Look.Box(transform, "lamp", new Vector3(0, h + 0.012f, -(w - t) / 2), new Vector3(w, 0.025f, t * 1.4f), Look.Glow(new Color(0.22f, 0.27f, 0.38f)), false).GetComponent<Renderer>();
            Look.Label(transform, "weight", Nibble.Weights[i].ToString(), Mathf.Min(0.2f, w * 0.5f), new Color(1f, 1f, 1f), new Vector3(0, h * 0.5f, -(w / 2 + 0.002f)));
            digit = Look.Label(transform, "bit", "0", 0.09f, new Color(1f, 1f, 1f), new Vector3(0, h * 0.5f - Mathf.Min(0.2f, w * 0.5f) * 0.85f, -(w / 2 + 0.002f)));

            inner = w - 2 * t;
            zone = gameObject.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0, h * 0.55f, 0);
            zone.size = new Vector3(inner, h * 0.5f, inner);
        }

        public void SetLit(bool on, Color c)
        {
            lamp.sharedMaterial.color = on ? c : new Color(0.22f, 0.27f, 0.38f);
            digit.text = on ? "1" : "0";
            digit.color = on ? c : Color.white;
        }

        void OnTriggerStay(Collider other)
        {
            var ball = other.GetComponent<Ball>();
            if (ball == null || ball.scored || ball.held) return;
            // the ball has to be properly inside, not clipping the rim on its way past
            var local = transform.InverseTransformPoint(ball.transform.position);
            if (Mathf.Abs(local.x) > inner * 0.5f || Mathf.Abs(local.z) > inner * 0.5f) return;
            ball.scored = true;
            arena.OnBin(index, ball);
            StartCoroutine(Fade(ball));
        }

        IEnumerator Fade(Ball ball)
        {
            yield return new WaitForSeconds(0.7f);
            if (ball == null) yield break;
            Sfx.Pop(ball.transform.position);
            Destroy(ball.gameObject);
        }
    }
}
