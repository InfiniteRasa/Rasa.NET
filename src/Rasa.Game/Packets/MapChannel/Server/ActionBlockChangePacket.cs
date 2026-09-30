namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// ActionBlockChange (765), on the player's own manifestation: an action blocked or unblocked.
    ///
    /// client/augmentations/actor.py Recv_ActionBlockChange(actionId, isBlocked) counts: True is
    /// BlockAction (one more block on the action), False is UnblockAction (one fewer, never below
    /// none), and IsActionBlocked is whether any are left. It then posts
    /// UI_SET_ACTION_BLOCK_CHANGED. A blocked action is refused before anything is sent -
    /// BaseActorAction.CheckAction, BaseActorAbility.CheckAction and WeaponReload.CheckAction all
    /// return PM_CANNOT_PERFORM_ACTION_NOW - and the ability drawer (abilitydrawerwindow.py,
    /// gameuiutil.py) draws it grey (Icon_Overlay_Grey over a WarningRed icon), apart from the red
    /// of one the player cannot afford. An ability's id is its action id: CreateAbilityAction
    /// makes CreateAction(actorId, abilityId, pumpLevel, ...).
    ///
    /// The counts live on the client's manifestation, which a map change recreates. Sent by
    /// ActionBlocks, which keeps the client at one block or none per action and sends them all
    /// again on map entry.
    /// </summary>
    public class ActionBlockChangePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActionBlockChange;

        public ActionId ActionId { get; set; }
        public bool IsBlocked { get; set; }

        public ActionBlockChangePacket(ActionId actionId, bool isBlocked)
        {
            ActionId = actionId;
            IsBlocked = isBlocked;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt((uint)ActionId);
            pw.WriteBool(IsBlocked);
        }
    }
}
