using DSP;
using NodeGraph;
using PianoRoll;
using ReactiveData.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveData.App
{
    public class ReactiveTrack : IKeyed
    {
        public Reactive<string> name;
        public Reactive<DTO.NodeResource> instrument;
        public Reactive<bool> isMuted;
        public Reactive<bool> isSoloed;
        public Reactive<float> volume;
        public Reactive<float> pan;
        public Reactive<MusicKey> keySignature;
        public ReactiveList<ReactiveNote> notes;

        private readonly ReactiveFloatNode volumeNode;
        public ReactiveFloatNode VolumeNode => volumeNode;

        public static ReactiveTrack Default => new("New Track", new("piano", false), false, false, 0.75f, 0.0f, MusicKey.CMajor, new());

        public ReactiveTrack(string name, DTO.NodeResource instrument, bool isMuted, bool isSoloed, float volume, float pan, MusicKey keySignature, ReactiveList<ReactiveNote> notes)
        {
            this.name = new(name);
            this.instrument = new(instrument);
            this.isMuted = new(isMuted);
            this.isSoloed = new(isSoloed);
            this.volume = new(volume);
            this.pan = new(pan);
            this.notes = notes;
            this.keySignature = new(keySignature);
            volumeNode = new ReactiveFloatNode(this.volume);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;

        public AudioNode BuildAudioNode(float startTime, List<ReactiveTempoEvent> tempoEvents)
        {
            var graphDatabase = Globals<GraphDatabase>.Instance;
            if (!graphDatabase.GetNodeFromTypeId(instrument.Value, null, out var audioNode))
            {
                throw new Exception($"Failed to create audio node of type {instrument}");
            }
            var sequencer = new Sequencer(startTime, BuildNotes(tempoEvents), () => audioNode.Clone());
            return sequencer;
        }

        private List<SequencerNote> BuildNotes(List<ReactiveTempoEvent> tempoEvents)
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var unsortedTempoNotes = notes.Select(note =>
            {
                var pitch = noteEditor.Key.StepsToFreq(note.pitch.Value);
                return new TempoNote(note.beat.Value, note.length.Value, pitch);
            }).ToList();
            var sequencerNotes = TempoController.ConvertNotes(unsortedTempoNotes, tempoEvents);
            return sequencerNotes;
        }

        public float GetDuration(List<ReactiveTempoEvent> tempoEvents)
        {
            var notes = BuildNotes(tempoEvents);
            if (notes.Count == 0) return 0;
            var lastNote = notes[^1];
            return lastNote.time + lastNote.duration;
        }
    }
}
