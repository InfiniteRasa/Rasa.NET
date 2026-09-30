namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// LockToActor (474), on a usable object: which actor has it, or 0 for nobody.
    ///
    /// client/augmentations/usable.py Recv_LockToActor(actorId) stores the id and posts
    /// UI_ACTOR_LOCK_CHANGED. For any player but the one named, IsAlreadyInUse() is then true and
    /// the HUD's use prompt reads ID_HUD_USABLE_INUSE_TEXT instead of "Press to use"
    /// (client/ui/usablewindow.py UpdateUseText). That is all the lock does client-side - it does
    /// not stop the client asking to use the object; refusing a second user is the server's job.
    ///
    /// It is also what lets <see cref="UseInterruptiblePacket"/> through: the in-use effect is
    /// only attached for the actor the object is locked to. A lock of 0 removes that effect and
    /// puts the object's state effect back.
    /// </summary>
    public class LockToActorPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LockToActor;

        /// <summary>The actor using the object, or 0 to unlock it.</summary>
        public ulong ActorId { get; set; }

        public LockToActorPacket(ulong actorId)
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
