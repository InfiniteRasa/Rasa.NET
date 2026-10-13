using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;

    /// <summary>
    /// A Maw's bite can leave a player held in its vines.
    ///
    /// The client has the vines but nothing that puts them on: MawVinesEffect (MAW_VINES 282,
    /// "Game effect that entangles a target in a Maw's vines") blocks its target's movement from
    /// its announce until it comes off, and its FX at level 1, EFFECT_CREATURE_MAW_VINES
    /// (creature_foreas_maw_ability_vines_resolve, with the vines' resolve and loop sounds), grow
    /// them round whoever has it. There is no action, ability data or tooltip text for it, and the
    /// Maw-side MAW_VINES_SOURCE (281) has no client class in what we have. So when and how long
    /// are ours: <see cref="ChancePercent"/> of the Maw bites (CR_MAW_MELEE) that land on a player
    /// hold them for <see cref="DurationMs"/>.
    ///
    /// A root (PlayerCrowdControl.Root): the server holds them from the attach, and they can
    /// still shoot. A bite on a player already in the vines does not hold them longer; one that
    /// lands as soon as they are free can hold them again.
    /// </summary>
    public static class MawVines
    {
        /// <summary>MAW_VINES.</summary>
        public const int TypeId = 282;

        /// <summary>The share of the bites that land which hold. Ours.</summary>
        public const int ChancePercent = 25;

        /// <summary>How long they hold. Ours.</summary>
        public const int DurationMs = 4000;

        /// <summary>The roll, 0-99, against <see cref="ChancePercent"/>: a test sets it.</summary>
        internal static Func<int> Roll = () => Random.Shared.Next(100);

        /// <summary>Whether a player is in a Maw's vines.</summary>
        public static bool Holds(Actor actor) =>
            actor != null && actor.ActiveEffects.Values.Any(e => e.TypeId == TypeId && !e.IsExpired);

        /// <summary>
        /// A creature's attack has landed on a player: if it is a Maw's bite, the vines may take
        /// them. Whether they did.
        /// </summary>
        public static bool OnHit(MapChannel mapChannel, Creature attacker, Manifestation player, ActionId actionId)
        {
            if (mapChannel == null || attacker == null || player == null || actionId != ActionId.CrMawMelee)
                return false;

            if (Holds(player) || Roll() >= ChancePercent)
                return false;

            return PlayerCrowdControl.Root(mapChannel, player, attacker, TypeId, DurationMs);
        }
    }
}
