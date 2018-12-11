using System.Numerics;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class WonkavatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Wonkavate;

        public uint MapContextId { get; set; }
        public uint MapInstanceId { get; set; }
        public uint MapVersion { get; set; }
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public WonkavatePacket(uint mapContextId, uint mapInstanceId, uint mapVersion, Vector3 position, Quaternion rotation)
        {
            MapContextId = mapContextId;
            MapInstanceId = mapInstanceId;
            MapVersion = mapVersion;
            Position = position;
            Rotation = rotation;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(MapContextId);
            pw.WriteUInt(MapInstanceId);
            pw.WriteUInt(MapVersion);
            pw.WriteTuple(3);
            pw.WriteDouble(Position.X);
            pw.WriteDouble(Position.Y);
            pw.WriteDouble(Position.Z);
            pw.WriteDouble(Rotation.X);
        }
    }
}
