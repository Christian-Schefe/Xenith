using DTO;
using ReactiveData.Core;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveData.App
{
    public static class DTOConverter
    {
        public static Song Serialize(ReactiveSong song)
        {
            var tracksList = song.tracks.Select(Serialize).ToList();
            var tempoEventsList = song.tempoEvents.Select(Serialize).ToList();
            return new Song(tracksList, tempoEventsList);
        }

        public static Track Serialize(ReactiveTrack track)
        {
            var notesList = track.notes.Select(Serialize).ToList();
            return new(track.name.Value, track.instrument.Value, track.isMuted.Value, track.isSoloed.Value, track.volume.Value, track.pan.Value, track.keySignature.Value, notesList);
        }

        public static Note Serialize(ReactiveNote note)
        {
            return new(note.beat.Value, note.pitch.Value, note.velocity.Value, note.length.Value);
        }

        public static TempoEvent Serialize(ReactiveTempoEvent tempoEvent)
        {
            return new(tempoEvent.beat.Value, tempoEvent.bps.Value);
        }

        public static Graph Serialize(ReactiveGraph graph)
        {
            var nodesList = graph.nodes.Select(Serialize).ToList();
            var indices = graph.nodes.Select((node, index) => new { node, index }).ToDictionary(x => x.node, x => x.index);
            var connectionsList = graph.connections.Select(c => Serialize(indices, c)).ToList();
            return new Graph(nodesList, connectionsList);
        }

        public static Connection Serialize(Dictionary<ReactiveNode, int> indices, ReactiveConnection connection)
        {
            int fromNode = indices[connection.fromNode.Value];
            int toNode = indices[connection.toNode.Value];
            return new(fromNode, connection.fromIndex.Value, toNode, connection.toIndex.Value);
        }

        public static Node Serialize(ReactiveNode node)
        {
            return new(node.position.Value, node.id.Value, node.serializedSettings.Value);
        }

        public static ReactiveSong Deserialize(string path, Song song)
        {
            var tracks = song.tracks.Select(Deserialize);
            var tempoEvents = song.tempoEvents.Select(Deserialize);
            return new ReactiveSong(path, tracks, tempoEvents);
        }

        public static ReactiveTrack Deserialize(Track track)
        {
            var notes = track.notes.Select(Deserialize);
            return new ReactiveTrack(track.name, track.instrument, track.isMuted, track.isSoloed, track.volume, track.pan, track.keySignature, notes);
        }

        public static ReactiveNote Deserialize(Note note)
        {
            return new ReactiveNote(note.beat, note.pitch, note.velocity, note.length);
        }

        public static ReactiveTempoEvent Deserialize(TempoEvent tempoEvent)
        {
            return new ReactiveTempoEvent(tempoEvent.beat, tempoEvent.bps);
        }

        public static ReactiveGraph Deserialize(string path, Graph graph)
        {
            var nodes = graph.nodes.Select(Deserialize).ToList();
            var connections = graph.connections.Select(c => Deserialize(nodes, c));
            return new ReactiveGraph(path, nodes, connections);
        }

        public static ReactiveConnection Deserialize(IList<ReactiveNode> nodes, Connection connection)
        {
            var fromNode = nodes[connection.fromNodeIndex];
            var toNode = nodes[connection.toNodeIndex];
            return new ReactiveConnection(fromNode, toNode, connection.fromNodeOutput, connection.toNodeInput);
        }

        public static ReactiveNode Deserialize(Node node)
        {
            return new ReactiveNode(node.position, node.id, node.serializedSettings);
        }
    }
}
