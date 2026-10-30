using UnityEngine;

namespace NibBall
{
    /// <summary>Runtime materials, layers and small mesh helpers. Shaders are looked up by name, so Editor/NibBallBuild.cs lists them as always-included.</summary>
    public static class Look
    {
        public const string Lit = "Mobile/Diffuse", Unlit = "Unlit/Color", Overlay = "NibBall/Overlay", Scan = "NibBall/Scan";

        static int ballLayer = -2, debrisLayer = -2;
        public static int BallLayer => ballLayer != -2 ? ballLayer : (ballLayer = Mathf.Max(0, LayerMask.NameToLayer("Ball")));
        public static int DebrisLayer => debrisLayer != -2 ? debrisLayer : (debrisLayer = Mathf.Max(0, LayerMask.NameToLayer("Debris")));

        public static void SetupPhysics()
        {
            Time.fixedDeltaTime = 1f / 72f;
            if (BallLayer != 0 && DebrisLayer != 0)
            {
                Physics.IgnoreLayerCollision(DebrisLayer, DebrisLayer, true);
                Physics.IgnoreLayerCollision(DebrisLayer, BallLayer, true);
            }
        }

        public static Material Mat(string shader, Color c)
        {
            var m = new Material(Shader.Find(shader) ?? Shader.Find(Unlit)) { color = c };
            return m;
        }

        public static Material Lit_(Color c) => Mat(Lit, c);
        public static Material Glow(Color c) => Mat(Unlit, c);

        public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material m, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        static Font font;
        /// <summary>Flat 3D text. 'height' is roughly the line height in metres.</summary>
        public static TextMesh Label(Transform parent, string name, string text, float height, Color color, Vector3 localPos, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = text;
            tm.fontSize = 100;
            tm.characterSize = height / 10f;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            // the font's own shader is not in the build; NibBall/Overlay (Assets/Shaders) draws the glyph atlas tinted by colour
            var fm = new Material(Shader.Find(Overlay) ?? Shader.Find(Unlit)) { color = Color.white };
            fm.mainTexture = font.material.mainTexture;
            fm.SetFloat("_AlphaOnly", 1f);
            go.GetComponent<MeshRenderer>().sharedMaterial = fm;
            return tm;
        }

        /// <summary>A double-sided rectangle in the local XZ plane (normal +Y), UV 0..1.</summary>
        public static Mesh PlaneMesh(float width, float depth)
        {
            float hx = width * 0.5f, hz = depth * 0.5f;
            var m = new Mesh { name = "NibBallPlane" };
            m.vertices = new[] { new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            return m;
        }
    }

    /// <summary>Turns a label to face the player around the vertical axis only.</summary>
    public sealed class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var d = transform.position - cam.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d, Vector3.up);
        }
    }
}
