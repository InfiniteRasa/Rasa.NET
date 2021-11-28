namespace Rasa.Packets.Party.Client
{
    using Data;
    using Memory;

    public class LeavePartyPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LeaveParty;

        // 0 elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
