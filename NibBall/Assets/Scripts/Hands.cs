using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace NibBall
{
    /// <summary>
    /// Throwing, by controller or by bare hand. Controller: squeeze the trigger to pick a ball out of the air, let go to throw.
    /// Hand tracking: pinch thumb and finger, let go to throw. Either way the throw velocity is the hand's own motion just
    /// before the release. Controller X, or a left fist held for a moment, re-fits the arena; Y, or a right fist, shows / hides the scan.
    /// </summary>
    public sealed class Hands : MonoBehaviour
    {
        public Transform trackingSpace;
        public System.Action ReplaceRequested;
        const int MaxBalls = 10;
        const float PinchOn = 0.025f, PinchOff = 0.04f, FistOn = 0.065f, FistOff = 0.09f, FistHold = 1.2f;

        sealed class Hand
        {
            public XRNode node;
            public Ball held;
            public readonly List<(float t, Vector3 p)> trail = new List<(float, Vector3)>();
            public bool wasDown, usingHand, fist;
            public float fistSince = -1f;
        }

        readonly Hand[] hands = { new Hand { node = XRNode.RightHand }, new Hand { node = XRNode.LeftHand } };
        readonly Queue<Ball> alive = new Queue<Ball>();
        readonly List<XRHandSubsystem> handSystems = new List<XRHandSubsystem>();
        XRHandSubsystem hs;
        bool xWas, yWas, aWas;

        /// <summary>What the input is doing right now, for the on-screen readout.</summary>
        public string Mode { get; private set; } = "no hands or controllers yet";

        void Update()
        {
            if (hs == null || !hs.running)
            {
                handSystems.Clear(); SubsystemManager.GetSubsystems(handSystems);
                hs = handSystems.Count > 0 ? handSystems[0] : null;
            }
            foreach (var h in hands) Tick(h);
            Mode = hands[0].usingHand || hands[1].usingHand ? "bare hands: pinch to pick up, open to throw"
                 : hands[0].held != null || hands[1].held != null || ControllerOn(XRNode.RightHand) || ControllerOn(XRNode.LeftHand) ? "controllers: squeeze to pick up, release to throw"
                 : hs != null ? "show me your hands" : "no hand tracking: turn it on in Quest Settings > Movement tracking";

            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool x = left.isValid && left.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed) && pressed;
            if (x && !xWas) ReplaceRequested?.Invoke();
            xWas = x;

            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool a = right.isValid && right.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed) && aPressed;
            if (a && !aWas) Toys.Next();
            aWas = a;

            bool y = left.isValid && left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool sec) && sec;
            if (y && !yWas) RoomMesh.Instance?.Toggle();
            yWas = y;

            // no buttons on bare hands: hold a fist (left = move the game, right = show / hide the scan)
            FistAction(hands[1], () => ReplaceRequested?.Invoke());
            FistAction(hands[0], () => RoomMesh.Instance?.Toggle());
        }

        void FistAction(Hand h, System.Action act)
        {
            if (h.usingHand && h.fist && h.held == null)
            {
                if (h.fistSince < 0f) h.fistSince = Time.time;
                else if (Time.time - h.fistSince > FistHold) { act(); h.fistSince = float.MaxValue; }
            }
            else if (!h.fist) h.fistSince = -1f;
        }

        static bool ControllerOn(XRNode node)
        {
            var d = InputDevices.GetDeviceAtXRNode(node);
            return d.isValid && (d.characteristics & InputDeviceCharacteristics.Controller) != 0
                && d.TryGetFeatureValue(CommonUsages.devicePosition, out _) && (!d.TryGetFeatureValue(CommonUsages.isTracked, out bool t) || t);
        }

        void Tick(Hand h)
        {
            Vector3 pos; Quaternion rot; Vector3 grip; Vector3 track; bool down;

            XRHand xh = hs != null ? (h.node == XRNode.LeftHand ? hs.leftHand : hs.rightHand) : default;
            bool handOn = hs != null && xh.isTracked && !ControllerOn(h.node);
            h.usingHand = handOn;

            if (handOn)
            {
                if (!xh.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var tip) || !xh.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumb)
                    || !xh.GetJoint(XRHandJointID.Palm).TryGetPose(out var palm)) { Lost(h); return; }
                float gap = Vector3.Distance(tip.position, thumb.position);
                down = h.wasDown ? gap < PinchOff : gap < PinchOn;
                grip = trackingSpace.TransformPoint((tip.position + thumb.position) * 0.5f);   // the pinch point: the ball sits between the fingertips
                track = trackingSpace.TransformPoint(palm.position);                           // the palm carries the throw; fingertips flick open at release
                rot = trackingSpace.rotation * palm.rotation;
                h.fist = IsFist(xh, palm.position, h.fist);
            }
            else
            {
                var dev = InputDevices.GetDeviceAtXRNode(h.node);
                if (!dev.isValid || !dev.TryGetFeatureValue(CommonUsages.devicePosition, out var lp)) { Lost(h); return; }
                dev.TryGetFeatureValue(CommonUsages.deviceRotation, out var lr);
                pos = trackingSpace.TransformPoint(lp);
                rot = trackingSpace.rotation * lr;
                grip = pos + rot * new Vector3(0f, 0.02f, 0.1f);
                track = pos;
                dev.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
                down = trigger > 0.6f;
                h.fist = false;
            }

            h.trail.Add((Time.time, track));
            while (h.trail.Count > 2 && Time.time - h.trail[0].t > 0.25f) h.trail.RemoveAt(0);

            if (down && !h.wasDown && h.held == null) Pick(h, grip);
            if (h.held != null)
            {
                if (down) h.held.transform.SetPositionAndRotation(grip, rot);
                else Throw(h, handOn);
            }
            h.wasDown = down;
        }

        // the middle, ring and little fingertips curled in to the palm
        static bool IsFist(XRHand xh, Vector3 palm, bool was)
        {
            float sum = 0f; int n = 0;
            foreach (var id in new[] { XRHandJointID.MiddleTip, XRHandJointID.RingTip, XRHandJointID.LittleTip })
                if (xh.GetJoint(id).TryGetPose(out var p)) { sum += Vector3.Distance(p.position, palm); n++; }
            if (n < 3) return false;
            float d = sum / n;
            return was ? d < FistOff : d < FistOn;
        }

        // tracking lost mid-hold: throw nothing, just drop the ball where it is
        void Lost(Hand h)
        {
            if (h.held != null) { h.held.Release(Vector3.zero); h.held = null; }
            h.wasDown = false; h.fist = false; h.trail.Clear();
        }

        void Pick(Hand h, Vector3 at)
        {
            var b = Ball.Create(at, RackItem.Choose(at));
            b.Hold();
            h.held = b;
            alive.Enqueue(b);
            while (alive.Count > MaxBalls) { var old = alive.Dequeue(); if (old != null && !old.held) Destroy(old.gameObject); }
        }

        void Throw(Hand h, bool bareHand)
        {
            // a hand's own motion over a short window that ends just before the release, so opening the fingers doesn't skew the throw
            var v = Vector3.zero;
            float end = Time.time - (bareHand ? 0.05f : 0f);
            float start = end - 0.1f;
            int i0 = -1, i1 = -1;
            for (int i = 0; i < h.trail.Count; i++)
            {
                if (h.trail[i].t >= start && i0 < 0) i0 = i;
                if (h.trail[i].t <= end) i1 = i;
            }
            if (i0 >= 0 && i1 > i0)
            {
                float dt = h.trail[i1].t - h.trail[i0].t;
                if (dt > 0.03f) v = (h.trail[i1].p - h.trail[i0].p) / dt;
            }
            v = Vector3.ClampMagnitude(v * (bareHand ? 1.35f : 1.1f), 12f);
            h.held.Release(v);
            h.held = null;
        }
    }
}
