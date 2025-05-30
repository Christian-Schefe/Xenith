using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace DSP
{
    public class DSPPlayer
    {
        private readonly AudioNode mixer;
        private readonly AudioNode[] instruments;

        private Context context;

        private readonly int channels;

        private const int BUFFER_FRAMES = 512;
        private readonly int ringBufferFrames;

        private float[][] instrumentBuffer, mixBuffer;

        private float[] ringBuffer;
        private int writeIndex;
        private int readIndex;

        private Barrier instrumentsDone;
        private CountdownEvent allShutdown;

        private bool running;
        private CancellationTokenSource cancellationTokenSource;

        public DSPPlayer(AudioNode[] instruments, AudioNode mixer, int channels, int minRingBufferFrames)
        {
            this.instruments = instruments;
            this.mixer = mixer;
            this.channels = channels;
            ringBufferFrames = ((minRingBufferFrames % BUFFER_FRAMES == 0) ?
                minRingBufferFrames :
                ((minRingBufferFrames / BUFFER_FRAMES) + 1) * BUFFER_FRAMES) + 1;
        }

        public void Start(Context context)
        {
            int instrumentCount = instruments.Length;
            instrumentBuffer = new float[instrumentCount][];
            mixBuffer = new float[instrumentCount][];
            instrumentsDone = new Barrier(instrumentCount + 1, _ =>
            {
                (mixBuffer, instrumentBuffer) = (instrumentBuffer, mixBuffer);
            });
            allShutdown = new CountdownEvent(instrumentCount + 1);
            ringBuffer = new float[ringBufferFrames * channels];
            readIndex = 0;
            writeIndex = 0;

            for (int i = 0; i < instrumentCount; i++)
            {
                instrumentBuffer[i] = new float[BUFFER_FRAMES * channels];
                mixBuffer[i] = new float[BUFFER_FRAMES * channels];
            }

            this.context = context;
            running = true;

            cancellationTokenSource = new CancellationTokenSource();

            for (int i = 0; i < instrumentCount; i++)
            {
                int id = i;
                instruments[i].Initialize();
                ThreadPool.QueueUserWorkItem(_ => InstrumentThread(cancellationTokenSource.Token, id));
            }

            mixer.Initialize();
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
            }).Start();
        }

        private void InstrumentThread(CancellationToken token, int id)
        {
            try
            {
                var instrument = instruments[id];
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
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"InstrumentThread {id} encountered an error: {ex.Message}\n{ex.StackTrace}");
                cancellationTokenSource.Cancel();
            }
            finally
            {
                allShutdown.Signal();
                Debug.Log($"InstrumentThread {id} has shut down.");
            }
        }

        private void MixerThread(CancellationToken token)
        {
            try
            {
                FloatValue[] outputValues = mixer.outputs.Select(o => (FloatValue)o.Value).ToArray();
                FloatValue[] inputValues = mixer.inputs.Select(i => (FloatValue)i.Value).ToArray();

                if (token.IsCancellationRequested || !running) return;
                instrumentsDone.SignalAndWait(token);

                while (!token.IsCancellationRequested && running)
                {
                    int required = BUFFER_FRAMES * channels;
                    while (RingBufferFreeSpace() < required)
                    {
                        if (token.IsCancellationRequested || !running) break;
                        Thread.Sleep(1);
                    }

                    if (token.IsCancellationRequested || !running) break;

                    for (int i = 0; i < BUFFER_FRAMES; i++)
                    {
                        for (int inst = 0; inst < instruments.Length; inst++)
                        {
                            var buffer = mixBuffer[inst];
                            for (int c = 0; c < channels; c++)
                            {
                                int index = i * channels + c;
                                inputValues[inst * channels + c].value = buffer[index];
                            }
                        }

                        mixer.Process(context);

                        for (int c = 0; c < channels; c++)
                        {
                            int rbIndex = (writeIndex + i * channels + c) % ringBuffer.Length;
                            ringBuffer[rbIndex] = outputValues[c].value;
                        }
                    }

                    writeIndex = (writeIndex + BUFFER_FRAMES * channels) % ringBuffer.Length;

                    if (token.IsCancellationRequested || !running) break;
                    instrumentsDone.SignalAndWait(token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.LogError($"MixerThread encountered an error: {ex.Message}\n{ex.StackTrace}");
                cancellationTokenSource.Cancel();
            }
            finally
            {
                allShutdown.Signal();
                Debug.Log("MixerThread has shut down.");
            }
        }

        private int RingBufferData()
        {
            return (ringBuffer.Length + writeIndex - readIndex) % ringBuffer.Length;
        }

        private int RingBufferFreeSpace()
        {
            return ringBuffer.Length - RingBufferData() - 1;
        }

        public bool TakeData(float[] data, int offset, int targetChannels, int frames, float volume)
        {
            int available = RingBufferData();

            if (available < frames * channels)
            {
                return false;
            }

            for (int i = 0; i < frames; i++)
            {
                if (targetChannels == channels)
                {
                    for (int c = 0; c < channels; c++)
                    {
                        int rbIndex = (readIndex + i * channels + c) % ringBuffer.Length;
                        data[offset + i * channels + c] = ringBuffer[rbIndex] * volume;
                    }
                }
                else if (targetChannels == 1)
                {
                    data[offset + i] = 0;
                    for (int c = 0; c < channels; c++)
                    {
                        int rbIndex = (readIndex + i * channels + c) % ringBuffer.Length;
                        data[offset + i] += ringBuffer[rbIndex] * (1.0f / channels) * volume;
                    }
                }
                else
                {
                    Debug.LogError($"Unsupported target channels: {targetChannels}. Only 1 or {channels} are supported.");
                    return false;
                }
            }
            readIndex = (readIndex + frames * channels) % ringBuffer.Length;
            return true;
        }
    }
}