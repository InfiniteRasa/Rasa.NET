namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class AcceptPartyInvitesChangedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AcceptPartyInvitesChanged;
        
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            pr.ReadTrueStruct();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
