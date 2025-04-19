using UnityEngine;

namespace PianoRoll
{
    public class NoteRow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bg;

        private int y;
        public void Initialize(int y)
        {
            this.y = y;
            Update();
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var size = new Vector2(1000, 1) * noteEditor.zoom;
            bg.color = y % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.3f, 0.3f, 0.3f);
            transform.localPosition = size * 0.5f + new Vector2(0, y) * noteEditor.zoom;
            transform.localScale = new Vector3(size.x, size.y, 1);
        }
    }
}