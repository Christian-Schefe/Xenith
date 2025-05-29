using PianoRoll;
using System.Collections.Generic;

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

    public struct Song
    {
        public List<Track> tracks;
        public List<TempoEvent> tempoEvents;

        public Song(List<Track> tracks, List<TempoEvent> tempoEvents)
        {
            this.tracks = tracks;
            this.tempoEvents = tempoEvents;
        }
    }

    public struct Track
    {
        public string name;
        public NodeResource instrument;
        public bool isMuted;
        public bool isSoloed;
        public float volume;
        public float pan;
        public MusicKey keySignature;
        public List<Note> notes;

        public Track(string name, NodeResource instrument, bool isMuted, bool isSoloed, float volume, float pan, MusicKey keySignature, List<Note> notes)
        {
            this.name = name;
            this.instrument = instrument;
            this.isMuted = isMuted;
            this.isSoloed = isSoloed;
            this.volume = volume;
            this.pan = pan;
            this.keySignature = keySignature;
            this.notes = notes;
        }
    }

    public struct TempoEvent
    {
        public float beat;
        public float bps;

        public TempoEvent(float beat, float bps)
        {
            this.beat = beat;
            this.bps = bps;
        }
    }

    public struct Note
    {
        public float beat;
        public int pitch;
        public float velocity;
        public float length;

        public Note(float beat, int pitch, float velocity, float length)
        {
            this.beat = beat;
            this.pitch = pitch;
            this.length = length;
            this.velocity = velocity;
        }
    }
}
