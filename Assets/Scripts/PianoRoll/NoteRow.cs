using UnityEngine;

namespace PianoRoll
{
    public class NoteRow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bg;
        [SerializeField] private Color evenColor, oddColor, specialColor;

        private int y;
        public void Initialize(int y)
        {
            this.y = y;
            Update();
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var size = new Vector2(1000, 1) * noteEditor.Zoom;
            if (noteEditor.IsSpecialRow(y))
            {
                bg.color = specialColor;
            }
            else
            {
                bg.color = y % 2 == 0 ? evenColor : oddColor;
            }
            transform.position = size * 0.5f + new Vector2(0, y) * noteEditor.Zoom;
            transform.localScale = new Vector3(size.x, size.y, 1);
        }
    }
}