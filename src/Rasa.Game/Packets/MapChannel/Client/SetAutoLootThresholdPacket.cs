namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SetAutoLootThresholdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetAutoLootThreshold;

        public int LootLevel { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            LootLevel = pr.ReadInt();
        }
    }
}
