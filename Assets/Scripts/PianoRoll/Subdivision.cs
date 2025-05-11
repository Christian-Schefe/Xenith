using System;
using UnityEngine;

namespace PianoRoll
{
    public class Subdivision : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Color normalColor, specialColor;

        private int indexOffset;

        public void Initialize(int indexOffset)
        {
            this.indexOffset = indexOffset;
            LateUpdate();
        }

        private void LateUpdate()
        {
            var timeBar = Globals<PianoRollVisuals>.Instance;
            var noteEditor = Globals<NoteEditor>.Instance;
            var index = timeBar.GetSubdivisionByIndex(indexOffset);
            sprite.color = index % 4 == 0 ? specialColor : normalColor;
            var viewWorldRect = noteEditor.ViewRectWorld();
            var worldPos = noteEditor.PianoToWorldCoords(new(index, 0));
            transform.localScale = new Vector3(1, viewWorldRect.height, 1);
            transform.position = new(worldPos.x, viewWorldRect.center.y, 0);
        }
    }
}
