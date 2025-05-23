using UnityEngine;

namespace FileFormat
{
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

        public static int ReadInt(byte[] data, ref int offset, bool littleEndian)
        {
            byte b1 = data[offset++];
            byte b2 = data[offset++];
            byte b3 = data[offset++];
            byte b4 = data[offset++];
            if (littleEndian)
            {
                return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
            }
            else
            {
                return b4 | (b3 << 8) | (b2 << 16) | (b1 << 24);
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

        public static short ReadShort(byte[] data, ref int offset, bool littleEndian)
        {
            byte b1 = data[offset++];
            byte b2 = data[offset++];
            if (littleEndian)
            {
                return (short)(b1 | (b2 << 8));
            }
            else
            {
                return (short)(b2 | (b1 << 8));
            }
        }

        public static int ReadVariableLength(byte[] data, ref int offset)
        {
            int value = 0;
            byte currentByte;

            do
            {
                currentByte = data[offset++];
                value = (value << 7) | (currentByte & 0x7F);
            } while ((currentByte & 0x80) != 0);

            return value;
        }

        public static void WriteVariableLength(byte[] data, ref int offset, int value)
        {
            int buffer = value & 0x7F;

            while ((value >>= 7) > 0)
            {
                buffer <<= 8;
                buffer |= ((value & 0x7F) | 0x80);
            }

            while (true)
            {
                data[offset++] = (byte)(buffer & 0xFF);
                if ((buffer & 0x80) == 0)
                    break;
                buffer >>= 8;
            }
        }

        public static int VariableLengthSize(int value)
        {
            int size = 0;
            do
            {
                size++;
                value >>= 7;
            } while (value != 0);
            return size;
        }
    }
}
