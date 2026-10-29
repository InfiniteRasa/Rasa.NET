using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Navigation;

namespace Rasa.Test.Missions.Content
{
    using Rasa.Test.Database;

    /// <summary>
    /// Every NPC and object a mission puts in the world, on the fully migrated World DB, stands where
    /// it can: on the map's navmesh, not inside a prop or in the air. The playthrough tests check a
    /// mission's logic only; mission 1975's actors passed them and in game stood inside the console at
    /// the middle of the room (on the shipped navmesh, which leaves the console's footprint out).
    ///
    /// What is checked, per mission (mission_scene_binding, mission_experience_binding, mission_spawn,
    /// mission_scenario_step):
    /// - a creature (scene Creature, PracticeTarget, PublicSpawn's pool, mission_spawn, player transfer
    ///   point) stands on the navmesh: <see cref="StandHorizontal"/> sideways and <see cref="StandVertical"/>
    ///   up or down of the nearest walkable point;
    /// - an object (scene Object, mission_scenario_step SpawnDynamicObject) is placed on purpose on
    ///   tables, walls and consoles, off the mesh: it needs walkable ground within the client's use range
    ///   (<see cref="ObjectReach"/>) so a player can stand and use it;
    /// - every route a scene runs (RunRouteIntent) has, leg by leg, the complete grounded path
    ///   SceneRouteController.Start asks for, from where its actor stands.
    ///
    /// A scene without a bound map (MissionSceneDefinition.BoundMapContextId) runs on the map it was taken
    /// on: the maps of its giver's and receiver's spawn pools, else of its spawn groups and areas.
    /// Where there are several, a point that stands on any of them passes.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionGroundTests
    {
        /// <summary>
        /// Sideways: the navmesh is eroded by the agent radius (BuildSettings.AgentRadius, 0.6 m) from
        /// every wall and prop, so an NPC standing against one is up to that far off it; its edge is
        /// simplified by up to BuildSettings.EdgeMaxError voxels (1.3 x 0.4 m = 0.52 m) and rasterised
        /// to one cell (BuildSettings.CellSize, 0.4 m). 0.6 + 0.52 + 0.4 = 1.52 m. A point inside a prop
        /// that the mesh leaves out, or out in the open beyond the mesh, is further.
        /// </summary>
        internal const float StandHorizontal = 1.5f;

        /// <summary>
        /// Up or down: what an agent steps onto (BuildSettings.AgentMaxClimb, 0.9 m) is ground to the
        /// mesh, so the mesh is up to that over the floor beside a crate or a sandbag line
        /// (SpawnPoolManager.SpawnPoint), plus one cell height (BuildSettings.CellHeight, 0.2 m).
        /// </summary>
        internal const float StandVertical = 1.1f;

        /// <summary>
        /// An object's use range: an actor's use range is 3 and a manifestation doubles it
        /// (client/augmentations/manifestation.py GetUseRange, DynamicObjectManager.MaxUseDistance).
        /// </summary>
        internal const float ObjectReach = 6.0f;

        /// <summary>SceneRouteController.Start: a leg's path must end this close to its waypoint, in 3D and in height.</summary>
        private const float RouteArrival = 0.5f;

        private static readonly RcVec3f StandExtents = NavMeshQuery.SearchExtents;
        private static readonly RcVec3f ObjectExtents = new(ObjectReach, 8f, ObjectReach);
        private static readonly RcVec3f FarExtents = new(100f, 300f, 100f);

        /// <summary>
        /// Placements flagged when the test was written (2026-10-25, shipped navmeshes), kept so the
        /// test passes and catches new ones; each is listed in tools/mission-crossref/NOTES-plateau-verify.md
        /// ("Ground test results") for the user to decide. Key: "mission|what". Remove an entry once its
        /// data is fixed - an entry that no longer fails fails the test.
        /// </summary>
        private static readonly Dictionary<string, string> Known = new(StringComparer.Ordinal)
        {
            // Self Preservation, Maligo base: the third security CPU is 6.28 m from the nearest walkable
            // point, past the client's 6 m use range (the client measures to the object's DAMAGE1 point,
            // which may be nearer). Not checked in game yet.
        };

        [TestMethod]
        public void EveryMissionPlacementStandsOnWalkableGround()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();

            var world = WorldData.Read(database.Context);
            var flagged = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var unchecked_ = new List<string>();
            var report = new List<string>();

            foreach (var placement in world.Placements())
            {
                if (placement.Maps.Count == 0)
                {
                    flagged[placement.Key] = $"{placement.Describe()}: no map known for it";
                    continue;
                }

                var results = placement.Maps.Select(map => (map, result: Check(world, placement, map))).ToArray();
                var best = results.FirstOrDefault(entry => entry.result.Passed);

                if (best.result == null)
                    best = results.First();

                report.Add($"{(best.result.Passed ? "ok  " : "FAIL")} {placement.Describe()} map {best.map}: {best.result.Detail}");

                if (best.result.Unchecked)
                    unchecked_.Add($"{placement.Describe()} map {best.map}: {best.result.Detail}");
                else if (!best.result.Passed)
                    flagged[placement.Key] = $"{placement.Describe()} map {best.map}: {best.result.Detail}";
            }

            var reportPath = Environment.GetEnvironmentVariable("RASA_MISSION_GROUND_REPORT");
            if (!string.IsNullOrEmpty(reportPath))
                File.WriteAllLines(reportPath, report.Concat(unchecked_.Select(line => "UNCHECKED " + line)));

            var newFailures = flagged.Where(entry => !Known.ContainsKey(entry.Key)).Select(entry => entry.Value).ToArray();
            var fixedKnown = Known.Keys.Where(key => !flagged.ContainsKey(key)).ToArray();

            Assert.IsTrue(newFailures.Length == 0 && fixedKnown.Length == 0 && unchecked_.Count == 0,
                (newFailures.Length > 0 ? $"{newFailures.Length} mission placement(s) not on walkable ground:\n  " +
                    string.Join("\n  ", newFailures) + "\n" : "") +
                (fixedKnown.Length > 0 ? "Known entries that now pass (remove them from MissionGroundTests.Known):\n  " +
                    string.Join("\n  ", fixedKnown) + "\n" : "") +
                (unchecked_.Count > 0 ? "Placements that could not be checked:\n  " + string.Join("\n  ", unchecked_) : ""));
        }

        /// <summary>
        /// What the ground check cannot see: the old 1975 actor points, which stood inside the large
        /// console at the middle of the room on map 2136 (Velon Hollow), are on the shipped navmesh -
        /// it has walkable floor under the console (0.0 m off it), because the console is not among the
        /// collision meshes the navmesh is built from. A prop the navmesh leaves in is invisible to this
        /// test. If a rebake ever cuts the console out, this fails: then turn it into a case that must fail.
        /// </summary>
        [TestMethod]
        [DataRow(364f, 64.3f, 126f)]
        [DataRow(366.7f, 64.3f, 127.7f)]
        public void TheOld1975PointsInsideTheConsoleAreOnTheShippedNavmesh(float x, float y, float z)
        {
            var result = CheckGround(Navmesh("adv_foreas_howlingmaw_deathburrow"),
                new Placement(1975, "old actor point", PlacementKind.Creature, new Vector3(x, y, z), new List<uint> { 2136 }));

            Assert.IsTrue(result.Passed, result.Detail);
            StringAssert.Contains(result.Detail, "(0.0 m sideways");
        }

        /// <summary>
        /// The check fails what is off the ground: the same 1975 point 3 m in the air or 3 m under the
        /// floor, and an object out of reach.
        /// </summary>
        [TestMethod]
        [DataRow(364f, 67.3f, 126f, PlacementKind.Creature, DisplayName = "3 m in the air")]
        [DataRow(364f, 61.3f, 126f, PlacementKind.Creature, DisplayName = "3 m under the floor")]
        [DataRow(364f, 82.3f, 126f, PlacementKind.Object, DisplayName = "an object 18 m over the floor")]
        public void APointOffTheGroundFails(float x, float y, float z, PlacementKind kind)
        {
            var result = CheckGround(Navmesh("adv_foreas_howlingmaw_deathburrow"),
                new Placement(1975, "test point", kind, new Vector3(x, y, z), new List<uint> { 2136 }));

            Assert.IsFalse(result.Passed, result.Detail);
        }

        /// <summary>A route whose waypoint is in the air has no grounded path, as SceneRouteController.Start refuses it.</summary>
        [TestMethod]
        public void ARouteToAPointInTheAirFails()
        {
            var placement = new Placement(1975, "test route", PlacementKind.Route, new Vector3(353.07f, 66.55f, 113.99f), new List<uint> { 2136 })
            {
                RoutePoints = new[] { new Vector3(364f, 67.3f, 126f) },
                RouteStarts = new[] { new RouteStart("tala's spawn point", new Vector3(353.07f, 66.55f, 113.99f)) }
            };

            var result = CheckRoute(Navmesh("adv_foreas_howlingmaw_deathburrow"), placement);

            Assert.IsFalse(result.Passed, result.Detail);
            StringAssert.Contains(result.Detail, "leg 0", result.Detail);
        }

        private static CheckResult Check(WorldData world, Placement placement, uint mapContextId)
        {
            if (!world.MapNames.TryGetValue(mapContextId, out var mapName))
                return CheckResult.NotChecked($"map {mapContextId} is not in map_info");

            var query = Navmesh(mapName);

            if (query == null)
                return CheckResult.NotChecked($"no navmesh for {mapName}");

            return placement.Kind == PlacementKind.Route ? CheckRoute(query, placement) : CheckGround(query, placement);
        }

        private static CheckResult CheckGround(NavMeshQuery query, Placement placement)
        {
            var point = placement.Point;
            var isObject = placement.Kind == PlacementKind.Object;
            var nearest = Nearest(query.NavMesh, point, isObject ? ObjectExtents : StandExtents);

            if (nearest is Vector3 ground)
            {
                var horizontal = Vector2.Distance(new Vector2(point.X, point.Z), new Vector2(ground.X, ground.Z));
                var vertical = Math.Abs(point.Y - ground.Y);
                var distance = Vector3.Distance(point, ground);
                var passed = isObject ? distance <= ObjectReach : horizontal <= StandHorizontal && vertical <= StandVertical;
                return new CheckResult(passed, false,
                    $"{Format(point)}, nearest navmesh {Format(ground)} ({F(horizontal)} m sideways, {F(point.Y - ground.Y)} m height, " +
                    $"{F(distance)} m away; allowed {(isObject ? $"{F(ObjectReach)} m away" : $"{F(StandHorizontal)} m sideways, {F(StandVertical)} m height")})");
            }

            var far = Nearest(query.NavMesh, point, FarExtents);
            return new CheckResult(false, false, far is Vector3 farGround
                ? $"{Format(point)}, nearest navmesh {Format(farGround)} ({F(Vector3.Distance(point, farGround))} m away)"
                : $"{Format(point)}, no navmesh within 100 m sideways / 300 m height");
        }

        /// <summary>As SceneRouteController.Start walks a route: leg by leg, each a complete path ending on its waypoint.</summary>
        private static CheckResult CheckRoute(NavMeshQuery query, Placement placement)
        {
            var failures = new List<string>();

            foreach (var start in placement.RouteStarts)
            {
                var problem = WalkRoute(query, start.Point, placement.RoutePoints, placement.StartWaypoint);
                if (problem == null)
                    return new CheckResult(true, false, $"from {start.Name} {Format(start.Point)}");
                failures.Add($"from {start.Name} {Format(start.Point)}: {problem}");
            }

            return new CheckResult(false, false, failures.Count == 0 ? "no start known for its actor" : string.Join("; ", failures));
        }

        private static string WalkRoute(NavMeshQuery query, Vector3 start, IReadOnlyList<Vector3> points, int startWaypoint)
        {
            var previous = start;

            for (var index = startWaypoint; index < points.Count; index++)
            {
                var destination = points[index];
                var path = query.FindPath(previous, destination, out var complete);

                if (path == null)
                    return $"leg {index} {Format(previous)} -> {Format(destination)}: an end is off the navmesh " +
                           $"(start {Distance(query, previous)}, waypoint {Distance(query, destination)})";
                if (!complete || path.Count == 0)
                    return $"leg {index} {Format(previous)} -> {Format(destination)}: no complete path (stops at {(path.Count > 0 ? Format(path[^1]) : "start")})";

                var end = path[^1];
                if (Vector3.Distance(end, destination) > RouteArrival || Math.Abs(end.Y - destination.Y) >= RouteArrival)
                    return $"leg {index} -> {Format(destination)}: path ends at {Format(end)}, {F(Vector3.Distance(end, destination))} m off " +
                           $"(height {F(destination.Y - end.Y)}; the server asks for {F(RouteArrival)} m)";

                previous = destination;
            }

            return null;
        }

        private static string Distance(NavMeshQuery query, Vector3 point)
        {
            var near = Nearest(query.NavMesh, point, FarExtents);
            return near is Vector3 ground ? $"{F(Vector3.Distance(point, ground))} m from navmesh" : "no navmesh near";
        }

        private static Vector3? Nearest(DtNavMesh mesh, Vector3 point, RcVec3f extents)
        {
            var query = new DtNavMeshQuery(mesh);
            var status = query.FindNearestPoly(new RcVec3f(point.X, point.Y, point.Z), extents, new DtQueryDefaultFilter(),
                out var poly, out var nearest, out _);
            return status.Succeeded() && poly != 0 ? new Vector3(nearest.X, nearest.Y, nearest.Z) : null;
        }

        private static readonly Dictionary<string, NavMeshQuery> Queries = new(StringComparer.OrdinalIgnoreCase);

        private static NavMeshQuery Navmesh(string mapName)
        {
            if (Queries.TryGetValue(mapName, out var cached))
                return cached;

            var path = NavMeshFile.PathFor(Path.Combine(RepositoryRoot(), "navmesh"), mapName);
            var query = File.Exists(path) ? TestNavMeshes.Query(path) : null;
            Queries[mapName] = query;
            return query;
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Repository root not found.");
            return directory.FullName;
        }

        private static string F(float value) => value.ToString("0.0#", CultureInfo.InvariantCulture);
        private static string Format(Vector3 point) => $"({F(point.X)}, {F(point.Y)}, {F(point.Z)})";

        public enum PlacementKind { Creature, Object, Route }

        internal sealed record RouteStart(string Name, Vector3 Point);

        internal sealed record Placement(uint MissionId, string What, PlacementKind Kind, Vector3 Point, List<uint> Maps)
        {
            internal string Owner { get; init; }
            internal IReadOnlyList<Vector3> RoutePoints { get; init; } = Array.Empty<Vector3>();
            internal int StartWaypoint { get; init; }
            internal IReadOnlyList<RouteStart> RouteStarts { get; init; } = Array.Empty<RouteStart>();

            internal string Key => $"{Owner ?? MissionId.ToString(CultureInfo.InvariantCulture)}|{What}";
            internal string Describe() => $"mission {Owner ?? MissionId.ToString(CultureInfo.InvariantCulture)} {What} ({Kind})";
        }

        private sealed record CheckResult(bool Passed, bool Unchecked, string Detail)
        {
            internal static CheckResult NotChecked(string why) => new(false, true, why);
        }

        /// <summary>The migrated rows the placements come from.</summary>
        private sealed class WorldData
        {
            internal Dictionary<uint, string> MapNames { get; } = new();
            private readonly Dictionary<uint, MissionSceneDefinition> _scenes = new();
            private readonly List<(string Key, uint Map, MissionSceneDefinition Scene)> _experiences = new();
            private readonly Dictionary<uint, (Vector3 Position, uint Map)> _pools = new();
            private readonly Dictionary<uint, HashSet<uint>> _creatureMaps = new();
            private readonly Dictionary<uint, (uint Giver, uint Receiver)> _givers = new();
            private readonly Dictionary<uint, HashSet<uint>> _missionMaps = new();
            private readonly List<(uint Mission, uint Group, uint Spawn, uint Creature, Vector3 Position, uint Map)> _spawns = new();
            private readonly List<(uint Mission, uint Step, int Kind, uint? Map, Vector3 Position, string Name)> _steps = new();

            internal static WorldData Read(DbContext context)
            {
                var data = new WorldData();
                var connection = context.Database.GetDbConnection();
                connection.Open();

                Query(connection, "SELECT map_context_id, map_name FROM map_info", row =>
                    data.MapNames[U(row, 0)] = row.GetString(1));
                Query(connection, "SELECT mission_id, bindings FROM mission_scene_binding", row =>
                    data._scenes[U(row, 0)] = JsonSerializer.Deserialize<MissionSceneDefinition>(row.GetString(1), MissionContentCodec.Options));
                Query(connection, "SELECT experience_key, map_context_id, bindings FROM mission_experience_binding", row =>
                    data._experiences.Add((row.GetString(0), U(row, 1),
                        JsonSerializer.Deserialize<MissionExperienceDefinition>(row.GetString(2), MissionContentCodec.Options).Scene)));
                Query(connection, "SELECT id, map_context_id, pos_x, pos_y, pos_z, creature_1_Id, creature_2_Id, creature_3_Id, " +
                                  "creature_4_Id, creature_5_Id, creature_6_Id FROM spawnpool", row =>
                {
                    var map = U(row, 1);
                    data._pools[U(row, 0)] = (new Vector3(Fl(row, 2), Fl(row, 3), Fl(row, 4)), map);
                    for (var column = 5; column <= 10; column++)
                        if (!row.IsDBNull(column) && U(row, column) != 0)
                            Add(data._creatureMaps, U(row, column), map);
                });
                Query(connection, "SELECT mission_id, giver_id, receiver_id FROM mission_content_definition", row =>
                    data._givers[U(row, 0)] = (row.IsDBNull(1) ? 0 : U(row, 1), row.IsDBNull(2) ? 0 : U(row, 2)));
                Query(connection, "SELECT mission_id, map_context_id FROM mission_spawn_group UNION " +
                                  "SELECT mission_id, map_context_id FROM mission_area UNION " +
                                  "SELECT mission_id, map_context_id FROM mission_scenario_step WHERE map_context_id IS NOT NULL", row =>
                {
                    if (!row.IsDBNull(1))
                        Add(data._missionMaps, U(row, 0), U(row, 1));
                });
                Query(connection, "SELECT s.mission_id, s.spawn_group_id, s.spawn_id, s.creature_id, s.pos_x, s.pos_y, s.pos_z, g.map_context_id " +
                                  "FROM mission_spawn s JOIN mission_spawn_group g ON g.mission_id = s.mission_id " +
                                  "AND g.content_revision = s.content_revision AND g.spawn_group_id = s.spawn_group_id", row =>
                    data._spawns.Add((U(row, 0), U(row, 1), U(row, 2), U(row, 3), new Vector3(Fl(row, 4), Fl(row, 5), Fl(row, 6)), U(row, 7))));
                Query(connection, "SELECT mission_id, step_id, kind, map_context_id, pos_x, pos_y, pos_z, dynamic_object_key " +
                                  "FROM mission_scenario_step WHERE pos_x IS NOT NULL", row =>
                    data._steps.Add((U(row, 0), U(row, 1), row.GetInt32(2), row.IsDBNull(3) ? null : U(row, 3),
                        new Vector3(Fl(row, 4), Fl(row, 5), Fl(row, 6)), row.IsDBNull(7) ? null : row.GetString(7))));

                return data;
            }

            private static void Query(DbConnection connection, string sql, Action<DbDataReader> read)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    read(reader);
            }

            private static uint U(DbDataReader row, int column) => Convert.ToUInt32(row.GetValue(column), CultureInfo.InvariantCulture);
            private static float Fl(DbDataReader row, int column) => Convert.ToSingle(row.GetValue(column), CultureInfo.InvariantCulture);
            private static void Add(Dictionary<uint, HashSet<uint>> map, uint key, uint value)
            {
                if (!map.TryGetValue(key, out var set))
                    map[key] = set = new HashSet<uint>();
                set.Add(value);
            }

            /// <summary>
            /// The maps a mission's unbound scene may run on: its giver's and receiver's spawn pools', and
            /// its spawn groups', areas' and scenario steps'. A Bootcamp giver has no pool of its own (the
            /// experience spawns it), and its receiver may stand on another map (1995: the receiver on 1220,
            /// the scene on 1985), so all of them are candidates.
            /// </summary>
            private List<uint> MissionMaps(uint missionId)
            {
                var maps = new SortedSet<uint>();
                if (_givers.TryGetValue(missionId, out var npcs))
                    foreach (var npc in new[] { npcs.Giver, npcs.Receiver })
                        if (_creatureMaps.TryGetValue(npc, out var found))
                            maps.UnionWith(found);
                if (_missionMaps.TryGetValue(missionId, out var own))
                    maps.UnionWith(own);
                return maps.ToList();
            }

            internal IEnumerable<Placement> Placements()
            {
                foreach (var (missionId, scene) in _scenes.OrderBy(entry => entry.Key))
                {
                    var maps = scene.BoundMapContextId is uint bound ? new List<uint> { bound } : MissionMaps(missionId);
                    foreach (var placement in ScenePlacements(missionId, null, scene, maps))
                        yield return placement;
                }

                foreach (var (key, map, scene) in _experiences)
                    foreach (var placement in ScenePlacements(0, $"experience {key}", scene, new List<uint> { map }))
                        yield return placement;

                foreach (var spawn in _spawns)
                    yield return new Placement(spawn.Mission, $"mission_spawn group {spawn.Group} spawn {spawn.Spawn} creature {spawn.Creature}",
                        PlacementKind.Creature, spawn.Position, new List<uint> { spawn.Map });

                foreach (var step in _steps)
                {
                    var maps = step.Map is uint map ? new List<uint> { map } : MissionMaps(step.Mission);
                    var kind = step.Kind == 20 ? PlacementKind.Object : PlacementKind.Creature;
                    var what = step.Kind == 20 ? $"scenario step {step.Step} dynamic object {step.Name}" : $"scenario step {step.Step} player transfer";
                    yield return new Placement(step.Mission, what, kind, step.Position, maps);
                }
            }

            private IEnumerable<Placement> ScenePlacements(uint missionId, string owner, MissionSceneDefinition scene, List<uint> maps)
            {
                var starts = new Dictionary<string, Vector3>(StringComparer.Ordinal);

                foreach (var (role, actor) in scene.Actors.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    Vector3 point;
                    var actorMaps = maps;

                    if (actor.Position != null)
                        point = V(actor.Position);
                    else if (actor.Kind == SceneActorKind.PublicSpawn && _pools.TryGetValue(actor.TemplateId, out var pool))
                    {
                        point = pool.Position;
                        actorMaps = new List<uint> { pool.Map };
                    }
                    else
                    {
                        yield return new Placement(missionId, $"actor {role} (template {actor.TemplateId})", PlacementKind.Creature,
                            Vector3.Zero, new List<uint>()) { Owner = owner };
                        continue;
                    }

                    starts[role] = point;
                    var kind = actor.Kind == SceneActorKind.Object ? PlacementKind.Object : PlacementKind.Creature;
                    var what = actor.Kind == SceneActorKind.PublicSpawn ? $"actor {role} (spawn pool {actor.TemplateId})" : $"actor {role} (template {actor.TemplateId})";
                    yield return new Placement(missionId, what, kind, point, actorMaps) { Owner = owner };
                }

                var intents = (scene.Sequences ?? new Dictionary<uint, SceneSequenceDefinition>()).Values
                    .SelectMany(sequence => sequence.World ?? new List<WorldIntent>()).ToArray();
                var routeIntents = intents.OfType<RunRouteIntent>()
                    .GroupBy(intent => (intent.Role, intent.Route, intent.StartWaypoint)).Select(group => group.First()).ToArray();

                foreach (var intent in routeIntents)
                {
                    var what = $"route {intent.Route} by {intent.Role}" + (intent.StartWaypoint > 0 ? $" from waypoint {intent.StartWaypoint}" : "");

                    if (!scene.Routes.TryGetValue(intent.Route, out var route))
                    {
                        yield return new Placement(missionId, what + " (route not defined)", PlacementKind.Route, Vector3.Zero, new List<uint>()) { Owner = owner };
                        continue;
                    }

                    var points = route.Points.Select(point => V(point.Position)).ToArray();
                    var candidates = new List<RouteStart>();

                    if (intent.StartWaypoint > 0 && intent.StartWaypoint <= points.Length)
                        candidates.Add(new RouteStart($"waypoint {intent.StartWaypoint - 1}", points[intent.StartWaypoint - 1]));
                    if (starts.TryGetValue(intent.Role, out var spawn))
                        candidates.Add(new RouteStart("its spawn point", spawn));
                    // An actor that walked another route first starts this one where that ended.
                    foreach (var other in routeIntents.Where(other => other.Role == intent.Role && other.Route != intent.Route))
                        if (scene.Routes.TryGetValue(other.Route, out var previous) && previous.Points.Count > 0)
                            candidates.Add(new RouteStart($"the end of route {other.Route}", V(previous.Points[^1].Position)));
                    foreach (var pose in intents.OfType<RestoreActorPoseIntent>().Where(pose => pose.Role == intent.Role && pose.Position != null))
                        candidates.Add(new RouteStart($"restored pose {pose.OperationKey}", V(pose.Position)));

                    yield return new Placement(missionId, what, PlacementKind.Route, points.FirstOrDefault(), maps)
                    {
                        Owner = owner, RoutePoints = points, StartWaypoint = intent.StartWaypoint, RouteStarts = candidates
                    };
                }

                foreach (var transfer in intents.OfType<TransferIntent>().Where(transfer => transfer.Position != null)
                             .GroupBy(transfer => transfer.OperationKey).Select(group => group.First()))
                    yield return new Placement(missionId, $"player transfer {transfer.OperationKey}", PlacementKind.Creature,
                        V(transfer.Position), new List<uint> { transfer.MapContextId }) { Owner = owner };
            }

            private static Vector3 V(ScenePosition position) => new(position.X, position.Y, position.Z);
        }
    }
}
