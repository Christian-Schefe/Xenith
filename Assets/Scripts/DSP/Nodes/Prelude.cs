using UnityEngine;

namespace DSP
{
    public static class Prelude
    {
        public static CombinatorNode<FloatValue> Multiply(int inputs)
        {
            return new CombinatorNode<FloatValue>(inputs, 1, (inputs, outputs) =>
            {
                float prod = 1;
                for (int i = 0; i < inputs.Length; i++)
                {
                    prod *= inputs[i].value;
                }
                outputs[0].value = prod;
            });
        }

        public static CombinatorNode<FloatValue> Add(int inputCount)
        {
            return new CombinatorNode<FloatValue>(inputCount, 1, (inputs, outputs) =>
            {
                float sum = 0;
                for (int i = 0; i < inputs.Length; i++)
                {
                    sum += inputs[i].value;
                }
                outputs[0].value = sum;
            });
        }

        public static CombinatorNode<FloatValue> Mix(int inputCount)
        {
            return new CombinatorNode<FloatValue>(2 * inputCount, 1, (inputs, outputs) =>
            {
                float sum = 0;
                for (int i = 0; i < inputCount; i++)
                {
                    sum += inputs[2 * i].value * inputs[2 * i + 1].value;
                }
                outputs[0].value = sum;
            });
        }

        public static int SupplyConst(NodeGraph graph, int node, int input, float val)
        {
            int constIndex = graph.AddNode(ConstFloatNode.New(val));
            graph.AddConnection(new(constIndex, 0, node, input));
            return constIndex;
        }

        public static NodeGraph Vibrato(float fadeIn)
        {
            var graph = new NodeGraph();
            int freqIn = graph.AddInput<FloatValue>("Frequency", 0);
            int depthIn = graph.AddInput<FloatValue>("Depth", 1);
            int adsrTimeIn = graph.AddInput<FloatValue>("Time Since Gate", 2);
            int valueIn = graph.AddInput<FloatValue>("Value", 3);
            int valueOut = graph.AddOutput<FloatValue>("Output", 0);
            int lfo = graph.AddNode(Oscillator.New(Oscillator.WaveformType.Sine));
            int one = graph.AddNode(ConstFloatNode.New(1));
            int combinator = graph.AddNode(new CombinatorNode<FloatValue>(4, 1, (inputs, outputs) =>
            {
                var oscVal = inputs[0].value * 0.5f + 0.5f;
                float depth = inputs[1].value;
                float timeSinceGate = inputs[2].value;
                float inVal = inputs[3].value;

                var factor = 1 + depth * Mathf.Min(timeSinceGate / fadeIn, 1f);
                outputs[0].value = inVal * Xerp(1f / factor, factor, oscVal);
            }));
            graph.AddConnection(new(freqIn, 0, lfo, 0));
            graph.AddConnection(new(one, 0, lfo, 1));

            graph.AddConnection(new(lfo, 0, combinator, 0));
            graph.AddConnection(new(depthIn, 0, combinator, 1));
            graph.AddConnection(new(adsrTimeIn, 0, combinator, 2));
            graph.AddConnection(new(valueIn, 0, combinator, 3));

            graph.AddConnection(new(combinator, 0, valueOut, 0));

            return graph;
        }

        private static float Xerp(float a, float b, float t)
        {
            return Mathf.Exp(Mathf.Lerp(Mathf.Log(a), Mathf.Log(b), t));
        }
    }
}
