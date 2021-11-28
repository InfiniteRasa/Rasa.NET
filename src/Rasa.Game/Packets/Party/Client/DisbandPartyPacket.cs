namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class DisbandPartyPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisbandParty;

        // 0 elements

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
