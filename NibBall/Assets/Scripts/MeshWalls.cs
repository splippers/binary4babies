using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    /// <summary>
    /// Finds the walls in the scanned room mesh when the headset hands over no labelled planes. Near-vertical triangles are
    /// binned by which way they face and how far along that direction they lie; the busiest bins are the walls. Windows and
    /// doors cannot be told from wall this way, so they come out as wall.
    /// </summary>
    public static class MeshWalls
    {
        public struct Wall { public Vector3 center, normal; public Vector2 size; }   // size: width along normal x up, then height

        struct Tri { public Vector3 a, b, c, n, centroid; public float area; public int hb, db; }

        const float HeadingStep = 6f, OffsetStep = 0.1f, MinArea = 2.0f, MinSide = 1.0f, MaxVerticalTilt = 0.25f;
        const int Headings = 60, MaxWalls = 10;

        /// <summary>Runs over several frames. 'meshes' are world-space triangle soups; 'viewer' picks which side of each wall faces into the room.</summary>
        public static IEnumerator Extract(IList<(Mesh mesh, Matrix4x4 toWorld)> meshes, Vector3 viewer, List<Wall> result)
        {
            result.Clear();
            var tris = new List<Tri>();
            int sinceYield = 0;
            var verts = new List<Vector3>();
            var idx = new List<int>();
            foreach (var (mesh, xf) in meshes)
            {
                if (mesh == null || !mesh.isReadable) continue;
                verts.Clear(); idx.Clear();
                mesh.GetVertices(verts);
                for (int s = 0; s < mesh.subMeshCount; s++) { var part = new List<int>(); mesh.GetIndices(part, s); idx.AddRange(part); }
                for (int i = 0; i + 2 < idx.Count; i += 3)
                {
                    var a = xf.MultiplyPoint3x4(verts[idx[i]]); var b = xf.MultiplyPoint3x4(verts[idx[i + 1]]); var c = xf.MultiplyPoint3x4(verts[idx[i + 2]]);
                    var cross = Vector3.Cross(b - a, c - a);
                    float len = cross.magnitude;
                    if (len < 2e-5f) continue;
                    var n = cross / len;
                    if (Mathf.Abs(n.y) > MaxVerticalTilt) continue;
                    var cen = (a + b + c) / 3f;
                    var flat = new Vector2(n.x, n.z).normalized;
                    float heading = Mathf.Atan2(flat.y, flat.x) * Mathf.Rad2Deg + 360f;
                    tris.Add(new Tri
                    {
                        a = a, b = b, c = c, n = n, centroid = cen, area = len * 0.5f,
                        hb = Mathf.RoundToInt(heading / HeadingStep) % Headings,
                        db = Mathf.RoundToInt((flat.x * cen.x + flat.y * cen.z) / OffsetStep),
                    });
                    if (++sinceYield > 20000) { sinceYield = 0; yield return null; }
                }
            }

            var bins = new Dictionary<long, float>();
            foreach (var t in tris) { var k = Key(t.hb, t.db); bins.TryGetValue(k, out float v); bins[k] = v + t.area; }
            var used = new bool[tris.Count];

            for (int found = 0; found < MaxWalls; found++)
            {
                long bestKey = 0; float best = MinArea; bool any = false;
                foreach (var kv in bins)
                {
                    float sum = Neighbourhood(bins, kv.Key);
                    if (sum > best) { best = sum; bestKey = kv.Key; any = true; }
                }
                if (!any) break;
                int bh = (int)(bestKey / 1000000), bd = (int)(bestKey % 1000000) - 500000;

                var members = new List<int>();
                for (int i = 0; i < tris.Count; i++)
                {
                    if (used[i]) continue;
                    int dh = Mathf.Abs(tris[i].hb - bh); dh = Mathf.Min(dh, Headings - dh);
                    if (dh <= 1 && Mathf.Abs(tris[i].db - bd) <= 1) members.Add(i);
                }
                for (int dh = -1; dh <= 1; dh++) for (int dd = -1; dd <= 1; dd++) bins.Remove(Key((bh + dh + Headings) % Headings, bd + dd));
                foreach (var i in members) used[i] = true;
                if (members.Count > 0 && Fit(tris, members, viewer, out var wall)) result.Add(wall);
                yield return null;
            }
        }

        static long Key(int hb, int db) => (long)hb * 1000000 + (db + 500000);

        static float Neighbourhood(Dictionary<long, float> bins, long key)
        {
            int hb = (int)(key / 1000000), db = (int)(key % 1000000) - 500000;
            float sum = 0f;
            for (int dh = -1; dh <= 1; dh++)
                for (int dd = -1; dd <= 1; dd++)
                    if (bins.TryGetValue(Key((hb + dh + Headings) % Headings, db + dd), out float v)) sum += v;
            return sum;
        }

        static bool Fit(List<Tri> tris, List<int> members, Vector3 viewer, out Wall wall)
        {
            wall = default;
            var nSum = Vector3.zero; var cSum = Vector3.zero; float aSum = 0f;
            foreach (var i in members) { var t = tris[i]; nSum += t.n * t.area; cSum += t.centroid * t.area; aSum += t.area; }
            var n = new Vector3(nSum.x, 0f, nSum.z).normalized;
            var c = cSum / aSum;
            if (Vector3.Dot(n, viewer - c) < 0f) n = -n;
            var right = Vector3.Cross(n, Vector3.up).normalized;

            var across = new List<float>(); var ups = new List<float>();
            foreach (var i in members)
            {
                var t = tris[i];
                foreach (var v in new[] { t.a, t.b, t.c }) { across.Add(Vector3.Dot(v - c, right)); ups.Add(v.y); }
            }
            across.Sort(); ups.Sort();
            float r0 = Percentile(across, 0.02f), r1 = Percentile(across, 0.98f), y0 = Percentile(ups, 0.02f), y1 = Percentile(ups, 0.98f);
            float w = r1 - r0, h = y1 - y0;
            if (w < MinSide || h < MinSide) return false;
            wall = new Wall { center = new Vector3(c.x, 0f, c.z) + right * ((r0 + r1) * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f), normal = n, size = new Vector2(w, h) };
            return true;
        }

        static float Percentile(List<float> sorted, float p) => sorted[Mathf.Clamp(Mathf.RoundToInt(p * (sorted.Count - 1)), 0, sorted.Count - 1)];
    }
}
