using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class ADSR : SettingsNode
    {
        public static ADSR New(float attack, float decay, float sustain, float release)
        {
            var node = new ADSR();
            node.attackSetting.value = attack;
            node.decaySetting.value = decay;
            node.sustainSetting.value = sustain;
            node.releaseSetting.value = release;
            node.OnSettingsChanged();
            return node;
        }

        private readonly NamedValue<BoolValue> gate = new("Gate", new BoolValue());
        private readonly NamedValue<FloatValue> output = new("Output", new FloatValue());
        private readonly NamedValue<FloatValue> timeSinceTriggerVal = new("Time Since Trigger", new FloatValue());

        private readonly FloatSetting attackSetting = new("Attack", 0.1f);
        private readonly FloatSetting decaySetting = new("Decay", 0.1f);
        private readonly FloatSetting sustainSetting = new("Sustain", 0.5f);
        private readonly FloatSetting releaseSetting = new("Release", 0.1f);

        private float attack;
        private float decay;
        private float sustain;
        private float release;

        private bool prevGate = false;
        private float prevOutput = 0f;
        private float ampAtGate = 0f;

        private long ticksSinceGate = 0;
        private long ticksSinceTrigger = 0;

        public override NodeSettings DefaultSettings => new(attackSetting, decaySetting, sustainSetting, releaseSetting);

        public override void OnSettingsChanged()
        {
            attack = attackSetting.value;
            decay = decaySetting.value;
            sustain = sustainSetting.value;
            release = releaseSetting.value;
        }

        protected override SettingsNode CloneWithoutSettings()
        {
            return new ADSR();
        }

        public override List<NamedValue> BuildInputs() => new() { gate };

        public override List<NamedValue> BuildOutputs() => new() { output, timeSinceTriggerVal };

        public override void Process(Context context)
        {
            var curGate = gate.value.value;
            if (prevGate != curGate)
            {
                prevGate = curGate;
                ticksSinceGate = 0;
                ampAtGate = prevOutput;
                if (curGate) ticksSinceTrigger = 0;
            }

            ticksSinceGate += 1;
            ticksSinceTrigger += 1;
            var timeSinceGate = ticksSinceGate * context.deltaTime;
            var timeSinceTrigger = ticksSinceTrigger * context.deltaTime;

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
            timeSinceTriggerVal.value.value = timeSinceTrigger;
        }

        public override void ResetState()
        {
            prevGate = false;
            ticksSinceGate = 0;
            ticksSinceTrigger = 0;
            ampAtGate = 0f;
        }
    }
}
