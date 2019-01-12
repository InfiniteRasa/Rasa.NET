namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class BeginTeleportPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BeginTeleport;

        // 0 elements
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
