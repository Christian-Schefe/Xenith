using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.StandaloneInputModule;

namespace DSP
{
    public class NodeGraph : AudioNode
    {
        private readonly List<AudioNode> nodes;
        private readonly List<Connection> connections;

        private List<int> inputNodes;
        private List<int> outputNodes;

        private List<int> executionOrder;
        private List<Connection>[] incomingConnections;
        private List<Connection>[] outgoingConnections;

        public NodeGraph()
        {
            nodes = new();
            connections = new();
            inputNodes = new();
            outputNodes = new();
        }

        public int AddInput<T>(string name) where T : Value, new()
        {
            var inputNode = new GraphEdgeNode<T>(true);
            inputNode.nameSetting.value = name;
            inputNode.OnSettingsChanged();
            int index = AddNode(inputNode);
            inputNodes.Add(index);
            return index;
        }

        public int AddOutput<T>(string name) where T : Value, new()
        {
            var outputNode = new GraphEdgeNode<T>(false);
            outputNode.nameSetting.value = name;
            outputNode.OnSettingsChanged();
            int index = AddNode(outputNode);
            outputNodes.Add(index);
            return index;
        }

        public int AddNode(AudioNode node)
        {
            nodes.Add(node);
            return nodes.Count - 1;
        }

        public void AddConnection(Connection connection)
        {
            connections.Add(connection);
        }

        public override List<NamedValue> BuildInputs()
        {
            var inputs = new List<NamedValue>();
            foreach (var i in inputNodes)
            {
                var inputNode = (IGraphEdgeNode)nodes[i];
                inputs.Add(inputNode.GetValue);
            }
            return inputs;
        }

        public override List<NamedValue> BuildOutputs()
        {
            var outputs = new List<NamedValue>();
            foreach (var node in outputNodes)
            {
                var outputNode = (IGraphEdgeNode)nodes[node];
                outputs.Add(outputNode.GetValue);
            }
            return outputs;
        }

        private void BuildConnectionMap()
        {
            incomingConnections = new List<Connection>[nodes.Count];
            outgoingConnections = new List<Connection>[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                incomingConnections[i] = new List<Connection>();
                outgoingConnections[i] = new List<Connection>();
            }
            foreach (var connection in connections)
            {
                incomingConnections[connection.toNode].Add(connection);
                outgoingConnections[connection.fromNode].Add(connection);
            }
        }

        private void ValidateNodes()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var coveredInputs = new bool[node.inputs.Count];
                var coveredOutputs = new bool[node.outputs.Count];

                var incoming = incomingConnections[i];
                var outgoing = outgoingConnections[i];
                foreach (var connection in incoming)
                {
                    if (connection.toInput >= node.inputs.Count)
                    {
                        throw new System.Exception($"Node {i} has invalid connection from {connection.fromNode} to input {connection.toInput}.");
                    }
                    if (coveredInputs[connection.toInput])
                    {
                        throw new System.Exception($"Node {i} has multiple connections to input {connection.toInput}.");
                    }
                    coveredInputs[connection.toInput] = true;
                }
                foreach (var connection in outgoing)
                {
                    if (connection.fromOutput >= node.outputs.Count)
                    {
                        throw new System.Exception($"Node {i} has invalid connection to {connection.toNode} from output {connection.fromOutput}.");
                    }
                    coveredOutputs[connection.fromOutput] = true;
                }
                for (int j = 0; j < node.inputs.Count; j++)
                {
                    if (!coveredInputs[j])
                    {
                        throw new System.Exception($"Node {j} has unconnected input {j}.");
                    }
                }
                bool anyOutput = node.outputs.Count == 0;
                for (int j = 0; j < node.outputs.Count; j++)
                {
                    if (coveredOutputs[j])
                    {
                        anyOutput = true;
                        break;
                    }
                }
                if (!anyOutput)
                {
                    throw new System.Exception($"Node {i} has no connected outputs.");
                }
            }
        }

        private void ConstructExecutionOrder()
        {
            executionOrder = new();
            var visited = new bool[nodes.Count];
            var currentlyVisiting = new bool[nodes.Count];

            void DFS(int index)
            {
                if (visited[index]) return;
                if (currentlyVisiting[index])
                {
                    throw new System.Exception("Graph contains a cycle.");
                }
                currentlyVisiting[index] = true;
                var incoming = incomingConnections[index];

                foreach (var connection in incoming)
                {
                    DFS(connection.fromNode);
                }
                currentlyVisiting[index] = false;
                visited[index] = true;

                executionOrder.Add(index);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                DFS(i);
            }

            foreach (var i in executionOrder)
            {
                Debug.Log($"Execution Order: {i}");
            }
        }

        public override void Initialize()
        {
            foreach (var node in nodes)
            {
                node.Initialize();
            }

            BuildConnectionMap();
            ValidateNodes();
            ConstructExecutionOrder();

            base.Initialize();
        }

        public override void Process(Context context)
        {
            foreach (var index in executionOrder)
            {
                var node = nodes[index];
                var incoming = incomingConnections[index];
                foreach (var connection in incoming)
                {
                    var fromValue = nodes[connection.fromNode].outputs[connection.fromOutput];
                    var toValue = node.inputs[connection.toInput];
                    toValue.Value.Set(fromValue.Value);
                }
                node.Process(context);
            }
        }

        public override void ResetState()
        {
            foreach (var node in nodes)
            {
                node.ResetState();
            }
        }
    }

    public class Connection
    {
        public int fromNode;
        public int fromOutput;
        public int toNode;
        public int toInput;

        public Connection(int fromNode, int fromOutput, int toNode, int toInput)
        {
            this.fromNode = fromNode;
            this.fromOutput = fromOutput;
            this.toNode = toNode;
            this.toInput = toInput;
        }
    }

    public interface IGraphEdgeNode
    {
        public NamedValue GetValue { get; }
    }

    public class GraphEdgeNode<T> : SettingsNode, IGraphEdgeNode where T : Value, new()
    {
        public NamedValue GetValue => isInput ? input : output;

        private readonly NamedValue<T> input;
        private readonly NamedValue<T> output;

        private readonly bool isInput;

        public readonly StringSetting nameSetting = new("Name", "Value");

        public override NodeSettings DefaultSettings => new(nameSetting);

        public GraphEdgeNode(bool isInput)
        {
            this.isInput = isInput;
            input = new NamedValue<T>("Input", new T());
            output = new NamedValue<T>("Output", new T());
        }

        public override void OnSettingsChanged()
        {
            input.name = nameSetting.value;
            output.name = nameSetting.value;
        }

        public override List<NamedValue> BuildInputs() => isInput ? new() { } : new() { input };

        public override List<NamedValue> BuildOutputs() => isInput ? new() { output } : new() { };

        public override void Process(Context context)
        {
            output.value.Set(input.value);
        }

        public override void ResetState() { }

    }
}
