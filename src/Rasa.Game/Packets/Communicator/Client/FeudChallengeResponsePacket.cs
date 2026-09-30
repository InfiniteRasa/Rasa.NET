namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// FeudChallengeResponse (671), communicator.AcceptWargameFeud / DeclineWargameFeud:
    /// <c>(challengerClanName, accepted)</c> - the dialog's Accept and Cancel, /accept_feud and
    /// /decline_feud.
    /// </summary>
    public class FeudChallengeResponsePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.FeudChallengeResponse;

        public string ClanName { get; set; } = "";
        public bool AcceptChalange { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanName = ChallengeClanToFeudPacket.ReadText(pr);
            AcceptChalange = pr.ReadBool();
        }
    }
}
