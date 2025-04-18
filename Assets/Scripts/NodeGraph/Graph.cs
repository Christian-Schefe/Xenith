using DSP;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeGraph
{
    public class Graph : MonoBehaviour
    {
        private List<GraphNode> nodes = new();
        private HashSet<GraphConnection> connections = new();

        private Dictionary<GraphNode, HashSet<GraphConnection>> incomingConnections = new();
        private Dictionary<GraphNode, HashSet<GraphConnection>> outgoingConnections = new();

        public NodeResource id;

        public void Initialize(NodeResource id)
        {
            this.id = id;
        }

        public List<GraphNode> GetNodes()
        {
            return nodes;
        }

        public List<GraphConnection> GetConnections(GraphNode node, bool incoming, bool outgoing)
        {
            var result = new List<GraphConnection>();
            if (incoming && incomingConnections.ContainsKey(node))
            {
                result.AddRange(incomingConnections[node]);
            }
            if (outgoing && outgoingConnections.ContainsKey(node))
            {
                result.AddRange(outgoingConnections[node]);
            }
            return result;
        }

        public void AddNode(Vector2 position, NodeResource type)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            var node = graphEditor.CreateNodeInstance();
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

        public GraphNode DuplicateNode(GraphNode node)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            var newNode = graphEditor.CreateNodeInstance();
            var serializedNode = node.Serialize();
            serializedNode.position += new Vector2(30, -30);
            newNode.Deserialize(serializedNode);
            nodes.Add(newNode);
            return newNode;
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

        public bool IsValidConnection(GraphNode from, GraphNode to, int fromIndex, int toIndex)
        {
            if (incomingConnections.ContainsKey(to) && incomingConnections[to].Any(c => c.toNodeInput == toIndex))
            {
                return false;
            }
            return IsValidExistingConnection(from, to, fromIndex, toIndex);
        }

        public void AddConnection(GraphConnection connection)
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

        public void RemoveConnection(GraphConnection connection)
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

        public void DestroySelf()
        {
            foreach (var node in nodes)
            {
                Destroy(node.gameObject);
            }
            nodes.Clear();
            foreach (var connection in connections)
            {
                Destroy(connection.gameObject);
            }
            connections.Clear();
            incomingConnections.Clear();
            outgoingConnections.Clear();
            Destroy(gameObject);
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
                connections = serializedConnections,
                id = id
            };
        }

        public void Deserialize(SerializedGraph graph)
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            Initialize(graph.id);
            foreach (var node in graph.nodes)
            {
                var nodeObj = graphEditor.CreateNodeInstance();
                nodeObj.Deserialize(node);
                nodes.Add(nodeObj);
            }
            foreach (var connection in graph.connections)
            {
                var connectionObj = graphEditor.CreateConnectionInstance();
                connectionObj.Deserialize(nodes, connection);
                AddConnection(connectionObj);
            }
            foreach (var node in nodes)
            {
                BreakInvalidConnections(node);
            }
        }
    }

    public struct SerializedGraph
    {
        public NodeResource id;
        public List<SerializedGraphNode> nodes;
        public List<SerializedGraphConnection> connections;

        public readonly bool TryCreateAudioNode(GraphEditor graphEditor, HashSet<NodeResource> visited, out AudioNode audioNode)
        {
            var graph = new DSP.NodeGraph();
            audioNode = graph;

            foreach (var node in nodes)
            {
                if (!graphEditor.GetNodeFromTypeIdInternal(node.id, visited, out var innerNode))
                {
                    return false;
                }
                if (innerNode is SettingsNode settingsNode)
                {
                    settingsNode.DeserializeSettings(node.serializedSettings);
                }
                graph.AddNode(innerNode);
            }
            foreach (var connection in connections)
            {
                graph.AddConnection(new(connection.fromNodeIndex, connection.fromNodeOutput, connection.toNodeIndex, connection.toNodeInput));
            }

            return true;
        }
    }
}
