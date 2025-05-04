using System.Collections.Generic;
using UnityEngine;

namespace PianoRoll
{
    public class TimeBar : MonoBehaviour
    {
        [SerializeField] private TimeBarBar barPrefab;

        private List<TimeBarBar> bars = new();

        public int GetBarByIndex(int index)
        {
            var noteEditor = Globals<NoteEditor>.Instance;

            var rect = noteEditor.ViewRectScreen();
            var topLeftPiano = noteEditor.ScreenToPianoCoords(new(rect.xMin, rect.yMax));
            var firstBar = noteEditor.GetBar(topLeftPiano);
            return firstBar + index;
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;

            var rect = noteEditor.ViewRectScreen();
            var topLeftPiano = noteEditor.ScreenToPianoCoords(new(rect.xMin, rect.yMax));
            var topRightPiano = noteEditor.ScreenToPianoCoords(rect.max);
            var firstBar = noteEditor.GetBar(topLeftPiano);
            var lastBar = noteEditor.GetBar(topRightPiano) + 1;
            var barCount = lastBar - firstBar + 1;

            if (bars.Count < barCount)
            {
                for (var i = bars.Count; i < barCount; i++)
                {
                    var bar = Instantiate(barPrefab);
                    bar.Initialize(i);
                    bars.Add(bar);
                }
            }
            else if (bars.Count > barCount)
            {
                for (var i = bars.Count - 1; i >= barCount; i--)
                {
                    Destroy(bars[i].gameObject);
                    bars.RemoveAt(i);
                }
            }
        }

        private void LateUpdate()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var rect = noteEditor.ViewRectScreen();
            var topLeftWorld = noteEditor.PianoToWorldCoords(noteEditor.ScreenToPianoCoords(new(rect.xMin, rect.yMax)));
            var topRightWorld = noteEditor.PianoToWorldCoords(noteEditor.ScreenToPianoCoords(rect.max));
            var topWorld = (topLeftWorld + topRightWorld) / 2f;
            var width = topRightWorld.x - topLeftWorld.x;

            transform.position = topWorld + Vector2.down * 0.5f;
            transform.localScale = new(width, 1, 1);
        }
    }
}
