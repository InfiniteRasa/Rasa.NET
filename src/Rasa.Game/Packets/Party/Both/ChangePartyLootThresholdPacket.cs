namespace Rasa.Packets.Party.Both
{
    using Data;
    using Memory;

    public class ChangePartyLootThresholdPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangePartyLootThreshold;

        internal PartyLootThreshold PartyLootThreshold { get; set; }

        public ChangePartyLootThresholdPacket()
        {
        }

        public ChangePartyLootThresholdPacket(PartyLootThreshold threshold)
        {
            PartyLootThreshold = threshold;
        }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            PartyLootThreshold = (PartyLootThreshold)pr.ReadUInt();
            Logger.WriteLog(LogType.AI, PartyLootThreshold);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt((uint)PartyLootThreshold);
        }
    }
}
