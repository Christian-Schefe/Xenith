using UnityEngine;

namespace PianoRoll
{
    public class TimeBarBar : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshPro text;
        private int index;

        public void Initialize(int index)
        {
            this.index = index;
            LateUpdate();
        }

        private void LateUpdate()
        {
            var timeBar = Globals<TimeBar>.Instance;
            var bar = timeBar.GetBarByIndex(index);
            text.text = bar.ToString();
            var noteEditor = Globals<NoteEditor>.Instance;
            var rect = noteEditor.ViewRectScreen();
            var topLeftPiano = noteEditor.ScreenToPianoCoords(new(rect.xMin, rect.yMax));

            var worldPos = noteEditor.PianoToWorldCoords(new(bar * 4, topLeftPiano.y));


            transform.position = worldPos + new Vector2(0.5f, -0.5f);
        }
    }
}
