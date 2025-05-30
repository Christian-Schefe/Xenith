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
                Dictionary<int, Dictionary<int, SimpleMidiNote>> onNotes = new();
                Dictionary<int, List<SimpleMidiNote>> notes = new();
                Dictionary<int, string> instrumentName = new();
                Dictionary<int, string> patchName = new();
                Dictionary<int, (int, bool)> keySignature = new();
                string trackName = null;
                int? currentChannel = null;

                int[] GetChannels() => currentChannel.HasValue ? new int[] { currentChannel.Value } : new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

                void TurnOnNote(int channel, int pitch, int velocity, float start)
                {
                    if (!onNotes.TryGetValue(channel, out var channelNotes))
                    {
                        channelNotes = new Dictionary<int, SimpleMidiNote>();
                        onNotes[channel] = channelNotes;
                    }
                    channelNotes[pitch] = new SimpleMidiNote(pitch, velocity / 127f, start, 0f);
                }

                void TurnOffNote(int channel, int pitch, float end)
                {
                    if (!onNotes.TryGetValue(channel, out var channelNotes))
                    {
                        channelNotes = new Dictionary<int, SimpleMidiNote>();
                        onNotes[channel] = channelNotes;
                    }
                    if (channelNotes.TryGetValue(pitch, out var note))
                    {
                        note.duration = end - note.start;
                        channelNotes.Remove(pitch);
                        if (!notes.TryGetValue(channel, out var channelNoteList))
                        {
                            channelNoteList = new List<SimpleMidiNote>();
                            notes[channel] = channelNoteList;
                        }
                        channelNoteList.Add(note);
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
                        else if (channelEvent.type == MidiChannelEventType.ProgramChange)
                        {
                            int program = channelEvent.eventData[0];
                            if (MidiPatchInfo.GetPatchName(program, out var name))
                            {
                                patchName[channelEvent.channel] = name;
                            }
                            Debug.Log($"Program Change: {program} ({patchName.GetValueOrDefault(channelEvent.channel, program.ToString())}) at time {time / (float)division} seconds for channel {channelEvent.channel}");
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
                                Debug.Log($"Track Name: {trackName} at time {time / (float)division} seconds");
                            }
                        }
                        else if (metaEvent.metaType == 0x20) // MIDI Channel Prefix
                        {
                            currentChannel = metaEvent.metaData[0];
                        }
                        else if (metaEvent.metaType == 0x04) // Instrument Name
                        {
                            foreach (var channel in GetChannels())
                            {
                                instrumentName[channel] = System.Text.Encoding.UTF8.GetString(metaEvent.metaData);
                                Debug.Log($"Instrument Name: {instrumentName[channel]} at time {time / (float)division} seconds for channel {channel}");
                            }
                        }
                        else if (metaEvent.metaType == 0x59) // Key Signature
                        {
                            foreach (var channel in GetChannels())
                            {
                                if (!keySignature.ContainsKey(channel))
                                {
                                    int key = metaEvent.metaData[0];
                                    bool isMajor = metaEvent.metaData[1] == 0;
                                    keySignature.Add(channel, (key, isMajor));
                                    Debug.Log($"Key Signature: {key} {(isMajor ? "Major" : "Minor")} at time {time / (float)division} seconds");
                                }
                            }
                        }
                    }
                }

                foreach (var (channel, noteList) in notes)
                {
                    var instName = instrumentName.TryGetValue(channel, out var _instName) && !string.IsNullOrEmpty(_instName) ? _instName : null;
                    var pName = patchName.TryGetValue(channel, out var _pName) && !string.IsNullOrEmpty(_pName) ? _pName : null;
                    var tName = string.IsNullOrEmpty(trackName) ? trackName : null;
                    var specialName = channel == 9 ? "Drums" : null; // Channel 9 is typically used for drums in MIDI files
                    var name = instName ?? pName ?? tName ?? specialName ?? $"Unnamed Track Ch.{channel + 1}";

                    var signature = keySignature.TryGetValue(channel, out var ks) ? ks : (0, true); // Default to C Major if no key signature is found
                    tracks.Add(new SimpleMidiTrack(name, signature, noteList));
                }
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
