using System.Numerics;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class TeleportPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Teleport;

        public Vector3 Position { get; set; }           // position
        public float Yaw { get; set; }                  // yaw
        public TeleportType TeleportType { get; set; }  // teleportType
        public uint Delay { get; set; }                 // delay
        public bool DoCancel { get; set; }              // doCancel

        public TeleportPacket(Vector3 position, float yaw, TeleportType teleportType, uint delay)
        {
            Position = position;
            Yaw = yaw;
            TeleportType = teleportType;
            Delay = delay;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteTuple(3);
                pw.WriteDouble(Position.X);
                pw.WriteDouble(Position.Y);
                pw.WriteDouble(Position.Z);
            pw.WriteDouble(Yaw);
            pw.WriteUInt((uint)TeleportType);
            pw.WriteUInt(Delay);
            pw.WriteNoneStruct();
        }
    }
}
