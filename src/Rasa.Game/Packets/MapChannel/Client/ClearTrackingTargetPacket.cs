namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ClearTrackingTargetPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClearTrackingTarget;

        // 0 element's
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
