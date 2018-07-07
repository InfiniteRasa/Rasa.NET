namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ForceStatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ForceState;
        
        public ActorState State { get; set; }
        public int WindupTimeMs { get; set; }

        public ForceStatePacket(ActorState state)
        {
            State = state;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt((int)State);
            pw.WriteInt(1000);     // ToDo WindupTimeMs
        }
    }
}
