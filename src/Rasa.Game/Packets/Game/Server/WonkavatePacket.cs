namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;

    public class WonkavatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Wonkavate;

        public uint MapContextId { get; set; }
        public uint MapInstanceId { get; set; }
        public uint MapVersion { get; set; }
        public Position Position { get; set; }
        public double Rotation { get; set; }

        public WonkavatePacket(uint mapContextId, uint mapInstanceId, uint mapVersion, Position position, double rotation)
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
            pw.WriteDouble(Position.PosX);
            pw.WriteDouble(Position.PosZ);
            pw.WriteDouble(Position.PosY);
            pw.WriteDouble(Rotation);
        }
    }
}
