using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;

    /// <summary>
    /// The chargers and the Adrenaline Booster, consumables used on oneself.
    ///
    /// Armor Charger (CONSUMABLE_RESTORE_KIT_ARMOR 467) and Power Charger
    /// (CONSUMABLE_RESTORE_KIT_POWER 486), abilities.restorekit, Class I to X. Each restores its
    /// ATTRIBUTE_ID (8 armour, 6 power) by HEAL_AMOUNT_MIN-MAX every INTERVAL second for
    /// DURATION seconds, as the medpack restores health: ARMOR_RESTORE_KIT (342) and
    /// POWER_RESTORE_KIT (343), whose client class, RestoreOverTime, adds each tick's amount to
    /// the bar itself (DelayedAttributeChange) and floats it. The server's bar is moved with it
    /// (ActorManager.Restore), and nothing else is sent for it. The item text gives the totals:
    /// "Recharges 500 Armor over 10 seconds" to 25000, "Restores 75 Power over 5 seconds" to 300.
    ///
    /// Adrenaline Booster (CONSUMABLE_ADRENALINE_BOOST 459), abilities.adrenalshot, Class I to X:
    /// "Instantly adds up to 100% Adrenaline. Cooldown time is 5 minutes. If you are Level 10 or
    /// higher, the amount of Adrenaline added is diminished per level" - level 10 for Class I, 5
    /// more each class after, "slightly diminished" at level 50 for Class IX, and Class X "Instantly
    /// adds 100% Adrenaline". The action level has NOMINAL_LEVEL 5 to 50 and LEVEL_PERCENT 10, so
    /// the full bar is given below NOMINAL_LEVEL + 5 and LEVEL_PERCENT less for each level from
    /// there (<see cref="AdrenalineBoostPercent"/>). Its EFFECT_DURATION_MS (20 s) is not used: the
    /// client's ADRENAL_SHOT_EFFECT (438) names a class, AdrenalShotEffect, that its adrenalshot
    /// module does not have.
    /// </summary>
    public partial class AbilityManager
    {
        public const string RestoreKitModule = "abilities.restorekit";
        public const string AdrenalineBoosterModule = "abilities.adrenalshot";

        private const int ArmorRestoreKitTypeId = 342;   // ARMOR_RESTORE_KIT
        private const int PowerRestoreKitTypeId = 343;   // POWER_RESTORE_KIT

        /// <summary>
        /// The Adrenaline Booster's share of the bar, in percent, for a player of this level: 100
        /// below NOMINAL_LEVEL + 5, then LEVEL_PERCENT less for each level from there, down to 0.
        /// </summary>
        public static int AdrenalineBoostPercent(int playerLevel, ActionLevelInfo info)
        {
            var nominal = info.Get(AbilityProperty.NominalLevel);
            var step = info.Get(AbilityProperty.LevelPercent, 10);
            var over = playerLevel - (nominal + 4);

            return over <= 0 ? 100 : Math.Max(0, 100 - step * over);
        }

        /// <summary>
        /// Why an Adrenaline Booster cannot be used now, or null. Full adrenaline is the client's
        /// own refusal (AdrenalShotAction.CheckAction); one that would add nothing - a player ten
        /// levels past what the class is for - is refused rather than spent for nothing.
        /// </summary>
        private static PlayerMessage? AdrenalineBoosterRefusal(Manifestation player, ActionInfo action, ActionLevelInfo info)
        {
            if (action.Module != AdrenalineBoosterModule)
                return null;

            if (!player.Attributes.TryGetValue(Attributes.Chi, out var chi) || chi.CurrentMax <= 0)
                return PlayerMessage.PmCannotPerformActionNow;

            if (chi.Current >= chi.CurrentMax)
                return PlayerMessage.PmCannotPerformActionFullAdrenaline;

            return info != null && AdrenalineBoostPercent(player.Level, info) <= 0
                ? PlayerMessage.PmCannotPerformActionNow
                : null;
        }

        /// <summary>The effect a charger puts on: its attribute restored each tick, as the medpack heals.</summary>
        private static void AttachRestoreKit(MapChannel mapChannel, Manifestation player, ActionLevelInfo info)
        {
            var attribute = (Attributes)info.Get(AbilityProperty.AttributeId);
            var typeId = attribute == Attributes.Power ? PowerRestoreKitTypeId : ArmorRestoreKitTypeId;

            var kit = NewEffect(mapChannel, player, info, typeId, info.Get(AbilityProperty.Duration, 10));
            kit.TickRestores = attribute == Attributes.Power ? Attributes.Power : Attributes.Armor;
            kit.TickHealMin = info.Get(AbilityProperty.HealAmountMin);
            kit.TickHealMax = Math.Max(kit.TickHealMin, info.Get(AbilityProperty.HealAmountMax, kit.TickHealMin));
            kit.TickIntervalMs = Math.Max(1, info.Get(AbilityProperty.Interval, 1)) * 1000;
            kit.NextTickTick = Environment.TickCount64;
            kit.AllowDetach = true;
            kit.Tooltip["healMin"] = kit.TickHealMin;
            kit.Tooltip["healMax"] = kit.TickHealMax;
            kit.Tooltip["interval"] = kit.TickIntervalMs / 1000;

            GameEffectManager.Instance.Attach(mapChannel, player, kit);
        }

        /// <summary>The Adrenaline Booster: its share of the bar at once, floated over it ("+N Adrenaline").</summary>
        private static void BoostAdrenaline(Client client, Manifestation player, ActionLevelInfo info)
        {
            if (!player.Attributes.TryGetValue(Attributes.Chi, out var chi))
                return;

            var amount = (int)Math.Round(chi.CurrentMax * AdrenalineBoostPercent(player.Level, info) / 100D);

            ManifestationManager.Instance.GainAdrenaline(client, amount);
        }
    }
}
