using DSP;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class GraphDatabase : MonoBehaviour
    {
        public PersistentBox<Dictionary<NodeResource, SerializedGraph>> graphs = new("graphs", new Dictionary<NodeResource, SerializedGraph>());

        public Dictionary<NodeResource, System.Func<AudioNode>> GetBuiltinNodeTypes() => new()
        {
            { new NodeResource("Invalid", "invalid", true), () => new EmptyNode() },
            { new NodeResource("Add", "add", true), () => Prelude.Add(2) },
            { new NodeResource("Multiply", "multiply", true), () => Prelude.Multiply(2) },
            { new NodeResource("Vibrato", "vibrato", true), () => Prelude.Vibrato(0.5f) },
            { new NodeResource("ADSR", "adsr", true), () => new ADSR() },
            { new NodeResource("Oscillator", "oscillator", true), () => new Oscillator() },
            { new NodeResource("Float", "const_float", true), () => new ConstFloatNode() },
            { new NodeResource("Input", "input", true), () => new GraphEdgeNode(true) },
            { new NodeResource("Output", "output", true), () => new GraphEdgeNode(false) },
        };

        private void Awake()
        {
            SceneSystem.AddListener(SceneSystem.EventType.BeforeSceneUnload, () =>
            {
                graphs.Detach();
            }, false);
        }

        public IEnumerable<SerializedGraph> GetGraphs()
        {
            var dict = graphs.Value;
            return dict.Values;
        }

        public bool TryGetGraph(NodeResource id, out SerializedGraph graph)
        {
            var dict = graphs.Value;
            Debug.Log($"Key: {id}, contains: {dict.ContainsKey(id)}");
            foreach (var pair in dict)
            {
                Debug.Log($"Key: {pair.Key}");
            }
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

        public bool GetNodeFromTypeId(NodeResource typeId, NodeResource? origin, out AudioNode audioNode)
        {
            var set = new HashSet<NodeResource>();
            if (origin != null)
            {
                set.Add(origin.Value);
            }
            return GetNodeFromTypeIdInternal(typeId, set, out audioNode);
        }

        public bool GetNodeFromTypeIdInternal(NodeResource typeId, HashSet<NodeResource> visited, out AudioNode audioNode)
        {
            audioNode = null;

            if (typeId.builtIn)
            {
                var builtIns = GetBuiltinNodeTypes();
                if (builtIns.TryGetValue(typeId, out var factory))
                {
                    audioNode = factory();
                    return true;
                }
                return false;
            }
            else
            {
                if (visited.Contains(typeId))
                {
                    audioNode = null;
                    return false;
                }
                visited.Add(typeId);
                bool success = false;
                if (TryGetGraph(typeId, out var graph))
                {
                    success = graph.TryCreateAudioNode(this, visited, out audioNode);
                }
                visited.Remove(typeId);
                return success;
            }
        }
    }
}
