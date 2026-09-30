namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// UseInterrupted (609), on a usable object: the actor using it stopped before the use was
    /// done. client/augmentations/usable.py Recv_UseInterrupted(actorId) removes the in-use
    /// effect <see cref="UseInterruptiblePacket"/> attached; the lock itself is cleared with
    /// LockToActor(0).
    /// </summary>
    public class UseInterruptedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UseInterrupted;

        public ulong ActorId { get; set; }

        public UseInterruptedPacket(ulong actorId)
        {
            ActorId = actorId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(ActorId);
        }
    }
}
