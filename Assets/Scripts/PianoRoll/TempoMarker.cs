using ReactiveData.App;
using ReactiveData.Core;
using UnityEngine;

namespace PianoRoll
{
    public class TempoMarker : MonoBehaviour, IReactor<ReactiveTempoEvent>
    {
        [SerializeField] private RectTransform rectTransform;

        private ReactiveTempoEvent tempoEvent;

        public void Bind(ReactiveTempoEvent data)
        {
            tempoEvent = data;
            tempoEvent.beat.AddAndCall(OnBeatChanged);
        }

        public void Unbind()
        {
            tempoEvent.beat.Remove(OnBeatChanged);
            tempoEvent = null;
        }

        private void OnBeatChanged(float beat)
        {
            Update();
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var screenPos = noteEditor.PianoToScreenCoords(new(tempoEvent.beat.Value, 0));
            rectTransform.position = new(screenPos.x, rectTransform.position.y);
        }
    }
}
