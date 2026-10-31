using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    public enum SurfaceKind { Floor, Ceiling, Wall, Window, Door, Table, Other }

    /// <summary>One scanned (or synthetic) rectangle of the room, in world space. Pure data for the placement solver.</summary>
    public sealed class Surface
    {
        public SurfaceKind kind;
        public Vector3 center;
        public Vector3 normal;      // faces into the room
        public Vector3 right;       // along the width
        public Vector2 size;        // width along right, extent along the other in-plane axis
        public bool IsVertical => Mathf.Abs(normal.y) < 0.3f;
    }

    public struct ArenaLayout
    {
        public Vector3 baseCenter;   // on the floor, at the wall (or where the wall would be)
        public Vector3 forward;      // horizontal, from the wall into the room
        public float binWidth;
        public float throwDistance;  // from the wall to the "stand here" ring
        public bool againstWall;
        public float depthAvailable;
    }

    /// <summary>
    /// Fits the row of four bins into the room that was scanned. Every wall wide enough is a candidate; the one with the
    /// most free floor in front of it wins, with a small nudge towards walls near the player. The bins shrink to fit a
    /// narrow wall and the throwing distance shrinks to fit a shallow room. No wall fits: stand the row in free space.
    /// </summary>
    public static class ArenaPlacer
    {
        public const float Gap = 0.06f;
        public const float MinBin = 0.22f, MaxBin = 0.5f;
        public const float MinDepth = 1.8f;
        const float OpenCap = 6f;

        static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);

        public static ArenaLayout Solve(IList<Surface> surfaces, Vector3 player, Vector3 gaze, float floorY)
        {
            var walls = new List<Surface>();
            foreach (var s in surfaces)
                if (s.kind == SurfaceKind.Wall && s.IsVertical && Flat(s.right).sqrMagnitude > 0.01f) walls.Add(s);

            bool found = false;
            var best = new ArenaLayout();
            float bestScore = float.NegativeInfinity;

            foreach (var w in walls)
            {
                float bin = Mathf.Clamp((w.size.x - 0.4f - 3f * Gap) / 4f, 0f, MaxBin);
                if (bin < MinBin) continue;

                var wc = Flat(w.center);
                var n = Flat(w.normal).normalized;
                if (Vector2.Dot(n, Flat(player) - wc) < 0f) n = -n;
                var t = Flat(w.right).normalized;
                float half = (4f * bin + 3f * Gap) * 0.5f;

                float depth = OpenCap;
                foreach (var off in new[] { 0f, -half, half })
                {
                    var origin = wc + t * off + n * 0.25f;
                    depth = Mathf.Min(depth, FreeDistance(walls, w, origin, n));
                }
                depth += 0.25f;
                if (depth < MinDepth) continue;

                float score = Mathf.Min(depth, 5f) + bin - 0.05f * Vector2.Distance(Flat(player), wc);
                if (score <= bestScore) continue;
                bestScore = score; found = true;
                best = new ArenaLayout
                {
                    baseCenter = new Vector3(wc.x, floorY, wc.y),
                    forward = new Vector3(n.x, 0f, n.y),
                    binWidth = bin,
                    throwDistance = Mathf.Clamp(depth * 0.55f, 1.4f, Mathf.Max(1.4f, depth - 0.5f)),
                    againstWall = true,
                    depthAvailable = depth,
                };
            }
            if (found) return best;

            var g = Flat(gaze);
            g = g.sqrMagnitude < 0.01f ? Vector2.up : g.normalized;
            var at = Flat(player) + g * 3f;
            return new ArenaLayout
            {
                baseCenter = new Vector3(at.x, floorY, at.y),
                forward = new Vector3(-g.x, 0f, -g.y),
                binWidth = 0.35f,
                throwDistance = 2.5f,
                againstWall = false,
                depthAvailable = OpenCap,
            };
        }

        /// <summary>How far a ray from origin along dir runs before it meets any wall other than 'except'.</summary>
        static float FreeDistance(List<Surface> walls, Surface except, Vector2 origin, Vector2 dir)
        {
            float best = OpenCap;
            foreach (var w in walls)
            {
                if (w == except) continue;
                var c = Flat(w.center);
                var t = Flat(w.right).normalized;
                float half = w.size.x * 0.5f;
                var a = c - t * half; var b = c + t * half;
                if (Intersect(origin, dir, a, b, out float d) && d < best) best = d;
            }
            return best;
        }

        internal static bool Intersect(Vector2 p, Vector2 d, Vector2 a, Vector2 b, out float t)
        {
            t = 0f;
            var e = b - a;
            float denom = d.x * e.y - d.y * e.x;
            if (Mathf.Abs(denom) < 1e-5f) return false;
            var ap = a - p;
            t = (ap.x * e.y - ap.y * e.x) / denom;
            float s = (ap.x * d.y - ap.y * d.x) / denom;
            return t > 0.02f && s >= 0f && s <= 1f;
        }
    }
}
