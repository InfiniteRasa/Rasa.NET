namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PostTeleportPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PostTeleport;

        // 0 elements
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
