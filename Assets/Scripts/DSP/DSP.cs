using UnityEngine;

namespace DSP
{
    public class DSP : MonoBehaviour
    {
        private AudioNode node;
        private bool isInitialized;
        private Context context;

        public float volume = 1.0f;

        public void Initialize(AudioNode node)
        {
            Debug.Log("Initializing DSP...");
            this.node = node;
            context = new Context(AudioSettings.outputSampleRate);
            node.Initialize();
            isInitialized = true;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isInitialized || node == null) return;
            if (node.inputs.Count != 0 || node.outputs.Count != channels)
            {
                Debug.LogError("Node input/output count does not match audio data.");
                return;
            }

            FloatValue[] outputValues = new FloatValue[channels];
            for (int i = 0; i < channels; i++)
            {
                outputValues[i] = (FloatValue)node.outputs[i].Value;
            }

            int frameCount = data.Length / channels;

            for (int i = 0; i < frameCount; i++)
            {
                node.Process(context);
                for (int c = 0; c < channels; c++)
                {
                    data[i * channels + c] = outputValues[c].value * volume;
                }
            }
        }
    }

    public class Context
    {
        public float sampleRate;
        public float deltaTime;

        public Context(float sampleRate)
        {
            this.sampleRate = sampleRate;
            deltaTime = 1.0f / sampleRate;
        }
    }
}
