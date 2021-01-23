namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestToggleRunPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestToggleRun;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
