namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SelectWaypointPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SelectWaypoint;

        public uint WaypointId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            pr.ReadNoneStruct();        // mapInstanceId    we wont use mapInstance
            WaypointId = pr.ReadUInt();
        }
    }
}
