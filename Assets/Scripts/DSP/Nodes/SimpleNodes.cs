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

        public override NodeSettings DefaultSettings => new(valueSetting);

        public override void OnSettingsChanged()
        {
            value.value.value = valueSetting.value;
        }

        public override List<NamedValue> BuildInputs() => new();

        public override List<NamedValue> BuildOutputs() => new() { value };

        public override void Process(Context context)
        {
        }

        public override void ResetState()
        {
        }

    }


    public class TransformerNode<T1, T2> : AudioNode where T1 : Value where T2 : Value
    {
        private NamedValue<T1> input;
        private NamedValue<T2> output;
        private System.Action<T1, T2> transformer;

        public TransformerNode(T1 fromValue, T2 toValue, System.Action<T1, T2> transformer)
        {
            input = new NamedValue<T1>("Input", fromValue);
            output = new NamedValue<T2>("Output", toValue);
            this.transformer = transformer;
        }

        public override List<NamedValue> BuildInputs() => new() { input };

        public override List<NamedValue> BuildOutputs() => new() { output };

        public override void Process(Context context)
        {
            transformer(input.value, output.value);
        }

        public override void ResetState()
        {
        }
    }
    public class CombinatorNode<T> : AudioNode where T : Value, new()
    {
        private List<NamedValue> namedInputs;
        private List<NamedValue> namedOutputs;
        private T[] inputVals;
        private T[] outputVals;
        private System.Action<T[], T[]> combinator;

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
    }
}