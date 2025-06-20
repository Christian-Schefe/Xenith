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

        private float prevGain;

        public ReactiveGainPanNode(IReactive<float> reactiveGain, IReactive<float> reactivePan)
        {
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
            return new ReactiveGainPanNode(reactiveGain, reactivePan);
        }

        public override void Process(Context context)
        {
            leftOut.value.value = leftIn.value.value * gain * (1 - pan);
            rightOut.value.value = rightIn.value.value * gain * (1 + pan);

            if (gain != prevGain) Debug.Log($"Gain changed from {prevGain} to {gain}");
            prevGain = gain;
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