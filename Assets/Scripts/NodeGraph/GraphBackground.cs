using Unity.VisualScripting;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NodeGraph
{
    public class GraphBackground : MonoBehaviour, IPointerDownHandler, IEndDragHandler, IDragHandler, IScrollHandler
    {
        [SerializeField] private UIImage dragSelectImage;

        private Vector2 dragSelectStart;
        private bool isDraggingSelect;
        private bool keepPreviousSelection;

        public void OnPointerDown(PointerEventData eventData)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!graphEditor.IsInteractable()) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                dragSelectStart = graphEditor.ScreenToNodePosition(eventData.position);
                isDraggingSelect = true;
                dragSelectImage.gameObject.SetActive(true);
                keepPreviousSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                DoSelect(eventData.position);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!graphEditor.IsInteractable()) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                graphEditor.AddOffset(eventData.delta);
            }

            if (eventData.button == PointerEventData.InputButton.Left && isDraggingSelect)
            {
                DoSelect(eventData.position);
            }
        }

        private void DoSelect(Vector2 pos)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            var dragSelectEnd = graphEditor.ScreenToNodePosition(pos);

            var center = (dragSelectStart + dragSelectEnd) / 2;
            var size = new Vector2(Mathf.Abs(dragSelectStart.x - dragSelectEnd.x), Mathf.Abs(dragSelectStart.y - dragSelectEnd.y));
            dragSelectImage.rectTransform.localPosition = center;
            dragSelectImage.rectTransform.sizeDelta = size;
            var position = center - size / 2;
            graphEditor.SelectWithinRect(new(position, size), keepPreviousSelection);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!graphEditor.IsInteractable()) return;

            if (eventData.button == PointerEventData.InputButton.Left && isDraggingSelect)
            {
                DoSelect(eventData.position);
                isDraggingSelect = false;
                dragSelectImage.gameObject.SetActive(false);
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!graphEditor.IsInteractable()) return;

            graphEditor.AddScroll(eventData.scrollDelta.y);
        }
    }
}
