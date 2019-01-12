namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ForceStatePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ForceState;
        
        public uint State { get; set; }
        public int WindupTimeMs { get; set; }

        public ForceStatePacket(uint state, int windupTimeMs)
        {
            State = state;
            WindupTimeMs = windupTimeMs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(State);
            pw.WriteInt(WindupTimeMs);
        }
    }
}
