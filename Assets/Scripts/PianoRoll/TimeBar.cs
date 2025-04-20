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
            var cam = Globals<CameraController>.Instance.Cam;

            var camTopLeft = (Vector2)cam.transform.position + new Vector2(-cam.orthographicSize * cam.aspect, cam.orthographicSize);
            var topLeftPiano = noteEditor.WorldToPianoCoords(camTopLeft);
            var firstBar = noteEditor.GetBar(topLeftPiano);
            return firstBar + index;
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var cam = Globals<CameraController>.Instance.Cam;

            var camTopLeft = (Vector2)cam.transform.position + new Vector2(-cam.orthographicSize * cam.aspect, cam.orthographicSize);
            var camTopRight = (Vector2)cam.transform.position + new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
            var topLeftPiano = noteEditor.WorldToPianoCoords(camTopLeft);
            var topRightPiano = noteEditor.WorldToPianoCoords(camTopRight);
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
            var cam = Globals<CameraController>.Instance.Cam;

            var camTop = (Vector2)cam.transform.position + cam.orthographicSize * Vector2.up;
            transform.position = camTop + Vector2.down * 0.5f;
            transform.localScale = new(cam.orthographicSize * cam.aspect * 2, 1, 1);
        }
    }
}
