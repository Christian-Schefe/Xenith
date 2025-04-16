using UnityEngine;
using UnityEngine.EventSystems;

namespace NodeGraph {
    public class GraphBackground : MonoBehaviour, IDragHandler, IScrollHandler
    {
        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                var graphEditor = Globals<GraphEditor>.Instance;
                graphEditor.AddOffset(eventData.delta);
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            graphEditor.AddScroll(eventData.scrollDelta.y);
        }
    }
}
