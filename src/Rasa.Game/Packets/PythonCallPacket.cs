using System;
using System.Diagnostics;
using System.IO;

namespace Rasa.Packets
{
    using Data;
    using Extensions;

    public class PythonCallPacket : IBasePacket
    {
        private readonly PythonPacket _packet;
        private readonly uint _entityId;

        public int Size { get; }
        public int DataSize { get; private set; }
        public bool? Return { get; private set; }
        public GameOpcode Opcode { get; private set; }
        public byte Type { get; private set; }
        public uint AccountId { get; private set; }
        public uint OneTimeKey { get; private set; }

        public PythonCallPacket(int size)
        {
            Size = size;
        }

        public PythonCallPacket(PythonPacket packet, uint entityId)
        {
            _packet = packet;
            _entityId = entityId;

            Opcode = packet.Opcode;
        }

        public void Read(BinaryReader br)
        {
            if (Size >= 0xFFFF)
                throw new Exception("Message is too big!");

            if (Size < 4)
            {
                Return = false;
                return;
            }

            if (Size > 3000)
                throw new Exception("Big packet! Check it...");

            var subSize = br.ReadUInt16();
            if (subSize != Size)
                Debugger.Break(); // for testing purposes

            var majorOpcode = br.ReadUInt16(); // 1 byte channel id, 1 byte unk?
            if (majorOpcode == 1)
            {
                // function from c++ project
                // mapChannel_decodeMovementPacket(mc, data+pIdx, subSize-pIdx);
                var movementPacket = new MovementCallPacket(subSize);
                movementPacket.Read(br);
                Return = true;
                return;
            }
            else if (majorOpcode != 0)
            {
                Return = true;
                return;
            }

            if (br.ReadByte() != 2)
                Debugger.Break();

            Type = br.ReadByte();

            if (br.ReadByte() != 0)
                Debugger.Break();

            if (br.ReadByte() != 3)
                Debugger.Break();

            if (br.ReadByte() != 3)
                Debugger.Break();

            if (Type == 2)
            {
                if (br.ReadByte() != 0x29)
                    Debugger.Break();

                if (br.ReadByte() != 3)
                    Debugger.Break();

                if (br.ReadByte() != 1)
                    Debugger.Break();

                if (br.ReadByte() != 7)
                    Debugger.Break();

                AccountId = br.ReadUInt32();

                if (br.ReadByte() != 7)
                    Debugger.Break();

                OneTimeKey = br.ReadUInt32();

                if (br.ReadByte() != 0xD)
                    Debugger.Break();

                if (br.ReadByte() != 0xCB)
                    Debugger.Break();

                var versionLength = br.ReadByte();
                var wrongVersion = versionLength != 8;
                var version = br.ReadUtf8StringOn(versionLength);

                if (wrongVersion || version != "1.16.5.0")
                    Logger.WriteLog(LogType.Error, $"Client version mismatch: Server: 1.16.5.0 | Client: {version}");

                if (br.ReadByte() != 0x2A)
                    Debugger.Break();

                return;
            }

            if (Type != 0x0C)
            {
                Return = true;
                return;
            }

            var val = br.ReadByte();
            if (val == 0)
            {
                Return = true;
                return;
            }

            if (val != 0x29)
                Debugger.Break();

            if (br.ReadByte() != 3)
                Debugger.Break();

            val = br.ReadByte();
            if (val == 0 || val > 0x10)
                Debugger.Break();

            if (br.ReadByte() != 7)
                Debugger.Break();

            Opcode = (GameOpcode) br.ReadUInt32();

            if (br.ReadByte() != 1)
                Debugger.Break();

            if (br.ReadByte() != 0xCB)
                Debugger.Break();

            var lenMask = br.ReadByte();
            switch (lenMask >> 6)
            {
                case 0:
                    DataSize = lenMask & 0x3F;
                    break;

                case 1:
                    DataSize = lenMask & 0x3F;
                    DataSize |= br.ReadByte() << 6;
                    break;

                default:
                    Debugger.Break();
                    break;
            }
        }

        public void Write(BinaryWriter bw)
        {
            var lenBegin = (int) bw.BaseStream.Position;

            bw.Write((ushort) 41); // Subsize
            bw.Write((byte) 0); // Packet 0 - if you specify something other than 0(except 0xFF) there will be some additional preleading data?
            bw.Write((byte) 1);

            var xorCheck = (int) bw.BaseStream.Position;

            // Header
            bw.Write((byte) 0); // 2:6 mask
            bw.Write((byte) (7 << 1)); // Opcode and flag --> 7 means the main packet handler

            var x = (int) bw.BaseStream.Position - xorCheck;

            bw.Write((byte) ((x & 0xFF) ^ ((x >> 8) & 0xFF) ^ ((x >> 16) & 0xFF) ^ ((x >> 24) & 0xFF)));
            xorCheck = (int) bw.BaseStream.Position;

            bw.Write((byte) 0); // Compression
            bw.Write((byte) 1); // Seletion table for opcode 7 --> 00782470   . 83EC 34        SUB ESP,34

            var entityId = _entityId;
            if (entityId == 0)
                bw.Write((byte) 0);
            else
            {
                while (entityId != 0)
                {
                    if ((entityId & ~0x7F) != 0)
                        bw.Write((byte) ((entityId & 0x7F) | 0x80));
                    else
                        bw.Write((byte) (entityId & 0x7F));

                    entityId >>= 7;
                }
            }

            var methodId = (uint) _packet.Opcode;

            if (methodId <= 0x7F)
                bw.Write((byte) methodId);
            else if (methodId <= 0x3FFF)
            {
                bw.Write((byte) (0x80 | (methodId & 0x7F)));
                bw.Write((byte) (methodId >> 7));
            }
            else if (methodId <= 0x1FFFFF)
            {
                bw.Write((byte) (0x80 | (methodId & 0x7F)));
                bw.Write((byte) (0x80 | ((methodId >> 7) & 0x7F)));
                bw.Write((byte) (methodId >> 14));
            }
            else if (methodId <= 0xFFFFFFF)
            {
                bw.Write((byte) (0x80 | (methodId & 0x7F)));
                bw.Write((byte) (0x80 | ((methodId >> 7) & 0x7F)));
                bw.Write((byte) (0x80 | ((methodId >> 14) & 0x7F)));
                bw.Write((byte) (methodId >> 21));
            }
            else
                Debugger.Break();

            bw.Write((byte) 0xFF); // Size of parameter block
            bw.Write((byte) 0xFF); // Size of parameter block

            var paramBlockBegin = bw.BaseStream.Position;

            bw.Write((byte) 0x4F);

            //var dataLenPosition = bw.BaseStream.Position;
            //bw.Write(0); // placeholder
            
            _packet.Write(bw);

            //bw.WriteAt((int) (bw.BaseStream.Position - dataLenPosition - 4), dataLenPosition); // Write data length

            bw.Write((byte) 0x66); // dunno why, client expects is

            var pbLen = (int) (bw.BaseStream.Position - paramBlockBegin);
            if (pbLen > 0xFFF)
                Debugger.Break();

            var currentPos = (int) bw.BaseStream.Position;

            bw.BaseStream.Position = paramBlockBegin - 2;

            bw.Write((byte) ((pbLen & 0x3F) | 0x40));
            bw.Write((byte) (pbLen >> 6));

            bw.BaseStream.Position = currentPos;

            x = currentPos - xorCheck;

            bw.Write((byte) ((x & 0xFF) ^ ((x >> 8) & 0xFF) ^ ((x >> 16) & 0xFF) ^ ((x >> 24) & 0xFF)));
            //xorCheck = (int)stream.Position;

            bw.Write((byte) 0x06); // Checksum 2
            bw.Write((byte) 0x06); // Checksum 2

            currentPos = (int) bw.BaseStream.Position;

            bw.BaseStream.Position = lenBegin;

            bw.Write((ushort) (currentPos - lenBegin));

            bw.BaseStream.Position = currentPos;
        }
    }
}
