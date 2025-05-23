using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FileFormat
{
    public class MidiFile
    {
        public MidiHeaderChunk headerChunk;
        public List<MidiTrackChunk> trackChunks;

        public static MidiFile Decode(byte[] bytes)
        {
            var offset = 0;
            MidiHeaderChunk headerChunk = null;
            var chunks = new List<MidiTrackChunk>();
            while (offset < bytes.Length)
            {
                var chunk = MidiChunk.DecodeChunk(bytes, ref offset);
                if (chunk is MidiHeaderChunk header)
                {
                    headerChunk = header;
                }
                else if (chunk is MidiTrackChunk track)
                {
                    chunks.Add(track);
                }
            }
            return new MidiFile
            {
                headerChunk = headerChunk,
                trackChunks = chunks
            };
        }

        public static MidiFile ReadFromFile(string path)
        {
            var bytes = System.IO.File.ReadAllBytes(path);
            return Decode(bytes);
        }

        public override string ToString()
        {
            return $"MidiFile(header: {headerChunk}, tracks: [\n{string.Join("\n", trackChunks.Select(t => t.ToString()))}\n])";
        }
    }

    public enum MidiFormat
    {
        SingleTrack,
        MultipleTracks,
        MultipleTracksWithSync
    }

    public static class MidiFormatInfo
    {
        public static short Code(this MidiFormat format)
        {
            return format switch
            {
                MidiFormat.SingleTrack => 0,
                MidiFormat.MultipleTracks => 1,
                MidiFormat.MultipleTracksWithSync => 2,
                _ => throw new System.ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }

        public static MidiFormat GetFormat(short code)
        {
            return code switch
            {
                0 => MidiFormat.SingleTrack,
                1 => MidiFormat.MultipleTracks,
                2 => MidiFormat.MultipleTracksWithSync,
                _ => throw new System.ArgumentOutOfRangeException(nameof(code), code, null)
            };
        }
    }

    public abstract class MidiChunk
    {
        public readonly string chunkId;

        protected abstract int InnerChunkSize { get; }

        public MidiChunk(string chunkId)
        {
            this.chunkId = chunkId;
        }

        public static MidiChunk DecodeChunk(byte[] data, ref int offset)
        {
            var chunkId = ReadString(data, ref offset, 4);
            MidiChunk chunk = chunkId switch
            {
                "MThd" => new MidiHeaderChunk(),
                "MTrk" => new MidiTrackChunk(),
                _ => new MidiAlienChunk(chunkId),
            };
            chunk.Decode(data, ref offset);
            return chunk;
        }

        private void Decode(byte[] data, ref int offset)
        {
            var chunkSize = ReadInt(data, ref offset);
            DecodeInner(data, chunkSize, ref offset);
        }

        public void Encode(byte[] data, ref int offset)
        {
            WriteString(data, ref offset, chunkId, 4);
            WriteInt(data, ref offset, InnerChunkSize);
            EncodeInner(data, ref offset);
        }

        public abstract override string ToString();

        protected abstract void DecodeInner(byte[] data, int chunkSize, ref int offset);

        protected abstract void EncodeInner(byte[] data, ref int offset);

        protected static string ReadString(byte[] data, ref int offset, int size)
        {
            var strBytes = new byte[size];
            System.Buffer.BlockCopy(data, offset, strBytes, 0, size);
            offset += size;
            return System.Text.Encoding.ASCII.GetString(strBytes);
        }

        protected static void WriteString(byte[] data, ref int offset, string str, int size)
        {
            var strBytes = System.Text.Encoding.ASCII.GetBytes(str);
            System.Buffer.BlockCopy(strBytes, 0, data, offset, size);
            offset += size;
        }

        protected static int ReadInt(byte[] data, ref int offset)
        {
            return ByteConverter.ReadInt(data, ref offset, false);
        }

        protected static void WriteInt(byte[] data, ref int offset, int value)
        {
            ByteConverter.WriteInt(data, ref offset, value, false);
        }

        protected static short ReadShort(byte[] data, ref int offset)
        {
            return ByteConverter.ReadShort(data, ref offset, false);
        }

        protected static void WriteShort(byte[] data, ref int offset, short value)
        {
            ByteConverter.WriteShort(data, ref offset, value, false);
        }
    }

    public class MidiAlienChunk : MidiChunk
    {
        protected override int InnerChunkSize => data.Length;

        public byte[] data;

        public MidiAlienChunk(string id) : base(id) { }

        protected override void DecodeInner(byte[] data, int chunkSize, ref int offset)
        {
            this.data = new byte[chunkSize];
            System.Buffer.BlockCopy(data, offset, this.data, 0, chunkSize);
            offset += chunkSize;
        }

        protected override void EncodeInner(byte[] data, ref int offset)
        {
            System.Buffer.BlockCopy(this.data, 0, data, offset, this.data.Length);
            offset += this.data.Length;
        }

        public override string ToString()
        {
            return $"Alien(id: {chunkId}, length: {data.Length})";
        }
    }

    public class MidiHeaderChunk : MidiChunk
    {
        public MidiFormat formatType;
        public int numberOfTracks;
        public int division;

        protected override int InnerChunkSize => 6;

        public MidiHeaderChunk() : base("MThd") { }

        protected override void DecodeInner(byte[] data, int chunkSize, ref int offset)
        {
            formatType = MidiFormatInfo.GetFormat(ReadShort(data, ref offset));
            numberOfTracks = ReadShort(data, ref offset);
            division = ReadShort(data, ref offset);
        }

        protected override void EncodeInner(byte[] data, ref int offset)
        {
            WriteShort(data, ref offset, formatType.Code());
            WriteShort(data, ref offset, (short)numberOfTracks);
            WriteShort(data, ref offset, (short)division);
        }

        public override string ToString()
        {
            return $"Header(format: {formatType}, tracks: {numberOfTracks}, division: {division})";
        }
    }

    public class MidiTrackChunk : MidiChunk
    {
        public List<MidiTrackEvent> events;

        protected override int InnerChunkSize => events.Select((e, i) => e.GetByteSize(events[i - 1] as MidiChannelEvent)).Sum();

        public MidiTrackChunk() : base("MTrk")
        {
            events = new List<MidiTrackEvent>();
        }

        protected override void DecodeInner(byte[] data, int chunkSize, ref int offset)
        {
            var chunkEnd = offset + chunkSize;
            while (offset < chunkEnd)
            {
                var prevChannelEvent = events.Count > 0 ? events[^1] as MidiChannelEvent : null;
                var midiEvent = MidiTrackEvent.DecodeEvent(data, prevChannelEvent, ref offset);
                events.Add(midiEvent);
            }
        }

        protected override void EncodeInner(byte[] data, ref int offset)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var prev = i > 0 ? events[i - 1] as MidiChannelEvent : null;
                events[i].Encode(data, prev, ref offset);
            }
        }

        public override string ToString()
        {
            return $"Track(events: [{string.Join(", ", events.Select(e => e.ToString()))}])";
        }
    }

    public abstract class MidiTrackEvent
    {
        public int deltaTime;

        public int GetByteSize(MidiChannelEvent prev)
        {
            var size = ByteConverter.VariableLengthSize(deltaTime);
            return size + GetInnerByteSize(prev);
        }

        protected abstract int GetInnerByteSize(MidiChannelEvent prev);

        public static MidiTrackEvent DecodeEvent(byte[] data, MidiChannelEvent prevChannelEvent, ref int offset)
        {
            var deltaTime = ByteConverter.ReadVariableLength(data, ref offset);
            var firstByte = data[offset];
            if (IsMidiChannelRunningStatus(firstByte))
            {
                firstByte = prevChannelEvent.type.Code(prevChannelEvent.channel);
            }
            else
            {
                offset++;
            }
            MidiTrackEvent trackEvent = firstByte switch
            {
                0xF0 or 0xF7 => new MidiSysExEvent(),
                0xFF => new MidiMetaEvent(),
                _ => IsMidiChannelEvent(firstByte) ? new MidiChannelEvent() : new MidiSystemEvent()
            };
            trackEvent.deltaTime = deltaTime;
            trackEvent.DecodeInner(data, firstByte, ref offset);
            return trackEvent;
        }

        protected abstract void DecodeInner(byte[] data, byte firstByte, ref int offset);

        public void Encode(byte[] data, MidiChannelEvent prevChannelEvent, ref int offset)
        {
            ByteConverter.WriteVariableLength(data, ref offset, deltaTime);
            EncodeInner(data, prevChannelEvent, ref offset);
        }

        protected abstract void EncodeInner(byte[] data, MidiChannelEvent prevChannelEvent, ref int offset);

        private static bool IsMidiChannelRunningStatus(byte firstByte)
        {
            return (firstByte & 0x80) == 0x00;
        }

        private static bool IsMidiChannelEvent(byte firstByte)
        {
            return (firstByte & 0xF0) != 0xF0;
        }

        public abstract override string ToString();
    }

    public enum MidiChannelEventType
    {
        NoteOn,
        NoteOff,
        PolyphonicKeyPressure,
        ControlChange,
        ProgramChange,
        ChannelPressure,
        PitchBend
    }

    public enum MidiSystemEventType
    {
        SongPositionPointer,
        SongSelect,
        TuneRequest,
        Undefined,
    }

    public static class MidiEventTypeInfo
    {
        public static int DataSize(this MidiChannelEventType type)
        {
            return type switch
            {
                MidiChannelEventType.NoteOn => 2,
                MidiChannelEventType.NoteOff => 2,
                MidiChannelEventType.PolyphonicKeyPressure => 2,
                MidiChannelEventType.ControlChange => 2,
                MidiChannelEventType.ProgramChange => 1,
                MidiChannelEventType.ChannelPressure => 1,
                MidiChannelEventType.PitchBend => 2,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static MidiChannelEventType GetChannelEventType(byte firstByte)
        {
            return (firstByte & 0xF0) switch
            {
                0x80 => MidiChannelEventType.NoteOff,
                0x90 => MidiChannelEventType.NoteOn,
                0xA0 => MidiChannelEventType.PolyphonicKeyPressure,
                0xB0 => MidiChannelEventType.ControlChange,
                0xC0 => MidiChannelEventType.ProgramChange,
                0xD0 => MidiChannelEventType.ChannelPressure,
                0xE0 => MidiChannelEventType.PitchBend,
                _ => throw new System.ArgumentOutOfRangeException(nameof(firstByte), (firstByte & 0xF0).ToString("X2"), null)
            };
        }

        public static byte Code(this MidiChannelEventType type, int channel)
        {
            byte codeByte = type switch
            {
                MidiChannelEventType.NoteOn => 0x90,
                MidiChannelEventType.NoteOff => 0x80,
                MidiChannelEventType.PolyphonicKeyPressure => 0xA0,
                MidiChannelEventType.ControlChange => 0xB0,
                MidiChannelEventType.ProgramChange => 0xC0,
                MidiChannelEventType.ChannelPressure => 0xD0,
                MidiChannelEventType.PitchBend => 0xE0,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
            };
            return (byte)(codeByte | (channel & 0x0F));
        }

        public static int DataSize(this MidiSystemEventType type)
        {
            return type switch
            {
                MidiSystemEventType.SongPositionPointer => 2,
                MidiSystemEventType.SongSelect => 1,
                MidiSystemEventType.TuneRequest => 0,
                MidiSystemEventType.Undefined => 0,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static MidiSystemEventType GetSystemEventType(byte firstByte)
        {
            return firstByte switch
            {
                0xF0 => MidiSystemEventType.SongPositionPointer,
                0xF1 => MidiSystemEventType.SongSelect,
                0xF2 => MidiSystemEventType.TuneRequest,
                _ => MidiSystemEventType.Undefined,
            };
        }

        public static byte Code(this MidiSystemEventType type)
        {
            return type switch
            {
                MidiSystemEventType.SongPositionPointer => 0xF2,
                MidiSystemEventType.SongSelect => 0xF3,
                MidiSystemEventType.TuneRequest => 0xF6,
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }

    public class MidiChannelEvent : MidiTrackEvent
    {
        public int channel;
        public MidiChannelEventType type;
        public byte[] eventData;

        protected override void DecodeInner(byte[] data, byte firstByte, ref int offset)
        {
            channel = firstByte & 0x0F;
            type = MidiEventTypeInfo.GetChannelEventType(firstByte);
            int dataSize = type.DataSize();
            eventData = new byte[dataSize];
            System.Buffer.BlockCopy(data, offset, eventData, 0, dataSize);
            offset += dataSize;
        }

        protected override void EncodeInner(byte[] data, MidiChannelEvent prev, ref int offset)
        {
            if (!UseRunningStatus(prev))
            {
                data[offset++] = type.Code(channel);
            }
            System.Buffer.BlockCopy(eventData, 0, data, offset, type.DataSize());
            offset += eventData.Length;
        }

        protected override int GetInnerByteSize(MidiChannelEvent prev)
        {
            return (UseRunningStatus(prev) ? 0 : 1) + type.DataSize();
        }

        private bool UseRunningStatus(MidiChannelEvent prev) => prev != null && prev.type == type && prev.channel == channel;

        public override string ToString()
        {
            return $"ChannelEvent(delta: {deltaTime}, type: {type}, channel: {channel}, data: [{string.Join(", ", eventData.Select(b => b.ToString("X2")))}])";
        }
    }

    public class MidiSystemEvent : MidiTrackEvent
    {
        public MidiSystemEventType type;
        public byte[] eventData;

        protected override void DecodeInner(byte[] data, byte firstByte, ref int offset)
        {
            type = MidiEventTypeInfo.GetSystemEventType(firstByte);
            int dataSize = type.DataSize();
            eventData = new byte[dataSize];
            System.Buffer.BlockCopy(data, offset, eventData, 0, dataSize);
            offset += dataSize;
        }

        protected override void EncodeInner(byte[] data, MidiChannelEvent prev, ref int offset)
        {
            data[offset++] = type.Code();
            System.Buffer.BlockCopy(eventData, 0, data, offset, type.DataSize());
            offset += eventData.Length;
        }

        protected override int GetInnerByteSize(MidiChannelEvent prev)
        {
            return 1 + type.DataSize();
        }

        public override string ToString()
        {
            return $"SystemEvent(delta: {deltaTime}, type: {type}, data: [{string.Join(", ", eventData.Select(b => b.ToString("X2")))}])";
        }
    }

    public class MidiMetaEvent : MidiTrackEvent
    {
        public byte metaType;
        public byte[] metaData;

        protected override void DecodeInner(byte[] data, byte firstByte, ref int offset)
        {
            metaType = data[offset++];
            int length = ByteConverter.ReadVariableLength(data, ref offset);
            metaData = new byte[length];
            System.Buffer.BlockCopy(data, offset, metaData, 0, length);
            offset += length;
        }

        protected override void EncodeInner(byte[] data, MidiChannelEvent prev, ref int offset)
        {
            data[offset++] = 0xFF;
            data[offset++] = metaType;
            ByteConverter.WriteVariableLength(data, ref offset, metaData.Length);
            System.Buffer.BlockCopy(metaData, 0, data, offset, metaData.Length);
            offset += metaData.Length;
        }

        protected override int GetInnerByteSize(MidiChannelEvent prev)
        {
            return 2 + ByteConverter.VariableLengthSize(metaData.Length) + metaData.Length;
        }

        public override string ToString()
        {
            return $"MetaEvent(delta: {deltaTime}, type: {metaType}, data: [{string.Join(", ", metaData.Select(b => b.ToString("X2")))}])";
        }
    }

    public class MidiSysExEvent : MidiTrackEvent
    {
        public byte initialByte;
        public byte[] metaData;

        protected override void DecodeInner(byte[] data, byte firstByte, ref int offset)
        {
            initialByte = firstByte;
            int length = ByteConverter.ReadVariableLength(data, ref offset);
            metaData = new byte[length];
            System.Buffer.BlockCopy(data, offset, metaData, 0, length);
            offset += length;
        }

        protected override void EncodeInner(byte[] data, MidiChannelEvent prev, ref int offset)
        {
            data[offset++] = initialByte;
            ByteConverter.WriteVariableLength(data, ref offset, metaData.Length);
            System.Buffer.BlockCopy(metaData, 0, data, offset, metaData.Length);
            offset += metaData.Length;
        }

        protected override int GetInnerByteSize(MidiChannelEvent prev)
        {
            return 1 + ByteConverter.VariableLengthSize(metaData.Length) + metaData.Length;
        }

        public override string ToString()
        {
            return $"SysExEvent(delta: {deltaTime}, initialByte: {initialByte}, data: [{string.Join(", ", metaData.Select(b => b.ToString("X2")))}])";
        }
    }
}
