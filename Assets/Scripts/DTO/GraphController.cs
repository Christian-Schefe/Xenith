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

        public void UnloadGraph(GraphID id)
        {
            if (graphs.ContainsKey(id))
            {
                graphs.Remove(id);
            }
            else
            {
                Debug.LogWarning($"Graph {id} not found in GraphController.");
            }
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
            var graphDatabase = Globals<GraphDatabase>.Instance;
            if (id != newId)
            {
                graphs.Remove(id);
                graph = graph.ToJson().FromJson<Graph>();
                graphs.Add(newId, graph);
            }
            graphDatabase.SaveGraph(newId, graph);
            return newId != id;
        }

        public void DeleteGraph(GraphID id)
        {
            graphs.Remove(id);
            var graphDatabase = Globals<GraphDatabase>.Instance;
            graphDatabase.DeleteGraph(id);
        }
    }
}
