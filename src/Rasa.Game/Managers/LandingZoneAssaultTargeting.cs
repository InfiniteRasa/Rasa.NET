using System;
using System.Linq;
using System.Numerics;
using Rasa.Structures;

namespace Rasa.Managers
{
    /// <summary>Firing positions for mobile assault troops; mounted defenders keep their authored locations.</summary>
    internal static class LandingZoneAssaultTargeting
    {
        internal static bool IsAssault(Creature creature) => creature?.SpawnPool?.IsControlPointAssault == true;

        internal static bool IsServiceTarget(Creature creature, Creature target)
        {
            if (!IsAssault(creature) || target?.SpawnPool == null) return false;
            var map = creature.RuntimeMapChannel ?? creature.SpawnPool.RuntimeMapChannel;
            return ControlPoints.Instance.ServicePointOf(map, target.SpawnPool) != null;
        }

        // Search only when the existing chase path is being rebuilt. Never bypass cover or teleport.
        internal static Vector3? FiringPosition(MapChannel map, Creature creature, Actor target)
        {
            if (!IsAssault(creature) || map?.NavMesh == null || target == null) return null;
            var attacks = creature.Actions.Where(a => a.MaxDamage > 0 && !CreatureSupport.Is(a) &&
                !CreatureHabits.Is(a) && !CreatureAttacks.IsMelee(a) && a.RangeMax > a.RangeMin).ToArray();
            if (attacks.Length == 0) return null;
            var max = Math.Min(60, attacks.Max(a => a.RangeMax) * .9);
            var candidates = new System.Collections.Generic.List<Vector3>();
            // Search both the attacker's ground layer and the target's elevated layer.
            foreach (var height in new[] { creature.Position.Y, target.Position.Y }.Distinct())
                for (var radius = 6.0; radius <= max; radius += 6)
                    for (var i = 0; i < 24; i++)
                    {
                        var angle = i * Math.PI / 12;
                        var probe = new Vector3(target.Position.X + (float)(Math.Cos(angle) * radius), height,
                            target.Position.Z + (float)(Math.Sin(angle) * radius));
                        var ground = map.NavMesh.Nearest(probe);
                        if (!ground.HasValue) continue;
                        var at = ground.Value;
                        var distance = Vector3.Distance(at, target.Position);
                        if (!attacks.Any(a => distance > a.RangeMin + .5 && distance < a.RangeMax - .5)) continue;
                        var eye = at + new Vector3(0, Cover.HeightOf(creature) * Cover.EyeFactor, 0);
                        if (Cover.Visible(map.Cover, eye, Cover.SamplePoints(target, at)) <= 0) continue;
                        if (!candidates.Contains(at)) candidates.Add(at);
                    }
            foreach (var at in candidates.OrderBy(p => Vector3.DistanceSquared(p, creature.Position)))
            {
                // A partial route to a disconnected roof is not a firing position.
                var path = NavMeshManager.FindPathBounded(map, creature.Position, at, out var complete);
                if (complete && path?.Count > 0) return at;
            }
            return null;
        }
    }
}
