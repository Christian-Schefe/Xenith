using UnityEngine;

namespace PianoRoll
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        public Camera Cam => cam;

        private Vector2 mouseDownPos;

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                mouseDownPos = cam.ScreenToWorldPoint(Input.mousePosition);
            }
            var pos = cam.transform.position;
            var size = cam.orthographicSize;
            if (Input.GetMouseButton(1))
            {
                Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 delta = mousePos - mouseDownPos;

                pos -= (Vector3)delta;
            }

            pos.x = Mathf.Max(pos.x, size * cam.aspect);
            pos.y = Mathf.Max(pos.y, size);
            cam.transform.position = pos;
        }
    }
}
