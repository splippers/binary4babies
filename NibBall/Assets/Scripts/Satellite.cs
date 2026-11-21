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
                        // nothing to hang on that way: float in open air, nearer in if the first try is inside something
                        bool ok = false; var at = Vector3.zero;
                        for (float d = 2.2f + (float)rng.NextDouble() * 1.2f; d > 1.0f && !ok; d -= 0.5f)
                        {
                            at = stand + dir * d; at.y = height;
                            ok = OpenAir(stand, at);
                        }
                        if (!ok) continue;   // boxed in this way: try another bearing
                        spot = new Spot { position = at, normal = -dir, floating = true };
                    }
                    if (layout.againstWall && Flat(spot.position - layout.baseCenter).magnitude < 1.3f) continue;   // keep clear of the bins
                    spots.Add(spot); used.Add(b);
                }
            }
            for (int k = 0; spots.Count < 4 && k < 24; k++)   // a cramped room: look for any open air around the player
            {
                var dir = Quaternion.Euler(0f, 180f + k * 37f, 0f) * toBins;
                var at = stand + dir * (1.2f + (k % 3) * 0.5f); at.y = floorY + 1.0f + (k % 4) * 0.25f;
                if (OpenAir(stand, at)) spots.Add(new Spot { position = at, normal = -dir, floating = true });
            }
            while (spots.Count < 4)   // nothing is open at all (no room data): put them in front of the player regardless
            {
                var dir = Quaternion.Euler(0f, 180f + spots.Count * 25f, 0f) * toBins;
                spots.Add(new Spot { position = stand + dir * 2.0f + Vector3.up * (floorY + 1.3f), normal = -dir, floating = true });
            }
            for (int i = spots.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }   // which bit goes where
            return spots;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>
        /// Finds a spot by looking along the bearing from head height at the real room, not at the plan of it: the first solid thing the
        /// line meets (wall or furniture) is where the target hangs, 5 cm proud of it, so it can never be buried. A line that crosses a
        /// doorway first is rejected, as it would put the target in another room.
        /// </summary>
        static bool TryWall(IList<Surface> room, Vector3 stand, Vector3 dir, float height, out Spot spot)
        {
            spot = default;
            Physics.SyncTransforms();
            var eye = stand + Vector3.up * Mathf.Max(1.4f, height);
            var aim = stand + dir * 8f; aim.y = height;
            if (Sight.Blocked(eye, aim, out var hit, out bool doorway))
            {
                if (doorway || hit.distance < 1.2f || hit.distance > 7f) return false;
                if (Mathf.Abs(hit.normal.y) > 0.5f) return false;   // a tabletop or the floor, not something to hang on
                var n = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
                var at = hit.point + hit.normal * 0.05f;
                if (Sight.Buried(at + n * 0.1f, 0.08f) || OnGlassOrDoor(at)) return false;
                spot = new Spot { position = at, normal = n, floating = false };
                return true;
            }
            return false;
        }

        // a target must not sit on a window (it would shatter) or a door
        static bool OnGlassOrDoor(Vector3 at)
        {
            foreach (var r in RoomSurface.All)
            {
                if (r.kind != SurfaceKind.Window && r.kind != SurfaceKind.Door) continue;
                var l = r.transform.InverseTransformPoint(at);
                if (Mathf.Abs(l.y) < 0.2f && Mathf.Abs(l.x) < r.size.x * 0.5f + 0.25f && Mathf.Abs(l.z) < r.size.y * 0.5f + 0.25f) return true;
            }
            return false;
        }

        /// <summary>Open air for a floating target: nothing solid within reach of it and a clear look from the player.</summary>
        public static bool OpenAir(Vector3 stand, Vector3 at)
        {
            Physics.SyncTransforms();
            return !Sight.Buried(at, 0.3f) && Sight.Clear(stand + Vector3.up * 1.4f, at);
        }
    }
}
