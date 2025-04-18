using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DSP
{
    public struct SequencerNote
    {
        public float pitch;
        public float time;
        public float duration;

        public SequencerNote(float pitch, float time, float duration)
        {
            this.pitch = pitch;
            this.time = time;
            this.duration = duration;
        }
    }

    public class Sequencer : AudioNode
    {
        private readonly System.Func<AudioNode> voiceFactory;
        private readonly List<AudioNode> voices = new();
        private readonly List<(float endTime, float pitch)> voiceData = new();
        private readonly List<(FloatValue, BoolValue)> voiceInputs = new();

        private readonly List<NamedValue<FloatValue>> floatOutputs;
        private readonly List<SequencerNote> notes;

        private int noteIndex = 0;
        private float time = 0;

        public Sequencer(List<SequencerNote> notes, System.Func<AudioNode> voiceFactory)
        {
            this.voiceFactory = voiceFactory;
            this.notes = notes;
            floatOutputs = new();

            var template = voiceFactory();
            var templateInputs = template.BuildInputs();
            if (templateInputs.Count != 2)
            {
                throw new System.Exception("Invalid Input");
            }
            if (templateInputs[0].Value.Type != ValueType.Float || templateInputs[1].Value.Type != ValueType.Bool)
            {
                throw new System.Exception("Invalid Input");
            }

            var templateOutputs = template.BuildOutputs();
            foreach (var output in templateOutputs)
            {
                if (output.Value.Type != ValueType.Float)
                {
                    throw new System.Exception($"Invalid Output of type {output.Value.Type}");
                }
                floatOutputs.Add(new(output.name, (FloatValue)output.Value.Clone()));
            }
        }

        public override List<NamedValue> BuildInputs() => new();

        public override List<NamedValue> BuildOutputs() => floatOutputs.Cast<NamedValue>().ToList();

        public override void Process(Context context)
        {
            time += context.deltaTime;

            if (noteIndex < notes.Count)
            {
                var note = notes[noteIndex];
                if (time >= note.time)
                {
                    var voice = voiceFactory();
                    voice.Initialize();
                    voiceData.Add((note.time + note.duration, note.pitch));
                    voiceInputs.Add(((FloatValue)voice.inputs[0].Value, (BoolValue)voice.inputs[1].Value));
                    voices.Add(voice);
                    noteIndex++;
                }

                while (voiceData.Count > 0 && voiceData[^1].endTime + 1 < time)
                {
                    voiceData.RemoveAt(voiceData.Count - 1);
                    voices.RemoveAt(voices.Count - 1);
                }
            }

            //Debug.Log($"Time: {time}, NoteIndex: {noteIndex}, Voices: {voices.Count}");

            foreach (var output in floatOutputs)
            {
                output.value.value = 0;
            }
            for (int i = 0; i < voices.Count; i++)
            {
                var voice = voices[i];

                voiceInputs[i].Item1.value = voiceData[i].pitch;
                voiceInputs[i].Item2.value = time < voiceData[i].endTime;

                voice.Process(context);
                for (int j = 0; j < floatOutputs.Count; j++)
                {
                    var output = floatOutputs[j];
                    var voiceOutput = voices[i].BuildOutputs()[j];
                    output.value.value += ((FloatValue)voiceOutput.Value).value;
                }
            }
        }

        public override void ResetState()
        {
            voices.Clear();
            voiceData.Clear();
            voiceInputs.Clear();

            noteIndex = 0;
            time = 0;
        }
    }
}