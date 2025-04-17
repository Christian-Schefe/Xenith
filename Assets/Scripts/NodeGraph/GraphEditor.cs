using DSP;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeGraph
{
    public class GraphEditor : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform contentParent;
        [SerializeField] private RectTransform nodeParent, connectionParent;
        [SerializeField] private AddNodeDialog addNodeDialog;

        public GraphNode nodePrefab;
        public GraphConnection connectionPrefab;

        public NodeIOLabel inputLabelPrefab;
        public NodeIOLabel outputLabelPrefab;

        private GraphConnection currentlyConnecting;
        private HashSet<(GraphNode, int)> connectionTargets = new();

        private List<GraphNode> nodes = new();
        private HashSet<GraphConnection> connections = new();

        private Dictionary<GraphNode, HashSet<GraphConnection>> incomingConnections = new();
        private Dictionary<GraphNode, HashSet<GraphConnection>> outgoingConnections = new();

        private float scale = 1;
        private Vector2 offset = Vector2.zero;

        private float scrollAccumulator = 0;
        private float scrollThreshold = 0.1f;
        private float scrollStep = 1.1f;

        private HashSet<GraphNode> selectedNodes = new();

        private Dictionary<GraphNode, Vector2> dragOffsets = new();

        public Dictionary<NodeResource, System.Func<AudioNode>> GetBuiltinNodeTypes() => new()
        {
            { new NodeResource("Add", "add", true), () => Prelude.Add(2) },
            { new NodeResource("Multiply", "multiply", true), () => Prelude.Multiply(2) },
            { new NodeResource("Vibrato", "vibrato", true), () => Prelude.Vibrato(0.5f) },
            { new NodeResource("ADSR", "adsr", true), () => new ADSR(0.1f, 0.1f, 0.1f, 0.1f) },
            { new NodeResource("Oscillator", "oscillator", true), () => new Oscillator() },
            { new NodeResource("Float", "const_float", true), () => new ConstFloatNode() },
            { new NodeResource("Input", "input", true), () => new GraphEdgeNode(true) },
            { new NodeResource("Output", "output", true), () => new GraphEdgeNode(false) },
        };

        public List<NodeResource> GetAllAvailableNodes()
        {
            var builtIns = GetBuiltinNodeTypes();
            var allNodes = new List<NodeResource>();
            foreach (var kvp in builtIns)
            {
                allNodes.Add(kvp.Key);
            }
            return allNodes;
        }

        public AudioNode GetNodeFromTypeId(NodeResource typeId)
        {
            if (typeId.builtIn)
            {
                var builtIns = GetBuiltinNodeTypes();
                if (builtIns.TryGetValue(typeId, out var factory))
                {
                    return factory();
                }
                else
                {
                    throw new System.Exception($"Unknown built-in node type: {typeId}");
                }
            }
            else
            {
                throw new System.NotImplementedException();
            }
        }

        private void Update()
        {
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
                    RemoveNode(node);
                }
                selectedNodes.Clear();
            }
        }

        public void AddSelectedNode(GraphNode node, bool keepPrevious)
        {
            if (!keepPrevious)
            {
                foreach (var selectedNode in selectedNodes)
                {
                    selectedNode.SetSelected(false);
                }
                selectedNodes.Clear();
            }
            selectedNodes.Add(node);
            node.SetSelected(true);
        }

        public void SelectWithinRect(Rect rect, bool keepPrevious)
        {
            if (!keepPrevious)
            {
                foreach (var selectedNode in selectedNodes)
                {
                    selectedNode.SetSelected(false);
                }
                selectedNodes.Clear();
            }

            foreach (var node in nodes)
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

        public void AddNode(Vector2 position, NodeResource type)
        {
            var node = Instantiate(nodePrefab, nodeParent);
            node.Initialize(type, position, null);
            nodes.Add(node);
        }

        public void RemoveNode(GraphNode node)
        {
            BreakAllConnections(node);
            nodes.Remove(node);
            Destroy(node.gameObject);
        }

        public void BreakAllConnections(GraphNode node)
        {
            var incoming = incomingConnections.GetValueOrDefault(node, new());
            var outgoing = outgoingConnections.GetValueOrDefault(node, new());
            var allConnections = incoming.ToList();
            allConnections.AddRange(outgoing);

            foreach (var conn in allConnections)
            {
                RemoveConnection(conn);
                Destroy(conn.gameObject);
            }
        }

        public void BreakInvalidConnections(GraphNode node)
        {
            var incoming = incomingConnections.GetValueOrDefault(node, new());
            var outgoing = outgoingConnections.GetValueOrDefault(node, new());
            var allConnections = incoming.ToList();
            allConnections.AddRange(outgoing);

            foreach (var conn in allConnections)
            {
                if (!IsValidExistingConnection(conn.fromNode, conn.toNode, conn.fromNodeOutput, conn.toNodeInput))
                {
                    RemoveConnection(conn);
                    Destroy(conn.gameObject);
                }
            }
        }

        public bool TryPickConnection(GraphNode to, int index)
        {
            if (currentlyConnecting != null)
            {
                throw new System.Exception("Already connecting");
            }
            var incoming = incomingConnections.GetValueOrDefault(to);
            if (incoming == null || incoming.Count == 0)
            {
                return false;
            }
            var connection = incoming.FirstOrDefault(c => c.toNodeInput == index);
            if (connection == null)
            {
                return false;
            }
            RemoveConnection(connection);
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
            var validTargets = connectionTargets.Where(x => IsValidConnection(currentlyConnecting.fromNode, x.Item1, currentlyConnecting.fromNodeOutput, x.Item2)).ToList();
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
                AddConnection(currentlyConnecting);
                currentlyConnecting = null;
            }
        }


        private bool IsValidExistingConnection(GraphNode from, GraphNode to, int fromIndex, int toIndex)
        {
            if (!from.TryGetConnector(false, fromIndex, out var fromConnector)) return false;
            if (!to.TryGetConnector(true, toIndex, out var toConnector)) return false;

            if (fromConnector.type != toConnector.type)
            {
                return false;
            }

            bool ContainsUpstream(GraphNode node)
            {
                if (node == to) return true;

                var incoming = incomingConnections.GetValueOrDefault(node);
                if (incoming != null)
                {
                    foreach (var conn in incoming)
                    {
                        if (ContainsUpstream(conn.fromNode))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return !ContainsUpstream(from);
        }

        private bool IsValidConnection(GraphNode from, GraphNode to, int fromIndex, int toIndex)
        {
            if (incomingConnections.ContainsKey(to) && incomingConnections[to].Any(c => c.toNodeInput == toIndex))
            {
                return false;
            }
            return IsValidExistingConnection(from, to, fromIndex, toIndex);
        }

        private void AddConnection(GraphConnection connection)
        {
            if (!connections.Add(connection)) return;

            var from = connection.fromNode;
            var to = connection.toNode;
            if (!incomingConnections.ContainsKey(to))
            {
                incomingConnections[to] = new();
            }
            incomingConnections[to].Add(connection);
            if (!outgoingConnections.ContainsKey(from))
            {
                outgoingConnections[from] = new();
            }
            outgoingConnections[from].Add(connection);
        }

        private void RemoveConnection(GraphConnection connection)
        {
            if (!connections.Remove(connection)) return;

            var from = connection.fromNode;
            var to = connection.toNode;
            if (incomingConnections.ContainsKey(to))
            {
                incomingConnections[to].Remove(connection);
                if (incomingConnections[to].Count == 0)
                {
                    incomingConnections.Remove(to);
                }
            }
            if (outgoingConnections.ContainsKey(from))
            {
                outgoingConnections[from].Remove(connection);
                if (outgoingConnections[from].Count == 0)
                {
                    outgoingConnections.Remove(from);
                }
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

        public SerializedGraph Serialize()
        {
            var serializedNodes = new List<SerializedGraphNode>();
            var nodeToIndex = new Dictionary<GraphNode, int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                nodeToIndex[node] = i;
                serializedNodes.Add(node.Serialize());
            }
            var serializedConnections = new List<SerializedGraphConnection>();
            foreach (var connection in connections)
            {
                serializedConnections.Add(connection.Serialize(nodeToIndex));
            }
            return new SerializedGraph
            {
                nodes = serializedNodes,
                connections = serializedConnections
            };
        }

        public void Deserialize(SerializedGraph graph)
        {
            foreach (var node in graph.nodes)
            {
                var nodeObj = Instantiate(nodePrefab, nodeParent);
                nodeObj.Deserialize(node);
                nodes.Add(nodeObj);
            }
            foreach (var connection in graph.connections)
            {
                var connectionObj = Instantiate(connectionPrefab, connectionParent);
                connectionObj.Deserialize(nodes, connection);
                AddConnection(connectionObj);
            }
        }
    }

    public struct SerializedGraph
    {
        public List<SerializedGraphNode> nodes;
        public List<SerializedGraphConnection> connections;
    }
}