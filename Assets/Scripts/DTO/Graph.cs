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

    public class Graph
    {
        public List<Node> nodes;
        public List<Connection> connections;

        public Graph()
        {
            nodes = new();
            connections = new();
        }

        public Graph(List<Node> nodes, List<Connection> connections)
        {
            this.nodes = nodes;
            this.connections = connections;
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
