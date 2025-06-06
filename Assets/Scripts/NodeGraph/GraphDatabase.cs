using DSP;
using DTO;
using System.Collections.Generic;
using UnityEngine;

namespace NodeGraph
{
    public class GraphDatabase : MonoBehaviour
    {
        public PersistentBox<Dictionary<string, DTO.Graph>> graphs = new("graphs", new Dictionary<string, DTO.Graph>());

        public Dictionary<NodeResource, System.Func<AudioNode>> GetBuiltinNodeTypes() => new()
        {
            { new NodeResource("invalid", true), () => new EmptyNode() },
            { new NodeResource("float_binary", true), () => new FloatBinaryNode() },
            { new NodeResource("bool_binary", true), () => new BoolBinaryNode() },
            { new NodeResource("vibrato", true), () => Prelude.Vibrato() },
            { new NodeResource("adsr", true), () => new ADSR() },
            { new NodeResource("oscillator", true), () => new Oscillator() },
            { new NodeResource("const_float", true), () => new ConstFloatNode() },
            { new NodeResource("input", true), () => new GraphEdgeNode(true) },
            { new NodeResource("output", true), () => new GraphEdgeNode(false) },
        };

        private void Awake()
        {
            SceneSystem.AddListener(SceneSystem.EventType.BeforeSceneUnload, () =>
            {
                graphs.Detach();
            }, false);
        }

        public IEnumerable<KeyValuePair<string, DTO.Graph>> GetGraphs()
        {
            var dict = graphs.Value;
            return dict;
        }

        public bool TryGetGraph(string id, out DTO.Graph graph)
        {
            var dict = graphs.Value;
            return dict.TryGetValue(id, out graph);
        }

        public void SaveGraph(string id, DTO.Graph graph)
        {
            var dict = graphs.Value;
            dict[id] = graph;
            graphs.TriggerChange();
        }

        public void DeleteGraph(string id)
        {
            var dict = graphs.Value;
            if (dict.ContainsKey(id))
            {
                dict.Remove(id);
                graphs.TriggerChange();
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
                if (TryGetGraph(typeId.id, out var graph))
                {
                    success = graph.TryCreateAudioNode(this, visited, out audioNode);
                }
                visited.Remove(typeId);
                return success;
            }
        }
    }
}
