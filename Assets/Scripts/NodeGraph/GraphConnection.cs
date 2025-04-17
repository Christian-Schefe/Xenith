using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class GraphConnection : MonoBehaviour
    {
        [SerializeField] private UILine line;

        public GraphNode fromNode;
        public int fromNodeOutput;
        public GraphNode toNode;
        public int toNodeInput;

        private Vector2? fromPosition = null;
        private Vector2? toPosition = null;

        public bool connectToMouse;

        public void SetConnection(bool from, GraphNode node, int index)
        {
            if (from)
            {
                fromNode = node;
                fromNodeOutput = index;
            }
            else
            {
                toNode = node;
                toNodeInput = index;
            }
            UpdatePositions();
        }

        public void UnsetConnection(bool from)
        {
            if (from)
            {
                fromNode = null;
                fromNodeOutput = -1;
            }
            else
            {
                toNode = null;
                toNodeInput = -1;
            }
            UpdatePositions();
        }

        private void Update()
        {
            var oldFromPosition = fromPosition;
            var oldToPosition = toPosition;
            var graphEditor = Globals<GraphEditor>.Instance;

            if (fromNode != null)
            {
                fromPosition = fromNode.GetConnectorPosition(false, fromNodeOutput);
            }
            else if (connectToMouse)
            {
                fromPosition = graphEditor.ScreenToNodePosition(Input.mousePosition);
            }
            else
            {
                fromPosition = null;
            }

            if (toNode != null)
            {
                toPosition = toNode.GetConnectorPosition(true, toNodeInput);
            }
            else if (connectToMouse)
            {
                toPosition = graphEditor.ScreenToNodePosition(Input.mousePosition);
            }
            else
            {
                toPosition = null;
            }

            if (oldFromPosition != fromPosition || oldToPosition != toPosition)
            {
                UpdatePositions();
            }
            line.color = fromNode.GetConnector(false, fromNodeOutput).GetConnectorColor();
        }

        private void UpdatePositions()
        {
            if (fromPosition is Vector2 from && toPosition is Vector2 to)
            {
                line.gameObject.SetActive(true);
                line.SetPositions(new Vector2[] { from, from + Vector2.right * 15, to + Vector2.left * 15, to });
                transform.localPosition = Vector3.zero;
            }
            else
            {
                line.gameObject.SetActive(false);
            }
        }

        public SerializedGraphConnection Serialize(Dictionary<GraphNode, int> nodeIndices)
        {
            return new SerializedGraphConnection()
            {
                fromNodeIndex = nodeIndices[fromNode],
                toNodeIndex = nodeIndices[toNode],
                fromNodeOutput = fromNodeOutput,
                toNodeInput = toNodeInput
            };
        }

        public void Deserialize(List<GraphNode> nodes, SerializedGraphConnection connection)
        {
            fromNode = nodes[connection.fromNodeIndex];
            toNode = nodes[connection.toNodeIndex];
            fromNodeOutput = connection.fromNodeOutput;
            toNodeInput = connection.toNodeInput;

            UpdatePositions();
        }
    }

    public struct SerializedGraphConnection
    {
        public int fromNodeIndex;
        public int toNodeIndex;
        public int fromNodeOutput;
        public int toNodeInput;
    }
}
