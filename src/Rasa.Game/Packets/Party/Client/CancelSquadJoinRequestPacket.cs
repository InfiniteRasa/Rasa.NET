namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class CancelSquadJoinRequestPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CancelSquadJoinRequest;

        internal string FamilyName { get; set; }
        
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            // The leader's name as the requester's client shows it, "(AFK)" and all (AfkNames).
            FamilyName = AfkNames.Strip(pr.ReadUnicodeString());
        }
    }
}
