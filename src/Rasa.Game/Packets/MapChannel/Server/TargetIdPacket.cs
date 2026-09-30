namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// Recv_TargetId(targetId) on an actor (actor.py): the actor's target, which SetTargetId
    /// stores and each kind of actor then aims with. A player's manifestation points its combat
    /// bone tracker at the target's DAMAGE1 point; a creature whose class has a bone tracker
    /// (the turrets, Stalkers, Striders, the Juggernaut) adds one and aims it the same way, and
    /// drops it on None. None, not 0, is no target: creature.py tests "is not None".
    /// </summary>
    public class TargetIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TargetId;

        public ulong TargetId { get; }

        public TargetIdPacket(ulong targetId)
        {
            TargetId = targetId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);

            if (TargetId == 0)
                pw.WriteNoneStruct();
            else
                pw.WriteULong(TargetId);
        }
    }
}
