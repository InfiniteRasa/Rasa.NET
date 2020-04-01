namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    class ClanDemotePlayerPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanDemotePlayer;

        public uint CharacterId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CharacterId = pr.ReadUInt();
        }
    }
}
