using DSP;
using DTO;
using ReactiveData.App;
using ReactiveData.Core;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
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

        public ReactiveGraph graph;

        private ReactiveUIBinder<ReactiveNode, GraphNode> nodeBinder;
        private ReactiveUIBinder<ReactiveConnection, GraphConnection> connectionBinder;

        private void Awake()
        {
            var main = Globals<Main>.Instance;
            main.app.openElement.AddAndCall(OnOpenElementChanged);
        }

        public GraphNode GetNode(ReactiveNode node)
        {
            return nodeBinder.TryGet(node, out GraphNode graphNode) ? graphNode : null;
        }

        private void InitializeBinders()
        {
            nodeBinder ??= new ReactiveUIBinder<ReactiveNode, GraphNode>(null, node => Instantiate(nodePrefab, nodeParent), node => Destroy(node.gameObject));
            connectionBinder ??= new ReactiveUIBinder<ReactiveConnection, GraphConnection>(null, connection => Instantiate(connectionPrefab, connectionParent), connection => Destroy(connection.gameObject));
        }

        private void BindGraph(ReactiveGraph graph)
        {
            this.graph = graph;
            InitializeBinders();
            nodeBinder.ChangeSource(graph.nodes);
            connectionBinder.ChangeSource(graph.connections);
        }

        private void UnbindGraph()
        {
            graph = null;
            InitializeBinders();
            nodeBinder.ChangeSource(null);
            connectionBinder.ChangeSource(null);
        }

        private void OnOpenElementChanged(Nand<ReactiveSong, ReactiveGraph> openElement)
        {
            bool visible = openElement.TryGet(out ReactiveGraph graph);
            viewFrame.gameObject.SetActive(visible);
            if (visible)
            {
                BindGraph(graph);
            }
            else
            {
                UnbindGraph();
            }
            addNodeDialog.Close();
            selectedNodes.Clear();
            currentlyConnecting.Hide();
            connectionTargets.Clear();
            dragOffsets.Clear();
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
                var resource = new NodeResource(id, false);
                if (!GetNodeFromTypeId(resource, out _)) continue;
                allNodes.Add(resource);
            }
            return allNodes;
        }

        public bool GetNodeFromTypeId(NodeResource typeId, out AudioNode audioNode)
        {
            return graphDatabase.GetNodeFromTypeId(typeId, out audioNode);
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
                    graph.nodes.Remove(node.node);
                }
                selectedNodes.Clear();
            }

            if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
            {
                var duplicatedNodes = new List<ReactiveNode>();
                foreach (var node in selectedNodes.ToList())
                {
                    var newNode = new ReactiveNode(node.node.position.Value, node.node.id.Value, node.node.settings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()));
                    graph.nodes.Add(newNode);
                    duplicatedNodes.Add(newNode);
                }
                DeselectAll();
                foreach (var node in duplicatedNodes)
                {
                    var graphNode = GetNode(node);
                    AddSelectedNode(graphNode, true);
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

            foreach (var node in nodeBinder.UIElements)
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
            var incoming = graph.connections.Where(c => c.toNode.Value == to.node && c.toIndex.Value == index)
                .ToList();
            if (incoming == null || incoming.Count == 0)
            {
                return false;
            }
            var connection = incoming.FirstOrDefault(c => c.toIndex.Value == index);
            if (connection == null)
            {
                return false;
            }
            var fromNode = GetNode(connection.fromNode.Value);
            currentlyConnecting.Show(fromNode, connection.fromIndex.Value);
            graph.connections.Remove(connection);
            connectionTargets.Clear();
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
        }

        public void ReleaseConnection()
        {
            if (!IsConnecting())
            {
                throw new System.Exception("Not connecting");
            }
            var validTargets = connectionTargets.Where(x => IsValidConnection(currentlyConnecting.fromNode.node, x.Item1.node, currentlyConnecting.fromNodeOutput, x.Item2)).ToList();
            if (validTargets.Count == 0)
            {
                currentlyConnecting.Hide();
            }
            else
            {
                var (node, index) = validTargets[0];
                graph.connections.Add(new(currentlyConnecting.fromNode.node, node.node, currentlyConnecting.fromNodeOutput, index));
                currentlyConnecting.Hide();
            }
        }


        public void BreakAllConnections(GraphNode node)
        {
            var allConnections = graph.connections.Where(c => c.toNode.Value == node.node || c.fromNode.Value == node.node).ToList();
            foreach (var conn in allConnections)
            {
                graph.connections.Remove(conn);
            }
        }

        public void BreakInvalidConnections(GraphNode node)
        {
            var allConnections = graph.connections.Where(c => c.toNode.Value == node.node || c.fromNode.Value == node.node).ToList();

            foreach (var conn in allConnections)
            {
                if (!IsValidExistingConnection(conn.fromNode.Value, conn.toNode.Value, conn.fromIndex.Value, conn.toIndex.Value))
                {
                    graph.connections.Remove(conn);
                }
            }
        }

        private bool IsValidExistingConnection(ReactiveNode from, ReactiveNode to, int fromIndex, int toIndex)
        {
            var fromNode = GetNode(from);
            var toNode = GetNode(to);
            if (!fromNode.TryGetConnector(false, fromIndex, out var fromConnector)) return false;
            if (!toNode.TryGetConnector(true, toIndex, out var toConnector)) return false;

            if (fromConnector.type != toConnector.type)
            {
                return false;
            }

            var incomingConnectionMap = graph.connections
                .GroupBy(c => c.toNode.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            bool ContainsUpstream(ReactiveNode node)
            {
                if (node == to) return true;

                var incoming = incomingConnectionMap.GetValueOrDefault(node);
                if (incoming != null)
                {
                    foreach (var conn in incoming)
                    {
                        if (ContainsUpstream(conn.fromNode.Value))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return !ContainsUpstream(from);
        }

        public bool IsValidConnection(ReactiveNode from, ReactiveNode to, int fromIndex, int toIndex)
        {
            if (graph.connections.Any(c => c.toNode.Value == to && c.toIndex.Value == toIndex))
            {
                return false;
            }
            return IsValidExistingConnection(from, to, fromIndex, toIndex);
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