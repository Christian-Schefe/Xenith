using DTO;
using NodeGraph;
using ReactiveData.Core;
using System;
using System.Collections.Generic;

namespace ReactiveData.App
{
    public class ReactiveGraph : IKeyed
    {
        public Reactive<string> path;
        public DerivedReactive<string, string> name;
        public ReactiveList<ReactiveNode> nodes;
        public ReactiveList<ReactiveConnection> connections;

        public ReactiveGraph(string path, IEnumerable<ReactiveNode> nodes, IEnumerable<ReactiveConnection> connections)
        {
            this.path = new(path);
            name = new DerivedReactive<string, string>(this.path, GetNameFromPath);
            this.nodes = new(nodes);
            this.connections = new(connections);
        }

        private string GetNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "New Song";
            }
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public static ReactiveGraph Default => new(null, new List<ReactiveNode>(), new List<ReactiveConnection>());

        public string ID { get; private set; } = Guid.NewGuid().ToString();
        public string Key => ID;
    }
}
