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
            FamilyName = pr.ReadUnicodeString();
        }
    }
}
