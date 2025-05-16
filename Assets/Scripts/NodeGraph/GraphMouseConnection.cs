using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class GraphMouseConnection : MonoBehaviour
    {
        [SerializeField] private UILine line;

        public GraphNode fromNode;
        public int fromNodeOutput;

        private Vector2? fromPosition = null;
        private Vector2? toPosition = null;

        public bool visible;

        public void Show(GraphNode fromNode, int fromNodeOutput)
        {
            visible = true;
            line.gameObject.SetActive(true);
            this.fromNode = fromNode;
            this.fromNodeOutput = fromNodeOutput;

            UpdatePositions();
        }

        public void Hide()
        {
            visible = false;
            line.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!visible) return;
            var oldFromPosition = fromPosition;
            var oldToPosition = toPosition;

            var graphEditor = Globals<GraphEditor>.Instance;

            fromPosition = fromNode.GetConnectorPosition(false, fromNodeOutput);
            toPosition = graphEditor.ScreenToNodePosition(Input.mousePosition);

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
    }
}
