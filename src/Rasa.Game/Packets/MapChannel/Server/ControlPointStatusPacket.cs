namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ControlPointStatusPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ControlPointStatus;

        public ControlPointStatus Status { get; set; }

        public ControlPointStatusPacket(ControlPointStatus status)
        {
            Status = status;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteStruct(Status);
        }
    }
}
