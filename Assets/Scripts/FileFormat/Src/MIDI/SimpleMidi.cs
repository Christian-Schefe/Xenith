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
                string trackName = null;
                string instrumentName = null;
                (int, bool)? keySignature = null;

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
                        else if (metaEvent.metaType == 0x03) // Track Name
                        {
                            if (string.IsNullOrEmpty(trackName))
                            {
                                trackName = System.Text.Encoding.UTF8.GetString(metaEvent.metaData);
                            }
                        }
                        else if (metaEvent.metaType == 0x04) // Instrument Name
                        {
                            if (string.IsNullOrEmpty(instrumentName))
                            {
                                instrumentName = System.Text.Encoding.UTF8.GetString(metaEvent.metaData);
                            }
                        }
                        else if (metaEvent.metaType == 0x59) // Key Signature
                        {
                            if (keySignature == null)
                            {
                                int key = metaEvent.metaData[0];
                                bool isMajor = metaEvent.metaData[1] == 0;
                                keySignature = (key, isMajor);
                                Debug.Log($"Key Signature: {key} {(isMajor ? "Major" : "Minor")} at time {time / (float)division} seconds");
                            }
                        }
                    }
                }

                if (notes.Count == 0) continue;

                if (string.IsNullOrEmpty(trackName)) trackName = string.IsNullOrEmpty(instrumentName) ? "Unnamed Track" : instrumentName;
                var signature = keySignature ?? (0, true); // Default to C Major if no key signature is found
                tracks.Add(new SimpleMidiTrack(trackName, signature, notes));
            }

            return new SimpleMidi(tracks, tempoEvents);
        }
    }

    public class SimpleMidiTrack
    {
        public string name;
        public (int key, bool isMajor) keySignature;
        public List<SimpleMidiNote> notes;

        public SimpleMidiTrack(string name, (int key, bool isMajor) keySignature, List<SimpleMidiNote> notes)
        {
            this.name = name;
            this.keySignature = keySignature;
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
