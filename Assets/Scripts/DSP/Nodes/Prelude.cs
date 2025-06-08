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

        public static NodeGraph Vibrato()
        {
            var graph = new NodeGraph();
            int freqIn = graph.AddInput<FloatValue>("Vibrato Freq", 0);
            int depthIn = graph.AddInput<FloatValue>("Depth", 1);
            int valueIn = graph.AddInput<FloatValue>("Freq", 2);
            int valueOut = graph.AddOutput<FloatValue>("Modulated Freq", 0);
            int lfo = graph.AddNode(Oscillator.New(Oscillator.WaveformType.Sine));
            int one = graph.AddNode(ConstFloatNode.New(1));
            int combinator = graph.AddNode(new CombinatorNode<FloatValue>(3, 1, (inputs, outputs) =>
            {
                var oscVal = inputs[0].value * 0.5f + 0.5f;
                float depth = inputs[1].value;
                float inVal = inputs[2].value;

                var factor = 1 + depth;
                outputs[0].value = inVal * Xerp(1f / factor, factor, oscVal);
            }));
            graph.AddConnection(new(freqIn, 0, lfo, 0));
            graph.AddConnection(new(one, 0, lfo, 1));

            graph.AddConnection(new(lfo, 0, combinator, 0));
            graph.AddConnection(new(depthIn, 0, combinator, 1));
            graph.AddConnection(new(valueIn, 0, combinator, 2));

            graph.AddConnection(new(combinator, 0, valueOut, 0));

            return graph;
        }

        public static NodeGraph DefaultSynth()
        {
            var graph = new NodeGraph();
            int freqIn = graph.AddInput<FloatValue>("Vibrato Freq", 0);
            int gateIn = graph.AddInput<BoolValue>("Depth", 1);

            var leftOut = graph.AddOutput<FloatValue>("Left", 0);
            var rightOut = graph.AddOutput<FloatValue>("Right", 1);

            var osc = graph.AddNode(Oscillator.New(Oscillator.WaveformType.Square));
            var adsr = graph.AddNode(ADSR.New(0.1f, 0.5f, 0.9f, 0.2f));

            graph.AddConnection(new(gateIn, 0, adsr, 0));
            graph.AddConnection(new(freqIn, 0, osc, 0));
            graph.AddConnection(new(adsr, 0, osc, 1));
            graph.AddConnection(new(osc, 0, leftOut, 0));
            graph.AddConnection(new(osc, 0, rightOut, 0));

            return graph;
        }

        private static float Xerp(float a, float b, float t)
        {
            return Mathf.Exp(Mathf.Lerp(Mathf.Log(a), Mathf.Log(b), t));
        }
    }
}
