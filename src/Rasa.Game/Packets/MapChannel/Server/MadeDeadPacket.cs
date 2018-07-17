namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class MadeDeadPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MadeDead;
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
