using DSP;
using DTO;
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
        public ReactiveList<ReactiveTrack> tracks;
        public ReactiveList<ReactiveTempoEvent> tempoEvents;

        public Reactive<ReactiveTrack> activeTrack;
        public Reactive<ReactiveTrack> editingPipeline;

        public ReactiveSong(string path, IEnumerable<ReactiveTrack> tracks, IEnumerable<ReactiveTempoEvent> tempoEvents)
        {
            this.path = new(path);
            name = new DerivedReactive<string, string>(this.path, GetNameFromPath);
            this.tracks = new(tracks);
            this.tempoEvents = new(tempoEvents);
            activeTrack = new Reactive<ReactiveTrack>(this.tracks.Count > 0 ? this.tracks[0] : null);
            editingPipeline = new(null);
        }

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;

        public static ReactiveSong Default => new(null, new List<ReactiveTrack>() { ReactiveTrack.Default }, new List<ReactiveTempoEvent>() { new(0, 2) });

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

        public AudioNode[] BuildInstrumentNodes(float startTime)
        {
            var hasSoloTracks = tracks.Any(t => t.isSoloed.Value);
            var filteredTracks = tracks.Where(t => (!hasSoloTracks || t.isSoloed.Value) && !t.isMuted.Value).ToList();
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
            var hasSoloTracks = tracks.Any(t => t.isSoloed.Value);
            var filteredTracks = tracks.Where(t => (!hasSoloTracks || t.isSoloed.Value) && !t.isMuted.Value).ToList();

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
}
