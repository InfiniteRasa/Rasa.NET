using System.Linq;
using System.Numerics;
using Rasa.Services.Preloader;
using Rasa.Structures;

namespace Rasa.Managers
{
    /// <summary>Landing Zone's breach order: outside defenders, force field, remaining garrison, boss claim.</summary>
    internal static class LandingZoneBreach
    {
        internal static bool IsFrontline(SpawnPool pool) => pool != null && LandingZoneBreachContent.FrontlinePools.Contains(pool.DbId);
        internal static bool FrontlineDown(MapChannel map)
        {
            if (map?.MapInfo?.MapContextId != LandingZoneGarrisonContent.Map || map.IsCopy) return false;
            return LandingZoneBreachContent.FrontlinePools.All(id =>
            {
                var pool = map.SpawnPools.FirstOrDefault(p => p.DbId == id);
                return pool != null && pool.HasSpawned && !pool.Suspended && pool.AliveCreatures == 0 && pool.QueuedCreatures == 0 && pool.DropshipQueue == 0;
            });
        }
        internal static bool AllowsHit(ForceFields.Field field, Creature attacker)
        {
            if (attacker?.SpawnPool?.IsControlPointAssault != true) return true;
            var map = attacker.RuntimeMapChannel ?? attacker.SpawnPool.RuntimeMapChannel;
            var point = ControlPoints.Instance.ById(LandingZoneGarrisonContent.Point);
            return point?.HeldByAfs == true && LandingZoneAssaults.ControlsCapture(map, point) &&
                Relevant(field, map, point) && FrontlineDown(map);
        }
        private static bool Relevant(ForceFields.Field field, MapChannel map, ControlPoints.Point point) =>
            field != null && field.MapChannel == map && field.Side == ForceFields.Side.A && !field.IsDestroyed && !field.Sealed &&
            Vector3.DistanceSquared(field.Position, point.Position) <= 10000;
        internal static ForceFields.Field StandingField(MapChannel map, ControlPoints.Point point) =>
            ForceFields.OnMap(map).Where(f => Relevant(f, map, point))
                .OrderBy(f => Vector3.DistanceSquared(f.Position, point.Position)).FirstOrDefault();

        internal static bool Order(MapChannel map, ControlPoints.Point point, Creature attacker, Creature[] guards)
        {
            var priority = guards.Where(c => IsFrontline(c.SpawnPool) || c.SpawnPool?.IsControlPointReinforcement == true).ToArray();
            var field = FrontlineDown(map) ? StandingField(map, point) : null;
            var fighting = attacker.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting;
            var current = attacker.Controller.ActionFighting.TargetEntityId;
            // Players can defend the outpost and draw attackers into a fight.
            if (fighting && EntityManager.Instance.Players.ContainsKey(current)) return true;
            if (priority.Length > 0)
            {
                if (fighting && priority.Any(c => c.EntityId == current)) return true;
                var target = priority.OrderBy(c => Vector3.DistanceSquared(c.Position, attacker.Position)).First();
                attacker.Hate.Clear();
                attacker.HomePos.Position = point.Position;
                return BehaviorManager.Instance.TrySetActionFighting(attacker, target.EntityId);
            }
            if (field != null)
            {
                if (fighting && current == field.Object.EntityId) return true;
                attacker.Hate.Clear();
                attacker.HomePos.Position = point.Position;
                return BehaviorManager.Instance.TrySetActionFighting(attacker, field.Object.EntityId);
            }
            if (fighting && ForceFields.Find(current) != null)
                BehaviorManager.Instance.GiveUp(attacker);
            return false;
        }
    }
}
