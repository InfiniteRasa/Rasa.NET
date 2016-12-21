namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LevelPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Level;

        public int Level { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Level);
        }
    }
}
