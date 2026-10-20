using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Young Forest Boargar pack west of Alia Das stands out of the meadow tripod turret's
    /// scan, on the same meadow, so the two no longer fight for as long as the server runs
    /// (Meadow_boargar).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MeadowBoargarTests
    {
        private const string Migration = "20270108000000_Meadow_boargar";
        private const string Previous = "20270107000000_Alia_das_third_look";

        [TestMethod]
        public void ValidatePoolsFindsThePackClearOfTheTurretAndOfSafeGround()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var pools = SpawnPoolManager.Instance;
            var pack = pools.LoadedSpawnPools[MeadowBoargar.Pool];
            var turret = pools.LoadedSpawnPools[MeadowBoargar.TurretPool];

            Assert.AreEqual(new Vector3((float)MeadowBoargar.ToX, (float)MeadowBoargar.ToY, (float)MeadowBoargar.ToZ), pack.Position);
            Assert.AreEqual(15f, pack.Radius);

            var edge = Flat(pack.Position, turret.Position) - pack.Radius;
            Assert.IsTrue(edge >= 20f && edge >= SpawnPoolManager.TurretScan, $"{edge:F1} m from the turret");

            var places = new List<(uint Map, Vector3 Position, string What)>();
            foreach (var teleporter in DynamicObjectManager.Instance.Teleporters.Values)
                if (teleporter.ObjectData is WaypointInfo info && (info.WaypointType == WaypointType.Hospital || info.WaypointType == WaypointType.Waypoint))
                    places.Add((teleporter.MapContextId, teleporter.Position, $"{info.WaypointType} {info.WaypointId}"));

            var found = pools.PoolsTooClose(places).Where(f => f.Map == 1220).Select(f => f.Message).ToList();
            Assert.IsFalse(found.Any(m => m.StartsWith($"spawnpool {MeadowBoargar.Pool}:")), string.Join("\n", found));
            Assert.IsFalse(found.Any(m => m.Contains($"turret of pool {MeadowBoargar.TurretPool}")), string.Join("\n", found));

            // Clear of the next pack's circle by the 20 m the Wilderness pools were laid out with.
            foreach (var other in pools.LoadedSpawnPools.Values.Where(p => p.MapContextId == 1220 && p.DbId != pack.DbId && p.DbId != turret.DbId
                         && p.SpawnSlot.Exists(s => s.CountMax > 0) && p.SpawnSlot.TrueForAll(s => CreatureManager.Instance.LoadedCreatures.TryGetValue(s.CreatureId, out var c) && c.TargetCategory == TargetCategory.Hostile)))
                Assert.IsTrue(Flat(pack.Position, other.Position) - pack.Radius - other.Radius >= 20f, $"pool {other.DbId}");
        }

        [TestMethod]
        public void TheWholeCircleIsOpenGroundJoinedToTheOldOne()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var nav = harness.Map.NavMesh;
            var centre = new Vector3((float)MeadowBoargar.ToX, (float)MeadowBoargar.ToY, (float)MeadowBoargar.ToZ);
            var lowest = float.MaxValue;
            var highest = float.MinValue;

            foreach (var spot in Circle(centre, 15f))
            {
                var ground = nav.GroundHeight(spot);
                Assert.IsTrue(ground.HasValue, $"ground at {spot}");
                var floor = nav.Nearest(new Vector3(spot.X, ground.Value, spot.Z));
                Assert.IsTrue(floor.HasValue && Flat(floor.Value, spot) <= 1f, $"mesh at {spot}: {floor}");
                Assert.IsFalse(nav.IsUnderground(floor.Value), $"{spot} is not underground");
                Assert.IsFalse(FallDamage.InWater("adv_foreas_concordia_wilderness", floor.Value), $"{spot} is not in water");
                lowest = Math.Min(lowest, floor.Value.Y);
                highest = Math.Max(highest, floor.Value.Y);
            }

            Assert.IsTrue(highest - lowest < 6f, $"a meadow, not a slope: {highest - lowest:F1} m");
            Assert.AreEqual(MeadowBoargar.ToY, nav.GroundHeight(centre).Value, 0.01, "the centre at the ground's height");

            var was = nav.Nearest(new Vector3(MeadowBoargar.FromX, MeadowBoargar.FromY, MeadowBoargar.FromZ)).Value;
            Assert.AreEqual(true, nav.Reachable(was, nav.Nearest(centre).Value), "the same meadow");
        }

        [TestMethod]
        public void DownPutsItBackAndTheUpIsGuarded()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.AreEqual((646.91, 439.32), Position(database));

            migrator.Migrate(Migration);
            Assert.AreEqual((624.0, 430.0), Position(database));

            foreach (var sql in MeadowBoargar.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual((624.0, 430.0), Position(database), "once");

            migrator.Migrate(Previous);
            Assert.AreEqual((646.91, 439.32), Position(database));
            // To the bit, as the preloader wrote it: single-precision floats.
            Assert.AreEqual((double)MeadowBoargar.FromY, database.Context.Database.SqlQueryRaw<double>($"SELECT pos_y AS Value FROM spawnpool WHERE id = {MeadowBoargar.Pool}").Single());

            // One a GM has put somewhere else is left there.
            database.Context.Database.ExecuteSqlRaw($"UPDATE spawnpool SET pos_x = 650 WHERE id = {MeadowBoargar.Pool}");
            migrator.Migrate(Migration);
            Assert.AreEqual((650.0, 439.32), Position(database));
        }

        private static IEnumerable<Vector3> Circle(Vector3 centre, float radius)
        {
            yield return centre;

            foreach (var ring in new[] { radius / 2, radius })
                for (var i = 0; i < 16; i++)
                {
                    var angle = i * MathF.PI / 8;
                    yield return new Vector3(centre.X + ring * MathF.Cos(angle), centre.Y, centre.Z + ring * MathF.Sin(angle));
                }
        }

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.X - b.X, a.Z - b.Z).Length();

        private static (double, double) Position(OrdinaryLootTestDatabase database)
        {
            var x = database.Context.Database.SqlQueryRaw<double>($"SELECT pos_x AS Value FROM spawnpool WHERE id = {MeadowBoargar.Pool}").Single();
            var z = database.Context.Database.SqlQueryRaw<double>($"SELECT pos_z AS Value FROM spawnpool WHERE id = {MeadowBoargar.Pool}").Single();
            return (Math.Round(x, 2), Math.Round(z, 2));
        }
    }
}
