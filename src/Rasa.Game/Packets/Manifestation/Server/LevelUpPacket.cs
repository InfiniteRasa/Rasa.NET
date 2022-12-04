namespace Rasa.Packets.Manifestation.Server
{
    using Data;
    using Memory;

    public class LevelUpPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LevelUp;

        public byte Level { get; set; }

        public LevelUpPacket(byte level)
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
