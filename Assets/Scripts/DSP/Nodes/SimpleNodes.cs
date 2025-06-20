using System.Collections.Generic;

namespace DSP
{
    public class ConstFloatNode : SettingsNode
    {
        public static ConstFloatNode New(float value)
        {
            var node = new ConstFloatNode();
            node.valueSetting.value = value;
            node.OnSettingsChanged();
            return node;
        }

        private readonly NamedValue<FloatValue> value = new("Value", new());

        public readonly FloatSetting valueSetting = new("Value", 0);

        public override List<NodeSetting> DefaultSettings => new() { valueSetting };

        public override void OnSettingsChanged()
        {
            value.value.value = valueSetting.value;
        }

        public override List<NamedValue> BuildInputs() => new();

        public override List<NamedValue> BuildOutputs() => new() { value };

        public override void Process(Context context) { }

        public override void ResetState() { }

        protected override SettingsNode CloneWithoutSettings()
        {
            return new ConstFloatNode();
        }
    }

    public class BoolBinaryNode : SettingsNode
    {
        public enum Operation
        {
            And, Or, Nand, Nor, Xor, Xnor
        }

        public static BoolBinaryNode New(Operation value)
        {
            var node = new BoolBinaryNode();
            node.operationSetting.value = (int)value;
            node.OnSettingsChanged();
            return node;
        }

        private readonly NamedValue<BoolValue> a = new("A", new());
        private readonly NamedValue<BoolValue> b = new("B", new());
        private readonly NamedValue<BoolValue> outVal = new("Out", new());

        public readonly EnumSetting<Operation> operationSetting = new("Operation", Operation.Or);

        private Operation operation = Operation.Or;

        public override List<NodeSetting> DefaultSettings => new() { operationSetting };

        public override void OnSettingsChanged()
        {
            operation = (Operation)operationSetting.value;
        }

        public override List<NamedValue> BuildInputs() => new() { a, b };
        public override List<NamedValue> BuildOutputs() => new() { outVal };

        public override void Process(Context context)
        {
            var aVal = a.value.value;
            var bVal = b.value.value;

            outVal.value.value = operation switch
            {
                Operation.And => aVal && bVal,
                Operation.Or => aVal || bVal,
                Operation.Nand => !(aVal && bVal),
                Operation.Nor => !(aVal || bVal),
                Operation.Xor => aVal ^ bVal,
                Operation.Xnor => !(aVal ^ bVal),
                _ => throw new System.Exception($"Invalid operation {operation} for BoolBinaryNode")
            };
        }

        public override void ResetState() { }

        protected override SettingsNode CloneWithoutSettings()
        {
            return new BoolBinaryNode();
        }
    }

    public class FloatBinaryNode : SettingsNode
    {
        public enum Operation
        {
            Add, Sub, Mul, Div, Mod, Min, Max
        }

        public static FloatBinaryNode New(Operation value)
        {
            var node = new FloatBinaryNode();
            node.operationSetting.value = (int)value;
            node.OnSettingsChanged();
            return node;
        }

        private readonly NamedValue<FloatValue> a = new("A", new());
        private readonly NamedValue<FloatValue> b = new("B", new());
        private readonly NamedValue<FloatValue> outVal = new("Out", new());

        public readonly EnumSetting<Operation> operationSetting = new("Operation", Operation.Add);

        private Operation operation = Operation.Add;

        public override List<NodeSetting> DefaultSettings => new() { operationSetting };

        public override void OnSettingsChanged()
        {
            operation = (Operation)operationSetting.value;
        }

        public override List<NamedValue> BuildInputs() => new() { a, b };
        public override List<NamedValue> BuildOutputs() => new() { outVal };

        public override void Process(Context context)
        {
            var aVal = a.value.value;
            var bVal = b.value.value;

            outVal.value.value = operation switch
            {
                Operation.Add => aVal + bVal,
                Operation.Sub => aVal - bVal,
                Operation.Mul => aVal * bVal,
                Operation.Div => aVal / bVal,
                Operation.Mod => aVal % bVal,
                Operation.Min => aVal < bVal ? aVal : bVal,
                Operation.Max => aVal > bVal ? aVal : bVal,
                _ => throw new System.Exception($"Invalid operation {operation} for FloatBinaryNode")
            };
        }

        public override void ResetState() { }

        protected override SettingsNode CloneWithoutSettings()
        {
            return new FloatBinaryNode();
        }
    }

    public class TransformerNode<T1, T2> : AudioNode where T1 : Value, new() where T2 : Value, new()
    {
        private readonly NamedValue<T1> input;
        private readonly NamedValue<T2> output;
        private readonly System.Action<T1, T2> transformer;

        public TransformerNode(System.Action<T1, T2> transformer)
        {
            input = new NamedValue<T1>("Input", new());
            output = new NamedValue<T2>("Output", new());
            this.transformer = transformer;
        }

        public override List<NamedValue> BuildInputs() => new() { input };

        public override List<NamedValue> BuildOutputs() => new() { output };

        public override void Process(Context context)
        {
            transformer(input.value, output.value);
        }

        public override void ResetState() { }

        public override AudioNode Clone()
        {
            return new TransformerNode<T1, T2>(transformer);
        }
    }

    public class CombinatorNode<T> : AudioNode where T : Value, new()
    {
        private readonly List<NamedValue> namedInputs;
        private readonly List<NamedValue> namedOutputs;
        private readonly T[] inputVals;
        private readonly T[] outputVals;
        private readonly System.Action<T[], T[]> combinator;

        public CombinatorNode(int inputCount, int outputCount, System.Action<T[], T[]> combinator)
        {
            namedInputs = new();
            namedOutputs = new();
            inputVals = new T[inputCount];
            outputVals = new T[outputCount];
            this.combinator = combinator;

            for (int i = 0; i < inputCount; i++)
            {
                inputVals[i] = new T();
                namedInputs.Add(new NamedValue<T>($"Input {i}", inputVals[i]));
            }
            for (int i = 0; i < outputCount; i++)
            {
                outputVals[i] = new T();
                namedOutputs.Add(new NamedValue<T>($"Output {i}", outputVals[i]));
            }
        }

        public override List<NamedValue> BuildInputs() => namedInputs;

        public override List<NamedValue> BuildOutputs() => namedOutputs;

        public override void Process(Context context)
        {
            combinator(inputVals, outputVals);
        }

        public override void ResetState()
        {
        }

        public override AudioNode Clone()
        {
            return new CombinatorNode<T>(inputVals.Length, outputVals.Length, combinator);
        }
    }

    public class PassThroughNode<T> : AudioNode where T : Value, new()
    {
        private readonly int channelCount;
        private readonly List<NamedValue> namedInputs;
        private readonly List<NamedValue> namedOutputs;
        private readonly T[] inputVals;
        private readonly T[] outputVals;

        public PassThroughNode(int channelCount)
        {
            this.channelCount = channelCount;
            namedInputs = new();
            namedOutputs = new();
            inputVals = new T[channelCount];
            outputVals = new T[channelCount];

            for (int i = 0; i < channelCount; i++)
            {
                inputVals[i] = new T();
                namedInputs.Add(new NamedValue<T>($"Input {i}", inputVals[i]));
            }
            for (int i = 0; i < channelCount; i++)
            {
                outputVals[i] = new T();
                namedOutputs.Add(new NamedValue<T>($"Output {i}", outputVals[i]));
            }
        }

        public override List<NamedValue> BuildInputs() => namedInputs;

        public override List<NamedValue> BuildOutputs() => namedOutputs;

        public override void Process(Context context)
        {
            for (int i = 0; i < channelCount; i++)
            {
                outputVals[i].Set(inputVals[i]);
            }
        }

        public override void ResetState()
        {
        }

        public override AudioNode Clone()
        {
            return new PassThroughNode<T>(channelCount);
        }
    }
}