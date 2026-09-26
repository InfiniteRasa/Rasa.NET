using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Game.Missions.World
{
    using Managers;
    using global::Rasa.Missions.Scenes;
    using Structures;

    internal sealed class SceneRouteController
    {
        private readonly Dictionary<(string Run, string Role), RouteRun> _routes = new();
        private readonly Func<MapChannel, ActorHandle, Creature> _resolve;
        private readonly Action<string, SceneObservation> _observe;
        internal SceneRouteController(Func<MapChannel, ActorHandle, Creature> resolve,
            Action<string, SceneObservation> observe)
        {
            _resolve = resolve; _observe = observe;
        }

        internal WorldEffectResult Start(MapChannel map, ActorHandle handle, Creature creature,
            RunRouteIntent intent, SceneRoute route)
        {
            var key = (handle.RunId, handle.Role);
            if (_routes.TryGetValue(key, out var existing) && existing.Handle == handle &&
                existing.Intent.OperationKey == intent.OperationKey)
                return WorldEffectResult.Running();
            if (!float.IsFinite(route.Speed) || route.Speed <= 0 || route.Points.Count == 0)
                return WorldEffectResult.Failed("Route has no valid points or speed.");
            if (map.NavMesh == null)
                return WorldEffectResult.Failed("Scripted routes require a loaded navmesh.");
            var previous = creature.Position;
            foreach (var point in route.Points.Skip(intent.StartWaypoint))
            {
                var destination = Position(point.Position);
                if (!CellManager.TryGetCellCoordinates(destination, out _, out _) || !double.IsFinite(point.Orientation))
                    return WorldEffectResult.Failed("Route contains a non-finite/out-of-world point.");
                if (map.NavMesh != null)
                {
                    var path = map.NavMesh.FindPath(previous, destination, out var complete);
                    if (!complete || path == null || path.Count == 0 || Vector3.Distance(path[^1], destination) > 0.5f ||
                        Math.Abs(path[^1].Y - destination.Y) >= 0.5f)
                        return WorldEffectResult.Failed($"Route {route.Key} has no complete grounded path to {destination}.");
                }
                previous = destination;
            }
            var run = new RouteRun(map, handle, intent, route, intent.StartWaypoint,
                existing?.PreviousSpeed ?? creature.RunSpeed, creature);
            creature.RunSpeed = route.Speed;
            if (!Move(run, creature))
            {
                creature.RunSpeed = run.PreviousSpeed;
                return WorldEffectResult.Failed($"Route {route.Key} could not start.");
            }
            _routes[key] = run;
            return WorldEffectResult.Running();
        }

        internal void Tick(MapChannel map, DateTime now)
        {
            foreach (var run in _routes.Values.Where(run => run.Map == map).ToArray())
            {
                if (!IsCurrent(run))
                    continue;
                var actor = _resolve(map, run.Handle);
                if (actor != null && actor.State != Data.CharacterState.Dead &&
                    run.Intent.ResumeAfterCombat && actor.Controller.CurrentAction != BehaviorManager.BehaviorActionScriptedMove)
                {
                    if (actor.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                        continue;
                    if (!Move(run, actor))
                    {
                        RestoreSpeed(run);
                        _observe(run.Handle.RunId, new SceneObservation(SceneEventKind.Cancelled,
                            run.Handle.Generation, "route-blocked", run.Handle.Role, run.Intent.OperationKey));
                        if (IsCurrent(run))
                            _routes.Remove((run.Handle.RunId, run.Handle.Role));
                    }
                    continue;
                }
                if (actor == null || actor.State == Data.CharacterState.Dead ||
                    !ReferenceEquals(actor.Controller.ScriptedMove, run.Move))
                {
                    RestoreSpeed(run);
                    _observe(run.Handle.RunId, new SceneObservation(SceneEventKind.ActorDied,
                        run.Handle.Generation, "route-lost-actor", run.Handle.Role, run.Intent.OperationKey));
                    if (IsCurrent(run))
                        _routes.Remove((run.Handle.RunId, run.Handle.Role));
                    continue;
                }
                if (!run.Move.Arrived)
                    continue;
                if (run.Index == run.Route.Points.Count)
                {
                    Complete(run, actor);
                    continue;
                }
                var point = run.Route.Points[run.Index];
                run.ResumeAt ??= now.AddMilliseconds(point.PauseMilliseconds);
                if (now < run.ResumeAt)
                    continue;
                _observe(run.Handle.RunId, new SceneObservation(SceneEventKind.WaypointReached,
                    run.Handle.Generation, Role: run.Handle.Role,
                    OperationKey: run.Intent.OperationKey, Waypoint: run.Index));
                if (!IsCurrent(run))
                    continue;
                run.Index++;
                run.ResumeAt = null;
                if (run.Index == run.Route.Points.Count)
                    Complete(run, actor);
                else if (!Move(run, actor))
                {
                    _routes.Remove((run.Handle.RunId, run.Handle.Role));
                    actor.RunSpeed = run.PreviousSpeed;
                    _observe(run.Handle.RunId, new SceneObservation(SceneEventKind.Cancelled,
                        run.Handle.Generation, "route-blocked", run.Handle.Role, run.Intent.OperationKey));
                }
            }
        }

        private void Complete(RouteRun run, Creature actor)
        {
            _observe(run.Handle.RunId, new SceneObservation(SceneEventKind.RouteCompleted,
                run.Handle.Generation, Role: run.Handle.Role, OperationKey: run.Intent.OperationKey,
                Position: new ScenePosition(actor.Position.X, actor.Position.Y, actor.Position.Z)));
            if (!IsCurrent(run))
                return;
            _routes.Remove((run.Handle.RunId, run.Handle.Role));
            actor.RunSpeed = run.PreviousSpeed;
        }

        private bool IsCurrent(RouteRun run) =>
            _routes.TryGetValue((run.Handle.RunId, run.Handle.Role), out var current) && ReferenceEquals(current, run);

        private static void RestoreSpeed(RouteRun run)
        {
            if (MapInstanceScope.Contains(run.Map, run.Actor) &&
                ReferenceEquals(run.Actor.Controller.ScriptedMove, run.Move))
                run.Actor.RunSpeed = run.PreviousSpeed;
        }

        internal void Cancel(string runId, string role = null, string operationKey = null, uint? generation = null)
        {
            foreach (var entry in _routes.Where(entry =>
                entry.Key.Run == runId && (role == null || entry.Key.Role == role) &&
                (operationKey == null || entry.Value.Intent.OperationKey == operationKey) &&
                (!generation.HasValue || entry.Value.Handle.Generation == generation.Value)).ToArray())
            {
                var actor = _resolve(entry.Value.Map, entry.Value.Handle) ?? entry.Value.Actor;
                if (actor != null && MapInstanceScope.Contains(entry.Value.Map, actor) &&
                    ReferenceEquals(actor.Controller.ScriptedMove, entry.Value.Move))
                {
                    actor.RunSpeed = entry.Value.PreviousSpeed;
                    actor.Controller.ScriptedMove = null;
                    if (actor.Controller.CurrentAction == BehaviorManager.BehaviorActionScriptedMove)
                        BehaviorManager.Instance.SetActionAnchor(actor, actor.Position);
                }
                _routes.Remove(entry.Key);
            }
        }

        private static bool Move(RouteRun run, Creature actor)
        {
            var point = run.Route.Points[run.Index];
            if (!BehaviorManager.Instance.SetActionScriptedMove(run.Map, actor, Position(point.Position), point.Orientation))
                return false;
            run.Move = actor.Controller.ScriptedMove;
            return true;
        }
        internal static Vector3 Position(ScenePosition position) => new(position.X, position.Y, position.Z);

        private sealed class RouteRun
        {
            internal MapChannel Map { get; }
            internal ActorHandle Handle { get; }
            internal RunRouteIntent Intent { get; }
            internal SceneRoute Route { get; }
            internal int Index { get; set; }
            internal float PreviousSpeed { get; }
            internal ScriptedMove Move { get; set; }
            internal DateTime? ResumeAt { get; set; }
            internal Creature Actor { get; }
            internal RouteRun(MapChannel map, ActorHandle handle, RunRouteIntent intent, SceneRoute route, int index, float speed, Creature actor)
            {
                Map = map; Handle = handle; Intent = intent; Route = route; Index = index; PreviousSpeed = speed; Actor = actor;
            }
        }
    }
}
