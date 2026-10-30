using UnityEngine;

namespace NibBall
{
    /// <summary>Shards, plaster and confetti: small cubes that fly, bounce off the room, then shrink away.</summary>
    public sealed class Debris : MonoBehaviour
    {
        float life, age;
        Vector3 start;

        public static void Burst(Vector3 at, Vector3 normal, Color color, int count, float speed, float minSize, float maxSize, float life = 3.5f, float spread = 0.5f)
        {
            var mat = Look.Glow(color);
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Debris";
                go.layer = Look.DebrisLayer;
                float s = Random.Range(minSize, maxSize);
                go.transform.position = at + Random.insideUnitSphere * spread * 0.2f;
                go.transform.rotation = Random.rotation;
                go.transform.localScale = new Vector3(s, s * Random.Range(0.15f, 1f), s * Random.Range(0.5f, 1f));
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.02f;
                rb.linearVelocity = (normal + Random.insideUnitSphere * spread * 2f).normalized * speed * Random.Range(0.4f, 1f);
                rb.angularVelocity = Random.insideUnitSphere * 12f;
                var d = go.AddComponent<Debris>();
                d.life = life * Random.Range(0.7f, 1.2f);
                d.start = go.transform.localScale;
            }
        }

        /// <summary>Confetti that rises first and then falls, for a correct answer.</summary>
        public static void Confetti(Vector3 at, int count)
        {
            Color[] palette = { new Color(1f, 0.82f, 0.1f), new Color(0.3f, 0.9f, 0.5f), new Color(0.3f, 0.7f, 1f), new Color(1f, 0.4f, 0.7f) };
            for (int i = 0; i < 4; i++) Burst(at, Vector3.up, palette[i], count / 4, 3.5f, 0.02f, 0.045f, 3.5f, 1.2f);
        }

        void Update()
        {
            age += Time.deltaTime;
            float fade = Mathf.Clamp01((life - age) / 0.6f);
            transform.localScale = start * fade;
            if (age >= life || transform.position.y < -3f) Destroy(gameObject);
        }
    }
}
