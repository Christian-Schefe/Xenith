using DSP;
using NodeGraph;
using System.Collections.Generic;
using UnityEngine;

namespace DTO
{
    public struct NodeResource
    {
        public string id;
        public bool builtIn;

        public NodeResource(string id, bool builtIn)
        {
            this.id = id;
            this.builtIn = builtIn;
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is NodeResource other)
            {
                return id == other.id && builtIn == other.builtIn;
            }
            return false;
        }

        public override readonly int GetHashCode()
        {
            return System.HashCode.Combine(id, builtIn);
        }

        public static bool operator ==(NodeResource a, NodeResource b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(NodeResource a, NodeResource b)
        {
            return !a.Equals(b);
        }
    }

    public class GraphID
    {
        public string path;

        public GraphID() { }

        public GraphID(string path)
        {
            this.path = path;
        }

        public virtual string GetName()
        {
            return path;
        }

        public override bool Equals(object obj)
        {
            if (obj is GraphID other)
            {
                return path == other.path;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return path.GetHashCode();
        }

        public virtual NodeResource ToResource()
        {
            return new NodeResource(path, false);
        }
    }

    public class UnsavedGraphID : GraphID
    {
        public int index;

        public UnsavedGraphID(int index) : base(null)
        {
            this.index = index;
        }

        public override string GetName()
        {
            return $"Untitled {index}";
        }

        public override bool Equals(object obj)
        {
            if (obj is UnsavedGraphID other)
            {
                return index == other.index;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return index.GetHashCode();
        }

        public override NodeResource ToResource()
        {
            throw new System.InvalidOperationException("UnsavedGraphID cannot be converted to NodeResource");
        }
    }

    public class Graph
    {
        public List<Node> nodes;
        public List<Connection> connections;

        public Graph()
        {
            nodes = new();
            connections = new();
        }

        public bool TryCreateAudioNode(GraphDatabase graphDatabase, HashSet<NodeResource> visited, out AudioNode audioNode)
        {
            var graph = new DSP.NodeGraph();
            audioNode = graph;

            foreach (var node in nodes)
            {
                if (!graphDatabase.GetNodeFromTypeIdInternal(node.id, visited, out var innerNode))
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

    public class Node
    {
        public Vector2 position;
        public NodeResource id;
        public string serializedSettings;

        public Node() { }

        public Node(Vector2 position, NodeResource id, string serializedSettings)
        {
            this.position = position;
            this.id = id;
            this.serializedSettings = serializedSettings;
        }
    }

    public class Connection
    {
        public int fromNodeIndex;
        public int toNodeIndex;
        public int fromNodeOutput;
        public int toNodeInput;

        public Connection() { }

        public Connection(int fromNodeIndex, int fromNodeOutput, int toNodeIndex, int toNodeInput)
        {
            this.fromNodeIndex = fromNodeIndex;
            this.fromNodeOutput = fromNodeOutput;
            this.toNodeIndex = toNodeIndex;
            this.toNodeInput = toNodeInput;
        }
    }
}
