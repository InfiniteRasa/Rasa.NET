namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WorldLocationDescriptorPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WorldLocationDescriptor;

        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double RotationX { get; set; }
        public double RotationY { get; set; }
        public double RotationZ { get; set; }
        public double Unknwon { get; set; }     // camera poss?
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteTuple(3);
            pw.WriteDouble(PosX);
            pw.WriteDouble(PosZ);
            pw.WriteDouble(PosY);
            pw.WriteTuple(4);
            pw.WriteDouble(RotationX);
            pw.WriteDouble(RotationZ);
            pw.WriteDouble(RotationY);
            pw.WriteDouble(Unknwon);
        }
    }
}
