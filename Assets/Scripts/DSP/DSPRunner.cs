using FileFormat;
using ReactiveData.App;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DSP
{
    public class DSPRunner : MonoBehaviour
    {
        private DSPPlayer player;
        private bool isRendering;

        public static bool IsUIElementActive()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                if (EventSystem.current.currentSelectedGameObject.TryGetComponent<TMPro.TMP_InputField>(out _))
                {
                    return true;
                }
            }
            return false;
        }

        private void Update()
        {
            if (!isRendering && Input.GetKeyDown(KeyCode.Space))
            {
                if (IsUIElementActive()) return;
                var main = Globals<Main>.Instance;
                if (!main.app.openElement.Value.TryGet(out ReactiveSong song)) return;

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
                var instruments = song.BuildInstrumentNodes(startTime);
                var master = song.BuildMasterNode();

                player = new DSPPlayer(instruments, master, dsp.Volume, 2, 8192);
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
            var main = Globals<Main>.Instance;
            if (!main.app.openElement.Value.TryGet(out ReactiveSong song)) return;
            isRendering = true;
            var instruments = song.BuildInstrumentNodes(0);
            var master = song.BuildMasterNode();
            var sampleRate = 44100;
            var duration = song.GetDuration() + 5;
            StartCoroutine(RenderWAV(instruments, master, duration, sampleRate, callback));
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

        private IEnumerator RenderWAV(DSPInstrument[] instruments, DSPMaster mixer, float duration, int sampleRate, System.Action<WavFile> callback)
        {
            int channels = 2;

            int frameCount = (int)(duration * sampleRate);
            var data = new float[frameCount * channels];
            int framesPerRead = frameCount / 100;

            var player = new DSPPlayer(instruments, mixer, 1.0f, channels, framesPerRead * 2);
            var context = new Context(sampleRate);
            player.Start(context);

            int framesRead = 0;

            while (framesRead < frameCount)
            {
                int framesToRead = Mathf.Min(framesPerRead, frameCount - framesRead);

                while (!player.TakeData(data, framesRead * channels, channels, framesToRead))
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