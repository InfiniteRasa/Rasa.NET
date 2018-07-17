namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class WorldLocationDescriptorPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WorldLocationDescriptor;

        public Position Position { get; set; }
        public Quaternion Rotation { get; set; }

        public WorldLocationDescriptorPacket(Position position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteTuple(3);
            pw.WriteDouble(Position.PosX);
            pw.WriteDouble(Position.PosZ);
            pw.WriteDouble(Position.PosY);
            pw.WriteTuple(4);
            pw.WriteDouble(Rotation.X);
            pw.WriteDouble(Rotation.Z);
            pw.WriteDouble(Rotation.Y);
            pw.WriteDouble(Rotation.W);
        }
    }
}
