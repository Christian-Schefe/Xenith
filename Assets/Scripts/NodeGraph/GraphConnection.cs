using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class GraphConnection : MonoBehaviour
    {
        [SerializeField] private UILine line;

        public DTO.Connection connection;

        public int FromNodeIndex => connection.fromNodeIndex;
        public GraphNode FromNode => Globals<Graph>.Instance.Nodes[connection.fromNodeIndex];
        public int FromNodeOutput => connection.fromNodeOutput;
        public int ToNodeIndex => connection.toNodeIndex;
        public GraphNode ToNode => Globals<Graph>.Instance.Nodes[connection.toNodeIndex];
        public int ToNodeInput => connection.toNodeInput;

        private Vector2? fromPosition = null;
        private Vector2? toPosition = null;

        public void Initialize(DTO.Connection connection)
        {
            this.connection = connection;

            UpdatePositions();
        }

        public void SetNodes(int newFromNode, int newToNode)
        {
            connection.fromNodeIndex = newFromNode;
            connection.toNodeIndex = newToNode;
            UpdatePositions();
        }

        private void Update()
        {
            var oldFromPosition = fromPosition;
            var oldToPosition = toPosition;

            fromPosition = FromNode.GetConnectorPosition(false, FromNodeOutput);
            toPosition = ToNode.GetConnectorPosition(true, ToNodeInput);

            if (oldFromPosition != fromPosition || oldToPosition != toPosition)
            {
                UpdatePositions();
            }
            line.color = FromNode.GetConnector(false, FromNodeOutput).GetConnectorColor();
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
    }
}
