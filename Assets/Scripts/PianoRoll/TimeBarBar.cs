using UnityEngine;

namespace PianoRoll
{
    public class TimeBarBar : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMPro.TextMeshProUGUI text;
        private int index;

        public void Initialize(int index)
        {
            this.index = index;
            LateUpdate();
        }

        private void LateUpdate()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var timeBar = Globals<TimeBar>.Instance;
            var bar = timeBar.GetBarByIndex(index);
            text.text = bar.ToString();
            var screenPos = noteEditor.PianoToScreenCoords(new(bar * 4, 0));
            var rightScreenPos = noteEditor.PianoToScreenCoords(new(bar * 4 + 4, 0));
            var canvasPos = noteEditor.ScreenToCanvasCoords(screenPos);
            var rightCanvasPos = noteEditor.ScreenToCanvasCoords(rightScreenPos);
            var width = rightCanvasPos.x - canvasPos.x;
            rectTransform.position = new((screenPos.x + rightScreenPos.x) * 0.5f, rectTransform.position.y);
            rectTransform.sizeDelta = new(width, rectTransform.sizeDelta.y);
        }
    }
}
