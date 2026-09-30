namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// ChallengeClanToFeud (669), communicator.ChallengeWargameFeud: <c>(clanName, doInvite)</c>.
    /// doInvite false is /feud &lt;clan&gt;, which only asks whether the challenge would be allowed
    /// (answered with ClanWargameTestSuccess); true is Send in the Declaration of War dialog.
    /// </summary>
    public class ChallengeClanToFeudPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChallengeClanToFeud;

        public string ClanName { get; set; } = "";
        public bool Invite { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClanName = ReadText(pr);
            Invite = pr.ReadBool();
        }

        /// <summary>A clan name as the client sends it: unicode from the dialog and the slash commands, but read leniently.</summary>
        internal static string ReadText(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.String:
                    return pr.ReadString() ?? "";

                case PythonType.UnicodeString:
                    return pr.ReadUnicodeString() ?? "";

                default:
                    pr.SkipValue();
                    return "";
            }
        }
    }
}
