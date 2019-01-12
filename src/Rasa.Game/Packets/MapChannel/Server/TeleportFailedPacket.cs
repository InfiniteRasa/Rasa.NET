namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class TeleportFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TeleportFailed;

        // 0 elements
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
