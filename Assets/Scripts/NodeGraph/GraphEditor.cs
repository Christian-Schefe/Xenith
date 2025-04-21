using DSP;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeGraph
{
    public class GraphEditor : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private RectTransform nodeParent, connectionParent;
        [SerializeField] private AddNodeDialog addNodeDialog;
        [SerializeField] private GraphDatabase graphDatabase;
        [SerializeField] private GraphEditorUI ui;

        public Graph graphPrefab;
        public GraphNode nodePrefab;
        public GraphConnection connectionPrefab;

        public NodeIOLabel inputLabelPrefab;
        public NodeIOLabel outputLabelPrefab;

        private GraphConnection currentlyConnecting;
        private readonly HashSet<(GraphNode, int)> connectionTargets = new();
        private readonly HashSet<GraphNode> selectedNodes = new();
        private readonly Dictionary<GraphNode, Vector2> dragOffsets = new();

        private float scale = 1;
        private Vector2 offset = Vector2.zero;

        private float scrollAccumulator = 0;
        private readonly float scrollThreshold = 0.1f;
        private readonly float scrollStep = 1.1f;
        private Graph graph;

        public Graph Graph => graph;


        public List<NodeResource> GetPlaceableNodes()
        {
            var builtIns = graphDatabase.GetBuiltinNodeTypes();
            var allNodes = new List<NodeResource>();
            foreach (var kvp in builtIns)
            {
                if (kvp.Key.id == "invalid") continue;
                allNodes.Add(kvp.Key);
            }
            foreach (var graph in graphDatabase.GetGraphs())
            {
                if (!GetNodeFromTypeId(graph.id, out _)) continue;
                allNodes.Add(graph.id);
            }
            return allNodes;
        }

        public bool GetNodeFromTypeId(NodeResource typeId, out AudioNode audioNode)
        {
            return graphDatabase.GetNodeFromTypeId(typeId, graph.id, out audioNode);
        }

        public bool IsInteractable()
        {
            return graph != null && !ui.IsDialogOpen();
        }

        public bool TryGetGraphDisplayName(out string displayName)
        {
            if (graph == null)
            {
                displayName = null;
                return false;
            }
            displayName = graph.id.displayName;
            return true;
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

        public void DeleteGraph()
        {
            if (graph == null) return;
            graphDatabase.DeleteGraph(graph.id);
            CloseGraph();
        }

        public void CloseGraph()
        {
            addNodeDialog.Close();
            graph.DestroySelf();
            selectedNodes.Clear();
            if (currentlyConnecting != null)
            {
                Destroy(currentlyConnecting.gameObject);
            }
            currentlyConnecting = null;
            connectionTargets.Clear();
            dragOffsets.Clear();
            graph = null;
        }

        public void OpenGraph(NodeResource graphId)
        {
            if (graph != null)
            {
                SaveGraph();
                CloseGraph();
            }
            if (!graphDatabase.TryGetGraph(graphId, out var loadedGraph))
            {
                throw new System.Exception($"Failed to load graph: {graphId}.");
            }
            graph = Instantiate(graphPrefab, contentParent);
            graph.Deserialize(loadedGraph);
        }

        public void NewGraph(NodeResource id)
        {
            if (graph != null)
            {
                SaveGraph();
                CloseGraph();
            }
            graph = Instantiate(graphPrefab, contentParent);
            graph.Initialize(id);
        }

        public void SaveGraph()
        {
            if (graph == null) return;
            var serializedGraph = graph.Serialize();
            graphDatabase.SaveGraph(serializedGraph);
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
                var offset = mousePos - node.position;
                dragOffsets.Add(node, offset);
            }
        }

        public void MoveSelectedNodes(Vector2 mousePos)
        {
            foreach (var node in selectedNodes)
            {
                if (!dragOffsets.ContainsKey(node)) continue;
                var pos = mousePos - dragOffsets[node];
                node.position = pos;
                node.rectTransform.localPosition = pos;
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
            if (currentlyConnecting != null)
            {
                throw new System.Exception("Already connecting");
            }
            var incoming = graph.GetConnections(to, true, false);
            if (incoming == null || incoming.Count == 0)
            {
                return false;
            }
            var connection = incoming.FirstOrDefault(c => c.toNodeInput == index);
            if (connection == null)
            {
                return false;
            }
            graph.RemoveConnection(connection);
            currentlyConnecting = connection;
            currentlyConnecting.UnsetConnection(false);
            currentlyConnecting.connectToMouse = true;
            connectionTargets.Clear();
            return true;
        }

        public void StartConnection(GraphNode from, int index)
        {
            if (currentlyConnecting != null)
            {
                throw new System.Exception("Already connecting");
            }
            currentlyConnecting = Instantiate(connectionPrefab, connectionParent);
            currentlyConnecting.SetConnection(true, from, index);
            currentlyConnecting.connectToMouse = true;
            connectionTargets.Clear();
        }

        public void ReleaseConnection()
        {
            if (currentlyConnecting == null)
            {
                throw new System.Exception("Not connecting");
            }
            var validTargets = connectionTargets.Where(x => graph.IsValidConnection(currentlyConnecting.fromNode, x.Item1, currentlyConnecting.fromNodeOutput, x.Item2)).ToList();
            if (validTargets.Count == 0)
            {
                Destroy(currentlyConnecting.gameObject);
                currentlyConnecting = null;
            }
            else
            {
                var (node, index) = validTargets[0];
                currentlyConnecting.SetConnection(false, node, index);
                currentlyConnecting.connectToMouse = false;
                graph.AddConnection(currentlyConnecting);
                currentlyConnecting = null;
            }
        }


        public bool IsConnecting()
        {
            return currentlyConnecting != null;
        }

        public void AddConnectionTarget(GraphNode to, int index)
        {
            connectionTargets.Add((to, index));
        }

        public void RemoveConnectionTarget(GraphNode to, int index)
        {
            connectionTargets.Remove((to, index));
        }

        public Vector2 ScreenToNodePosition(Vector2 screenPosition)
        {
            var cam = Globals<MainCamera>.Instance.cam;
            var canvasRect = canvas.GetComponent<RectTransform>().rect;
            var viewportPoint = cam.ScreenToViewportPoint(screenPosition);
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