using NodeGraph;
using Persistence;
using System.Collections.Generic;
using UnityEngine;
using Yeast;


namespace DTO
{
    public class GraphController : MonoBehaviour
    {
        private readonly Dictionary<GraphID, Graph> graphs = new();
        private int unsavedGraphIndex = 0;

        public Graph GetGraph(GraphID id)
        {
            return graphs[id];
        }

        public GraphID AddGraph()
        {
            var id = new UnsavedGraphID(unsavedGraphIndex);
            unsavedGraphIndex++;
            var graph = new Graph();
            graphs.Add(id, graph);
            return id;
        }

        public bool TryLoadGraph(GraphID id)
        {
            if (graphs.ContainsKey(id))
            {
                return false;
            }
            var graphDatabase = Globals<GraphDatabase>.Instance;
            if (!graphDatabase.TryGetGraph(id, out var g))
            {
                return false;
            }
            graphs.Add(id, g);
            return true;
        }

        public bool SaveGraph(GraphID id, GraphID newId)
        {
            var graph = graphs[id];
            if (id != newId)
            {
                graphs.Remove(id);
                graphs.Add(newId, graph);
            }
            var graphDatabase = Globals<GraphDatabase>.Instance;
            graphDatabase.SaveGraph(newId, graph);
            return newId != id;
        }
    }
}
