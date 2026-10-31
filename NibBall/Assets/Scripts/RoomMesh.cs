using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace NibBall
{
    /// <summary>Marks a scene-mesh chunk so a ball hit on it is recognised as room geometry.</summary>
    public sealed class RoomMeshChunk : MonoBehaviour { }

    /// <summary>
    /// The Quest's full room mesh (every lump and bump Room Setup captured). Each chunk gets a MeshCollider so balls bounce off
    /// furniture and clutter, and a faint overlay that is shown while scanning and toggled with Y.
    /// </summary>
    public sealed class RoomMesh : MonoBehaviour
    {
        public static RoomMesh Instance;
        public ARMeshManager manager;
        Material overlay;
        bool visible = true;
        float shownFor, nextSweep;
        int seen;

        /// <summary>Raised the first time walls have been worked out from the mesh.</summary>
        public System.Action WallsFound;
        readonly List<RoomSurface> derived = new List<RoomSurface>();
        bool deriving, notified;
        int derivedFromTris;
        float nextDerive;
        public int DerivedWalls => derived.Count;

        public int ChunkCount => manager != null ? manager.transform.childCount : 0;
        public int Triangles { get; private set; }

        public static RoomMesh Create(Transform parent)
        {
            var go = new GameObject("Room Mesh");
            go.transform.SetParent(parent, false);
            var template = new GameObject("Mesh Chunk");
            template.transform.SetParent(go.transform, false);
            template.AddComponent<MeshFilter>();
            template.AddComponent<MeshRenderer>();
            template.AddComponent<MeshCollider>();
            template.AddComponent<RoomMeshChunk>();
            template.SetActive(false);

            var rm = go.AddComponent<RoomMesh>();
            Instance = rm;
            rm.overlay = Look.Mat(Look.Scan, new Color(0.2f, 0.9f, 1f, 0.8f));
            rm.overlay.SetFloat("_Cell", 0.3f); rm.overlay.SetFloat("_Line", 0.02f); rm.overlay.SetFloat("_Fill", 0.05f);

            var mm = go.AddComponent<ARMeshManager>();
            mm.meshPrefab = template.GetComponent<MeshFilter>();
            mm.density = 1f;
            mm.normals = true;
            rm.manager = mm;
            return rm;
        }

        public void Toggle() { visible = !visible; shownFor = 1e6f; Sweep(); }

        void Update()
        {
            shownFor += Time.deltaTime;
            if (Time.time < nextSweep) return;
            nextSweep = Time.time + 0.5f;
            if (visible && shownFor > 90f) visible = false;   // fade out once the player has seen the scan
            Sweep();
            if (!deriving && Time.time > nextDerive && Triangles > 1500 && (derivedFromTris == 0 || Mathf.Abs(Triangles - derivedFromTris) > derivedFromTris * 0.15f))
                StartCoroutine(Derive());
        }

        /// <summary>The headset gave no labelled planes, so find the walls in the mesh itself and make them real, breakable surfaces.</summary>
        IEnumerator Derive()
        {
            deriving = true;
            derivedFromTris = Triangles;
            var meshes = new List<(Mesh, Matrix4x4)>();
            foreach (Transform c in manager.transform)
            {
                var mf = c.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) meshes.Add((mf.sharedMesh, c.localToWorldMatrix));
            }
            var cam = Camera.main;
            var found = new List<MeshWalls.Wall>();
            yield return StartCoroutine(MeshWalls.Extract(meshes, cam != null ? cam.transform.position : Vector3.zero, found));
            Apply(found);
            nextDerive = Time.time + 8f;
            deriving = false;
        }

        public void Apply(List<MeshWalls.Wall> walls)
        {
            // keep a real (labelled) scan if there is one; derived walls only stand in for it
            foreach (var s in RoomSurface.All) if (s.kind == SurfaceKind.Wall && !s.patch && !derived.Contains(s)) { Debug.Log("NibBall: labelled walls exist, not deriving from the mesh"); return; }
            foreach (var d in derived) if (d != null) Destroy(d.gameObject);
            derived.Clear();
            foreach (var w in walls)
            {
                var rot = Quaternion.LookRotation(Vector3.up, w.normal);   // local Y = normal, Z = up, X = along the wall
                var rs = RoomSurface.Build(null, SurfaceKind.Wall, w.center, rot, w.size);
                rs.name = "Surface derived wall";
                derived.Add(rs);
                Debug.Log($"NibBall: derived wall at {w.center:0.00} facing {w.normal:0.00} size {w.size.x:0.0} x {w.size.y:0.0} m");
            }
            Debug.Log($"NibBall: derived {derived.Count} walls from {Triangles} mesh triangles");
            if (derived.Count > 0 && !notified) { notified = true; WallsFound?.Invoke(); }
        }

        void Sweep()
        {
            if (manager == null) return;
            seen = 0;
            int tris = 0;
            foreach (Transform c in manager.transform)
            {
                var r = c.GetComponent<MeshRenderer>();
                if (r == null) continue;
                seen++;
                var mf = c.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    long idx = 0;
                    for (int s = 0; s < mf.sharedMesh.subMeshCount; s++) idx += mf.sharedMesh.GetIndexCount(s);
                    tris += (int)(idx > 0 ? idx / 3 : mf.sharedMesh.vertexCount / 3);
                }
                r.sharedMaterial = overlay;
                r.enabled = visible;
                if (c.GetComponent<RoomMeshChunk>() == null) c.gameObject.AddComponent<RoomMeshChunk>();
            }
            Triangles = tris;
        }
    }
}
