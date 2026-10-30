using UnityEngine;

namespace NibBall
{
    /// <summary>
    /// The thrown ball. Four coloured segments, one per bit. It remembers its velocity from before each physics step,
    /// because by the time a collision is reported the solver has already bounced it.
    /// </summary>
    public sealed class Ball : MonoBehaviour
    {
        public const float Radius = 0.07f;
        public Rigidbody rb;
        public SphereCollider col;
        public bool held, scored;
        Vector3 preVelocity;
        float age;
        static Material mat;
        static PhysicsMaterial phys;

        public static Ball Create(Vector3 at)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "NibBall";
            go.layer = Look.BallLayer;
            go.transform.position = at;
            go.transform.localScale = Vector3.one * Radius * 2f;
            if (mat == null)
            {
                var tex = new Texture2D(4, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                tex.SetPixels(new[] { new Color(1f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.75f, 1f), new Color(1f, 0.35f, 0.7f) });
                tex.Apply();
                mat = Look.Lit_(Color.white);
                mat.mainTexture = tex;
                phys = new PhysicsMaterial("NibBall") { bounciness = 0.92f, dynamicFriction = 0.25f, staticFriction = 0.25f, bounceCombine = PhysicsMaterialCombine.Maximum, frictionCombine = PhysicsMaterialCombine.Minimum };
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var b = go.AddComponent<Ball>();
            b.col = go.GetComponent<SphereCollider>();
            b.col.sharedMaterial = phys;
            b.rb = go.AddComponent<Rigidbody>();
            b.rb.mass = 0.15f;
            b.rb.linearDamping = 0.05f;
            b.rb.angularDamping = 0.5f;
            b.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            b.rb.interpolation = RigidbodyInterpolation.Interpolate;
            return b;
        }

        public void Hold() { held = true; rb.isKinematic = true; col.enabled = false; }

        public void Release(Vector3 velocity)
        {
            held = false;
            transform.SetParent(null, true);
            col.enabled = true;
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            preVelocity = velocity;
        }

        void FixedUpdate() { if (!held) preVelocity = rb.linearVelocity; }

        void Update()
        {
            age += Time.deltaTime;
            if (held) return;
            if (transform.position.y < -3f || transform.position.magnitude > 40f || age > 90f) Destroy(gameObject);
        }

        System.Collections.IEnumerator IgnoreBriefly(Collider other)
        {
            Physics.IgnoreCollision(col, other, true);
            yield return new WaitForSeconds(0.6f);
            if (other != null) Physics.IgnoreCollision(col, other, false);
        }

        void OnCollisionEnter(Collision c)
        {
            if (held) return;
            var contact = c.GetContact(0);
            var surface = c.collider.GetComponent<RoomSurface>();
            bool viaMesh = false;
            if (surface == null && c.collider.GetComponent<RoomMeshChunk>() != null) 
            {
                surface = RoomSurface.Near(contact.point);
                if (surface == null && Mathf.Abs(contact.normal.y) < 0.6f && Mathf.Abs(Vector3.Dot(preVelocity, contact.normal)) >= 3f) surface = RoomSurface.Patch(contact.point, contact.normal);
                viaMesh = surface != null;
            }
            float speed = preVelocity.magnitude;
            if (surface != null && surface.Impact(contact.point, preVelocity, this))
            {
                // through it: keep going, and ignore everything coplanar at this spot (wall behind a window, say)
                RoomSurface.IgnoreStackAt(this, contact.point, surface);
                rb.linearVelocity = preVelocity * 0.7f;
                if (viaMesh) StartCoroutine(IgnoreBriefly(c.collider));
                return;
            }
            if (speed > 0.8f) Sfx.Thud(contact.point, speed);
        }
    }
}
