namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class CharacterLogoutPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterLogout;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
