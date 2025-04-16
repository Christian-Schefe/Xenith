using DSP;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NodeGraph
{
    public class GraphNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform rectTransform;

        [SerializeField] private RectTransform inputLabelParent, outputLabelParent;
        [SerializeField] private TMPro.TextMeshProUGUI label;
        [SerializeField] private float headerHeight, labelWidth, labelHeight;

        private NodeResource id;
        public Vector2 position;

        private List<NodeIOLabel> inputLabels;
        private List<NodeIOLabel> outputLabels;

        private Vector2 dragOffset;
        private bool isDragging;

        private AudioNode audioNode;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(NodeResource id, Vector2 position)
        {
            inputLabels = new();
            outputLabels = new();

            this.id = id;
            this.position = position;
            var graphEditor = Globals<GraphEditor>.Instance;
            audioNode = graphEditor.GetNodeFromTypeId(id);

            audioNode.Initialize();
            for (int i = 0; i < audioNode.inputs.Count; i++)
            {
                var input = audioNode.inputs[i];
                var inputLabel = Instantiate(graphEditor.inputLabelPrefab, inputLabelParent);
                inputLabel.Initialize(this, i, true, input.name, input.Value.Type);
                inputLabels.Add(inputLabel);
            }
            for (int i = 0; i < audioNode.outputs.Count; i++)
            {
                var output = audioNode.outputs[i];
                var outputLabel = Instantiate(graphEditor.outputLabelPrefab, outputLabelParent);
                outputLabel.Initialize(this, i, false, output.name, output.Value.Type);
                outputLabels.Add(outputLabel);
            }

            var maxCount = Mathf.Max(inputLabels.Count, outputLabels.Count);
            var height = labelHeight * maxCount;

            var width = audioNode.inputs.Count > 0 && audioNode.outputs.Count > 0 ? (labelWidth * 2) : labelWidth;

            var inputRect = inputLabelParent.GetComponent<RectTransform>();
            var outputRect = outputLabelParent.GetComponent<RectTransform>();
            inputRect.sizeDelta = audioNode.inputs.Count > 0 ? new(labelWidth, height) : new(0, height);
            outputRect.sizeDelta = audioNode.outputs.Count > 0 ? new(labelWidth, height) : new(0, height);

            rectTransform.sizeDelta = new Vector2(width, height + headerHeight);

            label.text = id.displayName;
            rectTransform.localPosition = position;
        }

        public Vector2 GetConnectorPosition(bool input, int outputIndex)
        {
            var label = input ? inputLabels[outputIndex] : outputLabels[outputIndex];
            return label.GetConnectorPosition();
        }

        public NodeIOLabel GetConnector(bool input, int outputIndex)
        {
            return input ? inputLabels[outputIndex] : outputLabels[outputIndex];
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            var graphEditor = Globals<GraphEditor>.Instance;
            var mousePos = graphEditor.ScreenToNodePosition(eventData.position);
            position = mousePos - dragOffset;
            rectTransform.localPosition = position;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                return;
            }
            rectTransform.SetAsLastSibling();
            dragOffset = Globals<GraphEditor>.Instance.ScreenToNodePosition(eventData.position) - position;
            isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        public SerializedGraphNode Serialize()
        {
            string serializedSettings = null;
            if (audioNode is SettingsNode settingsNode)
            {
                serializedSettings = settingsNode.Settings.Serialize();
            }
            return new()
            {
                position = position,
                id = id,
                serializedSettings = serializedSettings
            };
        }

        public void Deserialize(SerializedGraphNode node)
        {
            Initialize(node.id, node.position);
            if (audioNode is SettingsNode settingsNode)
            {
                settingsNode.Settings.Deserialize(node.serializedSettings);
            }
        }
    }

    public struct SerializedGraphNode
    {
        public Vector2 position;
        public NodeResource id;
        public string serializedSettings;
    }
}
