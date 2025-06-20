using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public struct BiquadCoeffs
    {
        public float a1;
        public float a2;
        public float b0;
        public float b1;
        public float b2;

        public BiquadCoeffs(float a1, float a2, float b0, float b1, float b2)
        {
            this.a1 = a1;
            this.a2 = a2;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
        }

        public static BiquadCoeffs ButterworthLowpass(float sampleRate, float cutoff)
        {
            float f = Mathf.Tan(cutoff * Mathf.PI / sampleRate);
            float a0r = 1f / (1f + Mathf.Sqrt(2f) * f + f * f);
            float a1 = (2f * f * f - 2f) * a0r;
            float a2 = (1f - Mathf.Sqrt(2f) * f + f * f) * a0r;
            float b0 = f * f * a0r;
            float b1 = 2f * b0;
            float b2 = b0;
            return new BiquadCoeffs(a1, a2, b0, b1, b2);
        }

        public static BiquadCoeffs Lowpass(float sampleRate, float cutoff, float q)
        {
            float omega = 2f * Mathf.PI * cutoff / sampleRate;
            float alpha = Mathf.Sin(omega) / (2f * q);
            float beta = Mathf.Cos(omega);
            float a0r = 1f / (1f + alpha);
            float a1 = -2f * beta * a0r;
            float a2 = (1f - alpha) * a0r;
            float b0 = (1f - beta) * a0r * 0.5f;
            float b1 = (1f - beta) * a0r;
            float b2 = b0;
            return new BiquadCoeffs(a1, a2, b0, b1, b2);
        }
    }

    public abstract class BiquadFilter : AudioNode
    {
        private readonly NamedValue<FloatValue> input = new("Input", new());
        private readonly NamedValue<FloatValue> output = new("Output", new());

        protected BiquadCoeffs coeffs;
        private float x1, x2;
        private float y1, y2;

        public BiquadFilter()
        {
            x1 = 0f;
            x2 = 0f;
            y1 = 0f;
            y2 = 0f;
        }

        public override List<NamedValue> BuildInputs() => new() { input };

        public override List<NamedValue> BuildOutputs() => new() { output };

        public override void Process(Context context)
        {
            float x0 = input.value.value;
            float y0 = coeffs.b0 * x0 + coeffs.b1 * x1 + coeffs.b2 * x2 - coeffs.a1 * y1 - coeffs.a2 * y2;

            x2 = x1;
            x1 = x0;
            y2 = y1;
            y1 = y0;

            output.value.value = y0;
        }

        public override void ResetState()
        {
            x1 = 0f;
            x2 = 0f;
            y1 = 0f;
            y2 = 0f;
        }
    }

    public class ButterworthLowpassFilter : BiquadFilter
    {
        private readonly NamedValue<FloatValue> cutoff = new("Cutoff", new());

        private float sampleRate = 44100f;
        private float cutoffFrequency = 400f;

        public ButterworthLowpassFilter()
        {
            coeffs = BiquadCoeffs.ButterworthLowpass(sampleRate, cutoffFrequency);
        }

        public override List<NamedValue> BuildInputs()
        {
            var baseList = base.BuildInputs();
            baseList.Add(cutoff);
            return baseList;
        }

        public override AudioNode Clone()
        {
            return new ButterworthLowpassFilter();
        }

        public override void Initialize(Context context)
        {
            sampleRate = context.sampleRate;
            coeffs = BiquadCoeffs.ButterworthLowpass(sampleRate, cutoffFrequency);
        }

        public override void Process(Context context)
        {
            var prevSampleRate = sampleRate;
            var prevCutoff = cutoffFrequency;
            sampleRate = context.sampleRate;
            cutoffFrequency = cutoff.value.value;
            if (sampleRate != prevSampleRate || cutoffFrequency != prevCutoff)
            {
                coeffs = BiquadCoeffs.ButterworthLowpass(sampleRate, cutoffFrequency);
            }
            base.Process(context);
        }
    }
}