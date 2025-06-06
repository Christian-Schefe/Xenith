using DTO;
using ReactiveData.Core;

namespace ReactiveData.App
{
    public interface IReactiveTab : IKeyed
    {
        public IReactive<string> Name { get; }
    }

    public class SongTab : IReactiveTab, IKeyed
    {
        public readonly ReactiveSong song;

        public IReactive<string> Name => song.name;

        public string Key => song.Key;

        public SongTab(ReactiveSong song)
        {
            this.song = song;
        }
    }

    public class GraphTab : IReactiveTab, IKeyed
    {
        public readonly ReactiveGraph graph;

        public IReactive<string> Name => graph.name;

        public string Key => graph.Key;

        public GraphTab(ReactiveGraph graph)
        {
            this.graph = graph;
        }
    }
}
