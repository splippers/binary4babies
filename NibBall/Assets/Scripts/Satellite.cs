using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    /// <summary>A bullseye somewhere else in the room that flips one bit, the same as that bin. Fixed to a wall, or floating in the open if there is no wall that way.</summary>
    public sealed class Satellite : MonoBehaviour
    {
        public Arena arena;
        public int index;
        bool floating;
        Vector3 anchor;
        float phase, cooldown, pulse;
        Renderer centre;
        TextMesh digit;
        static readonly Color Dark = new Color(0.14f, 0.17f, 0.26f);

        public static Satellite Build(Arena a, int index, SatellitePlacer.Spot spot)
        {
            var go = new GameObject("Satellite " + Nibble.Weights[index]);
            go.transform.SetParent(a.transform, true);
            go.transform.position = spot.position;
            go.transform.rotation = Quaternion.LookRotation(-spot.normal, Vector3.up);   // local -Z faces the room
            var s = go.AddComponent<Satellite>();
            s.arena = a; s.index = index; s.floating = spot.floating; s.anchor = spot.position;
            s.phase = Random.value * 6.28f;
            s.Construct();
            if (spot.floating) go.AddComponent<Billboard>();
            return s;
        }

        void Construct()
        {
            var red = Look.Lit_(new Color(0.9f, 0.2f, 0.2f));
            var white = Look.Lit_(new Color(0.95f, 0.95f, 0.95f));
            Disc("outer", 0.36f, 0f, red);
            Disc("mid", 0.27f, -0.008f, white);
            Disc("inner", 0.2f, -0.016f, red);
            centre = Disc("centre", 0.15f, -0.024f, Look.Glow(Dark));
            digit = Look.Label(transform, "weight", Nibble.Weights[index].ToString(), 0.1f, Color.white, new Vector3(0f, 0f, -0.032f));

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0f, -0.012f);
            box.size = new Vector3(0.36f, 0.36f, 0.04f);
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        Renderer Disc(string name, float diameter, float z, Material m)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            d.name = name;
            Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(transform, false);
            d.transform.localPosition = new Vector3(0f, 0f, z);
            d.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            d.transform.localScale = new Vector3(diameter, 0.004f, diameter);
            var r = d.GetComponent<Renderer>();
            r.sharedMaterial = m;
            return r;
        }

        public void SetLit(bool on, Color c)
        {
            centre.sharedMaterial.color = on ? c : Dark;
            digit.color = on ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
        }

        void Update()
        {
            if (floating) transform.position = anchor + Vector3.up * Mathf.Sin(Time.time * 1.3f + phase) * 0.06f;
            if (pulse > 0f)
            {
                pulse -= Time.deltaTime * 3f;
                transform.localScale = Vector3.one * (1f + Mathf.Max(0f, pulse) * 0.25f);
            }
        }

        void OnCollisionEnter(Collision c)
        {
            var ball = c.collider.GetComponent<Ball>();
            if (ball == null || ball.held || Time.time < cooldown) return;
            cooldown = Time.time + 0.5f;
            pulse = 1f;
            arena.Flip(index, transform.position);
        }
    }

    /// <summary>Picks where the four satellites go: at different bearings around the player, on the wall that way if the scan has one, else floating.</summary>
    public static class SatellitePlacer
    {
        public struct Spot { public Vector3 position, normal; public bool floating; }

        static readonly float[] Bearings = { -150f, -115f, -80f, -45f, 45f, 80f, 115f, 150f };

        public static List<Spot> Solve(ArenaLayout layout, IList<Surface> room, float floorY, System.Random rng)
        {
            var stand = layout.baseCenter + layout.forward * layout.throwDistance;
            var toBins = -layout.forward; toBins.y = 0f; toBins.Normalize();
            var order = new List<float>(Bearings);
            for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (order[i], order[j]) = (order[j], order[i]); }

            var spots = new List<Spot>();
            var used = new List<float>();
            for (int pass = 0; pass < 2 && spots.Count < 4; pass++)
            {
                float minApart = pass == 0 ? 40f : 0f;
                foreach (var b in order)
                {
                    if (spots.Count >= 4) break;
                    if (used.Contains(b)) continue;
                    bool close = false;
                    foreach (var u in used) if (Mathf.Abs(Mathf.DeltaAngle(u, b)) < minApart) close = true;
                    if (close) continue;
                    var dir = Quaternion.Euler(0f, b, 0f) * toBins;
                    float height = floorY + 0.8f + (float)rng.NextDouble() * 1.1f;
                    if (!TryWall(room, stand, dir, height, out var spot))
                    {
                        float d = 2.2f + (float)rng.NextDouble() * 1.2f;
                        var at = stand + dir * d; at.y = height;
                        spot = new Spot { position = at, normal = -dir, floating = true };
                    }
                    if (layout.againstWall && Flat(spot.position - layout.baseCenter).magnitude < 1.3f) continue;   // keep clear of the bins
                    spots.Add(spot); used.Add(b);
                }
            }
            while (spots.Count < 4)   // a cramped room: stack the rest in the open
            {
                var dir = Quaternion.Euler(0f, 180f + spots.Count * 25f, 0f) * toBins;
                spots.Add(new Spot { position = stand + dir * 2.5f + Vector3.up * (floorY + 1.3f), normal = -dir, floating = true });
            }
            for (int i = spots.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }   // which bit goes where
            return spots;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        static bool TryWall(IList<Surface> room, Vector3 stand, Vector3 dir, float height, out Spot spot)
        {
            spot = default;
            float best = 7f; Surface hit = null;
            var p = new Vector2(stand.x, stand.z); var d = new Vector2(dir.x, dir.z);
            if (room != null)
                foreach (var s in room)
                {
                    if (s.kind != SurfaceKind.Wall || !s.IsVertical) continue;
                    var c = new Vector2(s.center.x, s.center.z);
                    var t = new Vector2(s.right.x, s.right.z).normalized;
                    float half = s.size.x * 0.5f;
                    if (ArenaPlacer.Intersect(p, d, c - t * half, c + t * half, out float dist) && dist > 1.2f && dist < best) { best = dist; hit = s; }
                }
            if (hit == null) return false;
            var n = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            if (Vector3.Dot(n, dir) > 0f) n = -n;
            var at = stand + dir * best + n * 0.06f; at.y = height;
            spot = new Spot { position = at, normal = n, floating = false };
            return true;
        }
    }
}
