namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ExitedWaypointPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ExitedWaypoint;
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
