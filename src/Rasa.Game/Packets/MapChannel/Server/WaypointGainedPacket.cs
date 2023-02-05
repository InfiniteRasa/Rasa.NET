namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class WaypointGainedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WaypointGained;

        public uint WaypointId { get; set; }
        public WaypointType WaypointType { get; set; }

        public WaypointGainedPacket(uint waypointId, WaypointType waypointType)
        {
            WaypointId = waypointId;
            WaypointType = waypointType;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WaypointId);
            pw.WriteInt((int)WaypointType);
        }
    }
}
