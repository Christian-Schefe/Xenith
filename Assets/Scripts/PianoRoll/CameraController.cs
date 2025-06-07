using UnityEngine;

namespace PianoRoll
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        public Camera Cam => cam;

        private Vector2? mouseDownPos;

        private Vector2 pianoPos;

        public void SetCenterXPosition(float pianoPosX)
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var viewSize = noteEditor.ViewRectPiano().size;
            pianoPos = new(Mathf.Max(0, pianoPosX - viewSize.x / 2f), pianoPos.y);
            Update();
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            if (Input.GetMouseButtonDown(1))
            {
                var viewRectScreen = noteEditor.ViewRectScreen();
                if (viewRectScreen.Contains(Input.mousePosition))
                {
                    mouseDownPos = noteEditor.ScreenToPianoCoords(Input.mousePosition);
                }
            }
            if (!Input.GetMouseButton(1))
            {
                mouseDownPos = null;
            }
            if (Input.GetMouseButton(1) && mouseDownPos.HasValue)
            {
                Vector2 mousePos = noteEditor.ScreenToPianoCoords(Input.mousePosition);
                Vector2 delta = mousePos - mouseDownPos.Value;

                pianoPos -= delta;
            }
            pianoPos = Vector2.Max(pianoPos, Vector2.zero);
            var viewRect = noteEditor.ViewRectWorld();
            var pianoYSize = noteEditor.ViewRectPiano().size.y;
            int maxY = noteEditor.stepsList.Count;
            if (pianoPos.y + pianoYSize > maxY)
            {
                pianoPos.y = Mathf.Max(0, maxY - pianoYSize);
            }
            var worldPos = noteEditor.PianoToWorldCoords(pianoPos);

            var posDiff = worldPos - viewRect.min;
            cam.transform.position += new Vector3(posDiff.x, posDiff.y);
        }
    }
}
