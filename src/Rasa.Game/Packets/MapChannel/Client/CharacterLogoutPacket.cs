namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class CharacterLogoutPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CharacterLogout;

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
