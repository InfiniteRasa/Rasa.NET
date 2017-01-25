using System;
using System.Diagnostics;
using System.IO;

namespace Rasa.Packets
{
    using Data;
    using Extensions;
    using Structures;
	// ToDo
    public class MovementCallPacket : IBasePacket
    {
        private readonly NetCompressedMovement _movement;
        private readonly uint _entityId;

        public int Size { get; }
        public int DataSize { get; private set; }
        public bool? Return { get; private set; }
        public byte Type { get; private set; }
        public uint AccountId { get; private set; }

        public MovementCallPacket(int size)
        {
            Size = size;
        }

        public MovementCallPacket(uint entityId, NetCompressedMovement movement)
        {
            _entityId = entityId;
            _movement = movement;
        }

        public void Read(BinaryReader br)
        {
            Console.WriteLine("Movement packet received");
            // ToDo
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((byte)1); // align byte

            var lenBegin = (int)bw.BaseStream.Position;

            bw.Write((ushort)41); // Subsize
            bw.Write((byte)0); // Packet 0 - if you specify something other than 0(except 0xFF) there will be some additional preleading data?
            bw.Write((byte)1);

            var xorCheck = (int)bw.BaseStream.Position;

            // Header
            bw.Write((byte)0); // 2:6 mask
            bw.Write((byte)(4 << 1)); // Opcode and flag --> 4 movementHandler B

            var x = (int)bw.BaseStream.Position - xorCheck;

            bw.Write((byte)((x & 0xFF) ^ ((x >> 8) & 0xFF) ^ ((x >> 16) & 0xFF) ^ ((x >> 24) & 0xFF)));
            xorCheck = (int)bw.BaseStream.Position;

            //data block 2 (header 2)
            bw.Write((byte)0); // Compression
            bw.Write((byte)2); // // formatB(2)

            var entityId = _entityId;
            while (entityId != 0)
            {
                if ((entityId & ~0x7F) != 0)
                    bw.Write((byte)((entityId & 0x7F) | 0x80));
                else
                    bw.Write((byte)(entityId & 0x7F));

                entityId >>= 7;
            }
            // formatA
            bw.Write((byte)0x00); // unknown (if not zero -> Position data, velocity and flag disabled?)
            // posX
            bw.Write((byte)(_movement.PosX24b >> 16) & 0xFF);
            bw.Write((byte)(_movement.PosX24b >> 8) & 0xFF);
            bw.Write((byte)(_movement.PosX24b >> 0) & 0xFF);
            // posY
            bw.Write((byte)(_movement.PosY24b >> 16) & 0xFF);
            bw.Write((byte)(_movement.PosY24b >> 8) & 0xFF);
            bw.Write((byte)(_movement.PosY24b >> 0) & 0xFF);
            // posZ
            bw.Write((byte)(_movement.PosZ24b >> 16) & 0xFF);
            bw.Write((byte)(_movement.PosZ24b >> 8) & 0xFF);
            bw.Write((byte)(_movement.PosZ24b >> 0) & 0xFF);
            // velocity
            if (_movement.Velocity <= 0x7F)
                bw.Write((byte)_movement.Velocity);
            else if (_movement.Velocity <= 0x3FFF)
            {
                bw.Write((byte)(0x80 | (_movement.Velocity & 0x7F)));
                bw.Write((byte)(_movement.Velocity >> 7));
            }
            else
            {
                bw.Write((byte)(0x80 | (_movement.Velocity & 0x7F)));
                bw.Write((byte)(0x80 | ((_movement.Velocity >> 7) & 0x7F)));
                bw.Write((byte)(_movement.Velocity >> 14));
            }
            //PacketOut_AddByte(&SPB, 10);
            //MOVEFLAG_STOPPED = 0
            //MOVEFLAG_FORWARD = 1
            //MOVEFLAG_BACKWARD = 2
            //MOVEFLAG_RIGHT = 4
            //MOVEFLAG_LEFT = 8

            // moveflag mapping:
            // set 0x8 -> Will make the game use PI*2 (if not set -> PI/2)
            // 
            // Flags
            bw.Write((byte)_movement.Flag); // multiple values -> 3:1:1
            // ViewX
            if (_movement.ViewX <= 0x7F)
            {
                bw.Write((byte)_movement.ViewX);
            }
            else if (_movement.ViewX <= 0x3FFF)
            {
                bw.Write((byte)(_movement.ViewX & 0x7F) | 0x80);
                bw.Write((byte)_movement.ViewX >> 7);
            }
            else
            {
                bw.Write((byte)(_movement.ViewX & 0x7F) | 0x80);
                bw.Write((byte)((_movement.ViewX >> 7) & 0x7F) | 0x80);
                bw.Write((byte)_movement.ViewX >> 14);
            }
            // ViewY
            if (_movement.ViewY <= 0x7F)
            {
                bw.Write((byte)_movement.ViewY);
            }
            else if (_movement.ViewY <= 0x3FFF)
            {
                bw.Write((byte)(_movement.ViewY & 0x7F) | 0x80);
                bw.Write((byte)_movement.ViewY >> 7);
            }
            else
            {
                bw.Write((byte)(_movement.ViewY & 0x7F) | 0x80);
                bw.Write((byte)((_movement.ViewY >> 7) & 0x7F) | 0x80);
                bw.Write((byte)_movement.ViewY >> 14);
            }
            // post-checksum
            bw.Write((byte)((x & 0xFF) ^ ((x >> 8) & 0xFF) ^ ((x >> 16) & 0xFF) ^ ((x >> 24) & 0xFF)));
            // Alignment
            var lenNow = (int)bw.BaseStream.Position - lenBegin;
            var paddingNeeded = (8 - (lenNow + 1) % 8) % 8;
            for (var i = 0; i < paddingNeeded; ++i)
                bw.Write((byte)0x3F);

            var currentPos = (int)bw.BaseStream.Position;

            bw.BaseStream.Position = lenBegin;

            bw.Write((ushort)(currentPos - lenBegin));

            bw.BaseStream.Position = currentPos;
        }
    }
}
