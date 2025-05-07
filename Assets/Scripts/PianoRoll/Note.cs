using UnityEngine;

namespace PianoRoll
{
    public class Note : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Color normalColor, selectedColor;

        public float xPos;
        public int ySteps;
        public float length;

        const float border = 0.1f;

        public Vector2 Position => new(xPos, GetYPos());
        public float EndX => xPos + length;

        public Rect Rect => new(xPos, GetYPos(), length, 1f);

        public int GetYPos()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            return noteEditor.StepsToPiano(ySteps);
        }

        public void Initialize(float xPos, int ySteps, float length)
        {
            SetSelected(false);
            SetLength(length);
            SetPosition(xPos, ySteps);
        }

        public void SetPosition(float xPos, int ySteps)
        {
            this.xPos = Mathf.Max(xPos, 0);
            this.ySteps = Mathf.Max(ySteps, 0);
            Update();
        }

        public void SetSelected(bool selected)
        {
            sprite.color = selected ? selectedColor : normalColor;
        }

        public bool IsWithin(float x)
        {
            return xPos <= x && x < xPos + length;
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var pianoPos = new Vector2(xPos + length * 0.5f, GetYPos() + 0.5f);
            transform.position = noteEditor.PianoToWorldCoords(pianoPos);
            sprite.size = noteEditor.Zoom * new Vector2(length, 1f - border) / sprite.transform.localScale;
        }

        public void SetLength(float length)
        {
            this.length = Mathf.Max(0.01f, length);
            Update();
        }
    }
}