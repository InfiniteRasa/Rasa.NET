namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The Rez Trauma Kit (CONSUMABLE_RES_TRAUMA_KIT 427, abilities.reztraumakit), Class I to X
    /// (Consumable_Res_Sickness_Kit_01_05 .. _10_50): used on oneself, it takes one stack of Rez
    /// Trauma off (PlayerDeath.RemoveRezTraumaStack) - one death's -20% Body, Mind and Spirit -
    /// and the rest runs out when it would have. The kit is used up and the action's 60 s reuse
    /// starts, as for any consumable.
    ///
    /// The client asks only with Rez Trauma on (RezTraumaKitAction.CheckAction:
    /// PM_CANNOT_PERFORM_ACTION_NO_REZ_SICKNESS), and the server holds to that at the request and
    /// again when the kit lands, so a kit is not spent on a Rez Trauma that ran out during the
    /// windup. RezTraumaKitAction has no DoAbility of its own: the recovery carries no hit data.
    ///
    /// Each class's action level has ATTRIBUTE_MAX_CHANGE 10 to 100 and DEATH_PENALTY_REDUCED 300
    /// in the client's data; neither is read here.
    /// </summary>
    public partial class AbilityManager
    {
        public const string RezTraumaKitModule = "abilities.reztraumakit";

        /// <summary>Why a Rez Trauma Kit cannot be used now, or null: there is no Rez Trauma on the player.</summary>
        private static PlayerMessage? RezTraumaKitRefusal(Manifestation player, ActionInfo action) =>
            action.Module == RezTraumaKitModule && PlayerDeath.CurrentRezTrauma(player) == null
                ? PlayerMessage.PmCannotPerformActionNoRezSickness
                : null;

        private static void UseRezTraumaKit(MapChannel mapChannel, Manifestation player, ActionData action)
        {
            PlayerDeath.RemoveRezTraumaStack(mapChannel, player);
            CellManager.Instance.CellCallMethod(mapChannel, player, new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.None));
        }
    }
}
