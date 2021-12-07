namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    public class RequestMovementBlockPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestMovementBlock;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
