namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class GetPvPClanMembershipStatusPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GetPvPClanMembershipStatus;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}