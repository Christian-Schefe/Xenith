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
            var cam = Globals<CameraController>.Instance.Cam;
            var camTop = cam.transform.position.y + cam.orthographicSize;
            var piano = noteEditor.WorldToPianoCoords(new(bar * 4, 0)).x;
            transform.position = new Vector2(piano, camTop) + new Vector2(0.5f, -0.5f);
        }
    }
}
