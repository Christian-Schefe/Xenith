using DSP;
using DTO;
using ReactiveData.App;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeGraph
{
    public class GraphDatabase : MonoBehaviour
    {
        public PersistentBox<Dictionary<string, Graph>> graphs = new("graphs", new Dictionary<string, Graph>());

        public Dictionary<NodeResource, System.Func<AudioNode>> GetBuiltinNodeTypes() => new()
        {
            { new NodeResource("invalid", true), () => new EmptyNode() },
            { new NodeResource("default_synth", true), () => Prelude.DefaultSynth() },
            { new NodeResource("float_binary", true), () => new FloatBinaryNode() },
            { new NodeResource("bool_binary", true), () => new BoolBinaryNode() },
            { new NodeResource("vibrato", true), () => Prelude.Vibrato() },
            { new NodeResource("adsr", true), () => new ADSR() },
            { new NodeResource("oscillator", true), () => new Oscillator() },
            { new NodeResource("const_float", true), () => new ConstFloatNode() },
            { new NodeResource("input", true), () => new GraphEdgeNode(true) },
            { new NodeResource("output", true), () => new GraphEdgeNode(false) },
            { new NodeResource("filter/butterworth", true), () => new ButterworthLowpassFilter() },
        };

        private void Awake()
        {
            SceneSystem.AddListener(SceneSystem.EventType.BeforeSceneUnload, () =>
            {
                graphs.Detach();
            }, false);
        }

        public IEnumerable<NodeResource> GetInstruments()
        {
            var allResources = GetBuiltinNodeTypes().Select(e => e.Key).Concat(graphs.Value.Keys.Select(id => new NodeResource(id, false)));
            return allResources.Where(IsInstrument);
        }

        public bool IsInstrument(NodeResource id)
        {
            if (!GetNodeFromTypeId(id, out var node)) return false;
            var inputs = node.BuildInputs();
            var outputs = node.BuildOutputs();
            if (inputs.Count != 2 || outputs.Count != 2) return false;
            if (inputs[0].Value.Type != ValueType.Float || inputs[1].Value.Type != ValueType.Bool) return false;
            return outputs[0].Value.Type == ValueType.Float && outputs[1].Value.Type == ValueType.Float;
        }

        public IEnumerable<KeyValuePair<string, Graph>> GetGraphs()
        {
            return graphs.Value;
        }

        public bool TryGetGraph(string id, out Graph graph)
        {
            var dict = graphs.Value;
            return dict.TryGetValue(id, out graph);
        }

        public void SaveGraph(string id, Graph graph)
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

        public bool GetNodeFromTypeId(NodeResource typeId, out AudioNode audioNode)
        {
            var set = new HashSet<NodeResource>();
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
                    success = DTOConverter.Deserialize(typeId.id, graph).TryCreateAudioNode(this, visited, out audioNode);
                }
                visited.Remove(typeId);
                return success;
            }
        }
    }
}
