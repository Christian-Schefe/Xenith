using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class ADSR : AudioNode
    {
        private NamedValue<BoolValue> gate = new("Gate", new BoolValue());
        private NamedValue<FloatValue> output = new("Output", new FloatValue());
        private NamedValue<FloatValue> timeSinceGateVal = new("Time Since Gate", new FloatValue());

        private float attack;
        private float decay;
        private float sustain;
        private float release;

        private bool prevGate = false;
        private float prevOutput = 0f;
        private float timeSinceGate = 0f;
        private float ampAtGate = 0f;

        public ADSR(float attack, float decay, float sustain, float release)
        {
            this.attack = attack;
            this.decay = decay;
            this.sustain = sustain;
            this.release = release;
        }

        public override List<NamedValue> BuildInputs() => new() { gate };

        public override List<NamedValue> BuildOutputs() => new() { output, timeSinceGateVal };

        public override void Process(Context context)
        {
            var curGate = gate.value.value;
            if (prevGate != curGate)
            {
                prevGate = curGate;
                timeSinceGate = 0f;
                ampAtGate = prevOutput;
            }

            timeSinceGate += context.deltaTime;
            float val;
            if (curGate)
            {
                if (timeSinceGate < attack)
                {
                    var alpha = Mathf.Clamp01(timeSinceGate / attack);
                    val = Mathf.Lerp(ampAtGate, 1f, alpha);
                }
                else
                {
                    var alpha = Mathf.Clamp01((timeSinceGate - attack) / decay);
                    val = Mathf.Lerp(1f, sustain, alpha);
                }
            }
            else
            {
                val = Mathf.Lerp(ampAtGate, 0f, timeSinceGate / release);
            }
            output.value.value = val;
            prevOutput = val;
            timeSinceGateVal.value.value = timeSinceGate;
        }

        public override void ResetState()
        {
            prevGate = false;
            timeSinceGate = 0f;
            ampAtGate = 0f;
        }
    }
}
