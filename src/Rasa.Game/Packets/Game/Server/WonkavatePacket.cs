namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    public class WonkavatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Wonkavate;

        public int MapContextId { get; set; }
        public int MapInstanceId { get; set; }
        public int MapVersion { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public double Rotation { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteInt(MapContextId);
            pw.WriteInt(MapInstanceId);
            pw.WriteInt(MapVersion);
            pw.WriteTuple(3);
            pw.WriteDouble(PosX);
            pw.WriteDouble(PosZ);
            pw.WriteDouble(PosY);
            pw.WriteDouble(Rotation);
        }
    }
}
