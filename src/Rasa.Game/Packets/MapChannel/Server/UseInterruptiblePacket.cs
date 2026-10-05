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
    /// using clan, to pick its AFS / Bane / your clan / other clan effect - and it must have it:
    /// its OnBeforeUseInterruptible(actorId, usingClanId) takes exactly the one. So an object of
    /// that kind is sent the clan (DynamicObjectManager.UseInterruptibleOf), and any other only
    /// the actor.
    /// </summary>
    public class UseInterruptiblePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UseInterruptible;

        public ulong ActorId { get; set; }

        /// <summary>The clan of the actor, for a clan control point; null for any other object, which is sent no such argument.</summary>
        public int? UsingClanId { get; set; }

        public UseInterruptiblePacket(ulong actorId, int? usingClanId = null)
        {
            ActorId = actorId;
            UsingClanId = usingClanId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(UsingClanId == null ? 1 : 2);
            pw.WriteULong(ActorId);

            if (UsingClanId != null)
                pw.WriteInt(UsingClanId.Value);
        }
    }
}
