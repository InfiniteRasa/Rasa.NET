namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// SetOwnerId (884): OwnableControlPoint.Recv_SetOwnerId(ownerId), on a control point of the
    /// OWNABLECONTROLPOINT kind (UsableControlPoint_OwnablePvP_ElohV01). The object's effect while
    /// it is held is picked by its owner - 1 the red one, 2 the blue (the team ids), 0 a third,
    /// and -1 none at all - and it thinks itself 1's until told.
    /// </summary>
    public class SetOwnerIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetOwnerId;

        public int OwnerId { get; }

        public SetOwnerIdPacket(int ownerId)
        {
            OwnerId = ownerId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(OwnerId);
        }
    }
}
