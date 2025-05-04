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
            if (Input.GetMouseButton(1))
            {
                Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 delta = mousePos - mouseDownPos;

                pos -= (Vector3)delta;
            }

            var noteEditor = Globals<NoteEditor>.Instance;
            var rect = noteEditor.ViewRectScreen();
            var bottomLeftWorld = noteEditor.PianoToWorldCoords(noteEditor.ScreenToPianoCoords(rect.min));
            var offset = (Vector2)cam.transform.position - bottomLeftWorld;

            pos.x = Mathf.Max(pos.x, offset.x);
            pos.y = Mathf.Max(pos.y, offset.y);
            cam.transform.position = pos;
        }
    }
}
