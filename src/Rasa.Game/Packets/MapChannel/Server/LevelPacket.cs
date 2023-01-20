namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LevelPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Level;

        public uint Level { get; set; }

        public LevelPacket(uint level)
        {
            Level = level;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(Level);
        }
    }
}
