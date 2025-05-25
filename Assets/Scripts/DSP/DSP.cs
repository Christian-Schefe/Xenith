using UnityEngine;

namespace DSP
{
    public class DSP : MonoBehaviour
    {
        public FloatValue playTime = new();
        public float volume = 1.0f;

        private bool isStreaming;
        private DSPPlayer player;
        private Context context;

        public void StartStreaming(DSPPlayer player, Context context)
        {
            playTime.value = 0;
            this.context = context;
            this.player = player;
            isStreaming = true;
        }

        public void StopStreaming()
        {
            if (!isStreaming) return;
            isStreaming = false;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isStreaming)
            {
                return;
            }

            int frameCount = data.Length / channels;
            if (player.TakeData(data, 0, channels, frameCount, volume))
            {
                playTime.value += context.deltaTime * frameCount;
            }
            else
            {
                Debug.LogWarning("DSPPlayer failed to take data.");
            }
        }
    }

    public class Context
    {
        public float sampleRate;
        public float deltaTime;
        public double deltaTimeDouble;

        public Context(float sampleRate)
        {
            this.sampleRate = sampleRate;
            deltaTime = 1.0f / sampleRate;
            deltaTimeDouble = 1.0 / (double)sampleRate;
        }
    }
}
