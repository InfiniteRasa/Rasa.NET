namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SelectWaypointPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SelectWaypoint;

        public uint WaypointId { get; set; }
        public uint MapInstanceId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            if (pr.PeekType() == PythonType.Int)
                MapInstanceId = pr.ReadUInt();
            else
                pr.ReadNoneStruct();

            WaypointId = pr.ReadUInt();
        }
    }
}
