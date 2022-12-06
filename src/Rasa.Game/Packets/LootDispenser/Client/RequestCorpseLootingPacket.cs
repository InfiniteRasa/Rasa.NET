namespace Rasa.Packets.LootDispenser.Client
{
    using Data;
    using Memory;

    public class RequestCorpseLootingPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestCorpseLooting;

        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
        }
    }
}
