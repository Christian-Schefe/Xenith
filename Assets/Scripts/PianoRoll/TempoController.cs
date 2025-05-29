using DSP;
using ReactiveData.App;
using System.Collections.Generic;
using System.Linq;

namespace PianoRoll
{
    public struct TempoNote
    {
        public float beat;
        public float length;
        public float pitch;

        public TempoNote(float beat, float length, float pitch)
        {
            this.beat = beat;
            this.length = length;
            this.pitch = pitch;
        }
    }

    public class TempoController
    {
        public static float GetBeatFromTime(float time, List<ReactiveTempoEvent> events)
        {
            var eventTimes = ComputeEventTimes(events);
            int i = 0;
            while (i < eventTimes.Count - 1 && eventTimes[i + 1] <= time)
            {
                i++;
            }
            var lastEvent = events[i];
            float timeDiff = time - eventTimes[i];
            float beat = lastEvent.beat.Value + timeDiff * lastEvent.bps.Value;
            return beat;
        }

        public static float GetTimeFromBeat(float beat, List<ReactiveTempoEvent> events)
        {
            var eventTimes = ComputeEventTimes(events);
            int i = 0;
            while (i < eventTimes.Count - 1 && events[i + 1].beat.Value <= beat)
            {
                i++;
            }
            var lastEvent = events[i];
            float beatDiff = beat - lastEvent.beat.Value;
            float time = eventTimes[i] + beatDiff / lastEvent.bps.Value;
            return time;
        }

        public static List<SequencerNote> ConvertNotes(List<TempoNote> notes, List<ReactiveTempoEvent> events)
        {
            notes = notes.OrderBy(n => n.beat).ToList();
            events = events.OrderBy(e => e.beat.Value).ToList();
            var eventTimes = ComputeEventTimes(events);

            var sequencerNotes = new List<SequencerNote>();

            int eventIndex = 0;

            for (int i = 0; i < notes.Count; i++)
            {
                while (eventIndex + 1 < events.Count && events[eventIndex + 1].beat.Value < notes[i].beat)
                {
                    eventIndex++;
                }
                float timeDiff = (notes[i].beat - events[eventIndex].beat.Value) / events[eventIndex].bps.Value;
                float prevTime = eventTimes[eventIndex];
                float noteTime = prevTime + timeDiff;
                int endEventIndex = eventIndex;
                while (endEventIndex + 1 < events.Count && events[endEventIndex + 1].beat.Value < notes[i].beat + notes[i].length)
                {
                    endEventIndex++;
                }
                float endTimeDiff = (notes[i].beat + notes[i].length - events[endEventIndex].beat.Value) / events[endEventIndex].bps.Value;
                float endPrevTime = eventTimes[endEventIndex];
                float noteEndTime = endPrevTime + endTimeDiff;
                float noteLength = noteEndTime - noteTime;

                var sequencerNote = new SequencerNote(notes[i].pitch, noteTime, noteLength);
                sequencerNotes.Add(sequencerNote);
            }

            return sequencerNotes;
        }

        private static List<float> ComputeEventTimes(List<ReactiveTempoEvent> events)
        {
            var eventTimes = new List<float>();

            var firstEvent = events[0];
            eventTimes.Add(firstEvent.beat.Value / firstEvent.bps.Value);

            for (int i = 1; i < events.Count; i++)
            {
                var currentEvent = events[i];
                var previousEvent = events[i - 1];

                float timeDiff = (currentEvent.beat.Value - previousEvent.beat.Value) / previousEvent.bps.Value;
                float prevTime = eventTimes[i - 1];

                eventTimes.Add(prevTime + timeDiff);
            }

            return eventTimes;
        }
    }
}
