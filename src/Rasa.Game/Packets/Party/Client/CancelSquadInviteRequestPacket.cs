namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class CancelSquadInviteRequestPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CancelSquadInviteRequest;

        internal string FamilyName { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            // The invitee's name as the inviter's client shows it, "(AFK)" and all (AfkNames). This
            // cut five characters off a name containing "(AFK)", which was the English element
            // alone; the French client appends "(Absent)".
            FamilyName = AfkNames.Strip(pr.ReadUnicodeString());
        }
    }
}
