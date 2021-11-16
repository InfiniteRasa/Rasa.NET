namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class WhoPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Who;
        
        public string FamilyName { get; set; }
        
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            FamilyName = pr.ReadUnicodeString();
        }
    }
}
 