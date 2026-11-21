using System.Collections.Generic;
using UnityEngine;

namespace NibBall
{
    /// <summary>What the room's solid shell is for sight-lines: scene-mesh chunks and wall, floor and ceiling surfaces. Doors and windows are see-through.</summary>
    public static class Sight
    {
        static readonly Comparison_ ByDistance = new Comparison_();
        sealed class Comparison_ : IComparer<RaycastHit> { public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance); }

        public static bool IsShell(Collider c)
        {
            if (c.GetComponent<RoomMeshChunk>() != null) return true;
            var s = c.GetComponent<RoomSurface>();
            return s != null && (s.kind == SurfaceKind.Wall || s.kind == SurfaceKind.Floor || s.kind == SurfaceKind.Ceiling);
        }

        static bool IsDoorway(Collider c) { var s = c.GetComponent<RoomSurface>(); return s != null && s.kind == SurfaceKind.Door; }

        /// <summary>First piece of the room shell on the way from 'from' to 'to'. 'doorway' says a door frame was crossed before it.</summary>
        public static bool Blocked(Vector3 from, Vector3 to, out RaycastHit hit, out bool doorway)
        {
            hit = default; doorway = false;
            var d = to - from; float dist = d.magnitude;
            if (dist < 0.01f) return false;
            var hits = Physics.RaycastAll(from, d / dist, dist, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, ByDistance);
            foreach (var h in hits)
            {
                if (IsDoorway(h.collider)) { doorway = true; continue; }
                if (!IsShell(h.collider)) continue;
                hit = h;
                return true;
            }
            return false;
        }

        public static bool Clear(Vector3 from, Vector3 to) => !Blocked(from, to, out _, out _);

        /// <summary>True if the room (scene mesh, or any scanned surface including tables) fills a ball of this radius at this point.</summary>
        public static bool Buried(Vector3 at, float radius)
        {
            foreach (var c in Physics.OverlapSphere(at, radius, ~0, QueryTriggerInteraction.Ignore))
                if (IsShell(c) || c.GetComponent<RoomSurface>() != null || c.GetComponent<RoomBox>() != null) return true;
            return false;
        }
    }

    /// <summary>
    /// Damage belongs to the room it is in. A hole in another room's wall is hidden unless there is a clear line of sight to
    /// it from the player's head, which in practice means only through a connecting doorway.
    /// </summary>
    public sealed class DamageVisibility : MonoBehaviour
    {
        public Camera cam;
        readonly List<Vector3> spots = new List<Vector3>();
        int next;

        void Update()
        {
            if (cam == null || RoomSurface.All.Count == 0) return;
            // a few surfaces a frame is plenty: damage does not move
            var all = RoomSurface.All;
            for (int n = 0; n < 4 && all.Count > 0; n++)
            {
                next = (next + 1) % all.Count;
                var s = all[next];
                if (!s.HasDamage) continue;
                s.DamageSpots(cam.transform.position, spots);
                bool seen = false;
                foreach (var p in spots) if (Sight.Clear(cam.transform.position, p)) { seen = true; break; }
                s.ShowDamage(seen);
            }
        }
    }
}
