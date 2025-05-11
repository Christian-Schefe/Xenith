using UnityEngine;

namespace PianoRoll
{
    public class Note : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Color normalColor, selectedColor;

        public DTO.Note note;

        public float Length => note.length;
        public float X => note.x;
        public int Y => note.y;

        const float border = 0.1f;

        public Vector2 Position => new(note.x, GetYPos());
        public float EndX => note.x + note.length;

        public Rect Rect => new(note.x, GetYPos(), note.length, 1f);

        public int GetYPos()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            return noteEditor.StepsToPiano(note.y);
        }

        public void Initialize(DTO.Note note)
        {
            this.note = note;
            SetSelected(false);
            Update();
        }

        public void SetPosition(float xPos, int ySteps)
        {
            note.x = xPos;
            note.y = ySteps;
            Update();
        }

        public void SetSelected(bool selected)
        {
            sprite.color = selected ? selectedColor : normalColor;
        }

        public bool IsWithin(float x)
        {
            return note.x <= x && x < EndX;
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var pianoPos = new Vector2(note.x + note.length * 0.5f, GetYPos() + 0.5f);
            transform.position = noteEditor.PianoToWorldCoords(pianoPos);
            sprite.size = noteEditor.Zoom * new Vector2(note.length, 1f - border) / sprite.transform.localScale;
        }

        public void SetLength(float length)
        {
            note.length = Mathf.Max(0.01f, length);
            Update();
        }
    }
}