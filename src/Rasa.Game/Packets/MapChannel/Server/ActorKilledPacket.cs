namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActorKilledPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorKilled;

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}