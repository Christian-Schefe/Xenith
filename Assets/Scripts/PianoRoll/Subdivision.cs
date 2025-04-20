using System;
using UnityEngine;

namespace PianoRoll
{
    public class Subdivision : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;

        private int index;

        public void Initialize(int index)
        {
            this.index = index;
            sprite.color = (index + 1) % 4 == 0 ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f);
            Update();
        }

        private void Update()
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            transform.localScale = new Vector3(1, 100, 1) * noteEditor.Zoom;
            transform.position = new Vector3(index + 1, 50, 0) * noteEditor.Zoom;
        }
    }
}
