using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// A creature action that puts an effect on every player around the creature, with no hit:
    ///
    ///  - Shriek (HowlerShriekAbility, CR_HOWLER_SHRIEK 512 - "Hunter pet's AE shriek"):
    ///    HOWLER_SHRIEK_EFFECT 458 on every player within RADIUS_AROUND_SOURCE (15 m) for
    ///    DURATION (15 s). The class is an AddEffectAbility: its DoAbility announces, on each
    ///    entity the recovery lists, the gameeffectdata id at the same index of the hitdata, so
    ///    the recovery carries the type id once per hit. The effect's FX is keyed at level 7
    ///    (specialFX (458, 7)), which is the level it is put on at.
    ///
    ///    What the shriek lowers the client does not say: the argument has DEBUFF_AMOUNT_MIN/MAX
    ///    -25, and the effect has no tooltip and no class of its own. Ours: the Howler's own
    ///    damage type, Sonic, 25 points of resistance down - the shape of the Miasma's gas cloud
    ///    and the Atta pheromone, which lower one resistance by their DEBUFF_AMOUNT.
    ///
    /// Used only when it would do something: when a player within reach does not already carry
    /// it. Otherwise the fighting loop goes on to the next action.
    /// </summary>
    public static class CreatureDebuffs
    {
        public const ActionId HowlerShriek = (ActionId)512;

        public const int HowlerShriekTypeId = 458;      // HOWLER_SHRIEK_EFFECT
        public const int HowlerShriekFxLevel = 7;

        public static bool Is(CreatureAction action) => action != null && action.ActionId == HowlerShriek;

        /// <summary>Uses the action if it would do something now; whether it did.</summary>
        public static bool Perform(MapChannel mapChannel, Creature creature, CreatureAction action)
        {
            if (mapChannel == null || creature == null || action == null || AbilityManager.Instance == null
                || !AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info))
                return false;

            switch (action.ActionId)
            {
                case HowlerShriek:
                    return Shriek(mapChannel, creature, action, info);
                default:
                    return false;
            }
        }

        /// <summary>The players a shriek reaches that do not already carry one.</summary>
        public static List<Manifestation> Unshrieked(IEnumerable<Manifestation> players)
        {
            return players.Where(p => !p.ActiveEffects.Values.Any(e => e.TypeId == HowlerShriekTypeId && !e.IsExpired)).ToList();
        }

        /// <summary>The resistance a shriek takes away: DEBUFF_AMOUNT, whichever sign the data gives it.</summary>
        public static int AmountOf(ActionLevelInfo info) => Math.Abs(info?.Get(AbilityProperty.DebuffAmountMin) ?? 0);

        private static bool Shriek(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info)
        {
            var amount = AmountOf(info);
            var radius = info.Get(AbilityProperty.RadiusAroundSource, 15);
            var reached = Unshrieked(CreatureBombs.Caught(mapChannel, creature, creature.Position, radius));

            if (amount <= 0 || reached.Count == 0)
                return false;

            CellManager.Instance.CellCallMethod(mapChannel, creature,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, action.ActionArgId, creature.EntityId));

            // The shriek goes out when the windup is done, on whoever is about then (CreatureWindups).
            CreatureWindups.After(mapChannel, creature, CreatureWindups.WindupMsOf(action, info),
                () => Shrieked(mapChannel, creature, action, info, amount, radius), action);

            return true;
        }

        private static void Shrieked(MapChannel mapChannel, Creature creature, CreatureAction action, ActionLevelInfo info, int amount, int radius)
        {
            var reached = Unshrieked(CreatureBombs.Caught(mapChannel, creature, creature.Position, radius));
            var durationMs = Math.Max(1, info.Get(AbilityProperty.Duration, 15)) * 1000L;
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.TypeIds);

            foreach (var player in reached)
            {
                var shriek = new GameEffect
                {
                    TypeId = HowlerShriekTypeId,
                    EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                    EffectLevel = HowlerShriekFxLevel,
                    ActionId = info.ActionId,
                    SourceId = creature.EntityId,
                    Source = creature,
                    SourceLevel = (int)creature.Level,
                    IsBuff = false,
                    ExpiresTick = Environment.TickCount64 + durationMs,
                    AnnounceOnAttach = false,       // the recovery announces it
                    ResistDamageType = DamageType.Sonic,
                    ResistModifier = -amount
                };

                GameEffectManager.Instance.Attach(mapChannel, player, shriek);

                // Turned away (Cure's guard): not listed.
                if (!player.ActiveEffects.ContainsKey(shriek.EffectId))
                    continue;

                recovery.Hits.Add(new AbilityHit { EntityId = player.EntityId });
                recovery.TypeIds.Add(HowlerShriekTypeId);
            }

            CellManager.Instance.CellCallMethod(mapChannel, creature, recovery);
        }
    }
}
