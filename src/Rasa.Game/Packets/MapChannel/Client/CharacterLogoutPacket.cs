namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class CharacterLogoutPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterLogout;

        // 0 Elements
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
