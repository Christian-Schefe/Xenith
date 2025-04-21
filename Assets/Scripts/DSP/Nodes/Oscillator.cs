using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class Oscillator : SettingsNode
    {
        public static Oscillator New(WaveformType type, float initialPhase = 0)
        {
            var node = new Oscillator();
            node.waveformSetting.value = (int)type;
            node.initialPhaseSetting.value = initialPhase;
            node.OnSettingsChanged();
            return node;
        }

        public enum WaveformType
        {
            Sine,
            Square,
            Sawtooth
        }

        private readonly NamedValue<FloatValue> frequency = new("Frequency", new(0));
        private readonly NamedValue<FloatValue> amplitude = new("Amplitude", new(0));
        private readonly NamedValue<FloatValue> output = new("Output", new(0));

        public override List<NamedValue> BuildInputs() => new() { frequency, amplitude };
        public override List<NamedValue> BuildOutputs() => new() { output };

        public delegate float Waveform(float phase);

        private float initialPhase = 0;
        private float phase = 0;
        private Waveform waveform;

        private readonly EnumSetting<WaveformType> waveformSetting = new("Waveform", WaveformType.Sine);
        private readonly FloatSetting initialPhaseSetting = new("InitialPhase", 0);

        public override NodeSettings DefaultSettings => new(waveformSetting);

        public override void OnSettingsChanged()
        {
            waveform = GetWaveform((WaveformType)waveformSetting.value);
            initialPhase = initialPhaseSetting.value;
        }

        public static Waveform GetWaveform(WaveformType type)
        {
            static float SineWave(float phase) => Mathf.Sin(phase * Mathf.PI * 2);
            static float SquareWave(float phase) => phase < 0.5f ? 1 : -1;
            static float SawtoothWave(float phase) => phase < 0.5f ? (2 * phase) : (2 * phase - 2);

            return type switch
            {
                WaveformType.Sine => SineWave,
                WaveformType.Square => SquareWave,
                WaveformType.Sawtooth => SawtoothWave,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, "Invalid Waveform Variant")
            };
        }

        public override void Process(Context context)
        {
            phase += frequency.value.value * context.deltaTime;
            while (phase >= 1.0f)
            {
                phase -= 1.0f;
            }
            output.value.value = amplitude.value.value * waveform(phase);
        }

        public override void ResetState()
        {
            phase = initialPhase;
        }

        protected override SettingsNode CloneWithoutSettings()
        {
            return new Oscillator();
        }
    }
}