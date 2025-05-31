using ReactiveData.App;
using ReactiveData.Core;
using UnityEngine;

namespace PianoRoll
{
    public class Note : MonoBehaviour, IReactor<ReactiveNote>
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Gradient velocityGradient;
        [SerializeField] private Color selectedColor;
        [SerializeField] private float alpha;

        public ReactiveNote note;

        const float border = 0.1f;

        public float Beat => note.beat.Value;
        public int Pitch => note.pitch.Value;
        public float Velocity => note.velocity.Value;
        public float Length => note.length.Value;
        public Vector2 Position => new(note.beat.Value, GetYPos());
        public float EndBeat => note.beat.Value + note.length.Value;

        public Rect Rect => new(note.beat.Value, GetYPos(), note.length.Value, 1f);

        public int GetYPos()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            return noteEditor.StepsToPiano(note.pitch.Value);
        }

        public bool IsWithin(float x)
        {
            return note.beat.Value <= x && x < EndBeat;
        }

        private void UpdateUI()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var pianoPos = new Vector2(note.beat.Value + note.length.Value * 0.5f, GetYPos() + 0.5f);
            transform.position = noteEditor.PianoToWorldCoords(pianoPos);
            sprite.size = noteEditor.Zoom * new Vector2(note.length.Value, 1f - border) / sprite.transform.localScale;
        }

        public void Bind(ReactiveNote note)
        {
            this.note = note;
            note.beat.Add(OnBeatChanged);
            note.pitch.Add(OnPitchChanged);
            note.velocity.Add(OnVelocityChanged);
            note.length.Add(OnLengthChanged);

            var noteEditor = Globals<NoteEditor>.Instance;
            noteEditor.selectedNotes.OnChanged += OnSelectionChanged;
            noteEditor.zoom.Add(OnZoomChanged);
            noteEditor.stepsList.OnChanged += OnStepsListChanged;
            OnSelectionChanged();
            UpdateUI();
        }

        public void Unbind()
        {
            note.beat.Remove(OnBeatChanged);
            note.pitch.Remove(OnPitchChanged);
            note.velocity.Remove(OnVelocityChanged);
            note.length.Remove(OnLengthChanged);
            note = null;
            var noteEditor = Globals<NoteEditor>.Instance;
            noteEditor.selectedNotes.OnChanged -= OnSelectionChanged;
            noteEditor.zoom.Remove(OnZoomChanged);
            noteEditor.stepsList.OnChanged -= OnStepsListChanged;
        }

        private void OnZoomChanged(Vector2 zoom)
        {
            UpdateUI();
        }

        private void OnStepsListChanged()
        {
            UpdateUI();
        }

        private void OnSelectionChanged()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var selected = noteEditor.selectedNotes.Contains(this);
            var col = selected ? selectedColor : velocityGradient.Evaluate(note.velocity.Value);
            col.a = alpha;
            sprite.color = col;
        }

        private void OnBeatChanged(float beat)
        {
            UpdateUI();
        }

        private void OnPitchChanged(int pitch)
        {
            UpdateUI();
        }

        private void OnVelocityChanged(float velocity)
        {
            UpdateUI();
            OnSelectionChanged();
        }

        private void OnLengthChanged(float length)
        {
            UpdateUI();
        }
    }
}