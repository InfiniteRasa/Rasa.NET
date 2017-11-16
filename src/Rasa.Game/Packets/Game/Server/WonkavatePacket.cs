namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;
    using Structures;
    public class WonkavatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Wonkavate;

        public int GameContextId { get; set; }
        public int MapInstanceId { get; set; }
        public int MapVersion { get; set; }
        public Position Position { get; set; }
        public double Rotation { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteInt(GameContextId);
            pw.WriteInt(MapInstanceId);
            pw.WriteInt(MapVersion);
            pw.WriteTuple(3);
            pw.WriteDouble(Position.PosX);
            pw.WriteDouble(Position.PosZ);
            pw.WriteDouble(Position.PosY);
            pw.WriteDouble(Rotation);
        }
    }
}
