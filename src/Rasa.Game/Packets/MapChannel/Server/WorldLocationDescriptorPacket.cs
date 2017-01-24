namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class WorldLocationDescriptorPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WorldLocationDescriptor;

        public Position Position { get; set; }
        public double RotationX { get; set; }
        public double RotationY { get; set; }
        public double RotationZ { get; set; }
        public double RotationW { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteTuple(3);
            pw.WriteDouble(Position.PosX);
            pw.WriteDouble(Position.PosZ);
            pw.WriteDouble(Position.PosY);
            pw.WriteTuple(4);
            pw.WriteDouble(RotationX);
            pw.WriteDouble(RotationZ);
            pw.WriteDouble(RotationY);
            pw.WriteDouble(RotationW);
        }
    }
}
