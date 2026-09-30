using System;
using System.Collections.Generic;
using System.Linq;

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
    ///    (50 %) of their health. Nothing can be resuscitated yet - players do not stay dead and
    ///    creatures are not revived - so these two perform and reach nobody. The client is told in
    ///    the same shape either way: its CureAction.DoAbility unpacks the recovery's hit data as
    ///    (reviveList, effectList), so both lists are sent even when both are empty.
    ///  - P4 Protect: "Removes Debuff Effects. Protect from Debuffs for DURATION s" -
    ///    CURE_DEBUFF_GUARD 181, which keeps every debuff off them while it lasts
    ///    (GameEffectManager.Attach refuses one) and may be right-clicked away (allowDetach).
    ///
    /// What counts as a debuff is what the client's tray counts: an effect that is not a buff. A
    /// skill's own standing effect is left alone - it is not on the tray and losing it would take
    /// the player's weapon skills with it.
    /// </summary>
    public partial class AbilityManager
    {
        private const int CureDebuffGuardTypeId = 181;      // CURE_DEBUFF_GUARD

        /// <summary>Every debuff on an actor: what Cure takes off, and what the tray shows in red.</summary>
        public static List<GameEffect> DebuffsOn(Actor actor)
        {
            return actor.ActiveEffects.Values.Where(e => !e.IsBuff && !e.IsSkillPassive).ToList();
        }

        private AbilityRecoveryPacket Cure(MapChannel mapChannel, Manifestation player, ActionData action, ActionLevelInfo info)
        {
            var recovery = new AbilityRecoveryPacket(action.ActionId, action.ActionArgId, AbilityRecoveryPacket.HitDataKind.CureLists);
            var radius = info.Get(AbilityProperty.RadiusAroundSource);

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
