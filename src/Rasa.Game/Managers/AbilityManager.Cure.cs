using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Cure (abilities.cure), the Biotechnician's cleanse, by the pumps' own tooltips:
    ///
    ///  - P1 Cleanse: "Target: Single Friendly ... Removes Debuff Effects".
    ///  - P2 Group Cleanse: "Target: None (Self, Squad), Radius: RADIUS_AROUND_SOURCE m ...
    ///    Removes Debuff Effects".
    ///  - P3 Resuscitate and P5 Group Resuscitate: bring a dead one back with ATTRIBUTE_MAX_CHANGE
    ///    (50 %) of their health. A dead player - the one targeted, or the squad around - is
    ///    offered the revive and comes back on accepting it (PlayerDeath.OfferRevive); creatures
    ///    are not revived. The client is told in the same shape either way: its
    ///    CureAction.DoAbility unpacks the recovery's hit data as (reviveList, effectList), so both
    ///    lists are sent even when both are empty.
    ///  - P4 Protect: "Removes Debuff Effects. Protect from Debuffs for DURATION s" -
    ///    CURE_DEBUFF_GUARD 181, which keeps every debuff off them while it lasts
    ///    (GameEffectManager.Attach refuses one) and may be right-clicked away (allowDetach).
    ///
    /// What counts as a debuff is what the client's tray counts: an effect that is not a buff. A
    /// skill's own standing effect is left alone - it is not on the tray and losing it would take
    /// the player's weapon skills with it. So is the price of a death, Rez Trauma and the
    /// no-healing after it (PlayerDeath.IsDeathPenalty): a cleanse is not a way out of it, as a
    /// map link and a relog are not. The game has its own item for Rez Trauma, the Rez Trauma Kit
    /// (CONSUMABLE_RES_TRAUMA_KIT), which asks for the sickness to be on its user.
    /// </summary>
    public partial class AbilityManager
    {
        private const int CureDebuffGuardTypeId = 181;      // CURE_DEBUFF_GUARD

        /// <summary>
        /// Every debuff on an actor that Cure takes off: what the tray shows in red, but for
        /// the world's own (GameEffect.Environmental) - burning in lava ends by leaving it - and
        /// a death's penalties (PlayerDeath.IsDeathPenalty), which run out on their own.
        /// </summary>
        public static List<GameEffect> DebuffsOn(Actor actor)
        {
            return actor.ActiveEffects.Values
                .Where(e => !e.IsBuff && !e.IsSkillPassive && !e.Environmental && !PlayerDeath.IsDeathPenalty(e.TypeId))
                .ToList();
        }

        /// <summary>Whether this pump of Cure brings back the dead: P3 Resuscitate and P5 Group Resuscitate (CureAction.canTargetDead).</summary>
        public static bool Resuscitates(uint level) => level == 3 || level == 5;

        /// <summary>
        /// Offers each dead player it reaches - the one targeted (P3), or the squad within
        /// RADIUS_AROUND_SOURCE (P5) - a revive with ATTRIBUTE_MAX_CHANGE percent of their health.
        /// </summary>
        private static void Resuscitate(MapChannel mapChannel, Manifestation player, ActionData action, ActionLevelInfo info, int radius, AbilityRecoveryPacket recovery)
        {
            var percent = Math.Clamp(info.Get(AbilityProperty.AttributeMaxChange, 50), 1, 100);
            var dead = new List<Manifestation>();

            if (radius > 0)
                dead.AddRange(DeadSquadWithin(mapChannel, player, radius));
            else if (action.TargetId != 0 && ResolveTarget(mapChannel, action.TargetId) is Manifestation target && target.State == CharacterState.Dead)
                dead.Add(target);

            foreach (var fallen in dead)
            {
                var max = fallen.Attributes.TryGetValue(Attributes.Health, out var health) ? health.CurrentMax : 1;

                if (PlayerDeath.OfferRevive(mapChannel, player, fallen, Math.Max(1, max * percent / 100)))
                    Hit(recovery, fallen);
            }
        }

        /// <summary>The dead of the performer's squad within radius metres of them.</summary>
        internal static List<Manifestation> DeadSquadWithin(MapChannel mapChannel, Manifestation player, float radius)
        {
            var found = new List<Manifestation>();

            if (radius <= 0 || player.PartyId == 0)
                return found;

            foreach (var cell in CellManager.CellsIn(mapChannel, player.Cells))
                foreach (var client in cell.ClientList)
                {
                    var other = client?.Player;

                    if (other != null && other != player && !found.Contains(other) && other.PartyId == player.PartyId
                        && other.State == CharacterState.Dead && Vector3.Distance(player.Position, other.Position) <= radius)
                        found.Add(other);
                }

            return found;
        }

        private AbilityRecoveryPacket Cure(MapChannel mapChannel, Manifestation player, ActionData action, ActionLevelInfo info)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.CureLists);
            var radius = info.Get(AbilityProperty.RadiusAroundSource);

            // Resuscitate and Group Resuscitate: the dead are offered a revive (PlayerDeath).
            if (Resuscitates(info.Level))
                Resuscitate(mapChannel, player, action, info, radius, recovery);

            // The squad pumps reach the performer and their squad around them; the rest one
            // friendly target, which is the performer when they targeted nobody.
            var targets = radius > 0
                ? SquadWithin(mapChannel, player, radius)
                : new List<Manifestation>();

            if (radius <= 0)
            {
                var single = FriendlyTarget(mapChannel, player, action);

                if (single != null)
                    targets.Add(single);
            }

            var guardMs = info.Get(AbilityProperty.Duration) * 1000;

            foreach (var target in targets)
            {
                foreach (var debuff in DebuffsOn(target))
                    GameEffectManager.Instance.DettachEffect(mapChannel, target, debuff);

                Hit(recovery, target);

                if (guardMs <= 0)
                    continue;

                var guard = NewEffect(mapChannel, player, info, CureDebuffGuardTypeId, null);

                guard.IsBuff = true;
                guard.AllowDetach = true;
                guard.BlocksDebuffs = true;
                guard.ExpiresTick = Environment.TickCount64 + guardMs;

                GameEffectManager.Instance.Attach(mapChannel, target, guard);

                // The client announces the guard itself, from the recovery's effect list.
                recovery.EffectIds.Add(target.EntityId);
            }

            return recovery;
        }
    }
}
