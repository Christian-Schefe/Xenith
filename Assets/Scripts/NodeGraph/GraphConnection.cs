using ReactiveData.App;
using ReactiveData.Core;
using UnityEngine;

namespace NodeGraph
{
    public class GraphConnection : MonoBehaviour, IReactor<ReactiveConnection>
    {
        [SerializeField] private UILine line;

        public ReactiveConnection connection;

        private Vector2? fromPosition = null;
        private Vector2? toPosition = null;

        private void Update()
        {
            var oldFromPosition = fromPosition;
            var oldToPosition = toPosition;

            var graphEditor = Globals<GraphEditor>.Instance;
            var fromNode = graphEditor.GetNode(connection.fromNode.Value);
            var toNode = graphEditor.GetNode(connection.toNode.Value);

            fromPosition = fromNode.GetConnectorPosition(false, connection.fromIndex.Value);
            toPosition = toNode.GetConnectorPosition(true, connection.toIndex.Value);

            if (oldFromPosition != fromPosition || oldToPosition != toPosition)
            {
                UpdatePositions();
            }
            line.color = fromNode.GetConnector(false, connection.fromIndex.Value).GetConnectorColor();
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

        public void Bind(ReactiveConnection data)
        {
            connection = data;
            UpdatePositions();
        }

        public void Unbind()
        {
            connection = null;
        }
    }
}
