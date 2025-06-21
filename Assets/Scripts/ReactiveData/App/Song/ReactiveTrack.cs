using DSP;
using NodeGraph;
using PianoRoll;
using ReactiveData.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveData.App
{
    public class ReactiveTrackBase : IKeyed
    {
        public Reactive<string> name;
        public ReactiveList<DTO.NodeResource> effects;
        public Reactive<float> volume;
        public Reactive<float> pan;

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;

        protected virtual ReactiveGainPanNode GetReactiveGainPanNode()
        {
            return new ReactiveGainPanNode(false, volume, pan);
        }

        public AudioNode BuildEffects()
        {
            var graphDatabase = Globals<GraphDatabase>.Instance;
            var effectNodes = effects.Select(effect =>
            {
                if (!graphDatabase.GetNodeFromTypeId(effect, out var effectNode))
                {
                    throw new Exception($"Failed to create effect node of type {effect.id}");
                }
                return effectNode;
            }).ToList();
            effectNodes.Add(GetReactiveGainPanNode());
            var effectPipeline = new Pipeline(effectNodes.ToArray());
            return effectPipeline;
        }
    }

    public class ReactiveTrack : ReactiveTrackBase
    {
        public Reactive<DTO.NodeResource> instrument;
        public Reactive<bool> isMuted;
        public Reactive<bool> isSoloed;
        public Reactive<MusicKey> keySignature;
        public ReactiveList<ReactiveNote> notes;

        public Reactive<bool> isBGVisible = new(false);

        public static ReactiveTrack Default => new("New Track", new("default_synth", true), new List<DTO.NodeResource>(), false, false, 0.75f, 0.0f, MusicKey.CMajor, new List<ReactiveNote>());

        public ReactiveTrack(string name, DTO.NodeResource instrument, IEnumerable<DTO.NodeResource> effects, bool isMuted, bool isSoloed, float volume, float pan, MusicKey keySignature, IEnumerable<ReactiveNote> notes)
        {
            this.name = new(name);
            this.instrument = new(instrument);
            this.effects = new(effects);
            this.isMuted = new(isMuted);
            this.isSoloed = new(isSoloed);
            this.volume = new(volume);
            this.pan = new(pan);
            this.notes = new(notes);
            this.keySignature = new(keySignature);
        }

        public DSPInstrument BuildInstrument(float startTime, IList<ReactiveTempoEvent> tempoEvents)
        {
            var graphDatabase = Globals<GraphDatabase>.Instance;
            if (!graphDatabase.GetNodeFromTypeId(instrument.Value, out var audioNode))
            {
                throw new Exception($"Failed to create audio node of type {instrument.Value.id}");
            }
            var sequencer = new Sequencer(startTime, BuildNotes(tempoEvents), () => audioNode.Clone());
            return new(sequencer, BuildEffects());
        }

        private List<SequencerNote> BuildNotes(IList<ReactiveTempoEvent> tempoEvents)
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

        public float GetDuration(IList<ReactiveTempoEvent> tempoEvents)
        {
            var notes = BuildNotes(tempoEvents);
            if (notes.Count == 0) return 0;
            var lastNote = notes[^1];
            return lastNote.time + lastNote.duration;
        }
    }

    public class ReactiveMasterTrack : ReactiveTrackBase
    {
        public static ReactiveMasterTrack Default => new(new List<DTO.NodeResource>(), 0.75f, 0.0f);

        public ReactiveMasterTrack(IEnumerable<DTO.NodeResource> effects, float volume, float pan)
        {
            name = new("Master");
            this.effects = new(effects);
            this.volume = new(volume);
            this.pan = new(pan);
        }

        public DSPMaster BuildMaster()
        {
            return new(BuildEffects());
        }

        protected override ReactiveGainPanNode GetReactiveGainPanNode()
        {
            return new ReactiveGainPanNode(true, volume, pan);
        }
    }
}
