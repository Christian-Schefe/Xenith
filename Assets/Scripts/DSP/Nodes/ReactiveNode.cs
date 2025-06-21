using ReactiveData.Core;
using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class ReactiveGainPanNode : AudioNode
    {
        private readonly NamedValue<FloatValue> leftIn = new("Left In", new());
        private readonly NamedValue<FloatValue> rightIn = new("Right In", new());
        private readonly NamedValue<FloatValue> leftOut = new("Left Out", new());
        private readonly NamedValue<FloatValue> rightOut = new("Right Out", new());

        private IReactive<float> reactiveGain;
        private IReactive<float> reactivePan;
        private float gain;
        private float pan;
        private bool isMaster;

        public ReactiveGainPanNode(bool isMaster, IReactive<float> reactiveGain, IReactive<float> reactivePan)
        {
            this.isMaster = isMaster;
            this.reactiveGain = reactiveGain;
            this.reactivePan = reactivePan;
            reactiveGain.OnChanged += OnGainChanged;
            reactivePan.OnChanged += OnPanChanged;
            OnGainChanged(reactiveGain.Value);
            OnPanChanged(reactivePan.Value);
        }

        private void OnGainChanged(float newGain)
        {
            gain = Mathf.Max(newGain, 0);
        }

        private void OnPanChanged(float newPan)
        {
            pan = Mathf.Clamp(newPan, -1.0f, 1.0f);
        }

        public override List<NamedValue> BuildInputs() => new() { leftIn, rightIn };

        public override List<NamedValue> BuildOutputs() => new() { leftOut, rightOut };

        public override AudioNode Clone()
        {
            return new ReactiveGainPanNode(isMaster, reactiveGain, reactivePan);
        }

        public override void Process(Context context)
        {
            if (isMaster)
            {
                // stereo rotation matrix with limited pan range
                float angle = pan * Mathf.PI * 0.125f;
                float outLeft = Mathf.Cos(angle) * leftIn.value.value - Mathf.Sin(angle) * rightIn.value.value;
                float outRight = Mathf.Sin(angle) * leftIn.value.value + Mathf.Cos(angle) * rightIn.value.value;

                leftOut.value.value = gain * outLeft;
                rightOut.value.value = gain * outRight;
            }
            else
            {
                // equal power pan
                float angle = (pan + 1f) * 0.25f * Mathf.PI;
                float outLeft = Mathf.Cos(angle) * leftIn.value.value;
                float outRight = Mathf.Sin(angle) * rightIn.value.value;

                leftOut.value.value = gain * outLeft;
                rightOut.value.value = gain * outRight;
            }
        }

        public override void ResetState() { }

        public override void Dispose()
        {
            if (reactiveGain != null)
            {
                reactiveGain.OnChanged -= OnGainChanged;
                reactiveGain = null;
            }
            if (reactivePan != null)
            {
                reactivePan.OnChanged -= OnPanChanged;
                reactivePan = null;
            }
        }
    }
}