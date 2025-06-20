using JsonPattern;
using PianoRoll;
using System.Collections.Generic;
using System.Linq;

namespace DTO
{
    public class SongSchema : VersionedSchema
    {
        public SongSchema() : base(new V1(), new() { })
        {
        }

        public class V1 : SchemaVersion
        {
            public static ClassProp<ObjectSchemaValue> master = new(nameof(master), new MasterTrackSchema());
            public static ClassProp<ArraySchemaValue> tracks = new(nameof(tracks), new ArraySchema(new TrackSchema()));
            public static ClassProp<ArraySchemaValue> tempoEvents = new(nameof(tempoEvents), new ArraySchema(new TempoEventSchema()));

            public V1() : base("1.0") { }

            protected override (string key, Schema val)[] Values => new[] { tracks.Key, tempoEvents.Key };

            public static ObjectSchemaValue Make(ObjectSchemaValue master, IEnumerable<SchemaValue> tracks, IEnumerable<SchemaValue> tempoEvents)
            {
                return new ObjectSchemaValue(
                    V1.master.Make(master),
                    V1.tracks.Make(new(tracks.ToList())),
                    V1.tempoEvents.Make(new(tempoEvents.ToList()))
                );
            }

            public class NoteSchema : ClassSchema
            {
                public static ClassProp<FloatSchemaValue> beat = new(nameof(beat), new FloatSchema());
                public static ClassProp<IntSchemaValue> pitch = new(nameof(pitch), new IntSchema());
                public static ClassProp<FloatSchemaValue> velocity = new(nameof(velocity), new FloatSchema());
                public static ClassProp<FloatSchemaValue> length = new(nameof(length), new FloatSchema());

                protected override (string key, Schema val)[] Values => new[] { beat.Key, pitch.Key, velocity.Key, length.Key };

                public static ObjectSchemaValue Make(float beat, int pitch, float velocity, float length)
                {
                    return new ObjectSchemaValue(
                        NoteSchema.beat.Make(new(beat)),
                        NoteSchema.pitch.Make(new(pitch)),
                        NoteSchema.velocity.Make(new(velocity)),
                        NoteSchema.length.Make(new(length))
                    );
                }
            }

            public class TempoEventSchema : ClassSchema
            {
                public static ClassProp<FloatSchemaValue> beat = new(nameof(beat), new FloatSchema());
                public static ClassProp<FloatSchemaValue> bps = new(nameof(bps), new FloatSchema());

                protected override (string key, Schema val)[] Values => new[] { beat.Key, bps.Key };

                public static ObjectSchemaValue Make(float beat, float bps)
                {
                    return new ObjectSchemaValue(
                        TempoEventSchema.beat.Make(new(beat)),
                        TempoEventSchema.bps.Make(new(bps))
                    );
                }
            }

            public class MasterTrackSchema : ClassSchema
            {
                public static ClassProp<ArraySchemaValue<AutoSchemaValue<NodeResource>>> effects = new(nameof(effects), new ArraySchema<AutoSchemaValue<NodeResource>>(new AutoSchema<NodeResource>()));
                public static ClassProp<FloatSchemaValue> volume = new(nameof(volume), new FloatSchema());
                public static ClassProp<FloatSchemaValue> pan = new(nameof(pan), new FloatSchema());

                protected override (string key, Schema val)[] Values => new[] { effects.Key, volume.Key, pan.Key };

                public static ObjectSchemaValue Make(IEnumerable<NodeResource> effects, float volume, float pan)
                {
                    return new ObjectSchemaValue(
                        MasterTrackSchema.effects.Make(new(effects.Select(e => new AutoSchemaValue<NodeResource>(e)).ToList())),
                        MasterTrackSchema.volume.Make(new(volume)),
                        MasterTrackSchema.pan.Make(new(pan))
                    );
                }
            }

            public class TrackSchema : ClassSchema
            {
                public static ClassProp<StringSchemaValue> name = new(nameof(name), new StringSchema());
                public static ClassProp<AutoSchemaValue<NodeResource>> instrument = new(nameof(instrument), new AutoSchema<NodeResource>());
                public static ClassProp<ArraySchemaValue<AutoSchemaValue<NodeResource>>> effects = new(nameof(effects), new ArraySchema<AutoSchemaValue<NodeResource>>(new AutoSchema<NodeResource>()));
                public static ClassProp<BoolSchemaValue> isMuted = new(nameof(isMuted), new BoolSchema());
                public static ClassProp<BoolSchemaValue> isSoloed = new(nameof(isSoloed), new BoolSchema());
                public static ClassProp<FloatSchemaValue> volume = new(nameof(volume), new FloatSchema());
                public static ClassProp<FloatSchemaValue> pan = new(nameof(pan), new FloatSchema());
                public static ClassProp<AutoSchemaValue<MusicKey>> keySignature = new(nameof(keySignature), new AutoSchema<MusicKey>());
                public static ClassProp<ArraySchemaValue> notes = new(nameof(notes), new ArraySchema(new NoteSchema()));

                protected override (string key, Schema val)[] Values => new[] { name.Key, instrument.Key, effects.Key, isMuted.Key, isSoloed.Key, volume.Key, pan.Key, keySignature.Key, notes.Key };

                public static ObjectSchemaValue Make(string name, NodeResource instrument, IEnumerable<NodeResource> effects, bool isMuted, bool isSoloed, float volume, float pan, MusicKey keySignature, IEnumerable<SchemaValue> notes)
                {
                    return new ObjectSchemaValue(
                        TrackSchema.name.Make(new(name)),
                        TrackSchema.instrument.Make(new(instrument)),
                        TrackSchema.effects.Make(new(effects.Select(e => new AutoSchemaValue<NodeResource>(e)).ToList())),
                        TrackSchema.isMuted.Make(new(isMuted)),
                        TrackSchema.isSoloed.Make(new(isSoloed)),
                        TrackSchema.volume.Make(new(volume)),
                        TrackSchema.pan.Make(new(pan)),
                        TrackSchema.keySignature.Make(new(keySignature)),
                        TrackSchema.notes.Make(new(notes.ToList()))
                    );
                }
            }
        }
    }
}
