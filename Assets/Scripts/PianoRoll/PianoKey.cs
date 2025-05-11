using TMPro;
using UnityEngine;

namespace PianoRoll
{
    public class PianoKey : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI text;

        private int yOffset;

        public void Initialize(int yOffset)
        {
            this.yOffset = yOffset;
        }

        private void LateUpdate()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var timeBar = Globals<PianoRollVisuals>.Instance;
            var y = timeBar.GetRowByOffset(yOffset);
            var steps = noteEditor.PianoToSteps(y);
            var octaves = steps / noteEditor.Key.edo;
            var innerSteps = steps % noteEditor.Key.edo;
            bool showOctave = innerSteps == 0;
            text.text = showOctave ? $"{octaves} - {innerSteps}" : innerSteps.ToString();
            var screenPos = noteEditor.PianoToScreenCoords(new(0, y));
            var upperScreenPos = noteEditor.PianoToScreenCoords(new(0, y + 1));

            var canvasPos = noteEditor.ScreenToCanvasCoords(screenPos);
            var upperCanvasPos = noteEditor.ScreenToCanvasCoords(upperScreenPos);

            var height = upperCanvasPos.y - canvasPos.y;
            rectTransform.position = new(rectTransform.position.x, (screenPos.y + upperScreenPos.y) * 0.5f);
            rectTransform.sizeDelta = new(rectTransform.sizeDelta.x, height);
        }
    }
}
