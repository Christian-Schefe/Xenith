using DSP;
using DTO;
using NodeGraph;
using ReactiveData.Core;
using System;
using System.Collections.Generic;

namespace ReactiveData.App
{
    public class ReactiveGraph : IKeyed
    {
        public Reactive<string> path;
        public DerivedReactive<string, string> name;
        public ReactiveList<ReactiveNode> nodes;
        public ReactiveList<ReactiveConnection> connections;

        public ReactiveGraph(string path, IEnumerable<ReactiveNode> nodes, IEnumerable<ReactiveConnection> connections)
        {
            this.path = new(path);
            name = new DerivedReactive<string, string>(this.path, GetNameFromPath);
            this.nodes = new(nodes);
            this.connections = new(connections);
        }

        private string GetNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "New Graph";
            }
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public static ReactiveGraph Default => new(null, new List<ReactiveNode>(), new List<ReactiveConnection>());

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;

        public bool IsEmpty()
        {
            return nodes.Count == 0 && connections.Count == 0;
        }

        public bool TryCreateAudioNode(GraphDatabase graphDatabase, HashSet<NodeResource> visited, out AudioNode audioNode)
        {
            var graph = new DSP.NodeGraph();
            audioNode = graph;

            var indexMap = new Dictionary<ReactiveNode, int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                indexMap[node] = i;

                if (!graphDatabase.GetNodeFromTypeIdInternal(node.id.Value, visited, out var innerNode))
                {
                    return false;
                }
                if (innerNode is SettingsNode settingsNode)
                {
                    node.ApplySettings(settingsNode);
                }
                graph.AddNode(innerNode);
            }
            foreach (var connection in connections)
            {
                graph.AddConnection(new(indexMap[connection.fromNode.Value], connection.fromIndex.Value, indexMap[connection.toNode.Value], connection.toIndex.Value));
            }

            return true;
        }
    }
}
