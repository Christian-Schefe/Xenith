using DTO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeGraph
{
    public class Graph : MonoBehaviour
    {
        private readonly List<GraphNode> nodes = new();
        private readonly HashSet<GraphConnection> connections = new();

        private readonly Dictionary<GraphNode, HashSet<GraphConnection>> incomingConnections = new();
        private readonly Dictionary<GraphNode, HashSet<GraphConnection>> outgoingConnections = new();

        public DTO.Graph openGraph;

        public List<GraphNode> GetNodes()
        {
            return nodes;
        }

        public Dictionary<GraphNode, int> GetNodeMap()
        {
            var nodeMap = new Dictionary<GraphNode, int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                nodeMap.Add(nodes[i], i);
            }
            return nodeMap;
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
            var dtoNode = new Node(position, type, null);
            node.Initialize(dtoNode);
            nodes.Add(node);
            openGraph.nodes.Add(dtoNode);
            UpdateAllNodeIndices();
        }

        public void RemoveNode(GraphNode node)
        {
            BreakAllConnections(node);
            nodes.Remove(node);
            Destroy(node.gameObject);
            openGraph.nodes.Remove(node.node);
            UpdateAllNodeIndices();
        }

        public void UpdateAllNodeIndices()
        {
            var indexMap = GetNodeMap();
            foreach (var connection in connections)
            {
                connection.UpdateNodeIndices(indexMap);
            }
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
                if (!IsValidExistingConnection(conn.fromNode, conn.toNode, conn.FromNodeOutput, conn.ToNodeInput))
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
            var dtoNode = new Node(node.Position, node.Id, node.SerializedSettings);
            dtoNode.position += new Vector2(30, -30);
            newNode.Initialize(dtoNode);
            nodes.Add(newNode);
            openGraph.nodes.Add(dtoNode);
            UpdateAllNodeIndices();
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
            if (incomingConnections.ContainsKey(to) && incomingConnections[to].Any(c => c.ToNodeInput == toIndex))
            {
                return false;
            }
            return IsValidExistingConnection(from, to, fromIndex, toIndex);
        }

        public void AddConnection(GraphConnection connection, bool isLoading = false)
        {
            if (!connections.Add(connection)) return;
            if (!isLoading)
            {
                openGraph.connections.Add(connection.connection);
            }

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
            openGraph.connections.Remove(connection.connection);

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

        public void ShowGraph(DTO.Graph graph)
        {
            openGraph = graph;
            LoadGraph();
        }

        private void LoadGraph()
        {
            var graphEditor = Globals<GraphEditor>.Instance;
            foreach (var node in openGraph.nodes)
            {
                var nodeObj = graphEditor.CreateNodeInstance();
                nodeObj.Initialize(node);
                nodes.Add(nodeObj);
            }
            foreach (var connection in openGraph.connections)
            {
                var connectionObj = graphEditor.CreateConnectionInstance();
                connectionObj.Initialize(nodes, connection);
                AddConnection(connectionObj, true);
            }
            foreach (var node in nodes)
            {
                BreakInvalidConnections(node);
            }
        }

        public void HideGraph()
        {
            openGraph = null;

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
        }
    }
}
