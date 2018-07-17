namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LevelPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Level;

        public int Level { get; set; }

        public LevelPacket(int level)
        {
            Level = level;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Level);
        }
    }
}
