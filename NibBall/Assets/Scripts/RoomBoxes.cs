using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace NibBall
{
    /// <summary>A piece of furniture the headset boxed in Room Setup. Solid for balls, and a place a target must not be buried in.</summary>
    public sealed class RoomBox : MonoBehaviour
    {
        public static readonly List<RoomBox> All = new List<RoomBox>();
        public string label;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
    }

    /// <summary>Gives each scanned bounding box (couch, table, TV...) a collider, so thrown toys bounce off furniture and targets keep clear of it.</summary>
    public sealed class RoomBoxes : MonoBehaviour
    {
        public ARBoundingBoxManager boxes;

        void Start()
        {
            if (boxes == null) return;
            boxes.trackablesChanged.AddListener(OnChanged);
            foreach (var b in boxes.trackables) Fit(b);
        }
        void OnDestroy() { if (boxes != null) boxes.trackablesChanged.RemoveListener(OnChanged); }

        void OnChanged(ARTrackablesChangedEventArgs<ARBoundingBox> args)
        {
            foreach (var b in args.added) { Fit(b); Debug.Log($"NibBall: box {b.trackableId} {b.classifications} size {b.size:0.00}"); }
            foreach (var b in args.updated) Fit(b);
        }

        static void Fit(ARBoundingBox b)
        {
            var box = b.GetComponent<BoxCollider>();
            if (box == null) { box = b.gameObject.AddComponent<BoxCollider>(); b.gameObject.AddComponent<RoomBox>().label = b.classifications.ToString(); }
            box.size = b.size;
        }
    }
}
