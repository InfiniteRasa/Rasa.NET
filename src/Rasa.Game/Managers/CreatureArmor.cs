using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Creature armour regeneration. A creature's armour comes from its creature_stat row (70 for
    /// most, 300-420 for bosses; 100 with no row), and hits already take it before health
    /// (MissileManager, ActorManager.Damage). What it never did was come back: the server did not
    /// regenerate creatures at all, and they were sent a refresh of 5 every 1000 seconds, so the
    /// clients - which predict a bar from the refresh amount and period they were last given
    /// (ActorAttribute._EvaluatePredictedRefresh) - showed none either.
    ///
    /// Now, like a player's (ActorManager.Regenerate, CombatRegen):
    /// - a living creature's armour regains RegenPercent of its maximum (at least 1) every
    ///   CombatRegen.RegenPeriodSeconds, or every CombatRegen.InCombatRegenPeriodSeconds while it
    ///   is fighting - one fifth the rate;
    /// - the effects on it change the amount through GameEffectManager.RegenAmount's
    ///   ArmorRegenPercent - Target Painting's EFFECT_ARMOR_REGEN_MODIFIER ("Armor Recharge:
    ///   100% / 0%") stops it outright;
    /// - whenever the rate or period changes the clients are sent the armour with the rate the
    ///   effects make (GameEffectManager.WithRegen), so their bars move as the server's does;
    ///   in between nothing is sent.
    /// The dead, and a creature in its Critical Death window, do not regenerate.
    ///
    /// RegenPercent is ours: nothing in the client gives creature armour a rate.
    /// </summary>
    public static class CreatureArmor
    {
        /// <summary>Percent of its maximum armour a creature regains each period. Not in the client.</summary>
        public const int RegenPercent = 5;

        /// <summary>A creature's base armour refresh: RegenPercent of its maximum, at least 1 (0 with no armour).</summary>
        public static int BaseRegen(int maxArmor)
        {
            return maxArmor <= 0 ? 0 : Math.Max(1, maxArmor * RegenPercent / 100);
        }

        /// <summary>Seconds between a creature's armour refreshes: five times as long while it fights.</summary>
        public static int PeriodFor(Creature creature)
        {
            return creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionFighting
                ? CombatRegen.InCombatRegenPeriodSeconds
                : CombatRegen.RegenPeriodSeconds;
        }

        private static bool Regenerates(Creature creature)
        {
            return creature.State != CharacterState.Dead && creature.State != CharacterState.Dying
                && creature.Attributes.TryGetValue(Attributes.Health, out var health) && health.Current > 0;
        }

        /// <summary>
        /// Brings the creature's armour rate and period up to date - the base rate from its
        /// maximum, the period from whether it is fighting - and tells the clients when either
        /// changed. Returns whether it changed.
        /// </summary>
        public static bool SyncRate(MapChannel mapChannel, Creature creature, ActorAttributes armor)
        {
            var amount = BaseRegen(armor.CurrentMax);
            var period = PeriodFor(creature);

            if (armor.RefreshAmount == amount && armor.RefreshPeriod == period)
                return false;

            armor.RefreshAmount = amount;
            armor.RefreshPeriod = period;

            if (mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateArmorPacket(GameEffectManager.WithRegen(creature, armor), creature.EntityId));

            return true;
        }

        /// <summary>One second of armour regeneration for every creature on the map.</summary>
        public static void Regenerate(MapChannel mapChannel)
        {
            // Over a reused copy of the cell table, and each cell's creatures by index: this used
            // to copy the table and then every cell's creature list, once a second per map.
            var cells = mapChannel.RegenCells;

            cells.Clear();
            cells.AddRange(mapChannel.MapCellInfo.Cells.Values);

            foreach (var cell in cells)
            {
                if (cell == null)
                    continue;

                for (var i = 0; i < cell.CreatureList.Count; i++)
                {
                    var creature = cell.CreatureList[i];
                    if (!Regenerates(creature) || !creature.Attributes.TryGetValue(Attributes.Armor, out var armor))
                        continue;

                    SyncRate(mapChannel, creature, armor);

                    creature.RegenSeconds++;
                    Tick(creature, armor, creature.RegenSeconds);
                }
            }
        }

        /// <summary>One second's regeneration: the rate the effects make, on the seconds the period falls on, up to the maximum.</summary>
        public static void Tick(Creature creature, ActorAttributes armor, long second)
        {
            var amount = GameEffectManager.RegenAmount(creature, armor);

            if (amount <= 0 || armor.Current >= armor.CurrentMax)
                return;

            if (second % Math.Max(1, armor.RefreshPeriod) != 0)
                return;

            armor.Current = Math.Min(armor.CurrentMax, armor.Current + amount);
        }
    }
}
