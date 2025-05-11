using UnityEngine;

namespace PianoRoll
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        public Camera Cam => cam;

        private Vector2 mouseDownPos;

        public void ResetPosition()
        {
            cam.transform.position = new Vector3(0, 0, cam.transform.position.z);
        }

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
            var rect = noteEditor.ViewRectWorld();
            var offset = (Vector2)cam.transform.position - rect.min;
            var maxYWorld = noteEditor.PianoToWorldCoords(new(0, noteEditor.stepsList.Count)).y;

            pos.x = Mathf.Max(pos.x, offset.x);
            pos.y = Mathf.Min(pos.y, maxYWorld - rect.height + offset.y);
            pos.y = Mathf.Max(pos.y, offset.y);
            cam.transform.position = pos;
        }
    }
}
