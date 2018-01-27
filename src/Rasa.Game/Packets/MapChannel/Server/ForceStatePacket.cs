namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ForceStatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ForceState;
        
        public int State { get; set; }
        public int WindupTimeMs { get; set; }

        public ForceStatePacket(int state)
        {
            State = state;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(State);
            pw.WriteInt(1000);     // ToDo WindupTimeMs
        }
    }
}
