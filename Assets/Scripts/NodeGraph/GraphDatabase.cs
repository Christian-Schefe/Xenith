using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class GraphDatabase : MonoBehaviour
    {
        public PersistentBox<Dictionary<NodeResource, SerializedGraph>> graphs = new("graphs", new Dictionary<NodeResource, SerializedGraph>());

        public IEnumerable<SerializedGraph> GetGraphs()
        {
            var dict = graphs.Value;
            return dict.Values;
        }

        public bool TryGetGraph(NodeResource id, out SerializedGraph graph)
        {
            var dict = graphs.Value;
            return dict.TryGetValue(id, out graph);
        }

        public void SaveGraph(SerializedGraph graph)
        {
            var dict = graphs.Value;
            dict[graph.id] = graph;
            graphs.TriggerChange();
        }

        public void DeleteGraph(NodeResource id)
        {
            var dict = graphs.Value;
            if (dict.ContainsKey(id))
            {
                dict.Remove(id);
                graphs.TriggerChange();
            }
        }

        public void RenameGraph(NodeResource oldId, NodeResource newId)
        {
            var dict = graphs.Value;
            var graph = dict[oldId];
            dict.Add(newId, graph);
            dict.Remove(oldId);
            graph.id = newId;

            foreach (var otherGraph in dict)
            {
                var nodes = otherGraph.Value.nodes;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i].id == oldId)
                    {
                        nodes[i] = nodes[i].WithId(newId);
                    }
                }
            }
        }
    }
}
