using DTO;
using ReactiveData.Core;
using System.Linq;

namespace ReactiveData.App
{
    public static class DTOConverter
    {
        public static Song Serialize(ReactiveSong song)
        {
            var tracksList = song.tracks.Select(Serialize).ToList();
            var tempoEventsList = song.tempoEvents.Select(Serialize).ToList();
            return new Song(tracksList, tempoEventsList);
        }

        public static Track Serialize(ReactiveTrack track)
        {
            var notesList = track.notes.Select(Serialize).ToList();
            return new(track.name.Value, track.instrument.Value, track.isMuted.Value, track.isSoloed.Value, track.volume.Value, track.pan.Value, track.keySignature.Value, notesList);
        }

        public static Note Serialize(ReactiveNote note)
        {
            return new(note.beat.Value, note.pitch.Value, note.velocity.Value, note.length.Value);
        }

        public static TempoEvent Serialize(ReactiveTempoEvent tempoEvent)
        {
            return new(tempoEvent.beat.Value, tempoEvent.bps.Value);
        }

        public static ReactiveSong Deserialize(Song song)
        {
            var tracks = new ReactiveList<ReactiveTrack>(song.tracks.Select(Deserialize).ToList());
            var tempoEvents = new ReactiveList<ReactiveTempoEvent>(song.tempoEvents.Select(Deserialize).ToList());
            return new ReactiveSong(tracks, tempoEvents);
        }

        public static ReactiveTrack Deserialize(Track track)
        {
            var notes = new ReactiveList<ReactiveNote>(track.notes.Select(Deserialize).ToList());
            return new ReactiveTrack(track.name, track.instrument, track.isMuted, track.isSoloed, track.volume, track.pan, track.keySignature, notes);
        }

        public static ReactiveNote Deserialize(Note note)
        {
            return new ReactiveNote(note.beat, note.pitch, note.velocity, note.length);
        }

        public static ReactiveTempoEvent Deserialize(TempoEvent tempoEvent)
        {
            return new ReactiveTempoEvent(tempoEvent.beat, tempoEvent.bps);
        }
    }
}
