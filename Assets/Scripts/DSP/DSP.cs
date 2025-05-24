using System.Threading;
using UnityEngine;

namespace DSP
{
    public class DSP : MonoBehaviour
    {
        private AudioNode mixer;
        private AudioNode[] instruments;

        private bool isInitialized;
        private Context context;
        public FloatValue playTime = new();

        public float volume = 1.0f;

        private const int BUFFER_SIZE = 256;
        private const int CHANNEL_COUNT = 2;
        private const int RING_BUFFER_SIZE = 65536;

        private float[][] instrumentBuffers;
        private float[] ringBuffer;
        private int writeIndex;
        private int readIndex;

        private Barrier instrumentsDone;
        private CountdownEvent allShutdown;

        private bool running;

        public void Initialize(AudioNode[] instruments, AudioNode mixer)
        {
            this.instruments = instruments;
            this.mixer = mixer;

            int instrumentCount = instruments.Length;
            instrumentBuffers = new float[instrumentCount][];
            instrumentsDone = new Barrier(instrumentCount + 1);
            allShutdown = new CountdownEvent(instrumentCount + 1);
            ringBuffer = new float[RING_BUFFER_SIZE];
            readIndex = 0;
            writeIndex = 0;

            for (int i = 0; i < instrumentCount; i++)
            {
                instrumentBuffers[i] = new float[BUFFER_SIZE * CHANNEL_COUNT];
            }

            playTime.value = 0;
            context = new Context(AudioSettings.outputSampleRate);
            running = true;

            for (int i = 0; i < instrumentCount; i++)
            {
                int id = i;
                instruments[i].Initialize();
                ThreadPool.QueueUserWorkItem(_ => InstrumentThread(id));
            }

            ThreadPool.QueueUserWorkItem(_ => MixerThread());
            mixer.Initialize();
            isInitialized = true;
        }

        public void ResetDSP()
        {
            isInitialized = false;
            running = false;
            new Thread(() =>
            {
                allShutdown.Wait();
                instrumentsDone.Dispose();
                allShutdown.Dispose();
                Debug.Log("DSP has been reset.");
            }).Start();
        }

        private void InstrumentThread(int id)
        {
            try
            {
                var instrument = instruments[id];
                var buffer = instrumentBuffers[id];

                FloatValue[] outputValues = new FloatValue[CHANNEL_COUNT];
                for (int i = 0; i < CHANNEL_COUNT; i++)
                    outputValues[i] = (FloatValue)instrument.outputs[i].Value;

                while (running)
                {
                    for (int i = 0; i < BUFFER_SIZE; i++)
                    {
                        instrument.Process(context);
                        for (int c = 0; c < CHANNEL_COUNT; c++)
                            buffer[i * CHANNEL_COUNT + c] = outputValues[c].value;
                    }

                    instrumentsDone.SignalAndWait(1000);
                    instrumentsDone.SignalAndWait(1000);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"InstrumentThread {id} encountered an error: {ex.Message} {ex.StackTrace}");
            }
            finally
            {
                allShutdown.Signal();
                Debug.Log($"InstrumentThread {id} has shut down.");
            }
        }

        private void MixerThread()
        {
            try
            {
                Debug.Log("Starting Mixing..." + running);
                FloatValue[] outputValues = new FloatValue[CHANNEL_COUNT];
                FloatValue[] inputValues = new FloatValue[instruments.Length * CHANNEL_COUNT];

                for (int i = 0; i < CHANNEL_COUNT; i++)
                    outputValues[i] = (FloatValue)mixer.outputs[i].Value;

                for (int i = 0; i < inputValues.Length; i++)
                    inputValues[i] = (FloatValue)mixer.inputs[i].Value;

                while (running)
                {
                    instrumentsDone.SignalAndWait(1000);

                    // Ensure ring buffer has enough space
                    int required = BUFFER_SIZE * CHANNEL_COUNT;
                    while (RingBufferFreeSpace() <= required)
                    {
                        if (!running) break;
                        Thread.Sleep(1);
                    }

                    if (!running)
                    {
                        instrumentsDone.SignalAndWait(1000);
                        break;
                    }

                    for (int i = 0; i < BUFFER_SIZE; i++)
                    {
                        for (int inst = 0; inst < instruments.Length; inst++)
                        {
                            for (int c = 0; c < CHANNEL_COUNT; c++)
                            {
                                int index = i * CHANNEL_COUNT + c;
                                inputValues[inst * CHANNEL_COUNT + c].value = instrumentBuffers[inst][index];
                            }
                        }

                        mixer.Process(context);

                        for (int c = 0; c < CHANNEL_COUNT; c++)
                        {
                            int rbIndex = (writeIndex + i * CHANNEL_COUNT + c) % RING_BUFFER_SIZE;
                            ringBuffer[rbIndex] = outputValues[c].value;
                        }
                    }

                    writeIndex = (writeIndex + BUFFER_SIZE * CHANNEL_COUNT) % RING_BUFFER_SIZE;

                    instrumentsDone.SignalAndWait(1000);
                }

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"MixerThread encountered an error: {ex.Message} {ex.StackTrace}");
            }
            finally
            {
                allShutdown.Signal();
                Debug.Log("MixerThread has shut down.");
            }
        }

        private int RingBufferData()
        {
            return (RING_BUFFER_SIZE + writeIndex - readIndex) % RING_BUFFER_SIZE;
        }

        private int RingBufferFreeSpace()
        {
            return RING_BUFFER_SIZE - RingBufferData() - 1;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isInitialized) return;

            int frameCount = data.Length / channels;
            int available = RingBufferData();

            if (available < data.Length)
            {
                Debug.LogWarning($"Not enough data in ring buffer: {available}/{data.Length}");
                return;
            }

            for (int i = 0; i < frameCount; i++)
            {
                playTime.value += context.deltaTime;
                for (int c = 0; c < channels; c++)
                {
                    int rbIndex = (readIndex + i * channels + c) % RING_BUFFER_SIZE;
                    data[i * channels + c] = ringBuffer[rbIndex] * volume;
                }
            }

            readIndex = (readIndex + data.Length) % RING_BUFFER_SIZE;
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
