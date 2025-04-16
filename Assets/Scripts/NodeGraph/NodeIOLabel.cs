using DSP;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NodeGraph
{
    public class NodeIOLabel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMPro.TextMeshProUGUI label;
        [SerializeField] private Image typeIndicator;

        private GraphNode node;
        private int index;
        private bool isInput;
        private ValueType type;

        private bool isDragging;

        public void Initialize(GraphNode node, int index, bool isInput, string text, ValueType type)
        {
            this.node = node;
            this.index = index;
            this.isInput = isInput;
            this.type = type;
            label.text = text;
            typeIndicator.color = GetConnectorColor();
        }

        public Vector2 GetConnectorPosition()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            return graphEditor.ScreenToNodePosition(typeIndicator.transform.position);
        }

        public Color GetConnectorColor()
        {
            return type switch
            {
                ValueType.Float => new Color(0.5f, 0.5f, 1f),
                ValueType.Bool => new Color(1f, 0.5f, 0.5f),
                _ => new Color(1f, 1f, 1f)
            };
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                return;
            }
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!isInput)
            {
                graphEditor.StartConnection(node, index);
                isDragging = true;
            }
            else
            {
                isDragging = graphEditor.TryPickConnection(node, index);
            }
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;
            var graphEditor = Globals<GraphEditor>.Instance;
            graphEditor.ReleaseConnection();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInput) return;
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!graphEditor.IsConnecting()) return;
            graphEditor.AddConnectionTarget(node, index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInput) return;
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!graphEditor.IsConnecting()) return;
            graphEditor.RemoveConnectionTarget(node, index);
        }
    }
}
