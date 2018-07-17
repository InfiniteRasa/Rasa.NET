namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class DevRQSWindowPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DevRQSWindow;
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
