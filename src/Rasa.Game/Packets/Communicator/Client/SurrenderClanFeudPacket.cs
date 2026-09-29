namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    /// <summary>SurrenderClanFeud (675), communicator.SurrenderWargameFeud (/surrender_feud, the Clan Warfare list's Surrender): <c>(clanName,)</c>, the clan being fought.</summary>
    public class SurrenderClanFeudPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SurrenderClanFeud;

        public string ClanName { get; set; } = "";

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanName = ChallengeClanToFeudPacket.ReadText(pr);
        }
    }
}
