using System.Collections.Generic;
using UnityEngine;

namespace FileFormat
{
    public class SimpleMidi
    {
        public List<SimpleMidiTrack> tracks;
        public List<SimpleMidiTempoEvent> tempoEvents;

        public SimpleMidi(List<SimpleMidiTrack> tracks, List<SimpleMidiTempoEvent> tempoEvents)
        {
            this.tempoEvents = tempoEvents;
            this.tracks = tracks;
        }

        public static SimpleMidi ReadFromFile(string path)
        {
            var file = MidiFile.ReadFromFile(path);
            return FromMidiFile(file);
        }

        public static SimpleMidi FromMidiFile(MidiFile midi)
        {
            int division = midi.headerChunk.division;
            var tracks = new List<SimpleMidiTrack>();
            var tempoEvents = new List<SimpleMidiTempoEvent>();
            foreach (var track in midi.trackChunks)
            {
                int time = 0;
                Dictionary<(int, int), SimpleMidiNote> onNotes = new();
                List<SimpleMidiNote> notes = new();

                void TurnOnNote(int channel, int pitch, int velocity, float start)
                {
                    var noteKey = (channel, pitch);
                    onNotes[noteKey] = new SimpleMidiNote(pitch, velocity / 127f, start, 0f);
                }

                void TurnOffNote(int channel, int pitch, float end)
                {
                    var noteKey = (channel, pitch);
                    if (onNotes.TryGetValue(noteKey, out var note))
                    {
                        note.duration = end - note.start;
                        onNotes.Remove(noteKey);
                        notes.Add(note);
                    }
                }

                foreach (var e in track.events)
                {
                    time += e.deltaTime;
                    if (e is MidiChannelEvent channelEvent)
                    {
                        if (channelEvent.type == MidiChannelEventType.NoteOn)
                        {
                            int pitch = channelEvent.eventData[0];
                            int velocity = channelEvent.eventData[1];
                            if (velocity == 0)
                            {
                                TurnOffNote(channelEvent.channel, pitch, time / (float)division);
                            }
                            else
                            {
                                TurnOnNote(channelEvent.channel, pitch, velocity, time / (float)division);
                            }
                        }
                        else if (channelEvent.type == MidiChannelEventType.NoteOff)
                        {
                            int pitch = channelEvent.eventData[0];
                            TurnOffNote(channelEvent.channel, pitch, time / (float)division);
                        }
                    }
                    else if (e is MidiMetaEvent metaEvent)
                    {
                        if (metaEvent.metaType == 0x51) // Set Tempo
                        {
                            int microsecondsPerBeat = (metaEvent.metaData[0] << 16) | (metaEvent.metaData[1] << 8) | metaEvent.metaData[2];
                            float bpm = 60000000f / microsecondsPerBeat;
                            tempoEvents.Add(new SimpleMidiTempoEvent(time / (float)division, bpm));
                        }
                    }
                }

                if (notes.Count == 0) continue;

                tracks.Add(new SimpleMidiTrack(notes));
            }

            return new SimpleMidi(tracks, tempoEvents);
        }
    }

    public class SimpleMidiTrack
    {
        public List<SimpleMidiNote> notes;

        public SimpleMidiTrack(List<SimpleMidiNote> notes)
        {
            this.notes = notes;
        }
    }

    public class SimpleMidiNote
    {
        public int pitch;
        public float velocity;
        public float start;
        public float duration;

        public SimpleMidiNote(int pitch, float velocity, float start, float duration)
        {
            this.pitch = pitch;
            this.velocity = velocity;
            this.start = start;
            this.duration = duration;
        }
    }

    public class SimpleMidiTempoEvent
    {
        public float time;
        public float bpm;

        public SimpleMidiTempoEvent(float time, float bpm)
        {
            this.time = time;
            this.bpm = bpm;
        }
    }
}
