namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ActionFailed (13), on an actor: its action did not happen - cancel it.
    ///
    /// client/augmentations/actor.py Recv_ActionFailed(actionId, actionArgId) cancels the actor's
    /// current action when it is that action and arg (_CancelCurrentAction: the action's
    /// animation, FX and windup state stopped, a charging weapon's charge dropped). Nothing else:
    /// no message, and the request is left on the client's __unresolvedActions, so it is no
    /// substitute for <see cref="UserActionFailedPacket"/> - a refusal of a player's own request
    /// needs both, as ActorManager.RefuseRequest sends them.
    ///
    /// Unlike ActionInterrupt it does not look at the action's noInterrupt (crab mine
    /// self-destruct, seeker detonation, the death and birth abilities, instant kill, GM stealth,
    /// invulnerability ignore an interrupt), does not reach back to the last action, and posts no
    /// UI event - ActionInterrupt floats "Interrupted" (PM_COMBAT_INTERRUPT) over the player's own
    /// head.
    /// </summary>
    public class ActionFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActionFailed;

        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }

        public ActionFailedPacket(ActionId actionId, uint actionArgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);
        }
    }
}
