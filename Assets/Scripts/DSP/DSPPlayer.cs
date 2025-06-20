using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace DSP
{
    public class DSPPlayer
    {
        private const int BUFFER_FRAMES = 512;

        private readonly DSPMaster master;
        private readonly DSPInstrument[] instruments;

        private readonly float globalVolume;
        private readonly int channels;
        private readonly int ringBufferFrames;

        private Context context;

        private float[][] instrumentBuffer, effectsBuffer, mixBuffer;
        private RingBuffer ringBuffer;

        private Barrier instrumentsDone;
        private CountdownEvent allShutdown;

        private bool running;
        private CancellationTokenSource cancellationTokenSource;

        public DSPPlayer(DSPInstrument[] instruments, DSPMaster master, float globalVolume, int channels, int minRingBufferFrames)
        {

            this.instruments = instruments;
            this.master = master;
            this.channels = channels;
            this.globalVolume = globalVolume;
            ringBufferFrames = ((minRingBufferFrames % BUFFER_FRAMES == 0) ?
                minRingBufferFrames :
                ((minRingBufferFrames / BUFFER_FRAMES) + 1) * BUFFER_FRAMES) + 1;

            foreach (var instrument in instruments) instrument.Validate();
            master.Validate();
        }

        public void Start(Context context)
        {
            int instrumentCount = instruments.Length;
            int participantCount = 2 * instrumentCount + 1;

            instrumentBuffer = new float[instrumentCount][];
            effectsBuffer = new float[instrumentCount][];
            mixBuffer = new float[instrumentCount][];

            instrumentsDone = new Barrier(participantCount, _ =>
            {
                (mixBuffer, effectsBuffer, instrumentBuffer) = (effectsBuffer, instrumentBuffer, mixBuffer);
            });
            allShutdown = new CountdownEvent(participantCount);

            ringBuffer = new RingBuffer(ringBufferFrames * channels);

            for (int i = 0; i < instrumentCount; i++)
            {
                instrumentBuffer[i] = new float[BUFFER_FRAMES * channels];
                effectsBuffer[i] = new float[BUFFER_FRAMES * channels];
                mixBuffer[i] = new float[BUFFER_FRAMES * channels];
            }

            this.context = context;
            running = true;

            cancellationTokenSource = new CancellationTokenSource();

            for (int i = 0; i < instrumentCount; i++)
            {
                int id = i;
                instruments[i].instrument.Initialize(context);
                instruments[i].effects.Initialize(context);
                ThreadPool.QueueUserWorkItem(_ => InstrumentThread(cancellationTokenSource.Token, id));
                ThreadPool.QueueUserWorkItem(_ => EffectsThread(cancellationTokenSource.Token, id));
            }

            master.effects.Initialize(context);
            ThreadPool.QueueUserWorkItem(_ => MixerThread(cancellationTokenSource.Token));
        }

        public void Stop()
        {
            if (!running) return;
            running = false;
            cancellationTokenSource.Cancel();
            new Thread(() =>
            {
                if (!allShutdown.Wait(2000))
                {
                    Debug.LogError("DSP shutdown timed out. Some threads may not have completed.");
                }
                instrumentsDone.Dispose();
                allShutdown.Dispose();
                Debug.Log("DSP has been reset.");

                master.effects.Dispose();
                foreach (var instrument in instruments)
                {
                    instrument.instrument.Dispose();
                    instrument.effects.Dispose();
                }
            }).Start();
        }

        private void ThreadWrapper(string name, Action run)
        {
            try
            {
                run();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"{name} encountered an error: {ex.Message}\n{ex.StackTrace}");
                cancellationTokenSource.Cancel();
            }
            finally
            {
                allShutdown.Signal();
                Debug.Log($"{name} has shut down.");
            }
        }

        private void InstrumentThread(CancellationToken token, int id)
        {
            ThreadWrapper($"InstrumentThread {id}", () =>
            {
                var instrument = instruments[id].instrument;
                var outputValues = instrument.outputs.Select(o => (FloatValue)o.Value).ToArray();

                while (!token.IsCancellationRequested && running)
                {
                    var buffer = instrumentBuffer[id];
                    for (int i = 0; i < BUFFER_FRAMES; i++)
                    {
                        instrument.Process(context);
                        for (int c = 0; c < channels; c++)
                        {
                            buffer[i * channels + c] = outputValues[c].value;
                        }
                    }

                    if (token.IsCancellationRequested || !running) break;
                    instrumentsDone.SignalAndWait(token);
                }
            });
        }

        private void EffectsThread(CancellationToken token, int id)
        {
            ThreadWrapper($"EffectsThread {id}", () =>
            {
                var effect = instruments[id].effects;
                var inputsValues = effect.inputs.Select(i => (FloatValue)i.Value).ToArray();
                var outputValues = effect.outputs.Select(o => (FloatValue)o.Value).ToArray();

                // wait once for instrument buffer to be filled
                if (token.IsCancellationRequested || !running) return;
                instrumentsDone.SignalAndWait(token);

                while (!token.IsCancellationRequested && running)
                {
                    var buffer = effectsBuffer[id];
                    for (int i = 0; i < BUFFER_FRAMES; i++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            inputsValues[c].value = buffer[i * channels + c];
                        }
                        effect.Process(context);
                        for (int c = 0; c < channels; c++)
                        {
                            buffer[i * channels + c] = outputValues[c].value;
                        }
                    }

                    if (token.IsCancellationRequested || !running) break;
                    instrumentsDone.SignalAndWait(token);
                }
            });
        }
        private void MixerThread(CancellationToken token)
        {
            ThreadWrapper("MixerThread", () =>
            {
                FloatValue[] effectsInputValues = master.effects.inputs.Select(i => (FloatValue)i.Value).ToArray();
                FloatValue[] effectsOutputValues = master.effects.outputs.Select(o => (FloatValue)o.Value).ToArray();

                var effects = master.effects;

                // wait twice for instrument and effects buffers to be filled
                if (token.IsCancellationRequested || !running) return;
                instrumentsDone.SignalAndWait(token);

                if (token.IsCancellationRequested || !running) return;
                instrumentsDone.SignalAndWait(token);

                while (!token.IsCancellationRequested && running)
                {
                    int dataToWriteCount = BUFFER_FRAMES * channels;
                    while (!ringBuffer.CanWrite(dataToWriteCount))
                    {
                        if (token.IsCancellationRequested || !running) break;
                        Thread.Sleep(1);
                    }

                    if (token.IsCancellationRequested || !running) break;

                    for (int i = 0; i < BUFFER_FRAMES; i++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            effectsInputValues[c].value = 0f;
                            for (int inst = 0; inst < instruments.Length; inst++)
                            {
                                effectsInputValues[c].value += mixBuffer[inst][i * channels + c];
                            }
                        }
                        effects.Process(context);
                        for (int c = 0; c < channels; c++)
                        {
                            mixBuffer[0][i * channels + c] = effectsOutputValues[c].value * globalVolume;
                        }
                    }

                    ringBuffer.WriteFrom(mixBuffer[0], 0, dataToWriteCount);

                    if (token.IsCancellationRequested || !running) break;
                    instrumentsDone.SignalAndWait(token);
                }
            });
        }

        public bool TakeData(float[] data, int offset, int targetChannels, int frames)
        {
            int dataToReadCount = frames * channels;
            if (!ringBuffer.CanRead(dataToReadCount))
            {
                return false;
            }

            if (targetChannels == channels)
            {
                ringBuffer.ReadInto(data, offset, dataToReadCount);
            }
            else if (targetChannels == 1)
            {
                var readData = new float[dataToReadCount];
                ringBuffer.ReadInto(readData, offset, dataToReadCount);
                for (int i = 0; i < frames; i++)
                {
                    data[offset + i] = 0;
                    float factor = 1f / channels;
                    for (int c = 0; c < channels; c++)
                    {
                        int index = i * channels + c;
                        data[offset + i] += readData[index] * factor;
                    }
                }
            }
            else
            {
                Debug.LogError($"Unsupported target channels: {targetChannels}. Only 1 or {channels} are supported.");
                return false;
            }
            return true;
        }
    }
}