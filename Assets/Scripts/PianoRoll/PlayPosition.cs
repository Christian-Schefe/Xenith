using UnityEngine;
using UnityEngine.UI;

namespace PianoRoll
{
    public class PlayPosition : MonoBehaviour
    {
        [SerializeField] private RectTransform rt;

        public float position;

        public void SetPosition(float pos)
        {
            position = Mathf.Max(pos, 0);
        }

        private void LateUpdate()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var screenPos = noteEditor.PianoToScreenCoords(new(position, 0));
            rt.transform.position = new(screenPos.x, rt.transform.position.y);
        }
    }
}