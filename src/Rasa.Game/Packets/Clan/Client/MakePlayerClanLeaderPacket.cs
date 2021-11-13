namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    public class MakePlayerClanLeaderPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.MakePlayerClanLeader;

        public uint CharacterId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            CharacterId = pr.ReadUInt();
        }
    }
}
