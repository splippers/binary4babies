using System;
using UnityEngine;

namespace NibBall
{
    /// <summary>
    /// Stock completion tests for any Quest 3 AR game. Each needs only what is named: RoomSurface / RoomMesh for the scan ones, a
    /// Hands for the input ones, PanelButton for pokes. All have a timeout in the step, so a missing scan or no hand tracking
    /// never traps the player.
    /// </summary>
    public sealed class StockSteps
    {
        readonly Hands hands;
        bool grabbed, threw, poked;
        float controllerSeen = -1f, handSeen = -1f;

        public StockSteps(Hands hands)
        {
            this.hands = hands;
            hands.Grabbed += _ => grabbed = true;
            hands.Threw += (_, speed) => { if (speed > 1.5f) threw = true; };
            PanelButton.AnyPressed += _ => poked = true;
        }

        /// <summary>Needs: a RoomSurface floor from the plane scan. The floor under the player is also known from the tracking origin, so this passes after a moment anyway.</summary>
        public Func<bool> FloorFound() => () => { foreach (var s in RoomSurface.All) if (s.kind == SurfaceKind.Floor) return true; return Time.time > 6f; };

        /// <summary>Needs: RoomScanner (walls) or RoomMesh (triangles). True when the headset has handed over a room worth playing in.</summary>
        public Func<bool> RoomScanned() => () => RoomSurface.All.Count >= 3 || (RoomMesh.Instance != null && RoomMesh.Instance.Triangles > 1500);

        /// <summary>Needs: Hands. True once the player has moved a controller about (not just set it down) or shown a tracked hand.</summary>
        public Func<bool> InputSeen() => () => hands.ControllerActive || hands.UsingHands;

        /// <summary>Needs: Hands with its Grabbed event.</summary>
        public Func<bool> Grab() => () => grabbed;
        public void ResetGrab() => grabbed = false;

        /// <summary>Needs: Hands with its Threw event. Speed over 1.5 m/s counts as a throw rather than a drop.</summary>
        public Func<bool> Throw() => () => threw;
        public void ResetThrow() => threw = false;

        /// <summary>Needs: PanelButton. Any button pressed by touch.</summary>
        public Func<bool> Poke() => () => poked;
        public void ResetPoke() => poked = false;
    }
}
