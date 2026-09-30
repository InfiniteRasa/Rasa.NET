namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// UseInterruptible (691), on a usable object: the actor it is locked to has started a timed
    /// use of it that can still be interrupted - a control point capture, a logos tablet being
    /// taken.
    ///
    /// client/augmentations/usable.py Recv_UseInterruptible(actorId, *args) does nothing unless
    /// the object is locked to that actor (<see cref="LockToActorPacket"/> first). Then it attaches
    /// the in-use effect, which runs from the object to the actor: the class's own
    /// OnGetInterruptiblePkgId, or usabledata.specialFX[(classId, state, state)]. The control
    /// point (3814) has one for each of its states, and so do nearly all the logos dispensers;
    /// footlockers have none. A control point turns its state effect off while it plays
    /// (SetInterruptibleSFXOverrlaps(False)).
    ///
    /// Only the clan control point (clancontrolpoint.py) reads an argument after the actor - the
    /// using clan, to pick its AFS / Bane / your clan / other clan effect. Nothing here is one, so
    /// only the actor is sent.
    /// </summary>
    public class UseInterruptiblePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UseInterruptible;

        public ulong ActorId { get; set; }

        public UseInterruptiblePacket(ulong actorId)
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
