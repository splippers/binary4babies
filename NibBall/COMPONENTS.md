# NibBall components and how proven they are on Quest 3

Candidates for the shared `com.craicken.core` (not moved there; that waits for Jon's go-ahead). "Proven" means seen working on the headset, not just on the desktop self-test.

| Component | File | State |
|---|---|---|
| Passthrough start, transparent camera, no ARCameraBackground | `NibBallApp.cs` | proven |
| Scene permission loop + `pm grant` / `user-set` trick | `NibBallApp.cs`, CLAUDE.md | proven |
| In-app room scan request (`TryRequestSceneCapture`) | `NibBallApp.RequestRoomScan` | proven (request accepted; mesh arrived after capture) |
| Scene mesh with colliders and a metre-grid look | `RoomMesh.cs`, `Shaders/NibBallScan.shader` | proven |
| Walls from the mesh when no planes arrive | `MeshWalls.cs` | desktop-tested only |
| Planes to breakable surfaces | `RoomScanner.cs`, `RoomSurface.cs` | was never subscribed (listener set up before the plane manager was assigned); fixed, untested on headset |
| Furniture boxes to colliders | `RoomBoxes.cs` | untested on headset |
| Sight-line shell test (rooms, doorways, "buried") | `Sight.cs` | desktop-tested only |
| Damage hidden unless in line of sight | `DamageVisibility` in `Sight.cs` | desktop-tested only |
| Throw by controller trigger/grip | `Hands.cs` | proven (trigger) |
| Bare-hand pinch throw (XR Hands) | `Hands.cs` | subsystem runs; hands never reported tracked yet |
| Idle controller yields to hands | `Hands.cs` | untested on headset |
| Poke buttons | `Panel.cs` | untested on headset |
| Tutorial runner + stock steps | `Tutorial/` | untested on headset |
| IL2CPP stripping guard (`link.xml`, Low stripping, KeepColliders) | `Assets/link.xml`, `Ball.cs` | proven (desktop never strips; this broke the device build once) |
| Hand-written Android manifest in an androidlib | `Plugins/Android/NibBallRoom.androidlib` | proven |

## Tutorial detectors: what each needs

`Assets/Scripts/Tutorial/` is game-neutral except `NibBallSteps.cs`.

- `TutorialRunner`: needs a Camera (head-locked card) and `PanelButton` (the SKIP button). Steps never trap the player: each has a timeout.
- `StockSteps.FloorFound`: a floor `RoomSurface`; passes after 6 s anyway.
- `StockSteps.RoomScanned`: `RoomSurface.All` (3+) or `RoomMesh.Instance.Triangles` (> 1500).
- `StockSteps.InputSeen`: `Hands.ControllerActive` / `Hands.UsingHands`.
- `StockSteps.Grab` / `Throw`: `Hands.Grabbed` / `Hands.Threw` events (throw = leaves faster than 1.5 m/s).
- `StockSteps.Poke`: `PanelButton.AnyPressed`.
- NibBall's own steps (bin a toy, move targets) are in `NibBallSteps.cs`.
