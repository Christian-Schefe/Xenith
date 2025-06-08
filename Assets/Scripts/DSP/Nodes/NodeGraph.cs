using System.Collections.Generic;

namespace DSP
{
    public class NodeGraph : AudioNode
    {
        private readonly List<AudioNode> nodes = new();
        private readonly List<Connection> connections = new();

        private readonly List<int> inputNodes = new();
        private readonly List<int> outputNodes = new();

        private List<int> executionOrder;
        private List<Connection>[] incomingConnections;
        private List<Connection>[] outgoingConnections;

        public int AddInput<T>(string name, int index) where T : Value, new() => AddEdgeNode<T>(name, index, true);

        public int AddOutput<T>(string name, int index) where T : Value, new() => AddEdgeNode<T>(name, index, false);

        public int AddEdgeNode<T>(string name, int index, bool isInput) where T : Value, new()
        {
            var node = new GraphEdgeNode(isInput);
            node.valueTypeSetting.value = (int)new T().Type;
            node.nameSetting.value = name;
            node.indexSetting.value = index;
            node.OnSettingsChanged();
            return AddNode(node);
        }

        public int AddNode(AudioNode node)
        {
            var index = nodes.Count;
            nodes.Add(node);
            if (node is IGraphEdgeNode edgeNode)
            {
                var list = edgeNode.IsInput ? inputNodes : outputNodes;
                list.Add(index);
            }
            return index;
        }

        public void AddConnection(Connection connection)
        {
            connections.Add(connection);
        }

        public List<NamedValue> BuildInputsOrOutputs(bool isInput)
        {
            var nodeList = isInput ? inputNodes : outputNodes;

            var numDict = new Dictionary<int, IGraphEdgeNode>();
            foreach (var edgeNodeIndex in nodeList)
            {
                var edgeNode = (IGraphEdgeNode)nodes[edgeNodeIndex];
                var edgeIndex = edgeNode.Index;
                if (numDict.ContainsKey(edgeIndex))
                {
                    throw new System.Exception($"Duplicate {(isInput ? "Input" : "Output")} index {edgeIndex} found in node graph.");
                }
                if (edgeIndex >= nodeList.Count || edgeIndex < 0)
                {
                    throw new System.Exception($"Invalid {(isInput ? "Input" : "Output")} index {edgeIndex} found in node graph.");
                }
                numDict.Add(edgeIndex, edgeNode);
            }
            var values = new List<NamedValue>();
            for (int i = 0; i < nodeList.Count; i++)
            {
                var edgeNode = numDict[i];
                values.Add(edgeNode.GetValue);
            }
            return values;
        }

        public override List<NamedValue> BuildInputs()
        {
            return BuildInputsOrOutputs(true);
        }

        public override List<NamedValue> BuildOutputs()
        {
            return BuildInputsOrOutputs(false);
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

        public override AudioNode Clone()
        {
            var clone = new NodeGraph();
            foreach (var node in nodes)
            {
                clone.AddNode(node.Clone());
            }
            foreach (var connection in connections)
            {
                clone.AddConnection(new Connection(connection.fromNode, connection.fromOutput, connection.toNode, connection.toInput));
            }
            return clone;
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
        public bool IsInput { get; }
        public int Index { get; }
    }

    public class GraphEdgeNode : SettingsNode, IGraphEdgeNode
    {
        public NamedValue GetValue => value;
        public bool IsInput => isInput;
        public int Index => indexSetting.value;

        private NamedValue value;

        private readonly bool isInput;

        public readonly EnumSetting<ValueType> valueTypeSetting = new("Value Variant", ValueType.Float);
        public readonly StringSetting nameSetting = new("Name", "Value");
        public readonly IntSetting indexSetting = new("Index", 0);

        public override List<NodeSetting> DefaultSettings => new() { nameSetting, valueTypeSetting, indexSetting };

        public GraphEdgeNode(bool isInput)
        {
            this.isInput = isInput;
        }

        public override void OnSettingsChanged()
        {
            var valueType = (ValueType)valueTypeSetting.value;
            if (value == null || value.Value.Type != valueType) value = new NamedValue<Value>(nameSetting.value, Value.NewFromType(valueType));
            value.name = nameSetting.value;
        }

        public override List<NamedValue> BuildInputs() => isInput ? new() { } : new() { value };

        public override List<NamedValue> BuildOutputs() => isInput ? new() { value } : new() { };

        public override void Process(Context context) { }

        public override void ResetState() { }

        protected override SettingsNode CloneWithoutSettings()
        {
            return new GraphEdgeNode(isInput);
        }
    }
}
