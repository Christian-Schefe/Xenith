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
            var master = Serialize(song.master.Value);
            var tracksList = song.tracks.Select(Serialize).ToList();
            var tempoEventsList = song.tempoEvents.Select(Serialize).ToList();
            return SongSchema.V1.Make(master, tracksList, tempoEventsList);
        }

        public static ObjectSchemaValue Serialize(ReactiveMasterTrack masterTrack)
        {
            var effectsList = masterTrack.effects.Select(Serialize).ToList();
            return SongSchema.V1.MasterTrackSchema.Make(effectsList, masterTrack.volume.Value, masterTrack.pan.Value);
        }

        public static ObjectSchemaValue Serialize(ReactiveTrack track)
        {
            var notesList = track.notes.Select(Serialize).ToList();
            var effectsList = track.effects.Select(Serialize).ToList();
            return SongSchema.V1.TrackSchema.Make(track.name.Value, track.instrument.Value, effectsList, track.isMuted.Value, track.isSoloed.Value, track.volume.Value, track.pan.Value, track.keySignature.Value, notesList);
        }

        public static ObjectSchemaValue Serialize(ReactiveNote note)
        {
            return SongSchema.V1.NoteSchema.Make(note.beat.Value, note.pitch.Value, note.velocity.Value, note.length.Value);
        }

        public static ObjectSchemaValue Serialize(ReactiveTempoEvent tempoEvent)
        {
            return SongSchema.V1.TempoEventSchema.Make(tempoEvent.beat.Value, tempoEvent.bps.Value);
        }

        public static ObjectSchemaValue Serialize(ReactiveEffect effect)
        {
            return SongSchema.V1.EffectSchema.Make(effect.effect.Value);
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
            var master = DeserializeMasterTrack(SongSchema.V1.master.Retrieve(song));
            var tracks = SongSchema.V1.tracks.Retrieve(song).Values.Select(DeserializeTrack).ToList();
            var tempoEvents = SongSchema.V1.tempoEvents.Retrieve(song).Values.Select(DeserializeTempoEvent).ToList();
            return new ReactiveSong(path, master, tracks, tempoEvents);
        }

        public static ReactiveMasterTrack DeserializeMasterTrack(SchemaValue masterTrack)
        {
            var effects = SongSchema.V1.TrackSchema.effects.Retrieve(masterTrack).Values.Select(DeserializeEffect).ToList();
            return new ReactiveMasterTrack(
                effects,
                SongSchema.V1.MasterTrackSchema.volume.Retrieve(masterTrack).Value,
                SongSchema.V1.MasterTrackSchema.pan.Retrieve(masterTrack).Value
            );
        }

        public static ReactiveTrack DeserializeTrack(SchemaValue track)
        {
            var notes = SongSchema.V1.TrackSchema.notes.Retrieve(track).Values.Select(DeserializeNote);
            var effects = SongSchema.V1.TrackSchema.effects.Retrieve(track).Values.Select(DeserializeEffect).ToList();
            return new ReactiveTrack(
                SongSchema.V1.TrackSchema.name.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.instrument.Retrieve(track).Value,
                effects,
                SongSchema.V1.TrackSchema.isMuted.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.isSoloed.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.volume.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.pan.Retrieve(track).Value,
                SongSchema.V1.TrackSchema.keySignature.Retrieve(track).Value,
                notes
            );
        }

        public static ReactiveEffect DeserializeEffect(SchemaValue effect)
        {
            return new ReactiveEffect(
                SongSchema.V1.EffectSchema.effect.Retrieve(effect).Value
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
