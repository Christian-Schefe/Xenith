using UnityEngine;

namespace PianoRoll
{
    public class PlayPosition : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer line;
        [SerializeField] private SpriteRenderer head;

        public float position;

        public void SetPosition(float pos)
        {
            position = Mathf.Max(pos, 0);
        }

        private void LateUpdate()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var worldPos = noteEditor.PianoToWorldCoords(new(position, 50));
            line.transform.position = (Vector3)worldPos;
            line.transform.localScale = new Vector3(1, 100, 1) * noteEditor.Zoom;

            var cam = Globals<CameraController>.Instance.Cam;
            var camTop = cam.transform.position.y + cam.orthographicSize;
            head.transform.position = new Vector3(worldPos.x, camTop - head.transform.localScale.y * 0.5f, 0);
        }
    }
}