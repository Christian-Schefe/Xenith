using System.Collections.Generic;
using UnityEngine;

namespace DSP
{
    public class DPSTest : MonoBehaviour
    {
        private FastRandom random = new(1234);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var semitone = Mathf.Pow(2, 1f / 12f);

                var noteEditor = Globals<PianoRoll.NoteEditor>.Instance;
                var notes = noteEditor.Serialize().GetNotes();
                foreach (var note in notes)
                {
                    Debug.Log($"Note: {note.pitch}, {note.time}, {note.duration}");
                }

                var node = new Sequencer(notes, SimpleInstrument);

                var dsp = Globals<DSP>.Instance;
                dsp.Initialize(node);
            }
        }

        public struct FastRandom
        {
            private uint state;

            public FastRandom(uint seed)
            {
                state = seed != 0 ? seed : 1;
            }

            public float NextFloat()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;

                return (state & 0xFFFFFF) / (float)(1 << 24);
            }
        }

        private AudioNode SimpleInstrument()
        {
            var node = new NodeGraph();

            var freq = node.AddInput<FloatValue>("Frequency");
            var gate = node.AddInput<BoolValue>("Gate");

            var left = node.AddOutput<FloatValue>("left");
            var right = node.AddOutput<FloatValue>("right");

            var adsr = node.AddNode(new ADSR(0.1f, 1.0f, 0.5f, 0.1f));
            var osc = node.AddNode(Oscillator.New(Oscillator.WaveformType.Square, random.NextFloat()));

            node.AddConnection(new(gate, 0, adsr, 0));
            node.AddConnection(new(freq, 0, osc, 0));
            node.AddConnection(new(adsr, 0, osc, 1));

            node.AddConnection(new(osc, 0, left, 0));
            node.AddConnection(new(osc, 0, right, 0));

            return node;
        }
    }
}