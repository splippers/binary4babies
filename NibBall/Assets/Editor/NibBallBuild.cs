using System.IO;
using NibBall;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;

public static class NibBallBuild
{
    const string ScenePath = "Assets/Scenes/NibBall.unity";

    // Meta Quest support, controllers, and the AR Foundation pieces the game uses: passthrough, session, planes.
    static readonly string[] Features =
    {
        "com.unity.openxr.feature.metaquest",
        "com.unity.openxr.feature.input.oculustouch",
        "com.unity.openxr.feature.input.metaquestplus",
        "com.unity.openxr.feature.input.metaquestpro",
        "MetaOpenXR-OpenXRLifeCycle",
        "com.unity.openxr.feature.arfoundation-meta-session",
        "com.unity.openxr.feature.arfoundation-meta-camera",
        "com.unity.openxr.feature.arfoundation-meta-plane",
        "com.unity.openxr.feature.arfoundation-meta-mesh",
        "com.unity.openxr.feature.input.handtracking",
    };

    [MenuItem("NibBall/Build Scene")]
    public static void BuildScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("NibBall").AddComponent<NibBallApp>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        ConfigurePlayer();
        AssetDatabase.SaveAssets();
        Debug.Log("NibBall: scene built at " + ScenePath);
    }

    static void EnsureFeatures()
    {
        bool changed = false;
        foreach (var id in Features)
        {
            var f = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, id);
            if (f == null) { Debug.LogWarning("NibBall: OpenXR feature not found: " + id); continue; }
            if (!f.enabled) { f.enabled = true; EditorUtility.SetDirty(f); changed = true; Debug.Log("NibBall: enabled OpenXR feature " + id); }
        }
        if (changed) AssetDatabase.SaveAssets();
    }

    static void EnsureLayers()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        void Set(int index, string name) { layers.GetArrayElementAtIndex(index).stringValue = name; }
        Set(8, "Ball");
        Set(9, "Debris");
        tm.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigurePlayer()
    {
        EnsureFeatures();
        EnsureLayers();
        PlayerSettings.companyName = "splippers";
        PlayerSettings.productName = "NibBall";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.splippers.nibball");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // every shader the game looks up by name has to be listed, or a player build leaves it out
        var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var always = gs.FindProperty("m_AlwaysIncludedShaders");
        always.arraySize = 0;   // start clean: a shader left over from an earlier try breaks the Android build
        foreach (var name in new[] { Look.Lit, Look.Unlit, Look.Overlay, Look.Scan })
        {
            var shader = Shader.Find(name);
            if (shader == null) { Debug.LogWarning("NibBall: shader not found: " + name); continue; }
            bool have = false;
            for (int i = 0; i < always.arraySize; i++) if (always.GetArrayElementAtIndex(i).objectReferenceValue == shader) have = true;
            if (!have) { always.arraySize++; always.GetArrayElementAtIndex(always.arraySize - 1).objectReferenceValue = shader; }
        }
        gs.ApplyModifiedPropertiesWithoutUndo();

        var qs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
        var levels = qs.FindProperty("m_QualitySettings");
        for (int i = 0; i < levels.arraySize; i++)
        {
            var lv = levels.GetArrayElementAtIndex(i);
            lv.FindPropertyRelative("antiAliasing").intValue = 4;
            lv.FindPropertyRelative("pixelLightCount").intValue = 2;
        }
        qs.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("NibBall/Build Quest APK")]
    public static void BuildApk()
    {
        BuildScene();
        EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);
        Directory.CreateDirectory("Builds");
        var rep = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/NibBall.apk", target = BuildTarget.Android, options = BuildOptions.None });
        Debug.Log("NibBall: APK build " + rep.summary.result + " -> " + rep.summary.outputPath);
        if (Application.isBatchMode) EditorApplication.Exit(rep.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>Desktop player for the self-test: run Builds/test/NibBallTest.x86_64 -batchmode -nographics -nibball-selftest -logFile -</summary>
    public static void BuildTestPlayer()
    {
        BuildScene();
        Directory.CreateDirectory("Builds/test");
        var rep = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = "Builds/test/NibBallTest.x86_64", target = BuildTarget.StandaloneLinux64, options = BuildOptions.None });
        Debug.Log("NibBall: test player build " + rep.summary.result);
        if (Application.isBatchMode) EditorApplication.Exit(rep.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>Placement-solver checks that need no scene: run with -executeMethod NibBallBuild.CheckPlacer.</summary>
    public static void CheckPlacer()
    {
        int fails = 0;
        void Check(bool ok, string what) { Debug.Log("PLACER " + (ok ? "ok   " : "FAIL ") + what); if (!ok) fails++; }
        Surface Wall(Vector3 c, Vector3 n, float w) => new Surface { kind = SurfaceKind.Wall, center = c, normal = n, right = Vector3.Cross(Vector3.up, n), size = new Vector2(w, 2.6f) };

        // 5 x 4 room, player in the middle: a wall on the long axis has 5 m in front of it
        var room = new System.Collections.Generic.List<Surface>
        {
            Wall(new Vector3(-2.5f, 1.3f, 0), Vector3.right, 4f), Wall(new Vector3(2.5f, 1.3f, 0), Vector3.left, 4f),
            Wall(new Vector3(0, 1.3f, -2f), Vector3.forward, 5f), Wall(new Vector3(0, 1.3f, 2f), Vector3.back, 5f),
        };
        var l = ArenaPlacer.Solve(room, new Vector3(0, 1.6f, 0), Vector3.forward, 0f);
        Check(l.againstWall && Mathf.Abs(l.forward.x) > 0.99f, "5x4 room: arena on a short wall, facing along the 5 m axis (forward " + l.forward + ")");
        Check(l.binWidth >= 0.3f && l.throwDistance >= 1.4f && l.throwDistance <= l.depthAvailable - 0.4f, "bin " + l.binWidth.ToString("0.00") + " m, throw " + l.throwDistance.ToString("0.0") + " m fit the room");

        // narrow cupboard of a room, 1.4 x 3: walls too narrow for four bins; the long walls (3 m) are fine but only 1.4 m deep
        var thin = new System.Collections.Generic.List<Surface>
        {
            Wall(new Vector3(-0.7f, 1.3f, 0), Vector3.right, 3f), Wall(new Vector3(0.7f, 1.3f, 0), Vector3.left, 3f),
            Wall(new Vector3(0, 1.3f, -1.5f), Vector3.forward, 1.4f), Wall(new Vector3(0, 1.3f, 1.5f), Vector3.back, 1.4f),
        };
        l = ArenaPlacer.Solve(thin, new Vector3(0, 1.6f, 0), Vector3.forward, 0f);
        Check(!l.againstWall, "tiny room: nothing fits, so the arena stands in free space");

        // narrow-ish wall: bins shrink
        var small = new System.Collections.Generic.List<Surface> { Wall(new Vector3(0, 1.3f, 3f), Vector3.back, 2.4f) };
        l = ArenaPlacer.Solve(small, new Vector3(0, 1.6f, 0), Vector3.forward, 0f);
        Check(l.againstWall && l.binWidth < 0.5f && l.binWidth >= 0.22f, "2.4 m wall: bins shrink to " + l.binWidth.ToString("0.00") + " m");

        var n = new Nibble();
        n.Toggle(0); n.Toggle(3);
        Check(n.Value == 9 && n.Binary == "1001", "nibble 8+1 = 9 = 1001");
        n.Set(6);
        Check(n.Binary == "0110" && !n.IsOn(0) && n.IsOn(1), "nibble.Set(6) = 0110");

        Debug.Log(fails == 0 ? "PLACER PASS" : "PLACER FAIL");
        EditorApplication.Exit(fails == 0 ? 0 : 1);
    }
}
