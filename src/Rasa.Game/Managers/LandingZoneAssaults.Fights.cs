using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Structures;

namespace Rasa.Managers
{
    internal static partial class LandingZoneAssaults
    {
        /// <summary>
        /// How long a planned point's assault troop or AFS sortie trooper fights one target without
        /// its health going down before it gives the target up (<see cref="FightWatch"/>; ESTIMATE).
        /// Live run 2026-11-05: Bane stood "fighting" at full health 22-58 m from Retread Outpost and
        /// Bane Assertion Camp for 3-4 minutes and never reached the point.
        /// </summary>
        internal const long NoHarmMs = 20000;

        /// <summary>How much nearer its target a troop must have come since the closest it was for its walk to count as progress (<see cref="FightWatch"/>).</summary>
        internal const float Closing = 1f;

        /// <summary>How long a target given up as out of reach is left alone by the troop that gave it up (ESTIMATE).</summary>
        internal const long ShunMs = 60000;

        /// <summary>
        /// What each troop of an assault or a sortie fights, and whether it gets anywhere with it:
        /// a fight whose target has not lost health for <see cref="NoHarmMs"/> - out of reach, out
        /// of sight, or behind something the troop cannot shoot through - is given up, and that
        /// target is left alone for <see cref="ShunMs"/>, so the troop walks on to the point or to
        /// the next of the garrison. A troop still coming nearer its target (by <see cref="Closing"/>
        /// since the closest it was) is getting somewhere: its walk from a drop 45-100 m out does not count.
        /// </summary>
        internal sealed class FightWatch
        {
            private readonly Dictionary<ulong, (ulong Target, float Health, float Closest, long Since)> _fights = new();
            private readonly Dictionary<ulong, (ulong Target, long Until)> _shunned = new();

            /// <summary>Whether the troop's fight has stalled (and is to be given up); its target is shunned from now.</summary>
            internal bool Stalled(Creature troop, long now)
            {
                if (troop.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                {
                    _fights.Remove(troop.EntityId);
                    return false;
                }
                var target = troop.Controller.ActionFighting.TargetEntityId;
                var actor = EntityManager.Instance.GetActor(target);
                if (actor == null || !actor.Attributes.TryGetValue(Attributes.Health, out var health))
                {
                    _fights.Remove(troop.EntityId);
                    return false;
                }
                var distance = System.Numerics.Vector3.Distance(troop.Position, actor.Position);
                if (!_fights.TryGetValue(troop.EntityId, out var fight) || fight.Target != target || health.Current < fight.Health || distance <= fight.Closest - Closing)
                {
                    _fights[troop.EntityId] = (target, health.Current, distance, now);
                    return false;
                }
                if (now - fight.Since < NoHarmMs)
                    return false;
                _fights.Remove(troop.EntityId);
                _shunned[troop.EntityId] = (target, now + ShunMs);
                return true;
            }

            /// <summary>Whether the troop leaves this target alone for now (one it gave up as out of reach).</summary>
            internal bool Shuns(Creature troop, Creature target, long now) =>
                _shunned.TryGetValue(troop.EntityId, out var shunned) && shunned.Target == target.EntityId && now < shunned.Until;

            internal void Clear()
            {
                _fights.Clear();
                _shunned.Clear();
            }
        }

        /// <summary>
        /// Whether the troop fights a creature it has no business with: not one of <paramref name="allowed"/>
        /// (the point's garrison), not a player, and not one fighting the troop. A vendor or a
        /// mission's NPC near the point is not the assault's to take on (live run 2026-11-05: AFS
        /// sortie troopers stood 110 m from Bane Assertion Camp, "fighting" a quest NPC 44 m from it,
        /// that never lost health).
        /// </summary>
        internal static bool FightsOffPlan(Creature troop, IEnumerable<Creature> allowed)
        {
            if (troop.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                return false;
            var id = troop.Controller.ActionFighting.TargetEntityId;
            var target = EntityManager.Instance.GetCreature(id);
            // A target that is down - one of the garrison as it dies, a turret's wreck on its mount -
            // is no NPC standing by (FightsTheDown).
            if (target == null || allowed.Any(c => c.EntityId == id) || !CreatureManager.IsLivingOnMap(troop.RuntimeMapChannel ?? target.RuntimeMapChannel, target))
                return false;
            return !(target.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting && target.Controller.ActionFighting.TargetEntityId == troop.EntityId);
        }
        /// <summary>
        /// Whether the troop is still set on a creature that is down: one of the garrison it has
        /// just killed, or a turret's wreck kept on its mount (AlternateMesh). It is taken off it at
        /// once, without a line in the log: live run 2026-11-05, Retread Outpost, every Bane on the
        /// AFS turret as it fell was logged as giving up a fight with "not the garrison".
        /// </summary>
        internal static bool FightsTheDown(MapChannel map, Creature troop)
        {
            if (troop.Controller.CurrentAction != BehaviorManager.BehaviorActionFighting)
                return false;
            var target = EntityManager.Instance.GetCreature(troop.Controller.ActionFighting.TargetEntityId);
            return target != null && !CreatureManager.IsLivingOnMap(map, target);
        }

        /// <summary>
        /// Takes off every creature of <paramref name="pools"/> still standing once ControlPoints.TakeOffLiving
        /// has done its part: TakeOffLiving finds the creatures in the map's cells, and one in none of them
        /// (an AFS relief soldier or sortie trooper of an assault or sortie that is over) stayed at the
        /// point, no defender of the next assault. Returns how many.
        /// </summary>
        internal static int TakeOffLeftovers(MapChannel map, IReadOnlyCollection<SpawnPool> pools)
        {
            if (map == null || pools.Count == 0) return 0;
            var left = EntityManager.Instance.Creatures.Values.Where(c => c.SpawnPool != null && pools.Contains(c.SpawnPool) && CreatureManager.IsLivingOnMap(map, c)).ToList();
            foreach (var creature in left)
            {
                creature.SpawnPool.AliveCreatures = System.Math.Max(0, creature.SpawnPool.AliveCreatures - 1);
                creature.SpawnPool.DeadCreatures++;
                MapReset.Remove(map, creature);
            }
            return left.Count;
        }
    }
}
