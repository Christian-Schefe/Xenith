using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WavFile
{
    public readonly WavFormat format;
    public readonly int sampleRate;
    public readonly int channels;
    public readonly float[] samples;

    public WavFile(WavFormat format, int sampleRate, int channels, float[] samples)
    {
        this.format = format;
        this.sampleRate = sampleRate;
        this.channels = channels;
        this.samples = samples;
    }

    public void Rescale(float maxAmplitude)
    {
        var maxSample = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            maxSample = Mathf.Max(Mathf.Abs(samples[i]), maxSample);
        }
        float factor = maxAmplitude / maxSample;
        if (maxSample > 0)
        {
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= factor;
            }
        }
    }

    public byte[] Encode()
    {
        var chunks = new List<WavChunk>
        {
            new FmtChunk(format, channels, sampleRate)
        };
        if (format != WavFormat.PCM)
        {
            chunks.Add(new FactChunk(samples.Length));
        }
        chunks.Add(new DataChunk(format, channels, samples));

        var riffChunk = new RiffChunk(chunks);

        var data = new byte[riffChunk.ChunkSize];
        int offset = 0;

        riffChunk.Encode(data, ref offset);

        return data;
    }

    public void WriteToFile(string path)
    {
        var data = Encode();
        System.IO.File.WriteAllBytes(path, data);
    }
}

public enum WavFormat
{
    PCM,
    IEEEFloat,
}

public static class WavFormatInfo
{
    public static int SampleSize(this WavFormat format)
    {
        return format switch
        {
            WavFormat.PCM => 2,
            WavFormat.IEEEFloat => 4,
            _ => throw new System.NotImplementedException(),
        };
    }

    public static short Code(this WavFormat format)
    {
        return format switch
        {
            WavFormat.PCM => 1,
            WavFormat.IEEEFloat => 3,
            _ => throw new System.NotImplementedException(),
        };
    }
}

public static class ByteConverter
{
    public static void WriteInt(byte[] data, ref int offset, int value, bool littleEndian)
    {
        byte b1 = (byte)(value & 0xFF);
        byte b2 = (byte)((value >> 8) & 0xFF);
        byte b3 = (byte)((value >> 16) & 0xFF);
        byte b4 = (byte)((value >> 24) & 0xFF);
        if (littleEndian)
        {
            data[offset++] = b1;
            data[offset++] = b2;
            data[offset++] = b3;
            data[offset++] = b4;
        }
        else
        {
            data[offset++] = b4;
            data[offset++] = b3;
            data[offset++] = b2;
            data[offset++] = b1;
        }
    }

    public static void WriteShort(byte[] data, ref int offset, short value, bool littleEndian)
    {
        byte b1 = (byte)(value & 0xFF);
        byte b2 = (byte)((value >> 8) & 0xFF);
        if (littleEndian)
        {
            data[offset++] = b1;
            data[offset++] = b2;
        }
        else
        {
            data[offset++] = b2;
            data[offset++] = b1;
        }
    }
}

public abstract class WavChunk
{
    public readonly string chunkId;

    public int ChunkSize => InnerChunkSize + 8;
    protected abstract int InnerChunkSize { get; }

    public WavChunk(string chunkId)
    {
        this.chunkId = chunkId;
    }

    public void Encode(byte[] data, ref int offset)
    {
        WriteString(data, ref offset, chunkId, 4);
        WriteInt(data, ref offset, InnerChunkSize);
        EncodeInner(data, ref offset);
    }

    protected abstract void EncodeInner(byte[] data, ref int offset);

    protected void WriteString(byte[] data, ref int offset, string str, int size)
    {
        var strBytes = System.Text.Encoding.ASCII.GetBytes(str);
        System.Buffer.BlockCopy(strBytes, 0, data, offset, size);
        offset += size;
    }

    protected void WriteInt(byte[] data, ref int offset, int value)
    {
        ByteConverter.WriteInt(data, ref offset, value, true);
    }

    protected void WriteShort(byte[] data, ref int offset, short value)
    {
        ByteConverter.WriteShort(data, ref offset, value, true);
    }
}

public class RiffChunk : WavChunk
{
    private readonly List<WavChunk> chunks;
    protected override int InnerChunkSize => 4 + chunks.Sum(chunk => chunk.ChunkSize);

    public RiffChunk(List<WavChunk> chunks) : base("RIFF")
    {
        this.chunks = chunks;
    }

    protected override void EncodeInner(byte[] data, ref int offset)
    {
        WriteString(data, ref offset, "WAVE", 4);
        foreach (var chunk in chunks)
        {
            chunk.Encode(data, ref offset);
        }
    }
}

public class FmtChunk : WavChunk
{
    private readonly WavFormat format;
    private readonly int channels;
    private readonly int sampleRate;

    protected override int InnerChunkSize => 16 + (HasExtension ? ExtensionSize + 2 : 0);

    private bool HasExtension => format switch
    {
        WavFormat.PCM => false,
        WavFormat.IEEEFloat => true,
        _ => throw new System.NotImplementedException(),
    };

    private int ExtensionSize => format switch
    {
        WavFormat.PCM => 0,
        WavFormat.IEEEFloat => 0,
        _ => throw new System.NotImplementedException(),
    };

    public FmtChunk(WavFormat format, int channels, int sampleRate) : base("fmt ")
    {
        this.format = format;
        this.channels = channels;
        this.sampleRate = sampleRate;
    }

    protected override void EncodeInner(byte[] data, ref int offset)
    {
        int sampleSize = format.SampleSize();
        WriteShort(data, ref offset, format.Code());
        WriteShort(data, ref offset, (short)channels);
        WriteInt(data, ref offset, sampleRate);
        WriteInt(data, ref offset, sampleRate * channels * sampleSize);
        WriteShort(data, ref offset, (short)(channels * sampleSize));
        WriteShort(data, ref offset, (short)(8 * sampleSize));
        if (HasExtension)
        {
            WriteShort(data, ref offset, (short)ExtensionSize);
        }
    }
}

public class FactChunk : WavChunk
{
    private readonly int sampleCount;

    protected override int InnerChunkSize => 4;

    public FactChunk(int sampleCount) : base("fact")
    {
        this.sampleCount = sampleCount;
    }

    protected override void EncodeInner(byte[] data, ref int offset)
    {
        WriteInt(data, ref offset, sampleCount);
    }
}

public class DataChunk : WavChunk
{
    private readonly WavFormat format;
    private readonly int channels;
    private readonly float[] samples;

    protected override int InnerChunkSize => samples.Length * format.SampleSize();

    public DataChunk(WavFormat format, int channels, float[] samples) : base("data")
    {
        this.format = format;
        this.channels = channels;
        this.samples = samples;
    }

    protected override void EncodeInner(byte[] data, ref int offset)
    {
        int blocks = samples.Length / channels;
        for (int i = 0; i < blocks; i++)
        {
            for (int j = 0; j < channels; j++)
            {
                var sample = samples[i * channels + j];
                if (format == WavFormat.PCM)
                {
                    short pcm = (short)(sample * short.MaxValue);
                    WriteShort(data, ref offset, pcm);
                }
                else if (format == WavFormat.IEEEFloat)
                {
                    var bytes = System.BitConverter.GetBytes(sample);
                    System.Buffer.BlockCopy(bytes, 0, data, offset, 4);
                    offset += 4;
                }
                else
                {
                    throw new System.NotImplementedException($"Format {format} is not supported.");
                }
            }
        }
    }
}