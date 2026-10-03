namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// ReturnToWormhole (686): clientmethod.OnReturnToWormhole(wormholeId), sent by the waypoint
    /// window when a "Temp Wormhole" row is picked. The id is the one EnteredWaypoint listed the
    /// row under, which the window has kept as text and turned back into a number: an int or a
    /// long, as it fits.
    /// </summary>
    public class ReturnToWormholePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ReturnToWormhole;

        public ulong WormholeId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            WormholeId = pr.PeekType() == PythonType.Long ? pr.ReadULong() : pr.ReadUInt();
        }
    }
}
