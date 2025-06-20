using System;

namespace DSP
{
    public class RingBuffer
    {
        private readonly float[] buffer;
        private int writeIndex;
        private int readIndex;

        public RingBuffer(int size)
        {
            buffer = new float[size];
            writeIndex = 0;
            readIndex = 0;
        }

        public bool CanWrite(int length) => length <= FreeSpace;
        public bool CanRead(int length) => length <= AvailableData;

        public void WriteFrom(float[] data, int offset, int length)
        {
            if (length > FreeSpace)
            {
                throw new InvalidOperationException("Not enough space in the ring buffer to write data.");
            }

            while (length > 0)
            {
                int spaceLeft = buffer.Length - writeIndex;
                int toWrite = Math.Min(length, spaceLeft);
                Array.Copy(data, offset, buffer, writeIndex, toWrite);
                writeIndex = (writeIndex + toWrite) % buffer.Length;
                length -= toWrite;
                offset += toWrite;
            }
        }

        public void ReadInto(float[] data, int offset, int length)
        {
            if (length > AvailableData)
            {
                throw new InvalidOperationException("Not enough data in the ring buffer to read.");
            }

            while (length > 0)
            {
                int spaceLeft = buffer.Length - readIndex;
                int toRead = Math.Min(length, spaceLeft);
                Array.Copy(buffer, readIndex, data, offset, toRead);
                readIndex = (readIndex + toRead) % buffer.Length;
                length -= toRead;
                offset += toRead;
            }
        }

        public int AvailableData => (writeIndex - readIndex + buffer.Length) % buffer.Length;
        public int FreeSpace => buffer.Length - AvailableData - 1;
    }
}
