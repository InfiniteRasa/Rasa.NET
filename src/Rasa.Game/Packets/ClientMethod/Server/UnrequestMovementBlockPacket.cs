namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    public class UnrequestMovementBlockPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UnrequestMovementBlock;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
