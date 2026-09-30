namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_SetTrackingTarget(targetId) on an actor (actor.py): "notification of tracking target
    /// change from server", handed straight to the engine, gameclient_world.SetTrackingTarget(body,
    /// targetId). 0 is none - what the client itself sets when its tracking ends. The client's
    /// own request is SetTrackingTargetPacket, under the same method name.
    /// </summary>
    public class TrackingTargetPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetTrackingTarget;

        public ulong TargetId { get; }

        public TrackingTargetPacket(ulong targetId)
        {
            TargetId = targetId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(TargetId);
        }
    }
}
