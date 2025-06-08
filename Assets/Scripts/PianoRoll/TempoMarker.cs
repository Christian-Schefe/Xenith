using ReactiveData.App;
using ReactiveData.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PianoRoll
{
    public class TempoMarker : MonoBehaviour, IReactor<ReactiveTempoEvent>, IDragHandler, IPointerDownHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image img;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Gradient tempoGradient;

        private ReactiveTempoEvent tempoEvent;

        public void Bind(ReactiveTempoEvent data)
        {
            tempoEvent = data;
            tempoEvent.beat.AddAndCall(OnBeatChanged);

            var noteEditor = Globals<NoteEditor>.Instance;
            noteEditor.selectedEvent.AddAndCall(OnSelectedEventChanged);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var noteEditor = Globals<NoteEditor>.Instance;
            var newPos = noteEditor.ScreenToPianoCoords(eventData.position).x;
            tempoEvent.beat.Value = noteEditor.SnapX(Mathf.Max(0, newPos));
            noteEditor.activeSong.SortTempoEvents();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (isShift)
            {
                var tempoBar = Globals<TempoBar>.Instance;
                tempoBar.OnPointerDown(eventData);
                return;
            }
            var noteEditor = Globals<NoteEditor>.Instance;
            noteEditor.selectedEvent.Value = tempoEvent;
        }

        public void Unbind()
        {
            tempoEvent.beat.Remove(OnBeatChanged);
            tempoEvent = null;

            var noteEditor = Globals<NoteEditor>.Instance;
            noteEditor.selectedEvent.Remove(OnSelectedEventChanged);
        }

        private void OnBeatChanged(float beat)
        {
            UpdateUI();
        }

        private void OnSelectedEventChanged(ReactiveTempoEvent selectedEvent)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var screenPos = noteEditor.PianoToScreenCoords(new(tempoEvent.beat.Value, 0));
            rectTransform.position = new(screenPos.x, rectTransform.position.y);
            var isSelected = noteEditor.selectedEvent.Value == tempoEvent;
            img.color = isSelected ? selectedColor : tempoGradient.Evaluate(tempoEvent.bps.Value);
            if (isSelected)
            {
                transform.SetAsLastSibling();
            }
        }

        private void Update()
        {
            UpdateUI();
        }
    }
}
