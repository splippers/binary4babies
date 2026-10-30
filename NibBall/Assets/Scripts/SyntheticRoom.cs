using UnityEngine;

namespace NibBall
{
    /// <summary>
    /// A made-up room for the desktop editor and the self-test: 5 x 4 m, 2.6 m high, centred on the origin, a window in
    /// the +X wall and a door in the -Z wall. Drawn in flat grey so it can be seen without passthrough.
    /// </summary>
    public static class SyntheticRoom
    {
        public const float HalfX = 2.5f, HalfZ = 2.0f, Height = 2.6f;

        public static Transform Build(bool visible)
        {
            var root = new GameObject("Synthetic Room").transform;
            Add(root, SurfaceKind.Floor, new Vector3(0, 0, 0), Vector3.up, Vector3.forward, new Vector2(2 * HalfX, 2 * HalfZ), visible, 0.30f);
            Add(root, SurfaceKind.Ceiling, new Vector3(0, Height, 0), Vector3.down, Vector3.forward, new Vector2(2 * HalfX, 2 * HalfZ), false, 0.5f);
            // walls: second axis is up, so size is (width, height)
            Wall(root, new Vector3(-HalfX, Height / 2, 0), Vector3.right, 2 * HalfZ, visible, 0.55f);
            Wall(root, new Vector3(HalfX, Height / 2, 0), Vector3.left, 2 * HalfZ, visible, 0.5f);
            Wall(root, new Vector3(0, Height / 2, -HalfZ), Vector3.forward, 2 * HalfX, visible, 0.6f);
            Wall(root, new Vector3(0, Height / 2, HalfZ), Vector3.back, 2 * HalfX, visible, 0.45f);
            Add(root, SurfaceKind.Window, new Vector3(HalfX, 1.5f, 0.3f), Vector3.left, Vector3.up, new Vector2(1.4f, 1.1f), visible, 0.8f);
            Add(root, SurfaceKind.Door, new Vector3(-0.8f, 1.0f, -HalfZ), Vector3.forward, Vector3.up, new Vector2(0.9f, 2.0f), visible, 0.35f);
            return root;
        }

        static void Wall(Transform root, Vector3 centre, Vector3 normal, float width, bool visible, float shade) =>
            Add(root, SurfaceKind.Wall, centre, normal, Vector3.up, new Vector2(width, Height), visible, shade);

        static void Add(Transform root, SurfaceKind kind, Vector3 centre, Vector3 normal, Vector3 second, Vector2 size, bool visible, float shade)
        {
            var rot = Quaternion.LookRotation(second, normal);
            var rs = RoomSurface.Build(root, kind, centre, rot, size);
            if (!visible) return;
            var go = new GameObject("Look");
            go.transform.SetParent(rs.transform, false);
            go.transform.localPosition = new Vector3(0f, kind == SurfaceKind.Window || kind == SurfaceKind.Door ? 0.012f : -0.002f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = Look.PlaneMesh(size.x, size.y);
            var tint = kind == SurfaceKind.Window ? new Color(0.55f, 0.8f, 0.95f) : kind == SurfaceKind.Door ? new Color(0.5f, 0.32f, 0.2f) : new Color(shade, shade, shade * 1.05f);
            go.AddComponent<MeshRenderer>().sharedMaterial = Look.Glow(tint);
        }
    }
}
