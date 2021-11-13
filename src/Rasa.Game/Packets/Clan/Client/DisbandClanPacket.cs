namespace Rasa.Packets.Clan.Client
{
    using Data;
    using Memory;

    public class DisbandClanPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.DisbandClan;

        public uint ClanId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanId = pr.ReadUInt();
        }
    }
}
