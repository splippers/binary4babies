using System;
using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    /// <summary>
    /// A real wall, window, door, floor or table: an invisible collider the ball can hit, plus a see-through overlay that
    /// collects the damage. The room itself is never touched; a fast ball punches a dark crater into a wall, or shatters a
    /// window's glass, and the ball carries on through into the "outside". The GameObject's local frame is the plane's:
    /// Y is the normal, X the width, Z the other in-plane axis.
    /// </summary>
    public sealed class RoomSurface : MonoBehaviour
    {
        public static readonly List<RoomSurface> All = new List<RoomSurface>();
        /// <summary>Raised once per window broken or wall hole made.</summary>
        public static Action<SurfaceKind, Vector3> Damaged;

        const float PxPerMetre = 48f;
        const int MaxHoles = 14;

        public SurfaceKind kind;
        public Vector2 size;
        public BoxCollider col;
        public bool broken;
        /// <summary>A stand-in made where a ball hit the scene mesh with no scanned wall behind it; never used to place the arena.</summary>
        public bool patch;

        struct Hole { public Vector2 pos; public float r; }
        readonly List<Hole> holes = new List<Hole>();
        Texture2D tex;
        Color32[] pix;
        int tw, th;
        float seed;

        public bool Breakable => kind == SurfaceKind.Window || kind == SurfaceKind.Wall || kind == SurfaceKind.Door;
        float Threshold => kind == SurfaceKind.Window ? 2.0f : 3.0f;

        public static RoomSurface Build(Transform parent, SurfaceKind kind, Vector3 localPos, Quaternion localRot, Vector2 size)
        {
            var go = new GameObject("Surface " + kind);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            var rs = go.AddComponent<RoomSurface>();
            rs.kind = kind;
            rs.size = size;
            rs.seed = UnityEngine.Random.value * 10f;

            // glass and doors stand a hair proud of the wall they sit in, so the ball meets them first
            float face = (kind == SurfaceKind.Window || kind == SurfaceKind.Door) ? 0.02f : 0f;
            const float thick = 0.06f;
            rs.col = go.AddComponent<BoxCollider>();
            rs.col.center = new Vector3(0f, face - thick * 0.5f, 0f);
            rs.col.size = new Vector3(size.x, thick, size.y);
            return rs;
        }

        /// <summary>A breakable wall patch lying on the scene mesh where a hard ball hit it, so any real wall can be broken, scanned as a wall or not.</summary>
        public static RoomSurface Patch(Vector3 point, Vector3 normal)
        {
            var up = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (up.sqrMagnitude < 0.01f) return null;
            var rs = Build(null, SurfaceKind.Wall, point, Quaternion.LookRotation(up.normalized, normal), new Vector2(1.4f, 1.4f));
            rs.patch = true;
            rs.name = "Surface patch";
            return rs;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public Surface Snapshot() => new Surface
        {
            kind = kind,
            center = transform.position,
            normal = transform.up,
            right = transform.right,
            size = size,
        };

        bool InHole(Vector2 p)
        {
            if (broken) return true;
            foreach (var h in holes) if ((h.pos - p).sqrMagnitude < h.r * h.r) return true;
            return false;
        }

        /// <summary>The breakable surface whose face is within a few cm of this point (scene-mesh hits stand in front of it).</summary>
        public static RoomSurface Near(Vector3 point)
        {
            RoomSurface best = null; float bestD = 0.12f;
            foreach (var s in All)
            {
                if (!s.Breakable) continue;
                var l = s.transform.InverseTransformPoint(point);
                if (Mathf.Abs(l.x) > s.size.x * 0.5f + 0.03f || Mathf.Abs(l.z) > s.size.y * 0.5f + 0.03f) continue;
                float d = Mathf.Abs(l.y);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        /// <summary>Called by a ball that has just hit this surface. True means it went through (an old hole, or a new one it just made).</summary>
        public bool Impact(Vector3 point, Vector3 preVelocity, Ball ball)
        {
            if (!Breakable) return false;
            var local = transform.InverseTransformPoint(point);
            var p = new Vector2(local.x, local.z);
            if (InHole(p)) return true;

            float normalSpeed = Mathf.Abs(Vector3.Dot(preVelocity, transform.up));
            if (normalSpeed < Threshold) return false;

            if (kind == SurfaceKind.Window) ShatterPane(point, preVelocity.normalized);
            else PunchHole(p, point, preVelocity.normalized);
            return true;
        }

        /// <summary>The ball passed this surface: also ignore every other surface lying in the same spot (the wall behind a window).</summary>
        public static void IgnoreStackAt(Ball ball, Vector3 point, RoomSurface hit)
        {
            Physics.IgnoreCollision(ball.col, hit.col, true);
            foreach (var other in All)
            {
                if (other == hit || other.col == null) continue;
                if (Mathf.Abs(Vector3.Dot(other.transform.up, hit.transform.up)) < 0.9f) continue;
                var l = other.transform.InverseTransformPoint(point);
                if (Mathf.Abs(l.y) > 0.15f || Mathf.Abs(l.x) > other.size.x * 0.5f + 0.05f || Mathf.Abs(l.z) > other.size.y * 0.5f + 0.05f) continue;
                Physics.IgnoreCollision(ball.col, other.col, true);
            }
        }

        // ------------------------------------------------------------ damage

        void PunchHole(Vector2 p, Vector3 world, Vector3 dir)
        {
            if (holes.Count >= MaxHoles) return;
            float r = UnityEngine.Random.Range(0.10f, 0.17f);
            holes.Add(new Hole { pos = p, r = r });
            EnsureOverlay();
            Disc(p, r, new Color32(12, 12, 16, 245), new Color32(205, 200, 190, 235));
            int cracks = UnityEngine.Random.Range(5, 9);
            for (int i = 0; i < cracks; i++)
                Crack(p, UnityEngine.Random.value * Mathf.PI * 2f, r * UnityEngine.Random.Range(1.6f, 3.4f), new Color32(25, 25, 28, 220), 1);
            Upload();

            Debris.Burst(world, -dir * 0.3f + transform.up, new Color(0.88f, 0.86f, 0.8f), 16, 2.6f, 0.015f, 0.05f);
            Debris.Burst(world, transform.up, new Color(0.35f, 0.32f, 0.3f), 8, 2.2f, 0.02f, 0.06f);
            Sfx.Crash(world);
            Damaged?.Invoke(kind, world);
        }

        void ShatterPane(Vector3 world, Vector3 dir)
        {
            broken = true;
            var local = transform.InverseTransformPoint(world);
            var p = new Vector2(local.x, local.z);
            EnsureOverlay();
            Disc(p, 0.11f, new Color32(0, 0, 0, 0), new Color32(235, 245, 255, 210));
            float reach = Mathf.Max(size.x, size.y);
            for (int i = 0; i < 16; i++)
                Crack(p, UnityEngine.Random.value * Mathf.PI * 2f, reach * UnityEngine.Random.Range(0.35f, 0.9f), new Color32(235, 245, 255, 230), 2);
            Upload();

            int shards = Mathf.Clamp(Mathf.RoundToInt(size.x * size.y * 45f), 14, 70);
            for (int i = 0; i < shards; i++)
            {
                var at = transform.TransformPoint(new Vector3(UnityEngine.Random.Range(-size.x, size.x) * 0.5f, 0.03f, UnityEngine.Random.Range(-size.y, size.y) * 0.5f));
                Debris.Burst(at, dir, new Color(0.75f, 0.92f, 1f), 1, 2.4f, 0.02f, 0.07f, 4f, 0.6f);
            }
            Sfx.Shatter(world);
            Damaged?.Invoke(kind, world);
        }

        // ------------------------------------------------------------ overlay painting

        void EnsureOverlay()
        {
            if (tex != null) return;
            tw = Mathf.Clamp(Mathf.RoundToInt(size.x * PxPerMetre), 8, 256);
            th = Mathf.Clamp(Mathf.RoundToInt(size.y * PxPerMetre), 8, 256);
            tex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            pix = new Color32[tw * th];
            tex.SetPixels32(pix);
            tex.Apply();
            var go = new GameObject("Damage");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, (kind == SurfaceKind.Window || kind == SurfaceKind.Door ? 0.02f : 0f) + 0.004f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = Look.PlaneMesh(size.x, size.y);
            var m = Look.Mat(Look.Overlay, Color.white);
            m.mainTexture = tex;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
        }

        void Upload() { tex.SetPixels32(pix); tex.Apply(); }

        Vector2Int ToPx(Vector2 m) => new Vector2Int(Mathf.FloorToInt((m.x / size.x + 0.5f) * tw), Mathf.FloorToInt((m.y / size.y + 0.5f) * th));

        void Put(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= tw || y >= th) return;
            int i = y * tw + x;
            if (c.a >= pix[i].a) pix[i] = c;
        }

        void Disc(Vector2 c, float r, Color32 fill, Color32 rim)
        {
            var lo = ToPx(c - Vector2.one * r * 1.4f); var hi = ToPx(c + Vector2.one * r * 1.4f);
            for (int y = Mathf.Max(0, lo.y); y <= Mathf.Min(th - 1, hi.y); y++)
                for (int x = Mathf.Max(0, lo.x); x <= Mathf.Min(tw - 1, hi.x); x++)
                {
                    var m = new Vector2(((x + 0.5f) / tw - 0.5f) * size.x, ((y + 0.5f) / th - 0.5f) * size.y) - c;
                    float d = m.magnitude, a = Mathf.Atan2(m.y, m.x);
                    float rr = r * (0.85f + 0.15f * Mathf.Sin(a * 5f + seed) + 0.1f * Mathf.Sin(a * 9f + seed * 2f));
                    int i = y * tw + x;
                    if (d < rr) pix[i] = fill;
                    else if (d < rr * 1.3f && pix[i].a < rim.a) pix[i] = rim;
                }
        }

        void Crack(Vector2 from, float angle, float length, Color32 colour, int branches)
        {
            var p = from;
            float step = 1f / (PxPerMetre * 1.2f);
            for (float walked = 0f; walked < length; walked += step)
            {
                angle += UnityEngine.Random.Range(-0.12f, 0.12f);
                p += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * step;
                if (Mathf.Abs(p.x) > size.x * 0.5f || Mathf.Abs(p.y) > size.y * 0.5f) return;
                var px = ToPx(p);
                Put(px.x, px.y, colour);
                if (walked < length * 0.5f) Put(px.x + 1, px.y, colour);
                if (branches > 0 && UnityEngine.Random.value < 0.012f)
                    Crack(p, angle + UnityEngine.Random.Range(0.4f, 0.9f) * (UnityEngine.Random.value < 0.5f ? -1f : 1f), (length - walked) * 0.6f, colour, branches - 1);
            }
        }
    }
}
