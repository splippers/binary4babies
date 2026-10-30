# CLAUDE.md — NibBall (Binary4Babies)

Unity 6000.4.5f1, Built-in RP, OpenXR + Meta OpenXR (AR Foundation 6.6), Quest 3 passthrough. The scene is empty bar one
`NibBallApp` object; everything else is built at run time (same approach as ~/sic-auditorium and boredoom).

| Topic | File |
|---|---|
| Rig, passthrough, scan, placement | `Assets/Scripts/NibBallApp.cs` |
| Plane -> surface, classification | `Assets/Scripts/RoomScanner.cs` |
| Where the bins go | `Assets/Scripts/ArenaPlacer.cs` (pure maths, unit-checked) |
| Bins, rules, board | `Assets/Scripts/Arena.cs`, `Nibble.cs` |
| Breaking windows/walls | `Assets/Scripts/RoomSurface.cs`, `Debris.cs`, `Ball.cs` |
| Controllers | `Assets/Scripts/Hands.cs` (legacy `InputDevices`; project uses the old input handler) |
| Self-test (desktop player) | `Assets/Scripts/SelfTest.cs`, run with `-nibball-selftest` |
| Build, OpenXR features, shaders | `Assets/Editor/NibBallBuild.cs` |

Gotchas
- Do not add `Sprites/Default` or `GUI/Text Shader` to the always-included shaders: the player build then fails writing `unity_builtin_extra`.
- Building under /tmp worked; the source tree is on gluster (slow), so copy to a local disk for Unity builds (rsync, exclude Library/Builds).
- Needs Room Setup done on the headset and the `com.oculus.permission.USE_SCENE` permission (requested at run time).
- Layers 8 "Ball" and 9 "Debris" are set by the build script; debris ignores balls and other debris.

## Headset gotchas (learned the hard way)
- Unity's Meta manifest processors do not run in our headless builds, so `Assets/Plugins/Android/NibBallRoom.androidlib/AndroidManifest.xml` declares everything by hand: USE_SCENE, USE_ANCHOR_API, PASSTHROUGH, BOUNDARYLESS_APP, and the `com.oculus.intent.category.VR` filter + `focusaware`. Without the VR category Quest opens the app as a flat 2D window (a "black square"). Check with `aapt2 dump xmltree --file AndroidManifest.xml Builds/NibBall.apk`.
- No `ARCameraBackground`: Meta passthrough is a compositor layer and that component paints it black.
- `ConfigurePlayer` rebuilds the always-included shader list from scratch. A stale entry in GraphicsSettings.asset makes every Android build fail with "Failed to write file ... unity_builtin_extra".
- Text uses `NibBall/Overlay` (Assets/Shaders) with `_AlphaOnly`; the built-in font shader is not in the build.
- Scene mesh (`RoomMesh`): MeshCollider per chunk. A hard hit on mesh with no scanned wall makes a breakable `RoomSurface.Patch`.
- Debugging on the headset: `adb shell am broadcast -a com.oculus.vrpowermanager.prox_close` fakes "worn" (undo: `automation_disable`), but the system "Finding position" dialog blocks apps unless it can really track. `adb shell screencap` shows the headset view.
- Hands: `Hands.cs` uses controllers if tracked, else XR Hands (pinch = pick up, open = throw, palm velocity ending 50 ms before release). Needs package `com.unity.xr.hands`, OpenXR feature `input.handtracking`, and the HAND_TRACKING permission + `oculus.software.handtracking` (required=false) in the androidlib manifest. Bare-hand gestures: left fist held 1.2 s re-fits the arena, right fist toggles the scan. Quest Settings > Movement tracking > Hand tracking must be on.
- Spatial data: `NibBallApp` re-asks for USE_SCENE every 8 s and shows `Spatial data: <state>` in the readout; scanners restart if it is granted late. Build: `Unity -batchmode -nographics -quit -projectPath ~/.nibball-test -executeMethod NibBallBuild.BuildApk` (class has no namespace).
