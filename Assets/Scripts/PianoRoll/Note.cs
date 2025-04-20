using UnityEngine;

namespace PianoRoll
{
    public class Note : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Color normalColor, selectedColor;

        private float xPos;
        public int yPos;
        public float length;

        const float border = 0.1f;

        private bool selected;

        public Vector2 Position => new(xPos, yPos);
        public float EndX => xPos + length;

        public Rect Rect => new(xPos, yPos, length, 1f);

        public void Initialize(float xPos, int yPos, float length)
        {
            SetSelected(false);
            SetLength(length);
            SetPosition(xPos, yPos);
        }

        public void SetPosition(float xPos, int yPos)
        {
            this.xPos = Mathf.Max(xPos, 0);
            this.yPos = Mathf.Max(yPos, 0);
            Update();
        }

        public void SetSelected(bool selected)
        {
            this.selected = selected;
            sprite.color = selected ? selectedColor : normalColor;
        }

        public bool IsWithin(float x)
        {
            return xPos <= x && x < xPos + length;
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var pianoPos = new Vector2(xPos + length * 0.5f, yPos + 0.5f);
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