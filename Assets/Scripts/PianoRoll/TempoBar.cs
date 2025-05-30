using ReactiveData.App;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PianoRoll
{
    public class TempoBar : MonoBehaviour, IPointerDownHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                var newX = noteEditor.ScreenToPianoCoords(eventData.position).x;
                var snappedX = noteEditor.SnapX(Mathf.Max(0, newX));
                noteEditor.activeSong.tempoEvents.Add(new(snappedX, 2));
                noteEditor.activeSong.SortTempoEvents();
            }
            noteEditor.selectedEvent.Value = null;
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            if (Input.GetMouseButtonDown(0))
            {
                var rect = GetComponent<RectTransform>();
                bool isInside = RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);
                if (isInside) return;
                noteEditor.selectedEvent.Value = null;
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                noteEditor.activeSong.tempoEvents.Remove(noteEditor.selectedEvent.Value);
                noteEditor.selectedEvent.Value = null;
            }
        }
    }
}
