using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    public enum Toy { NibBall, PingPong, Tennis, Banana, BeachBall }

    /// <summary>What you throw. A pinch (or trigger) near a toy on the rack picks that toy; anywhere else picks the last one chosen. A cycles on controllers.</summary>
    public static class Toys
    {
        public static readonly Toy[] All = { Toy.NibBall, Toy.PingPong, Toy.Tennis, Toy.Banana, Toy.BeachBall };
        public static Toy Current = Toy.NibBall;

        public static string Name(Toy t)
        {
            switch (t)
            {
                case Toy.PingPong: return "ping pong";
                case Toy.Tennis: return "tennis";
                case Toy.Banana: return "banana";
                case Toy.BeachBall: return "beach ball";
                default: return "nibball";
            }
        }

        public static void Next()
        {
            Current = All[(System.Array.IndexOf(All, Current) + 1) % All.Length];
        }
    }

    /// <summary>One toy on show on the rack, slowly turning; the chosen one is bigger and carries a gold marker.</summary>
    public sealed class RackItem : MonoBehaviour
    {
        public static readonly List<RackItem> Items = new List<RackItem>();
        public Toy kind;
        public GameObject marker;
        Vector3 baseScale;

        void OnEnable() { Items.Add(this); baseScale = transform.localScale; }
        void OnDisable() { Items.Remove(this); }

        void Update()
        {
            bool chosen = Toys.Current == kind;
            transform.Rotate(0f, (chosen ? 90f : 35f) * Time.deltaTime, 0f, Space.Self);
            transform.localScale = baseScale * (chosen ? 1.25f : 1f);
            if (marker != null) marker.SetActive(chosen);
        }

        /// <summary>The toy whose rack item is within reach of this point (and chooses it), or the one chosen last.</summary>
        public static Toy Choose(Vector3 at, float reach = 0.14f)
        {
            RackItem best = null; float bestD = reach;
            foreach (var r in Items)
            {
                float d = Vector3.Distance(r.transform.position, at);
                if (d < bestD) { best = r; bestD = d; }
            }
            if (best != null) Toys.Current = best.kind;
            return Toys.Current;
        }
    }

    public static class ToyRack
    {
        /// <summary>A row of the toys at hand height beside the stand-here ring. 'across' runs along the row.</summary>
        public static void Build(Transform parent, Vector3 center, Vector3 across)
        {
            var root = new GameObject("Toy Rack").transform;
            root.SetParent(parent, true);
            root.position = center;
            var glow = Look.Glow(new Color(1f, 0.8f, 0.15f));
            int n = Toys.All.Length;
            for (int i = 0; i < n; i++)
            {
                var kind = Toys.All[i];
                var pos = center + across * ((i - (n - 1) * 0.5f) * 0.15f);
                var item = Ball.Body(kind);
                item.name = "Rack " + kind;
                var col = item.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                item.transform.SetParent(root, true);
                item.transform.position = pos;
                item.transform.localScale *= Mathf.Clamp(0.075f / Ball.Spec(kind).radius, 0.55f, 2.5f);
                var r = item.AddComponent<RackItem>();
                r.kind = kind;
                r.marker = Look.Box(root, "marker " + kind, pos - center + Vector3.down * 0.1f, new Vector3(0.1f, 0.006f, 0.1f), glow, false);
                r.marker.transform.localPosition = pos - center + Vector3.down * 0.1f;
                var label = Look.Label(root, "name " + kind, Toys.Name(kind), 0.03f, Color.white, Vector3.zero);
                label.transform.position = pos + Vector3.down * 0.15f;
                label.gameObject.AddComponent<Billboard>();
            }
            var title = Look.Label(root, "title", "TOYS   (pinch one, or A)", 0.035f, new Color(1f, 0.85f, 0.2f), Vector3.zero);
            title.transform.position = center + Vector3.up * 0.16f;
            title.gameObject.AddComponent<Billboard>();
        }
    }
}
