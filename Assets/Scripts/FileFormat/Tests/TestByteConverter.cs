using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using FileFormat;
using System.Linq;

public class TestByteConverter
{
    private static List<(int intVal, bool littleEndian, byte[] bytesVal)> intCases = new()
    {
        (32, false, new byte[] { 0, 0, 0, 32 }),
        (32, true, new byte[] { 32, 0, 0, 0 }),
        (0x12345678, false, new byte[] { 18, 52, 86, 120 }),
        (0x12345678, true, new byte[] { 120, 86, 52, 18 }),
        (-2, false, new byte[] { 255, 255, 255, 254 }),
        (-2, true, new byte[] { 254, 255, 255, 255 }),
    };

    private static List<(short shortVal, bool littleEndian, byte[] bytesVal)> shortCases = new()
    {
        (32, false, new byte[] { 0, 32 }),
        (32, true, new byte[] { 32, 0 }),
        (0x1234, false, new byte[] { 18, 52 }),
        (0x1234, true, new byte[] { 52, 18 }),
        (-2, false, new byte[] { 255, 254 }),
        (-2, true, new byte[] { 254, 255 }),
    };

    private static List<(int intVal, byte[] bytesVal)> variableLengthCases = new()
    {
        (0, new byte[] { 0x00 }),
        (1, new byte[] { 0x01 }),
        (127, new byte[] { 0x7F }),
        (128, new byte[] { 0x81, 0x00 }),
        (272, new byte[] { 0x82, 0x10 }),
    };

    [Test]
    public void TestWriteInt()
    {
        var allData = new byte[intCases.Sum(c => c.bytesVal.Length)];
        int allOffset = 0;

        foreach (var (intVal, littleEndian, bytesVal) in intCases)
        {
            var data = new byte[bytesVal.Length];
            int offset = 0;
            ByteConverter.WriteInt(data, ref offset, intVal, littleEndian);
            Assert.AreEqual(bytesVal, data);
            Assert.AreEqual(bytesVal.Length, offset);

            ByteConverter.WriteInt(allData, ref allOffset, intVal, littleEndian);
        }

        Assert.AreEqual(allData, intCases.SelectMany(c => c.bytesVal).ToArray());
        Assert.AreEqual(allOffset, allData.Length);
    }

    [Test]
    public void TestWriteShort()
    {
        var allData = new byte[shortCases.Sum(c => c.bytesVal.Length)];
        int allOffset = 0;

        foreach (var (shortVal, littleEndian, bytesVal) in shortCases)
        {
            var data = new byte[bytesVal.Length];
            int offset = 0;
            ByteConverter.WriteShort(data, ref offset, shortVal, littleEndian);
            Assert.AreEqual(bytesVal, data);
            Assert.AreEqual(bytesVal.Length, offset);

            ByteConverter.WriteShort(allData, ref allOffset, shortVal, littleEndian);
        }

        Assert.AreEqual(allData, shortCases.SelectMany(c => c.bytesVal).ToArray());
        Assert.AreEqual(allOffset, allData.Length);
    }

    [Test]
    public void TestWriteVariableLength()
    {
        var allData = new byte[variableLengthCases.Sum(c => c.bytesVal.Length)];
        int allOffset = 0;

        foreach (var (intVal, bytesVal) in variableLengthCases)
        {
            var data = new byte[bytesVal.Length];
            int offset = 0;
            ByteConverter.WriteVariableLength(data, ref offset, intVal);
            Assert.AreEqual(bytesVal, data, "Checking:" + intVal);
            Assert.AreEqual(bytesVal.Length, offset);

            ByteConverter.WriteVariableLength(allData, ref allOffset, intVal);
        }

        Assert.AreEqual(allData, variableLengthCases.SelectMany(c => c.bytesVal).ToArray());
        Assert.AreEqual(allOffset, allData.Length);
    }

    [Test]
    public void TestReadInt()
    {
        var allData = intCases.SelectMany(c => c.bytesVal).ToArray();
        int allOffset = 0;

        foreach (var (intVal, littleEndian, bytesVal) in intCases)
        {
            int offset = 0;
            int result = ByteConverter.ReadInt(bytesVal, ref offset, littleEndian);
            Assert.AreEqual(intVal, result);
            Assert.AreEqual(bytesVal.Length, offset);

            result = ByteConverter.ReadInt(allData, ref allOffset, littleEndian);
            Assert.AreEqual(intVal, result);
        }

        Assert.AreEqual(allOffset, allData.Length);
    }

    [Test]
    public void TestReadShort()
    {
        var allData = shortCases.SelectMany(c => c.bytesVal).ToArray();
        int allOffset = 0;

        foreach (var (shortVal, littleEndian, bytesVal) in shortCases)
        {
            int offset = 0;
            short result = ByteConverter.ReadShort(bytesVal, ref offset, littleEndian);
            Assert.AreEqual(shortVal, result);
            Assert.AreEqual(bytesVal.Length, offset);

            result = ByteConverter.ReadShort(allData, ref allOffset, littleEndian);
            Assert.AreEqual(shortVal, result);
        }

        Assert.AreEqual(allOffset, allData.Length);
    }

    [Test]
    public void TestReadVariableLength()
    {
        var allData = variableLengthCases.SelectMany(c => c.bytesVal).ToArray();
        int allOffset = 0;

        foreach (var (intVal, bytesVal) in variableLengthCases)
        {
            int offset = 0;
            int result = ByteConverter.ReadVariableLength(bytesVal, ref offset);
            Assert.AreEqual(intVal, result);
            Assert.AreEqual(bytesVal.Length, offset);

            result = ByteConverter.ReadVariableLength(allData, ref allOffset);
            Assert.AreEqual(intVal, result);
        }

        Assert.AreEqual(allOffset, allData.Length);
    }
}
