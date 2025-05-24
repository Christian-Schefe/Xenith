using FileFormat;
using System.Threading.Tasks;
using UnityEngine;

namespace DSP
{
    public class DPSTest : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var main = Globals<Main>.Instance;
                if (main.CurrentSongId == null) return;

                var noteEditor = Globals<PianoRoll.NoteEditor>.Instance;
                var dsp = Globals<DSP>.Instance;
                if (noteEditor.isPlaying)
                {
                    noteEditor.StopPlaying();
                    dsp.ResetDSP();
                    return;
                }
                noteEditor.StartPlaying();

                var startTime = noteEditor.GetPlayStartTime();
                var instruments = main.CurrentSong.BuildInstrumentNodes(startTime);
                var mixer = main.CurrentSong.BuildMixerNode();

                dsp.Initialize(instruments, mixer);
            }
        }

        //TODO: Multithread instrument rendering
        public void Render(System.Action<WavFile> callback)
        {
            var main = Globals<Main>.Instance;
            var node = main.CurrentSong.BuildRenderNode(0);
            var sampleRate = 44100;
            var duration = main.CurrentSong.GetDuration() + 5;
            var task = RenderWAV(node, duration, sampleRate).GetAwaiter();
            task.OnCompleted(() => callback(task.GetResult()));
        }


        private async Awaitable<WavFile> RenderWAV(AudioNode node, float duration, int sampleRate)
        {
            node.Initialize();
            int channels = 2;
            var context = new Context(sampleRate);

            FloatValue[] outputValues = new FloatValue[channels];
            for (int i = 0; i < channels; i++)
            {
                outputValues[i] = (FloatValue)node.outputs[i].Value;
            }

            int frameCount = (int)(duration * sampleRate);
            var data = new float[frameCount * channels];

            await Task.Run(() =>
            {
                for (int i = 0; i < frameCount; i++)
                {
                    node.Process(context);
                    for (int c = 0; c < channels; c++)
                    {
                        data[i * channels + c] = outputValues[c].value;
                    }
                }
            });

            var wav = new WavFile(WavFormat.IEEEFloat, sampleRate, channels, data);
            wav.Rescale(0.95f);
            return wav;
        }
    }
}