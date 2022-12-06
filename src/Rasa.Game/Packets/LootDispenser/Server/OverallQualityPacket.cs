namespace Rasa.Packets.LootDispenser.Server
{
    using Data;
    using Memory;

    public class OverallQualityPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.OverallQuality;

        public LootQuality OverallQuality { get; set; }

        public OverallQualityPacket(LootQuality overallQuality)
        {
            OverallQuality = overallQuality;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int)OverallQuality);
        }
    }
}
