namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class StopAutoFirePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StopAutoFire;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
