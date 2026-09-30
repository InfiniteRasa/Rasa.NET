namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Usable.Recv_BlockInfo(actorId, doesBlock), called on a force field: whether it blocks that
    /// actor. ForceFieldBase acts on it only for the client's own manifestation - its collision,
    /// whether it can be picked and whether it can be targeted - and a field blocks until it is
    /// told otherwise. Any other usable takes it and does nothing.
    /// </summary>
    public class BlockInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BlockInfo;

        public ulong ActorId { get; set; }
        public bool DoesBlock { get; set; }

        public BlockInfoPacket(ulong actorId, bool doesBlock)
        {
            ActorId = actorId;
            DoesBlock = doesBlock;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteULong(ActorId);
            pw.WriteBool(DoesBlock);
        }
    }
}
