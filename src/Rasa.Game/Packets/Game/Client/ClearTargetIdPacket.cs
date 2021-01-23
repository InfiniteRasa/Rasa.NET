namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class ClearTargetIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ClearTargetId;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
