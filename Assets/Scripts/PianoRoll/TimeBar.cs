using UnityEngine;
using UnityEngine.EventSystems;

namespace PianoRoll
{
    public class TimeBar : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            DoDrag(eventData.position);
        }

        private void DoDrag(Vector2 screenPos)
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var playPosition = Globals<PlayPosition>.Instance;
            var newX = noteEditor.ScreenToPianoCoords(screenPos).x;
            playPosition.SetPosition(noteEditor.SnapX(newX));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            DoDrag(eventData.position);
        }
    }
}
