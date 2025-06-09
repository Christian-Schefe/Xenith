using NodeGraph;
using Persistence;
using ReactiveData.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yeast;

namespace ReactiveData.App
{
    public class ReactiveApp
    {
        public ReactiveList<ReactiveSong> songs;
        public ReactiveList<ReactiveGraph> graphs;

        public Reactive<Nand<ReactiveSong, ReactiveGraph>> openElement;

        public DerivedReactive<Nand<ReactiveSong, ReactiveGraph>, ReactiveSong> openSong;
        public DerivedReactive<Nand<ReactiveSong, ReactiveGraph>, ReactiveGraph> openGraph;

        public ReactiveApp(IEnumerable<ReactiveSong> songs, IEnumerable<ReactiveGraph> graphs)
        {
            this.songs = new(songs);
            this.graphs = new(graphs);
            openElement = new(this.songs.Count > 0 ? new(this.songs[0]) : (this.graphs.Count > 0 ? new(this.graphs[0]) : new()));
            openSong = new(openElement, e => e.TryGet(out ReactiveSong song) ? song : null);
            openGraph = new(openElement, e => e.TryGet(out ReactiveGraph graph) ? graph : null);
        }

        public static ReactiveApp Default => new(new ReactiveList<ReactiveSong>() { ReactiveSong.Default }, new ReactiveList<ReactiveGraph>());

        public bool TryLoadSong(string path, out ReactiveSong song)
        {
            if (songs.Any(s => s.path.Value == path))
            {
                song = songs.First(s => s.path.Value == path);
                return false;
            }

            if (!FilePersistence.TryLoadFullPath(path, out var json))
            {
                Debug.LogError($"Failed to read file from {path}");
            }

            if (json.TryFromJson(out DTO.Song dtoSong))
            {
                song = DTOConverter.Deserialize(path, dtoSong);
                songs.Add(song);
                return true;
            }

            song = null;
            Debug.LogError($"Failed to parse song from {path}");
            return false;
        }

        public void SaveSong(ReactiveSong song, string path)
        {
            song.path.Value = path;
            FilePersistence.SaveFullPath(path, DTOConverter.Serialize(song).ToJson());
        }

        public bool TryLoadGraph(string path, out ReactiveGraph graph)
        {
            if (graphs.Any(g => g.path.Value == path))
            {
                graph = graphs.First(g => g.path.Value == path);
                return false;
            }
            var graphDatabase = Globals<GraphDatabase>.Instance;
            if (graphDatabase.TryGetGraph(path, out var dtoGraph))
            {
                graph = DTOConverter.Deserialize(path, dtoGraph);
                graphs.Add(graph);
                return true;
            }
            graph = null;
            return false;
        }

        public void SaveGraph(ReactiveGraph graph, string path)
        {
            graph.path.Value = path;
            var graphDatabase = Globals<GraphDatabase>.Instance;
            graphDatabase.SaveGraph(path, DTOConverter.Serialize(graph));
        }

        public void DeleteGraph(ReactiveGraph graph)
        {
            if (graph.path.Value != null)
            {
                Globals<GraphDatabase>.Instance.DeleteGraph(graph.path.Value);
            }
            graphs.Remove(graph);
            if (openElement.Value.TryGet(out ReactiveGraph openGraph) && openGraph == graph)
            {
                openElement.Value = new();
            }
        }
    }
}
