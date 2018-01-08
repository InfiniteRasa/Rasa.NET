namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class MadeDeadPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MadeDead;

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}