namespace Rasa.Packets.Social.Client
{
    using Data;
    using Memory;

    public class AddFriendByNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AddFriendByName;

        public string FamilyName { get; set; }
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FamilyName = pr.ReadUnicodeString();
        }
    }
}
