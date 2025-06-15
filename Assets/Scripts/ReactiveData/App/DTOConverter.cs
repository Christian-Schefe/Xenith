using DTO;
using JsonPattern;
using ReactiveData.Core;
using System.Collections.Generic;
using System.Linq;
using Yeast;

namespace ReactiveData.App
{
    public static class DTOConverter
    {
        public static SchemaValue Serialize(ReactiveSong song)
        {
            var tracksList = song.tracks.Select(Serialize).ToList();
            var tempoEventsList = song.tempoEvents.Select(Serialize).ToList();
            return SongSchema.V1.Make(tracksList, tempoEventsList);
        }

        public static SchemaValue Serialize(ReactiveTrack track)
        {
            var notesList = track.notes.Select(Serialize).ToList();
            return SongSchema.V1.TrackSchema.Make(track.name.Value, track.instrument.Value, track.effects.ToList(), track.isMuted.Value, track.isSoloed.Value, track.volume.Value, track.pan.Value, track.keySignature.Value, notesList);
        }

        public static SchemaValue Serialize(ReactiveNote note)
        {
            return SongSchema.V1.NoteSchema.Make(note.beat.Value, note.pitch.Value, note.velocity.Value, note.length.Value);
        }

        public static SchemaValue Serialize(ReactiveTempoEvent tempoEvent)
        {
            return SongSchema.V1.TempoEventSchema.Make(tempoEvent.beat.Value, tempoEvent.bps.Value);
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
            var serializedSettings = node.settings.Values.Select(setting => (setting.name.Value, setting.Type, setting.Serialize())).ToList().ToJson();
            return new(node.position.Value, node.id.Value, serializedSettings);
        }

        public static ReactiveSong DeserializeSong(string path, SchemaValue song)
        {
            var tracks = SongSchema.V1.tracks.Retrieve(song).Values.Select(DeserializeTrack).ToList();
            var tempoEvents = SongSchema.V1.tempoEvents.Retrieve(song).Values.Select(DeserializeTempoEvent).ToList();
            return new ReactiveSong(path, tracks, tempoEvents);
        }

        public static ReactiveTrack DeserializeTrack(SchemaValue track)
        {
            var notes = SongSchema.V1.TrackSchema.notes.Retrieve(track).Values.Select(DeserializeNote);
            return new ReactiveTrack(
                SongSchema.V1.TrackSchema.name.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.instrument.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.effects.Retrieve(track).Values.Select(e => e.Value).ToList(),
                SongSchema.V1.TrackSchema.isMuted.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.isSoloed.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.volume.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.pan.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.keySignature.Retrieve(track).Value,
                notes
            );
        }

        public static ReactiveNote DeserializeNote(SchemaValue note)
        {
            return new ReactiveNote(
                SongSchema.V1.NoteSchema.beat.Retrieve(note).Value,
                SongSchema.V1.NoteSchema.pitch.Retrieve(note).Value,
                SongSchema.V1.NoteSchema.velocity.Retrieve(note).Value,
                SongSchema.V1.NoteSchema.length.Retrieve(note).Value
            );
        }

        public static ReactiveTempoEvent DeserializeTempoEvent(SchemaValue tempoEvent)
        {
            return new ReactiveTempoEvent(
                SongSchema.V1.TempoEventSchema.beat.Retrieve(tempoEvent).Value,
                SongSchema.V1.TempoEventSchema.bps.Retrieve(tempoEvent).Value
            );
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
            var settings = node.serializedSettings.FromJson<List<(string name, ReactiveSettingType type, string value)>>();
            var settingsDict = settings.ToDictionary(
                s => s.name,
                s => ReactiveNodeSetting.Deserialize(s.name, s.type, s.value)
            );
            return new ReactiveNode(node.position, node.id, settingsDict);
        }
    }
}
