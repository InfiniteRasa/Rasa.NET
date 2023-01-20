using System;
using System.IO;
using System.Net;
using System.Numerics;
using System.Text;

namespace Rasa.Memory
{
    using Models;

    [Flags]
    public enum ProtocolBufferFlags : byte
    {
        // Settable clientside
        Debug = 0x01,
        DontFragment = 0x02,
        Unk04 = 0x04,
        Unk08 = 0x08,
        Unk10 = 0x10,
        Unk20 = 0x20,

        // Unsettable clientside
        Unk40 = 0x40,
        Unk80 = 0x80
    }

    public class ProtocolBufferReader : IDisposable
    {
        public BinaryReader Reader { get; }
        public ProtocolBufferFlags Flags { get; set; }
        public int UnknownValue { get; set; }

        public ProtocolBufferReader(BinaryReader reader, ProtocolBufferFlags flags)
        {
            Reader = reader;
            Flags = flags;
        }

        public void ReadDebugByte(byte value)
        {
            if ((Flags & ProtocolBufferFlags.Debug) != ProtocolBufferFlags.Debug)
                return;

            var val = Reader.ReadByte();
            if (val != value)
                throw new Exception($"ProtocolBufferReader::ReadDebugByte(): Expected {value}, found {val}");
        }

        public byte[] ReadArray()
        {
            ReadDebugByte(1);

            return Reader.ReadBytes(ReadCount());
        }

        public byte ReadByte()
        {
            ReadDebugByte(3);

            return Reader.ReadByte();
        }

        public ushort ReadUShort()
        {
            ReadDebugByte(5);

            return (Flags & ProtocolBufferFlags.DontFragment) == ProtocolBufferFlags.DontFragment ? Reader.ReadUInt16() : ReadUShortBySevenBits();
        }

        public int ReadInt()
        {
            ReadDebugByte(6);

            return (Flags & ProtocolBufferFlags.DontFragment) == ProtocolBufferFlags.DontFragment ? Reader.ReadInt32() : ReadIntBySevenBits();
        }

        public uint ReadUInt()
        {
            ReadDebugByte(7);

            return (Flags & ProtocolBufferFlags.DontFragment) == ProtocolBufferFlags.DontFragment ? Reader.ReadUInt32() : ReadUIntBySevenBits();
        }

        public ulong ReadULong()
        {
            ReadDebugByte(9);

            return (Flags & ProtocolBufferFlags.DontFragment) == ProtocolBufferFlags.DontFragment ? Reader.ReadUInt64() : ReadULongBySevenBits();
        }

        public int ReadCount()
        {
            ReadDebugByte(203);

            var lenMask = PeekByte();
            var lenCount = lenMask >> 6;

            var dest = new byte[4];

            Reader.Read(dest, 0, lenCount + 1);

            return (dest[0] & 0x3F) | (dest[1] << 6) | (dest[2] << 14) | (dest[3] << 22);
        }

        public string ReadString()
        {
            ReadDebugByte(13);

            var length = ReadCount();

            var strBytes = Reader.ReadBytes(length);

            return Encoding.UTF8.GetString(strBytes, 0, length);
        }

        public void ReadProtocolFlags()
        {
            if ((Flags & ProtocolBufferFlags.Unk80) == ProtocolBufferFlags.Unk80)
                return;

            var flagByte = Reader.ReadByte();

            Flags = (ProtocolBufferFlags)(flagByte & 0x3F);
            UnknownValue = flagByte >> 6;

            if (UnknownValue != 0)
                throw new NotImplementedException("Reading is disabled if UnknownValue isn't 0!");
        }

        public void ReadXORCheck(int length)
        {
            if ((Flags & ProtocolBufferFlags.Unk80) == ProtocolBufferFlags.Unk80)
                return;

            if (Reader.ReadByte() != (byte)((length & 0xFF) ^ ((length >> 8) & 0xFF) ^ ((length >> 16) & 0xFF) ^ ((length >> 24) & 0xFF)))
                throw new Exception("XORCheck failed!");
        }

        public void ReadPacketType(out ushort type, out bool compress)
        {
            ReadDebugByte(41);

            var typeVal = ReadUShort();

            type = (ushort)(typeVal >> 1);
            compress = (typeVal & 1) == 1;

            ReadDebugByte(42);
        }

        public IPEndPoint ReadNetAddress()
        {
            ReadDebugByte(41);

            var addr = ReadUInt(); // todo: maybe ntohl?
            var port = ReadInt() & 0xFFFF; // todo: maybe htons?

            ReadUInt(); // unknown, unused value

            ReadDebugByte(42);

            return new IPEndPoint(addr, port);
        }

        public Movement ReadMovement()
        {
            this.ReadDebugByte(41);

            this.ReadDebugByte(3);
            var unknownByte = this.ReadByte();

            var x = this.ReadPackedFloat();
            var y = this.ReadPackedFloat();
            var z = this.ReadPackedFloat();
            var position = new Vector3(x, y, z);

            var velocity = this.ReadPackedVelocity();
            var flags = this.ReadByte();

            var (viewX, viewY) = this.ReadPackedViewCoords();
            var viewDirection = new Vector2(viewX, viewY);

            this.ReadDebugByte(42);

            return new Movement(unknownByte, position, velocity, flags, viewDirection);
        }

        public float ReadPackedFloat()
        {
            ReadDebugByte(41);

            var value = (ReadByte() << 16)  | (ReadByte() << 8) | ReadByte();

            if ((value & 0x00800000) > 0)
                value -= 0xFFFFFF;

            ReadDebugByte(42);

            return value / 256.0f;
        }

        public float ReadPackedVelocity()
        {
            ReadDebugByte(41);

            var velocity = ReadUShort() / 1024.0f;

            ReadDebugByte(42);

            return velocity;
        }

        public (float viewX, float viewY) ReadPackedViewCoords()
        {
            ReadDebugByte(41);

            var viewX = ReadUShort() / 10430.378f;
            var viewY = ReadUShort() / 10430.378f;

            ReadDebugByte(42);

            return (viewX, viewY);
        }

        public short ReadShortBySevenBits()
        {
            int byteCount;
            var valueBytes = GatherSevenBitBytes(6, out byteCount);

            var bitCount = 0;
            short value = 0;

            for (var i = 0; i < byteCount;)
            {
                if (bitCount >= 16)
                    throw new Exception("Bitcount can't be higher or equal to 32!");

                value = (short)(((valueBytes[i] & 0x7F) << bitCount) | (value & ((1 << bitCount) - 1)));

                bitCount += 7;

                if ((valueBytes[i++] & 0x80) == 0)
                    return value;
            }

            throw new Exception("Input value is not over, but the array is!");
        }

        public ushort ReadUShortBySevenBits()
        {
            int byteCount;
            var valueBytes = GatherSevenBitBytes(6, out byteCount);

            var bitCount = 0;
            ushort value = 0;

            for (var i = 0; i < byteCount;)
            {
                if (bitCount >= 16)
                    throw new Exception("Bitcount can't be higher or equal to 32!");

                value = (ushort)(((valueBytes[i] & 0x7F) << bitCount) | (value & ((1 << bitCount) - 1)));

                bitCount += 7;

                if ((valueBytes[i++] & 0x80) == 0)
                    return value;
            }

            throw new Exception("Input value is not over, but the array is!");
        }

        public int ReadIntBySevenBits()
        {
            int byteCount;
            var valueBytes = GatherSevenBitBytes(6, out byteCount);

            var negative = (valueBytes[0] & 0x40) != 0;
            var value = valueBytes[0] & 0x3F;

            if ((valueBytes[0] & 0x80) == 0)
                return negative ? -value : value;
            
            var bitCount = 6;

            for (var i = 1; i < byteCount;)
            {
                if (bitCount >= 32)
                    throw new Exception("Bitcount can't be higher or equal to 32!");

                value = ((valueBytes[i] & 0x7F) << bitCount) | (value & ((1 << bitCount) - 1));

                bitCount += 7;

                if ((valueBytes[i++] & 0x80) == 0)
                    return negative ? -value : value;
            }

            throw new Exception("Input value is not over, but the array is!");
        }

        public uint ReadUIntBySevenBits()
        {
            int byteCount;
            var valueBytes = GatherSevenBitBytes(6, out byteCount);

            var bitCount = 0;
            var value = 0U;

            for (var i = 0; i < byteCount;)
            {
                if (bitCount >= 32)
                    throw new Exception("Bitcount can't be higher or equal to 32!");

                value = (uint) (((valueBytes[i] & 0x7F) << bitCount) | ((int) value & ((1 << bitCount) - 1)));

                bitCount += 7;

                if ((valueBytes[i++] & 0x80) == 0)
                    return value;
            }

            throw new Exception("Input value is not over, but the array is!");
        }

        public ulong ReadULongBySevenBits()
        {
            int byteCount;
            var valueBytes = GatherSevenBitBytes(10, out byteCount);

            var bitCount = 0;
            var value = 0UL;

            for (var i = 0; i < byteCount;)
            {
                if (bitCount >= 64)
                    throw new Exception("Bitcount can't be higher or equal to 64!");

                value = ((valueBytes[i] & 0x7FUL) << bitCount) | (value & ((1UL << bitCount) - 1UL));

                bitCount += 7;

                if ((valueBytes[i++] & 0x80) == 0)
                    return value;
            }

            throw new Exception("Input value is not over, but the array is!");
        }

        private byte[] GatherSevenBitBytes(int length, out int byteInd)
        {
            var hasValue = false;
            var dest = new byte[length];

            for (byteInd = 0; byteInd < length;)
            {
                dest[byteInd] = Reader.ReadByte();

                hasValue = (dest[byteInd++] & 0x80) != 0;
                if (!hasValue)
                    break;
            }

            if (hasValue)
                throw new Exception("Reading int from rasa bytes should have been continued, but ran out of available bytes!");

            return dest;
        }

        private byte PeekByte()
        {
            var peekedValue = Reader.ReadByte();

            Reader.BaseStream.Position -= 1;

            return peekedValue;
        }

        public void Dispose()
        {
        }
    }
}
