using DSP;
using DTO;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeGraph
{
    public class GraphEditor : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform viewFrame;
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private RectTransform nodeParent, connectionParent;
        [SerializeField] private AddNodeDialog addNodeDialog;
        [SerializeField] private GraphDatabase graphDatabase;

        [SerializeField] private Graph graph;
        public GraphNode nodePrefab;
        public GraphConnection connectionPrefab;

        public NodeIOLabel inputLabelPrefab;
        public NodeIOLabel outputLabelPrefab;

        public GraphMouseConnection currentlyConnecting;
        private readonly HashSet<(GraphNode, int)> connectionTargets = new();
        private readonly HashSet<GraphNode> selectedNodes = new();
        private readonly Dictionary<GraphNode, Vector2> dragOffsets = new();

        private float scale = 1;
        private Vector2 offset = Vector2.zero;

        private float scrollAccumulator = 0;
        private readonly float scrollThreshold = 0.1f;
        private readonly float scrollStep = 1.1f;

        private GraphID graphId;

        public Graph Graph => graph;

        public void Show()
        {
            var main = Globals<Main>.Instance;
            graphId = main.CurrentGraphId;
            graph.ShowGraph(main.CurrentGraph);
            viewFrame.gameObject.SetActive(true);
        }

        public void Hide()
        {
            graphId = null;

            addNodeDialog.Close();
            graph.HideGraph();
            selectedNodes.Clear();
            currentlyConnecting.Hide();
            connectionTargets.Clear();
            dragOffsets.Clear();
            viewFrame.gameObject.SetActive(false);
        }

        public List<NodeResource> GetPlaceableNodes()
        {
            var builtIns = graphDatabase.GetBuiltinNodeTypes();
            var allNodes = new List<NodeResource>();
            foreach (var kvp in builtIns)
            {
                if (kvp.Key.id == "invalid") continue;
                allNodes.Add(kvp.Key);
            }
            foreach (var (id, _) in graphDatabase.GetGraphs())
            {
                var resource = new NodeResource(id.path, false);
                if (!GetNodeFromTypeId(resource, out _)) continue;
                allNodes.Add(resource);
            }
            return allNodes;
        }

        public bool GetNodeFromTypeId(NodeResource typeId, out AudioNode audioNode)
        {
            return graphDatabase.GetNodeFromTypeId(typeId, new(graphId.path, false), out audioNode);
        }

        public bool IsInteractable()
        {
            return graph != null;
        }

        private void Update()
        {
            if (!IsInteractable())
            {
                addNodeDialog.Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                addNodeDialog.Open(ScreenToUnscaledNodePosition(Input.mousePosition), ScreenToNodePosition(Input.mousePosition));
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                addNodeDialog.Close();
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                foreach (var node in selectedNodes.ToList())
                {
                    graph.RemoveNode(node);
                }
                selectedNodes.Clear();
            }

            if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
            {
                var duplicatedNodes = new List<GraphNode>();
                foreach (var node in selectedNodes.ToList())
                {
                    var newNode = graph.DuplicateNode(node);
                    duplicatedNodes.Add(newNode);
                }
                DeselectAll();
                foreach (var node in duplicatedNodes)
                {
                    AddSelectedNode(node, true);
                }
            }
        }

        public GraphNode CreateNodeInstance()
        {
            return Instantiate(nodePrefab, nodeParent);
        }

        public GraphConnection CreateConnectionInstance()
        {
            return Instantiate(connectionPrefab, connectionParent);
        }

        public void DeselectAll()
        {
            foreach (var selectedNode in selectedNodes)
            {
                selectedNode.SetSelected(false);
            }
            selectedNodes.Clear();
        }

        public void AddSelectedNode(GraphNode node, bool keepPrevious)
        {
            if (!keepPrevious)
            {
                DeselectAll();
            }
            selectedNodes.Add(node);
            node.SetSelected(true);
        }

        public void SelectWithinRect(Rect rect, bool keepPrevious)
        {
            if (!keepPrevious)
            {
                DeselectAll();
            }

            foreach (var node in graph.GetNodes())
            {
                if (rect.Overlaps(node.GetRect()))
                {
                    selectedNodes.Add(node);
                    node.SetSelected(true);
                }
            }
        }

        public void SetDragOffset(Vector2 mousePos)
        {
            dragOffsets.Clear();
            foreach (var node in selectedNodes)
            {
                var offset = mousePos - node.Position;
                dragOffsets.Add(node, offset);
            }
        }

        public void MoveSelectedNodes(Vector2 mousePos)
        {
            foreach (var node in selectedNodes)
            {
                if (!dragOffsets.ContainsKey(node)) continue;
                var pos = mousePos - dragOffsets[node];
                node.SetPosition(pos);
            }
        }

        public void AddOffset(Vector2 screenDelta)
        {
            var nodeDelta = ScreenToNodeVector(screenDelta);
            offset += nodeDelta;
            contentParent.localPosition = offset * scale;
        }

        public void AddScroll(float scrollDelta)
        {
            var oldMousePos = ScreenToNodePosition(Input.mousePosition);

            scrollAccumulator += scrollDelta;
            int signedSteps = Mathf.FloorToInt(scrollAccumulator / scrollThreshold);
            var clampedSteps = Mathf.Clamp(signedSteps, -1, 1); // Limit to one step at a time

            if (clampedSteps != 0)
            {
                var direction = Mathf.Sign(clampedSteps);
                scale *= Mathf.Pow(direction < 0 ? (1f / scrollStep) : scrollStep, Mathf.Abs(clampedSteps));
                scrollAccumulator = 0;
            }
            scale = Mathf.Clamp(scale, 0.01f, 10f);
            contentParent.localScale = new Vector3(scale, scale, 1);

            var newMousePos = ScreenToNodePosition(Input.mousePosition);
            var delta = newMousePos - oldMousePos;
            offset += delta;
            contentParent.localPosition = offset * scale;
        }

        public bool TryPickConnection(GraphNode to, int index)
        {
            if (IsConnecting())
            {
                throw new System.Exception("Already connecting");
            }
            var incoming = graph.GetConnections(to, true, false);
            if (incoming == null || incoming.Count == 0)
            {
                return false;
            }
            var connection = incoming.FirstOrDefault(c => c.ToNodeInput == index);
            if (connection == null)
            {
                return false;
            }
            graph.RemoveConnection(connection);
            currentlyConnecting.Show(connection.fromNode, connection.FromNodeOutput);
            connectionTargets.Clear();
            Destroy(connection.gameObject);
            return true;
        }

        public void StartConnection(GraphNode from, int index)
        {
            if (IsConnecting())
            {
                throw new System.Exception("Already connecting");
            }
            currentlyConnecting.Show(from, index);
            connectionTargets.Clear();
            graph.UpdateAllNodeIndices();
        }

        public void ReleaseConnection()
        {
            if (!IsConnecting())
            {
                throw new System.Exception("Not connecting");
            }
            var validTargets = connectionTargets.Where(x => graph.IsValidConnection(currentlyConnecting.fromNode, x.Item1, currentlyConnecting.fromNodeOutput, x.Item2)).ToList();
            if (validTargets.Count == 0)
            {
                currentlyConnecting.Hide();
            }
            else
            {
                var (node, index) = validTargets[0];
                var nodeMap = graph.GetNodeMap();
                var fromIndex = nodeMap[currentlyConnecting.fromNode];
                var toIndex = nodeMap[node];

                var connection = Instantiate(connectionPrefab, connectionParent);
                connection.Initialize(graph.GetNodes(), new DTO.Connection(fromIndex, currentlyConnecting.fromNodeOutput, toIndex, index));
                graph.AddConnection(connection);
                currentlyConnecting.Hide();
            }
        }

        public bool IsConnecting()
        {
            return currentlyConnecting.visible;
        }

        public void AddConnectionTarget(GraphNode to, int index)
        {
            connectionTargets.Add((to, index));
        }

        public void RemoveConnectionTarget(GraphNode to, int index)
        {
            connectionTargets.Remove((to, index));
        }

        public Rect ViewRectScreen()
        {
            Vector3[] corners = new Vector3[4];
            viewFrame.GetWorldCorners(corners);

            // Calculate the screen space rectangle
            float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
            float width = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x) - minX;
            float height = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y) - minY;

            // Display the screen space rectangle
            Rect screenRect = new(minX, minY, width, height);
            return screenRect;
        }

        public Vector2 ScreenToNodePosition(Vector2 screenPosition)
        {
            var cam = Globals<MainCamera>.Instance.cam;
            var canvasRect = canvas.GetComponent<RectTransform>().rect;
            var viewRect = ViewRectScreen();
            var viewportPoint = cam.ScreenToViewportPoint(screenPosition - viewRect.min);
            var canvasPos = canvasRect.size * viewportPoint / scale - offset;
            return canvasPos;
        }

        public Vector2 ScreenToUnscaledNodePosition(Vector2 screenPosition)
        {
            var cam = Globals<MainCamera>.Instance.cam;
            var canvasRect = canvas.GetComponent<RectTransform>().rect;
            var viewportPoint = cam.ScreenToViewportPoint(screenPosition);
            var canvasPos = canvasRect.size * viewportPoint;
            return canvasPos;
        }

        public Vector2 ScreenToNodeVector(Vector2 screenVector)
        {
            var cam = Globals<MainCamera>.Instance.cam;
            var canvasRect = canvas.GetComponent<RectTransform>().rect;
            var viewportVec = cam.ScreenToViewportPoint(screenVector);
            var canvasVec = canvasRect.size * viewportVec / scale;
            return canvasVec;
        }
    }
}