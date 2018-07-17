namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestLogoutPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestLogout;
        
        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
