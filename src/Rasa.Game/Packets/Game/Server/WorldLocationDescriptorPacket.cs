﻿using System.Numerics;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class WorldLocationDescriptorPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WorldLocationDescriptor;

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public WorldLocationDescriptorPacket(Vector3 position, float rotation)
        {
            Position = position;
            Rotation = Quaternion.CreateFromYawPitchRoll(rotation, 0f, 0f);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteTuple(3);
            pw.WriteDouble(Position.X);
            pw.WriteDouble(Position.Y);
            pw.WriteDouble(Position.Z);
            pw.WriteTuple(4);
            pw.WriteDouble(Rotation.X);
            pw.WriteDouble(Rotation.Y);
            pw.WriteDouble(Rotation.Z);
            pw.WriteDouble(Rotation.W);
        }
    }
}
