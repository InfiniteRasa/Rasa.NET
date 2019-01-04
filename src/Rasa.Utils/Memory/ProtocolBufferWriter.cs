using System;
using System.IO;
using System.Net;
using System.Text;

namespace Rasa.Memory
{
    public class ProtocolBufferWriter : IDisposable
    {
        public BinaryWriter Writer { get; }
        public ProtocolBufferFlags Flags { get; }
        public int UnknownValue { get; set; }

        public ProtocolBufferWriter(BinaryWriter writer, ProtocolBufferFlags flags)
        {
            Writer = writer;
            Flags = flags;
        }

        public void WriteDebugByte(byte debug)
        {
            if ((Flags & ProtocolBufferFlags.Debug) != ProtocolBufferFlags.Debug)
                return;

            Writer.Write(debug);
        }

        public void WriteArray(byte[] bytes)
        {
            WriteArray(bytes, 0, bytes.Length);
        }

        public void WriteArray(byte[] bytes, int offset, int length)
        {
            WriteDebugByte(1);

            WriteCount(length);

            Writer.Write(bytes, offset, length);
        }

        public void WriteByte(byte value)
        {
            WriteDebugByte(3);

            Writer.Write(value);
        }

        public void WriteUShort(ushort value)
        {
            WriteDebugByte(5);

            if ((Flags & ProtocolBufferFlags.DontFragment) == ProtocolBufferFlags.DontFragment)
                Writer.Write(value);
            else
                WriteUShortBySevenBits(value);
        }

        public void WriteUInt(uint value)
        {
            WriteDebugByte(7);

            if ((Flags & ProtocolBufferFlags.DontFragment) == ProtocolBufferFlags.DontFragment)
                Writer.Write(value);
            else
                WriteUIntBySevenBits(value);
        }

        public void WriteULong(ulong value)
        {
            WriteDebugByte(9);

            if ((Flags & ProtocolBufferFlags.DontFragment) == ProtocolBufferFlags.DontFragment)
                Writer.Write(value);
            else
                WriteULongBySevenBits(value);
        }

        public void WriteString(string value)
        {
            WriteDebugByte(13);

            WriteCount(value.Length);

            var bytes = Encoding.UTF8.GetBytes(value);

            Writer.Write(bytes, 0, bytes.Length);
        }

        public void WriteCount(int count)
        {
            if ((count & 0xC0000000) != 0)
                throw new Exception("Count is too big!");

            WriteDebugByte(203);

            var bytes = new[]
            {
                (byte) (count & 0x3F),
                (byte) ((count >> 6) & 0xFF),
                (byte) ((count >> 14) & 0xFF),
                (byte) ((count >> 22) & 0xFF)
            };

            var byteCount = 0;

            if (bytes[3] != 0)
                byteCount = 3;
            else if (bytes[2] != 0)
                byteCount = 2;
            else if (bytes[1] != 0)
                byteCount = 1;

            bytes[0] |= (byte) (byteCount << 6);

            Writer.Write(bytes, 0, byteCount + 1);
        }

        public void WriteProtocolFlags()
        {
            if ((Flags & ProtocolBufferFlags.Unk80) == ProtocolBufferFlags.Unk80)
                return;

            var flagByte = (byte) (((byte) Flags & 0x3F) | ((UnknownValue & 0xFF) << 6));

            Writer.Write(flagByte);
        }

        public void WriteXORCheck(int length)
        {
            if ((Flags & ProtocolBufferFlags.Unk80) == ProtocolBufferFlags.Unk80)
                return;

            Writer.Write((byte) ((length & 0xFF) ^ ((length >> 8) & 0xFF) ^ ((length >> 16) & 0xFF) ^ ((length >> 24) & 0xFF)));
        }

        public void WritePacketType(ushort type, bool compress)
        {
            WriteDebugByte(41);

            WriteUShort((ushort) (type << 1 | (compress ? 1 : 0)));

            WriteDebugByte(42);
        }

        public void WriteNetAddress(IPEndPoint address)
        {

        }

        public void WriteMovementData()
        {

        }

        public void WritePackedFloat(float value)
        {
            WriteDebugByte(41);

            WriteByte((byte)((ushort)(value * 256.0f) >> 16));
            WriteByte((byte)((ushort)(value * 256.0f) >> 8));
            WriteByte((byte)((ushort)(value * 256.0f)));

            WriteDebugByte(42);
        }

        public void WritePackedVelocity(float value)
        {
            WriteDebugByte(41);

            WriteUShort((ushort)(value * 1024.0f));

            WriteDebugByte(42);
        }

        public void WriteViewCoords(float viewX, float viewY)
        {
            WriteDebugByte(41);

            WriteUShort((ushort)(viewX * 10430.378f));
            WriteUShort((ushort)(viewY * 10430.378f));

            WriteDebugByte(42);
        }

        public void WriteUShortBySevenBits(ushort value)
        {
            int byteCount;
            var bytes = CreateUSevenBitBytes(value, 4, out byteCount);

            Writer.Write(bytes, 0, byteCount);
        }

        public void WriteUIntBySevenBits(uint value)
        {
            int byteCount;
            var bytes = CreateUSevenBitBytes(value, 6, out byteCount);

            Writer.Write(bytes, 0, byteCount);
        }

        public void WriteULongBySevenBits(ulong value)
        {
            int byteCount;
            var bytes = CreateUSevenBitBytes(value, 10, out byteCount);

            Writer.Write(bytes, 0, byteCount);
        }

        private static byte[] CreateUSevenBitBytes(ulong value, int destLength, out int byteCount)
        {
            var dest = new byte[destLength];
            bool empty;

            byteCount = 0;

            do
            {
                if (byteCount >= destLength)
                    break;

                dest[byteCount] = (byte) (value & 0x7F);

                value >>= 7;
                empty = value == 0;

                if (!empty)
                    dest[byteCount] |= 0x80;

                ++byteCount;
            }
            while (!empty);

            return dest;
        }

        public void Dispose()
        {
        }
    }
}
