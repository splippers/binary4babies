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
        public Collider col;
        public Toy kind;
        public bool held, scored;
        Vector3 preVelocity;
        float age;
        static readonly System.Collections.Generic.Dictionary<Toy, Material> mats = new System.Collections.Generic.Dictionary<Toy, Material>();
        static readonly System.Collections.Generic.Dictionary<Toy, PhysicsMaterial> physics = new System.Collections.Generic.Dictionary<Toy, PhysicsMaterial>();

        public struct ToySpec { public float radius, mass, bounce, drag, spinDrag; public PhysicsMaterialCombine combine; }

        /// <summary>How each toy flies: size, weight, springiness and air drag. The beach ball floats, the ping pong ball is all spring, the banana thuds.</summary>
        public static ToySpec Spec(Toy k)
        {
            var max = PhysicsMaterialCombine.Maximum;
            switch (k)
            {
                case Toy.PingPong: return new ToySpec { radius = 0.02f, mass = 0.0027f, bounce = 0.96f, drag = 0.3f, spinDrag = 0.1f, combine = max };
                case Toy.Tennis: return new ToySpec { radius = 0.033f, mass = 0.058f, bounce = 0.8f, drag = 0.08f, spinDrag = 0.4f, combine = max };
                case Toy.Banana: return new ToySpec { radius = 0.07f, mass = 0.12f, bounce = 0.25f, drag = 0.05f, spinDrag = 0.15f, combine = PhysicsMaterialCombine.Average };
                case Toy.BeachBall: return new ToySpec { radius = 0.13f, mass = 0.05f, bounce = 0.85f, drag = 1.2f, spinDrag = 0.8f, combine = max };
                default: return new ToySpec { radius = Radius, mass = 0.15f, bounce = 0.92f, drag = 0.05f, spinDrag = 0.5f, combine = max };
            }
        }

        static Material Mat(Toy k)
        {
            if (mats.TryGetValue(k, out var m)) return m;
            switch (k)
            {
                case Toy.PingPong: m = Look.Lit_(new Color(1f, 0.97f, 0.9f)); break;
                case Toy.Tennis: m = Look.Lit_(new Color(0.8f, 0.95f, 0.2f)); break;
                case Toy.BeachBall: m = Look.Lit_(Color.white); m.mainTexture = Stripes(new Color(0.95f, 0.2f, 0.2f), Color.white, new Color(0.2f, 0.45f, 0.95f), new Color(1f, 0.85f, 0.1f)); break;
                default: m = Look.Lit_(Color.white); m.mainTexture = Stripes(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.75f, 1f), new Color(1f, 0.35f, 0.7f)); break;
            }
            return mats[k] = m;
        }

        static Texture2D Stripes(params Color[] c)
        {
            var tex = new Texture2D(c.Length, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            tex.SetPixels(c);
            tex.Apply();
            return tex;
        }

        static PhysicsMaterial Phys(Toy k)
        {
            if (physics.TryGetValue(k, out var p)) return p;
            var sp = Spec(k);
            return physics[k] = new PhysicsMaterial("Toy " + k) { bounciness = sp.bounce, dynamicFriction = 0.25f, staticFriction = 0.25f, bounceCombine = sp.combine, frictionCombine = PhysicsMaterialCombine.Minimum };
        }

        /// <summary>The toy's body: shape, looks and a collider, but no physics. The rack shows these; Create adds a Rigidbody.</summary>
        public static GameObject Body(Toy k)
        {
            if (k == Toy.Banana) return BananaBody();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * Spec(k).radius * 2f;
            go.GetComponent<Renderer>().sharedMaterial = Mat(k);
            return go;
        }

        // five capsules along a gentle arc, lying along local X, brown at the stalk and the tip
        static GameObject BananaBody()
        {
            var root = new GameObject("Banana");
            var fruit = Look.Lit_(new Color(1f, 0.88f, 0.15f));
            var tip = Look.Lit_(new Color(0.35f, 0.25f, 0.1f));
            const int segs = 5; const float arc = 70f, R = 0.16f;
            for (int i = 0; i < segs; i++)
            {
                float a = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, i / (segs - 1f));
                var seg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.Destroy(seg.GetComponent<Collider>());
                seg.transform.SetParent(root.transform, false);
                seg.transform.localPosition = new Vector3(R * Mathf.Sin(a * Mathf.Deg2Rad), -R * (1f - Mathf.Cos(a * Mathf.Deg2Rad)) + 0.02f, 0f);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, -90f - a);
                bool end = i == 0 || i == segs - 1;
                seg.transform.localScale = end ? new Vector3(0.035f, 0.03f, 0.035f) : new Vector3(0.056f, 0.032f, 0.056f);
                seg.GetComponent<Renderer>().sharedMaterial = end ? tip : fruit;
            }
            var cc = root.AddComponent<CapsuleCollider>();
            cc.direction = 0; cc.radius = 0.03f; cc.height = 0.22f; cc.center = new Vector3(0f, -0.005f, 0f);
            return root;
        }

        public static Ball Create(Vector3 at, Toy kind = Toy.NibBall)
        {
            var sp = Spec(kind);
            var go = Body(kind);
            go.name = kind == Toy.NibBall ? "NibBall" : kind.ToString();
            go.layer = Look.BallLayer;
            go.transform.position = at;
            var b = go.AddComponent<Ball>();
            b.kind = kind;
            b.col = go.GetComponent<Collider>();
            b.col.sharedMaterial = Phys(kind);
            b.rb = go.AddComponent<Rigidbody>();
            b.rb.mass = sp.mass;
            b.rb.linearDamping = sp.drag;
            b.rb.angularDamping = sp.spinDrag;
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
