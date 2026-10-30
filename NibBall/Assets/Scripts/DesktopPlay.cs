using UnityEngine;

namespace NibBall
{
    /// <summary>Editor/desktop stand-in for the headset: hold the right mouse button to look, click to throw along the cursor, Space to re-fit the arena.</summary>
    public sealed class DesktopPlay : MonoBehaviour
    {
        public Camera cam;
        public System.Action ReplaceRequested;
        float yaw, pitch;

        void Update()
        {
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * 3f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, -80f, 80f);
                cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            if (Input.GetMouseButtonDown(0))
            {
                var ray = cam.ScreenPointToRay(Input.mousePosition);
                var b = Ball.Create(ray.origin + ray.direction * 0.4f);
                b.Release(ray.direction * 7f + Vector3.up * 1.5f);
            }
            if (Input.GetKeyDown(KeyCode.Space)) ReplaceRequested?.Invoke();
        }
    }
}
