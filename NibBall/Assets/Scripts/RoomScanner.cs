using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace NibBall
{
    /// <summary>
    /// Turns the Quest's Room Setup planes into RoomSurfaces. Each surface hangs under its ARPlane, so it follows the
    /// headset's tracking as the room is re-anchored, and is destroyed when the plane is removed.
    /// </summary>
    public sealed class RoomScanner : MonoBehaviour
    {
        public ARPlaneManager planes;
        readonly Dictionary<TrackableId, RoomSurface> map = new Dictionary<TrackableId, RoomSurface>();

        public int WallCount { get { int n = 0; foreach (var s in RoomSurface.All) if (s.kind == SurfaceKind.Wall && !s.patch) n++; return n; } }

        void OnEnable() { if (planes != null) planes.trackablesChanged.AddListener(OnChanged); }
        void OnDisable() { if (planes != null) planes.trackablesChanged.RemoveListener(OnChanged); }

        void OnChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            foreach (var p in args.added) Sync(p);
            foreach (var p in args.updated) Sync(p);
            foreach (var kv in args.removed) map.Remove(kv.Key);   // the surface is a child of the plane, which AR Foundation destroys
        }

        public static SurfaceKind Classify(ARPlane p)
        {
            var c = p.classifications;
            if ((c & PlaneClassifications.Floor) != 0) return SurfaceKind.Floor;
            if ((c & PlaneClassifications.Ceiling) != 0) return SurfaceKind.Ceiling;
            if ((c & (PlaneClassifications.WindowFrame | PlaneClassifications.WallArt)) != 0) return SurfaceKind.Window;
            if ((c & PlaneClassifications.DoorFrame) != 0) return SurfaceKind.Door;
            if ((c & (PlaneClassifications.WallFace | PlaneClassifications.InnerWallFace)) != 0) return SurfaceKind.Wall;
            if ((c & (PlaneClassifications.Table | PlaneClassifications.Couch | PlaneClassifications.Seat)) != 0) return SurfaceKind.Table;
            if ((c & PlaneClassifications.InvisibleWallFace) != 0) return SurfaceKind.Other;
            return p.alignment == PlaneAlignment.Vertical ? SurfaceKind.Wall : SurfaceKind.Other;
        }

        void Sync(ARPlane p)
        {
            var kind = Classify(p);
            if (kind == SurfaceKind.Other) return;
            var localPos = new Vector3(p.centerInPlaneSpace.x, 0f, p.centerInPlaneSpace.y);
            if (map.TryGetValue(p.trackableId, out var existing) && existing != null)
            {
                bool same = existing.kind == kind && (existing.size - p.size).sqrMagnitude < 0.01f;
                if (same) { existing.transform.localPosition = localPos; return; }
                Destroy(existing.gameObject);   // reshaped or reclassified: start the surface afresh
            }
            map[p.trackableId] = RoomSurface.Build(p.transform, kind, localPos, Quaternion.identity, p.size);
        }

        /// <summary>The current room as plain data for the placement solver.</summary>
        public static List<Surface> Snapshot()
        {
            var list = new List<Surface>();
            foreach (var s in RoomSurface.All) if (!s.patch) list.Add(s.Snapshot());
            return list;
        }

        public static float FloorY()
        {
            foreach (var s in RoomSurface.All) if (s.kind == SurfaceKind.Floor) return s.transform.position.y;
            return 0f;
        }
    }
}
