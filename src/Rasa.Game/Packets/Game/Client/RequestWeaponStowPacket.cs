namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestWeaponStowPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestWeaponStow;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
