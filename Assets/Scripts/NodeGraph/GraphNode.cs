using DSP;
using DTO;
using ReactiveData.App;
using ReactiveData.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NodeGraph
{
    public class GraphNode : MonoBehaviour, IReactor<ReactiveNode>, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform rectTransform;

        [SerializeField] private UIImage background;
        [SerializeField] private RectTransform inputLabelParent, outputLabelParent, footer;
        [SerializeField] private TMPro.TextMeshProUGUI label;
        [SerializeField] private NodeSettingsContainer settingsContainer;
        [SerializeField] private float headerHeight, labelWidth, labelHeight;

        public ReactiveNode node;

        public Vector2 Position => node.position.Value;

        private readonly List<NodeIOLabel> inputLabels = new();
        private readonly List<NodeIOLabel> outputLabels = new();

        private bool isDragging;
        private bool isSelected;
        private bool isHovered;

        private AudioNode audioNode;

        private bool isRebuilding;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetPosition(Vector2 position)
        {
            node.position.Value = position;
        }

        public void OnSettingsChanged()
        {
            if (NeedsRebuild())
            {
                Rebuild();
            }
        }

        public bool NeedsRebuild()
        {
            var inputs = audioNode.BuildInputs();
            var outputs = audioNode.BuildOutputs();
            if (inputs.Count != inputLabels.Count) return true;
            if (outputs.Count != outputLabels.Count) return true;

            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                var inputLabel = inputLabels[i];
                if (inputLabel.type != input.Value.Type) return true;
                if (inputLabel.text != input.name) return true;
            }
            for (int i = 0; i < outputs.Count; i++)
            {
                var output = outputs[i];
                var outputLabel = outputLabels[i];
                if (outputLabel.type != output.Value.Type) return true;
                if (outputLabel.text != output.name) return true;
            }
            return false;
        }

        public void Rebuild()
        {
            if (isRebuilding)
            {
                Debug.LogError("Rebuilding caused loop.");
                return;
            }
            isRebuilding = true;
            var graphEditor = Globals<GraphEditor>.Instance;
            RebuildVisuals();
            graphEditor.Graph.BreakInvalidConnections(this);
            isRebuilding = false;
        }

        private void RebuildVisuals()
        {
            foreach (var label in inputLabels)
            {
                Destroy(label.gameObject);
            }
            foreach (var label in outputLabels)
            {
                Destroy(label.gameObject);
            }
            inputLabels.Clear();
            outputLabels.Clear();

            var graphEditor = Globals<GraphEditor>.Instance;

            var inputs = audioNode.BuildInputs();
            var outputs = audioNode.BuildOutputs();
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                var inputLabel = Instantiate(graphEditor.inputLabelPrefab, inputLabelParent);
                inputLabel.Initialize(this, i, true, input.name, input.Value.Type);
                inputLabels.Add(inputLabel);
            }
            for (int i = 0; i < outputs.Count; i++)
            {
                var output = outputs[i];
                var outputLabel = Instantiate(graphEditor.outputLabelPrefab, outputLabelParent);
                outputLabel.Initialize(this, i, false, output.name, output.Value.Type);
                outputLabels.Add(outputLabel);
            }
            var settingsHeight = settingsContainer.ApplyHeight();

            var maxCount = Mathf.Max(inputLabels.Count, outputLabels.Count);
            var height = labelHeight * maxCount;

            var isTwoColumns = settingsContainer.HasSettings() || (inputs.Count > 0 && outputs.Count > 0);
            var width = isTwoColumns ? (labelWidth * 2) : labelWidth;

            var inputRect = inputLabelParent.GetComponent<RectTransform>();
            var outputRect = outputLabelParent.GetComponent<RectTransform>();
            inputRect.sizeDelta = isTwoColumns || inputs.Count > 0 ? new(labelWidth, height) : new(0, height);
            outputRect.sizeDelta = isTwoColumns || outputs.Count > 0 ? new(labelWidth, height) : new(0, height);

            rectTransform.sizeDelta = new Vector2(width, height + headerHeight + settingsHeight);

            footer.offsetMax = new Vector2(0, -headerHeight - settingsHeight);

            label.text = node.id.Value.id;
            rectTransform.localPosition = node.position.Value;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateOutline();
        }

        private void UpdateOutline()
        {
            bool visible = isSelected || isHovered;
            float width = isSelected ? 2.5f : 1.5f;
            background.outlineWidth = width;
            background.outline = visible;
        }

        public Vector2 GetConnectorPosition(bool input, int index)
        {
            var label = input ? inputLabels[index] : outputLabels[index];
            return label.GetConnectorPosition();
        }

        public bool TryGetConnector(bool input, int index, out NodeIOLabel connector)
        {
            var list = input ? inputLabels : outputLabels;
            if (index < 0 || index >= list.Count)
            {
                connector = null;
                return false;
            }
            connector = list[index];
            return true;
        }

        public NodeIOLabel GetConnector(bool input, int index)
        {
            return input ? inputLabels[index] : outputLabels[index];
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                return;
            }
            if (!isDragging) return;
            var graphEditor = Globals<GraphEditor>.Instance;
            var mousePos = graphEditor.ScreenToNodePosition(eventData.position);
            graphEditor.MoveSelectedNodes(mousePos);
        }

        public Rect GetRect()
        {
            var size = rectTransform.sizeDelta;
            var pos = node.position.Value - size / 2;
            return new Rect(pos, size);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                return;
            }
            rectTransform.SetAsLastSibling();
            var graphEditor = Globals<GraphEditor>.Instance;
            var mousePos = graphEditor.ScreenToNodePosition(eventData.position);
            graphEditor.SetDragOffset(mousePos);
            isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                return;
            }
            isDragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isSelected) return;
            var graphEditor = Globals<GraphEditor>.Instance;
            bool keepPrevious = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            graphEditor.AddSelectedNode(this, keepPrevious);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            UpdateOutline();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            UpdateOutline();
        }

        public void Bind(ReactiveNode node)
        {
            this.node = node;
            settingsContainer.Bind(OnSettingsChanged, node);

            node.position.AddAndCall(OnPositionChanged);
            node.id.AddAndCall(OnNodeIdChanged);

            RebuildVisuals();
        }

        private void OnPositionChanged(Vector2 position)
        {
            transform.localPosition = position;
        }

        private void OnNodeIdChanged(NodeResource id)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            if (!graphEditor.GetNodeFromTypeId(id, out audioNode))
            {
                graphEditor.GetNodeFromTypeId(new("invalid", true), out audioNode);
            }
            if (audioNode is SettingsNode settingsNode)
            {
            }

            try
            {
                audioNode.Initialize();
            }
            catch
            {
                graphEditor.GetNodeFromTypeId(new("invalid", true), out audioNode);
            }
        }

        public void Unbind()
        {
            node.position.Remove(OnPositionChanged);
            node.id.Remove(OnNodeIdChanged);
            settingsContainer.Unbind();
            node = null;
        }
    }
}
