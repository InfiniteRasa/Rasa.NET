namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class CancelLogoutRequestPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CancelLogoutRequest;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
