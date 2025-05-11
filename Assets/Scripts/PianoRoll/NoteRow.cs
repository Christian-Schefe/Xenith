using UnityEngine;

namespace PianoRoll
{
    public class NoteRow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bg;
        [SerializeField] private Color evenColor, oddColor, specialColor, accidentalColor;

        private int yOffset;

        public void Initialize(int yOffset)
        {
            this.yOffset = yOffset;
            Update();
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var timeBar = Globals<PianoRollVisuals>.Instance;
            var y = timeBar.GetRowByOffset(yOffset);
            var steps = noteEditor.PianoToSteps(y);
            if (noteEditor.IsSpecialRow(steps))
            {
                bg.color = specialColor;
            }
            else if (noteEditor.IsAccidentalRow(steps))
            {
                bg.color = accidentalColor;
            }
            else
            {
                bg.color = steps % 2 == 0 ? evenColor : oddColor;
            }
            var worldRect = noteEditor.ViewRectWorld();
            var worldPos = noteEditor.PianoToWorldCoords(new(0, y));
            var upperWorldPos = noteEditor.PianoToWorldCoords(new(0, y + 1));
            var height = upperWorldPos.y - worldPos.y;
            transform.position = new(worldRect.center.x, worldPos.y + height * 0.5f, 0);
            transform.localScale = new Vector3(worldRect.width, height, 1);
        }
    }
}