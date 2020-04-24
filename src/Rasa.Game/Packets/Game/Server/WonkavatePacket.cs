using System.Numerics;

namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class WonkavatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Wonkavate;

        public uint ContextId { get; set; }
        public uint InstanceId { get; set; }
        public uint Version { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }

        public WonkavatePacket(uint contextId, uint instanceId, uint version, Vector3 position, float rotation)
        {
            ContextId = contextId;
            InstanceId = instanceId;
            Version = version;
            Position = position;
            Rotation = rotation;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(ContextId);
            pw.WriteUInt(InstanceId);
            pw.WriteUInt(Version);
            pw.WriteTuple(3);
            pw.WriteDouble(Position.X);
            pw.WriteDouble(Position.Y);
            pw.WriteDouble(Position.Z);
            pw.WriteDouble(Rotation);
        }
    }
}
