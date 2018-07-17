namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActorKilledPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorKilled;
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
