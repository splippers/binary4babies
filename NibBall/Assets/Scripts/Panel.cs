using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    /// <summary>A button you press by touching it with a fingertip (or the tip of a controller) or by hitting it with a toy.</summary>
    public sealed class PanelButton : MonoBehaviour
    {
        public static readonly List<PanelButton> All = new List<PanelButton>();
        public System.Action Pressed;
        /// <summary>Raised with the button's name whenever any button is pressed (the tutorial listens).</summary>
        public static System.Action<string> AnyPressed;
        Renderer face;
        float cooldown, flash;
        static readonly Color Idle = new Color(0.2f, 0.35f, 0.6f), Lit = new Color(1f, 0.85f, 0.2f);

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static PanelButton Build(Transform parent, string text, Vector3 localPos, Vector2 size, System.Action pressed)
        {
            var go = new GameObject("Button " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var b = go.AddComponent<PanelButton>();
            b.Pressed = pressed;
            b.face = Look.Box(go.transform, "face", Vector3.zero, new Vector3(size.x, size.y, 0.012f), Look.Glow(Idle), false).GetComponent<Renderer>();
            Look.Label(go.transform, "text", text, size.y * 0.5f, Color.white, new Vector3(0f, 0f, -0.01f));
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x, size.y, 0.03f);
            go.AddComponent<Rigidbody>().isKinematic = true;
            return b;
        }

        void Update()
        {
            if (flash > 0f) { flash -= Time.deltaTime * 3f; face.sharedMaterial.color = Color.Lerp(Idle, Lit, Mathf.Clamp01(flash)); }
        }

        public void Press()
        {
            if (Time.time < cooldown) return;
            cooldown = Time.time + 0.8f;
            flash = 1f;
            Pressed?.Invoke();
            AnyPressed?.Invoke(name);
        }

        void OnCollisionEnter(Collision c)
        {
            if (c.collider.GetComponent<Ball>() != null) Press();
        }

        /// <summary>A fingertip or controller tip at this world point: press the button it is touching, if any.</summary>
        public static void Poke(Vector3 tip)
        {
            foreach (var b in All)
            {
                var l = b.transform.InverseTransformPoint(tip);
                var box = b.GetComponent<BoxCollider>();
                if (Mathf.Abs(l.x) < box.size.x * 0.5f + 0.02f && Mathf.Abs(l.y) < box.size.y * 0.5f + 0.02f && Mathf.Abs(l.z) < 0.05f) { b.Press(); return; }
            }
        }
    }

    public static class ControlPanel
    {
        /// <summary>Three buttons hung beside the stand-here ring, facing it: mend the damage, move the bins, move the targets.</summary>
        public static void Build(Arena arena, Transform parent, Vector3 center, Vector3 towardRing)
        {
            var root = new GameObject("Control Panel").transform;
            root.SetParent(parent, true);
            root.position = center;
            towardRing.y = 0f;
            root.rotation = Quaternion.LookRotation(-towardRing.normalized, Vector3.up);   // local -Z faces the player
            var size = new Vector2(0.26f, 0.07f);
            PanelButton.Build(root, "REPAIR", new Vector3(0f, 0.09f, 0f), size, () => arena.RepairRequested?.Invoke());
            PanelButton.Build(root, "MOVE BINS", new Vector3(0f, 0f, 0f), size, () => arena.RefitRequested?.Invoke());
            PanelButton.Build(root, "MOVE TARGETS", new Vector3(0f, -0.09f, 0f), size, () => arena.MoveTargets());
        }
    }
}
