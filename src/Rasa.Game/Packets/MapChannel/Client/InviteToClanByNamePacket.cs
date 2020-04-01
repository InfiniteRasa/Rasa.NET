namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    class InviteToClanByNamePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.InviteToClanByName;

        public string FamilyName { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FamilyName = pr.ReadUnicodeString();
        }
    }
}
