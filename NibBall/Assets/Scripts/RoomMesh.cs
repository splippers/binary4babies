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
            rm.overlay = Look.Mat(Look.Scan, new Color(0.2f, 0.9f, 1f, 0.9f));

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
                if (mf != null && mf.sharedMesh != null) tris += (int)(mf.sharedMesh.GetIndexCount(0) / 3);
                r.sharedMaterial = overlay;
                r.enabled = visible;
                if (c.GetComponent<RoomMeshChunk>() == null) c.gameObject.AddComponent<RoomMeshChunk>();
            }
            Triangles = tris;
        }
    }
}
