namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class ClearTrackingTargetPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClearTrackingTarget;

        // 0 element's
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
