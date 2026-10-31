using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace NibBall
{
    /// <summary>
    /// Builds everything at run time (the scene is empty bar this object): the tracked rig with passthrough, the room scan,
    /// and the arena fitted into whatever room it finds. On a desktop it uses a made-up room and the mouse instead.
    /// </summary>
    public sealed class NibBallApp : MonoBehaviour
    {
        public Arena Arena { get; private set; }
        public Camera Cam { get; private set; }
        Transform trackingSpace;
        TextMesh status;
        bool inHeadset;
        string permissionState = "n/a";
        const string ScenePermission = "com.oculus.permission.USE_SCENE";
        ARPlaneManager planeManager;

        public static bool SelfTestRequested
        {
            get { foreach (var a in Environment.GetCommandLineArgs()) if (a == "-nibball-selftest") return true; return false; }
        }

        IEnumerator Start()
        {
            Look.SetupPhysics();
            BuildRig();
            var displays = new System.Collections.Generic.List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            inHeadset = displays.Count > 0 && displays[0].running;
            AddBaseFloor();

            if (inHeadset)
            {
                yield return StartPassthroughAndScan();
            }
            else
            {
                SyntheticRoom.Build(visible: true);
                var d = gameObject.AddComponent<DesktopPlay>();
                d.cam = Cam; d.ReplaceRequested = Place;
                Cam.transform.position = new Vector3(0f, 1.6f, 0f);
                Cam.transform.rotation = Quaternion.identity;
                yield return null;
                Place();
                if (SelfTestRequested) gameObject.AddComponent<SelfTest>().app = this;
            }
        }

        void BuildRig()
        {
            var originGo = new GameObject("XR Origin");
            var origin = originGo.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originGo.transform, false);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(offset.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);   // transparent: passthrough shows through
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<HeadTracker>();
            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            Cam = cam;
            trackingSpace = offset.transform;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            status = Look.Label(camGo.transform, "Status", "", 0.05f, new Color(1f, 0.9f, 0.4f), new Vector3(0f, -0.18f, 1.2f));
        }

        /// <summary>An invisible floor at the tracking origin, so a ball never falls through a part of the floor the scan missed.</summary>
        void AddBaseFloor()
        {
            var floor = new GameObject("Base Floor").AddComponent<BoxCollider>();
            floor.size = new Vector3(40f, 0.2f, 40f);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
        }

        IEnumerator StartPassthroughAndScan()
        {
            Show("Starting up...");
            new GameObject("AR Session").AddComponent<ARSession>();
            Cam.gameObject.AddComponent<ARCameraManager>();
            // no ARCameraBackground: Meta's passthrough is a compositor layer, and that component would paint an opaque background over it

#if UNITY_ANDROID && !UNITY_EDITOR
            // "Spatial data" is a run-time permission on Quest. Ask again every few seconds until it is granted, and say so on screen.
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += _ => permissionState = "DENIED";
            callbacks.PermissionGranted += _ => permissionState = "granted";
            callbacks.PermissionDeniedAndDontAskAgain += _ => permissionState = "DENIED for good (Quest Settings > Apps > NibBall > Permissions > Spatial data)";
            float waited = 0f, nextAsk = 0f;
            while (!Permission.HasUserAuthorizedPermission(ScenePermission) && waited < 45f)
            {
                if (waited >= nextAsk)
                {
                    permissionState = "asking";
                    Show("Please tap Allow on the Spatial Data prompt so the game can see your room");
                    Permission.RequestUserPermission(ScenePermission, callbacks);
                    nextAsk = waited + 8f;
                }
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (Permission.HasUserAuthorizedPermission(ScenePermission)) permissionState = "granted";
            Debug.Log("NibBall: spatial data permission " + permissionState);
#endif
            var origin = FindAnyObjectByType<XROrigin>();
            var pm = origin.gameObject.AddComponent<ARPlaneManager>();
            planeManager = pm;
            pm.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;
            var scanner = origin.gameObject.AddComponent<RoomScanner>();
            scanner.planes = pm;

            RoomMesh.Create(origin.TrackablesParent);

            var hands = gameObject.AddComponent<Hands>();
            hands.trackingSpace = trackingSpace;
            hands.ReplaceRequested = Place;

            Show("Looking at your room...  (Y shows / hides the scan)");
            float t = 0f, sinceFirstWall = -1f;
            while (t < 12f)
            {
                t += Time.deltaTime;
                if (scanner.WallCount > 0 && sinceFirstWall < 0f) sinceFirstWall = 0f;
                if (sinceFirstWall >= 0f) { sinceFirstWall += Time.deltaTime; if (sinceFirstWall > 2.5f) break; }
                yield return null;
            }
            if (scanner.WallCount == 0) Show("No room scan found. Run Room Setup in Quest settings. Playing in open space.");
            Place();
            StartCoroutine(ScanReadout(scanner, hands));
        }

        /// <summary>For the first minute, says what the room scan found, so a missing scan is obvious from inside the headset.</summary>
        IEnumerator ScanReadout(RoomScanner scanner, Hands hands)
        {
            bool restarted = false;
            for (float t = 0f; t < 90f; t += 1f)
            {
                int chunks = RoomMesh.Instance != null ? RoomMesh.Instance.ChunkCount : 0;
                int tris = RoomMesh.Instance != null ? RoomMesh.Instance.Triangles : 0;
#if UNITY_ANDROID && !UNITY_EDITOR
                // the permission arrived after the scanners started: restart them so they pick the room up
                if (!restarted && permissionState != "granted" && Permission.HasUserAuthorizedPermission(ScenePermission))
                {
                    restarted = true; permissionState = "granted";
                    if (planeManager != null) { planeManager.enabled = false; planeManager.enabled = true; }
                    if (RoomMesh.Instance != null && RoomMesh.Instance.manager != null) { RoomMesh.Instance.manager.enabled = false; RoomMesh.Instance.manager.enabled = true; }
                }
#endif
                Show($"Spatial data: {permissionState}\nRoom scan: {RoomSurface.All.Count} surfaces, {scanner.WallCount} walls, {chunks} mesh pieces, {tris} triangles\n{hands.Mode}");
                if (t == 20f) Debug.Log($"NibBall: scan permission {permissionState}, {RoomSurface.All.Count} surfaces, {scanner.WallCount} walls, {chunks} mesh chunks, {tris} triangles; input: {hands.Mode}");
                yield return new WaitForSeconds(1f);
            }
            Show("");
        }

        void Show(string text) { if (status != null) status.text = text; }

        /// <summary>(Re)fits the arena to the room as scanned now, from where the player stands and looks.</summary>
        public void Place()
        {
            var surfaces = RoomScanner.Snapshot();
            float floorY = RoomScanner.FloorY();
            var layout = ArenaPlacer.Solve(surfaces, Cam.transform.position, Cam.transform.forward, floorY);
            int score = 0;
            if (Arena != null) { score = Arena.score; Destroy(Arena.gameObject); }
            Arena = Arena.Build(layout, surfaces);
            Arena.score = score;
            Arena.SetHint(layout.againstWall
                ? "Stand on the ring.  Pinch a toy off the rack, swing, let go.  Bullseyes around the room flip bits too"
                : "No wall to fit against, so the bins stand in open space.  Bullseyes around the room flip bits too");
            Debug.Log($"NibBall: arena at {layout.baseCenter} facing {layout.forward}, bins {layout.binWidth:0.00} m, throw {layout.throwDistance:0.0} m, wall {layout.againstWall}, room depth {layout.depthAvailable:0.0} m");
        }
    }

    /// <summary>Drives the camera from the headset pose, as the SIC project does.</summary>
    public sealed class HeadTracker : MonoBehaviour
    {
        void OnEnable() { Application.onBeforeRender += Apply; }
        void OnDisable() { Application.onBeforeRender -= Apply; }
        void Update() => Apply();

        void Apply()
        {
            var dev = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!dev.isValid) dev = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!dev.isValid) return;
            if (dev.TryGetFeatureValue(CommonUsages.centerEyePosition, out var p) || dev.TryGetFeatureValue(CommonUsages.devicePosition, out p)) transform.localPosition = p;
            if (dev.TryGetFeatureValue(CommonUsages.centerEyeRotation, out var r) || dev.TryGetFeatureValue(CommonUsages.deviceRotation, out r)) transform.localRotation = r;
        }
    }
}
