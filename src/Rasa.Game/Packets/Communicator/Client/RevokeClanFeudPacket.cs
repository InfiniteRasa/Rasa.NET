namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    /// <summary>RevokeClanFeud (673), communicator.RevokeWargameFeud (/revoke_feud): <c>(clanName,)</c>, the clan challenged.</summary>
    public class RevokeClanFeudPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RevokeClanFeud;

        public string ClanName { get; set; } = "";

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanName = ChallengeClanToFeudPacket.ReadText(pr);
        }
    }
}
