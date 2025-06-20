using DSP;
using ReactiveData.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveData.App
{
    public class ReactiveSong : IKeyed
    {
        public Reactive<string> path;
        public DerivedReactive<string, string> name;
        public Reactive<ReactiveMasterTrack> master;
        public ReactiveList<ReactiveTrack> tracks;
        public ReactiveList<ReactiveTempoEvent> tempoEvents;

        public Reactive<ReactiveTrack> activeTrack;
        public Reactive<ReactiveTrack> editingPipeline;

        public ReactiveSong(string path, ReactiveMasterTrack masterTrack, IEnumerable<ReactiveTrack> tracks, IEnumerable<ReactiveTempoEvent> tempoEvents)
        {
            this.path = new(path);
            name = new DerivedReactive<string, string>(this.path, GetNameFromPath);
            master = new(masterTrack);
            this.tracks = new(tracks);
            this.tempoEvents = new(tempoEvents);
            activeTrack = new Reactive<ReactiveTrack>(this.tracks.Count > 0 ? this.tracks[0] : null);
            editingPipeline = new(null);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;

        public static ReactiveSong Default => new(null, ReactiveMasterTrack.Default, new List<ReactiveTrack>() { ReactiveTrack.Default }, new List<ReactiveTempoEvent>() { new(0, 2) });

        private string GetNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "New Song";
            }
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public void SortTempoEvents()
        {
            tempoEvents.Sort((a, b) => a.beat.Value.CompareTo(b.beat.Value));
        }

        public bool IsEmpty()
        {
            if (tracks.Count == 0) return true;
            if (tracks.Count > 1) return false;
            var track = tracks[0];
            return track.notes.Count == 0;
        }

        public DSPInstrument[] BuildInstrumentNodes(float startTime)
        {
            var hasSoloTracks = tracks.Any(t => t.isSoloed.Value);
            var filteredTracks = tracks.Where(t => (!hasSoloTracks || t.isSoloed.Value) && !t.isMuted.Value);
            return filteredTracks.Select(t => t.BuildInstrument(startTime, tempoEvents)).ToArray();
        }

        public DSPMaster BuildMasterNode()
        {
            return master.Value.BuildMaster();
        }

        public float GetDuration()
        {
            if (tracks.Count == 0) return 0;
            return tracks.Select(t => t.GetDuration(tempoEvents)).Max();
        }
    }
}
