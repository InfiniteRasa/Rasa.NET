namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Manifestation.Recv_GraveyardGained(waypointId): "You just gained %(graveyard)s."
    /// (PM_GAINED_GRAVEYARD), the hospital named from waypointlanguage by its teleporter id.
    /// </summary>
    public class GraveyardGainedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GraveyardGained;

        public uint WaypointId { get; set; }

        public GraveyardGainedPacket(uint waypointId)
        {
            WaypointId = waypointId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(WaypointId);
        }
    }
}
