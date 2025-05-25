using DSP;
using NodeGraph;
using PianoRoll;
using System.Collections.Generic;
using System.Linq;

namespace DTO
{
    public class SongID
    {
        public string path;

        public SongID(string path)
        {
            this.path = path;
        }

        public virtual string GetName()
        {
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public override bool Equals(object obj)
        {
            if (obj is SongID other)
            {
                return path == other.path;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return path.GetHashCode();
        }

        public static bool operator ==(SongID a, SongID b)
        {
            return a is null ? b is null : a.Equals(b);
        }

        public static bool operator !=(SongID a, SongID b)
        {
            return a is null ? b is not null : !a.Equals(b);
        }
    }

    public class UnsavedSongID : SongID
    {
        public int index;

        public UnsavedSongID(int index) : base(null)
        {
            this.index = index;
        }

        public override string GetName()
        {
            return $"Untitled {index}";
        }

        public override bool Equals(object obj)
        {
            if (obj is UnsavedSongID other)
            {
                return index == other.index;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return index.GetHashCode();
        }
    }

    public class Song
    {
        public List<Track> tracks;
        public List<TempoEvent> tempoEvents;

        public Song()
        {
            tracks = new List<Track>();
            tempoEvents = new List<TempoEvent>();
        }

        public static Song Default()
        {
            var song = new Song();
            song.tracks.Add(Track.Default());
            song.tempoEvents.Add(new(0, 2));
            return song;
        }

        public bool IsEmpty()
        {
            if (tracks.Count == 0) return true;
            if (tracks.Count > 1) return false;
            var track = tracks[0];
            return track.notes.Count == 0;
        }

        public AudioNode[] BuildInstrumentNodes(float startTime)
        {
            var hasSoloTracks = tracks.Any(t => t.isSoloed);
            var filteredTracks = tracks.Where(t => (!hasSoloTracks || t.isSoloed) && !t.isMuted).ToList();
            var nodes = new AudioNode[filteredTracks.Count];

            for (int i = 0; i < filteredTracks.Count; i++)
            {
                var track = filteredTracks[i];
                nodes[i] = track.BuildAudioNode(startTime, tempoEvents);
            }

            return nodes;
        }

        public AudioNode BuildMixerNode()
        {
            var hasSoloTracks = tracks.Any(t => t.isSoloed);
            var filteredTracks = tracks.Where(t => (!hasSoloTracks || t.isSoloed) && !t.isMuted).ToList();

            var graph = new DSP.NodeGraph();
            int outLeft = graph.AddOutput<FloatValue>("Left", 0);
            int outRight = graph.AddOutput<FloatValue>("Right", 1);
            int mixLeft = graph.AddNode(Prelude.Mix(filteredTracks.Count));
            int mixRight = graph.AddNode(Prelude.Mix(filteredTracks.Count));
            graph.AddConnection(new(mixLeft, 0, outLeft, 0));
            graph.AddConnection(new(mixRight, 0, outRight, 0));

            for (int i = 0; i < filteredTracks.Count; i++)
            {
                var volume = graph.AddNode(filteredTracks[i].VolumeNode);
                int inLeft = graph.AddInput<FloatValue>($"Left {i}", 2 * i);
                int inRight = graph.AddInput<FloatValue>($"Right {i}", 2 * i + 1);
                graph.AddConnection(new(inLeft, 0, mixLeft, 2 * i));
                graph.AddConnection(new(inRight, 0, mixRight, 2 * i));
                graph.AddConnection(new(volume, 0, mixLeft, 2 * i + 1));
                graph.AddConnection(new(volume, 0, mixRight, 2 * i + 1));
            }
            return graph;
        }

        public float GetDuration()
        {
            if (tracks.Count == 0) return 0;
            return tracks.Select(t => t.GetDuration(tempoEvents)).Max();
        }
    }

    public class Track
    {
        public string name;
        public NodeResource instrument;
        public bool isMuted;
        public bool isSoloed;
        public float volume;
        public float pan;
        public List<Note> notes;

        [System.NonSerialized]
        private ConstFloatNode volumeNode = null;
        public ConstFloatNode VolumeNode => volumeNode ??= ConstFloatNode.New(volume);

        public Track() { }

        public Track(string name, NodeResource instrument, bool isMuted, bool isSoloed, float volume, float pan, List<Note> notes)
        {
            this.name = name;
            this.instrument = instrument;
            this.isMuted = isMuted;
            this.isSoloed = isSoloed;
            this.volume = volume;
            this.pan = pan;
            this.notes = notes;
        }

        public void SetVolume(float volume)
        {
            this.volume = volume;
            VolumeNode.valueSetting.value = volume;
            VolumeNode.OnSettingsChanged();
        }

        public static Track Default()
        {
            return new Track("New Track", new NodeResource("piano", false), false, false, 0.9f, 0.0f, new());
        }

        public AudioNode BuildAudioNode(float startTime, List<TempoEvent> tempoEvents)
        {
            var graphDatabase = Globals<GraphDatabase>.Instance;
            if (!graphDatabase.GetNodeFromTypeId(instrument, null, out var audioNode))
            {
                throw new System.Exception($"Failed to create audio node of type {instrument}");
            }
            var sequencer = new Sequencer(startTime, BuildNotes(tempoEvents), () => audioNode.Clone());
            return sequencer;
        }

        private List<SequencerNote> BuildNotes(List<TempoEvent> tempoEvents)
        {
            var noteEditor = Globals<NoteEditor>.Instance;
            var unsortedTempoNotes = notes.Select(note =>
            {
                var pitch = noteEditor.Key.StepsToFreq(note.y);
                return new TempoNote(note.x, note.length, pitch);
            }).ToList();
            var sequencerNotes = TempoController.ConvertNotes(unsortedTempoNotes, tempoEvents);
            return sequencerNotes;
        }

        public float GetDuration(List<TempoEvent> tempoEvents)
        {
            var notes = BuildNotes(tempoEvents);
            if (notes.Count == 0) return 0;
            var lastNote = notes[^1];
            return lastNote.time + lastNote.duration;
        }
    }

    public class TempoEvent
    {
        public float beat;
        public float bps;

        public TempoEvent(float beat, float bps)
        {
            this.beat = beat;
            this.bps = bps;
        }

        public TempoEvent() { }
    }

    public class Note
    {
        public float x;
        public int y;
        public float length;

        public Note(float x, int y, float length)
        {
            this.x = x;
            this.y = y;
            this.length = length;
        }

        public Note() { }
    }
}
