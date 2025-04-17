using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class Oscillator : SettingsNode
    {
        public static Oscillator New(WaveformType type)
        {
            var node = new Oscillator();
            node.waveformSetting.value = (int)type;
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

        private float phase = 0;
        private Waveform waveform;

        private readonly EnumSetting<WaveformType> waveformSetting = new("Waveform", WaveformType.Sine);

        public override NodeSettings DefaultSettings => new(waveformSetting);

        public override void OnSettingsChanged()
        {
            waveform = GetWaveform((WaveformType)waveformSetting.value);
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
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, "Invalid Waveform Type")
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
            phase = 0;
        }
    }
}