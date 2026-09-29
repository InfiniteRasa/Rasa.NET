namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/augmentations/actor.py:1356 - Recv_UserActionFailed(actionId, actionArgId, msgId).
    ///
    /// The server's refusal of an action this player asked for, addressed to the player's own
    /// actor. Client-side it:
    ///
    ///  - shows the player message, when one is given (msgId may be None);
    ///  - if the action is still the current one, posts UI_INTERRUPTIBLE_CANCELLED (which clears
    ///    the usable progress bar) and stops auto-fire - it does not cancel the action itself;
    ///  - pops the request off <c>__unresolvedActions</c>.
    ///
    /// The client pushes every request onto that list when it sends it, and only a recovery or
    /// this takes it off. One left there is reused as the action object the next time a windup or
    /// recovery for the same action and arg arrives with nothing current, and puts every later
    /// refusal of it one entry behind.
    ///
    /// The action itself goes on: most actions play their recovery locally once the windup has
    /// run (BaseActorAction.doLocalDoAction), so a refused ability still looked performed, and one
    /// that waits on the server - a reload - stayed wound up. Cancelling it is ActionFailed
    /// (<see cref="ActionFailedPacket"/>); a refusal sends both, through ActorManager.RefuseRequest.
    /// Sent alone, with no message, it only closes a request the client has already cancelled
    /// itself - one it interrupted (ActorManager.ResolveInterruptedRequest).
    /// </summary>
    public class UserActionFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UserActionFailed;

        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }

        /// <summary>The message to show, or null to fail the action silently.</summary>
        public PlayerMessage? MsgId { get; set; }

        internal UserActionFailedPacket(ActionId actionId, uint actionArgId, PlayerMessage? msgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            MsgId = msgId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);

            if (MsgId.HasValue)
                pw.WriteUInt((uint)MsgId.Value);
            else
                pw.WriteNoneStruct();
        }
    }
}
