using System.Collections;
using UnityEngine;

namespace DSP
{
    public class DSP : MonoBehaviour
    {
        public FloatValue playTime = new();
        public float volume = 1.0f;

        private long playTicks = 0;
        private bool isStreaming;
        private DSPPlayer player;
        private Context context;
        private Coroutine delayedStreamingCoroutine;

        public float Volume => volume;

        public void StartStreaming(DSPPlayer player, Context context)
        {
            playTime.value = 0;
            playTicks = 0;
            this.context = context;
            this.player = player;
            delayedStreamingCoroutine = StartCoroutine(StartDelayedStreaming(0.05f));
        }

        private IEnumerator StartDelayedStreaming(float delay)
        {
            yield return new WaitForSeconds(delay);
            isStreaming = true;
        }

        public void StopStreaming()
        {
            if (delayedStreamingCoroutine != null)
            {
                StopCoroutine(delayedStreamingCoroutine);
                delayedStreamingCoroutine = null;
            }
            if (!isStreaming) return;
            isStreaming = false;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isStreaming) return;

            int frameCount = data.Length / channels;
            if (player.TakeData(data, 0, channels, frameCount))
            {
                playTicks += frameCount;
                playTime.value = (float)(playTicks * context.deltaTimeDouble);
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
