using FileFormat;
using System.Collections;
using UnityEngine;

namespace DSP
{
    public class DPSTest : MonoBehaviour
    {
        private DSPPlayer player;
        private bool isRendering;

        private void Update()
        {
            if (!isRendering && Input.GetKeyDown(KeyCode.Space))
            {
                var main = Globals<Main>.Instance;
                if (main.CurrentSongId == null) return;

                var noteEditor = Globals<PianoRoll.NoteEditor>.Instance;
                var dsp = Globals<DSP>.Instance;
                if (noteEditor.isPlaying)
                {
                    noteEditor.StopPlaying();
                    dsp.StopStreaming();
                    if (player != null)
                    {
                        player.Stop();
                        player = null;
                    }
                    return;
                }
                noteEditor.StartPlaying();

                var startTime = noteEditor.GetPlayStartTime();
                var instruments = main.CurrentSong.BuildInstrumentNodes(startTime);
                var mixer = main.CurrentSong.BuildMixerNode();

                player = new DSPPlayer(instruments, mixer, 2, 65536);
                var context = new Context(AudioSettings.outputSampleRate);
                player.Start(context);

                dsp.StartStreaming(player, context);
            }
        }

        public void Render(System.Action<WavFile> callback)
        {
            if (isRendering)
            {
                Debug.LogWarning("Rendering is already in progress.");
                return;
            }
            isRendering = true;
            var main = Globals<Main>.Instance;
            var instruments = main.CurrentSong.BuildInstrumentNodes(0);
            var mixer = main.CurrentSong.BuildMixerNode();
            var sampleRate = 44100;
            var duration = main.CurrentSong.GetDuration() + 5;
            StartCoroutine(RenderWAV(instruments, mixer, duration, sampleRate, callback));
        }

        private void OnApplicationQuit()
        {
            Globals<DSP>.Instance.StopStreaming();
            if (player != null)
            {
                player.Stop();
                player = null;
            }
        }

        private IEnumerator RenderWAV(AudioNode[] instruments, AudioNode mixer, float duration, int sampleRate, System.Action<WavFile> callback)
        {
            int channels = 2;

            int frameCount = (int)(duration * sampleRate);
            var data = new float[frameCount * channels];
            int framesPerRead = frameCount / 100;

            var player = new DSPPlayer(instruments, mixer, channels, framesPerRead * 2);
            var context = new Context(sampleRate);
            player.Start(context);

            int framesRead = 0;

            while (framesRead < frameCount)
            {
                int framesToRead = Mathf.Min(framesPerRead, frameCount - framesRead);

                while (!player.TakeData(data, framesRead * channels, channels, framesToRead, 1.0f))
                {
                    yield return null;
                }
                framesRead += framesToRead;
                Debug.Log($"Rendered {framesRead} frames out of {frameCount} ({(float)framesRead / frameCount * 100:F2}%)");
            }

            var wav = new WavFile(WavFormat.IEEEFloat, sampleRate, channels, data);
            wav.Rescale(0.95f);
            isRendering = false;
            callback?.Invoke(wav);
        }
    }
}