using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Services.Preloader;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// The Proving Grounds' rifle guards and Training Officer as the client's ambient figures
    /// (BootcampAmbientGuards, Add_bootcamp_ambient_guards): eight guards at ease on the
    /// riflemen's spots and a drill sergeant pacing the officer's beat, in place of the pools.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampAmbientGuardTests
    {
        /// <summary>The migration before; the tests of what it took out run on the world as it was then.</summary>
        internal const string Before = "20261129000000_Add_npc_greeting_important";

        private const string Migration = "20261130000000_Add_bootcamp_ambient_guards";

        [TestMethod]
        public void TheGuardsStandWhereTheRiflemenStoodAndTheirPoolsAndTheOfficersAreGone()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                var figures = new AmbientNpcRepository(world).Get().Where(row => row.Id >= BootcampAmbientGuards.FirstId && row.Id <= BootcampAmbientGuards.LastId).ToList();
                var classes = world.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id);

                CollectionAssert.AreEqual(new uint[] { 7, 8, 9, 10, 11, 12, 13, 14, 15 }, figures.Select(row => row.Id).ToArray());
                Assert.AreEqual("UsableStatelessNPCDrillSGTV01", classes[figures[0].ClassId].ClassName);
                Assert.IsTrue(figures.Skip(1).All(row => classes[row.ClassId].ClassName == "UsableStatelessNPCMaleGuard"));

                foreach (var row in figures)
                {
                    Assert.AreEqual(BootcampPostedNpcs.BootcampMapContextId, row.MapContextId);
                    Assert.AreEqual("8", classes[row.ClassId].AugList, "a stateless switch and nothing else");
                    Assert.AreEqual((byte)0, classes[row.ClassId].TargetFlag);
                    StringAssert.StartsWith(row.Comment, "Proving Grounds: ");
                }

                // No pool, pose or patrol left of them; the creature rows stay.
                var pools = BootcampAmbientGuards.PoolIds;
                var spawnpools = new SpawnpoolRepository(world);

                Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(pool => pools.Contains(pool.Id)));
                Assert.IsFalse(spawnpools.GetPoses().Any(row => pools.Contains(row.Id)));
                Assert.IsFalse(spawnpools.GetPatrols().Any());
                CollectionAssert.AreEqual(new uint[] { 400001, 400002 }, spawnpools.GetPoses().Select(row => row.Id).OrderBy(id => id).ToArray(),
                    "the Forean Warrior and the unarmed Infantryman keep their posts");
                Assert.IsTrue(world.CreatureEntries.AsNoTracking().Any(row => row.Id == BootcampPostedNpcs.InfantrymanWithRifleId));
                Assert.IsTrue(world.CreatureEntries.AsNoTracking().Any(row => row.Id == BootcampTrainingOfficer.CreatureId));

                // Down: the pools as they were, and the guards each on its rifleman's spot, facing his way.
                var migrator = world.GetService<IMigrator>();
                migrator.Migrate(Before);

                Assert.IsFalse(new AmbientNpcRepository(world).Get().Any(row => row.Id >= BootcampAmbientGuards.FirstId));
                Assert.HasCount(4, spawnpools.GetPatrols());

                var riflemen = world.SpawnPoolEntries.AsNoTracking()
                    .Where(pool => BootcampAmbientGuards.RiflemanPoolIds.Contains(pool.Id)).ToDictionary(pool => pool.Id);
                var poses = spawnpools.GetPoses().ToDictionary(row => row.Id, row => row.Pose);

                Assert.HasCount(8, riflemen);
                Assert.IsTrue(world.SpawnPoolEntries.AsNoTracking().Any(pool => pool.Id == BootcampTrainingOfficer.PoolId));

                for (var i = 0; i < BootcampAmbientGuards.RiflemanPoolIds.Length; i++)
                {
                    var pool = riflemen[BootcampAmbientGuards.RiflemanPoolIds[i]];
                    var guard = figures[i + 1];

                    Assert.AreEqual(BootcampAmbientGuards.FirstGuardId + (uint)i, guard.Id);
                    Assert.AreEqual(BootcampPostedNpcs.InfantrymanWithRifleId, pool.Creature1Id);
                    Assert.AreEqual(BootcampPostedNpcs.WeaponOut, poses[pool.Id]);
                    Assert.AreEqual(pool.PosX, guard.PosX, 0.0001);
                    Assert.AreEqual(pool.PosY, guard.PosY, 0.0001);
                    Assert.AreEqual(pool.PosZ, guard.PosZ, 0.0001);
                    Assert.AreEqual(pool.Rotation, guard.Rotation, 0.0001);
                }

                // And up again.
                migrator.Migrate(Migration);

                Assert.HasCount(9, new AmbientNpcRepository(world).Get().Where(row => row.Id >= BootcampAmbientGuards.FirstId));
                Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(pool => pools.Contains(pool.Id)));
                Assert.IsFalse(spawnpools.GetPatrols().Any());
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheDrillSergeantPacesTheMiddleOfTheOfficersBeatFacingTheWayTheOfficerStopped()
        {
            var x = BootcampAmbientGuards.DrillSergeantX;
            var rotation = BootcampAmbientGuards.DrillSergeantRotation;

            // A point on his model, (mx, mz), is at x + mx cos r + mz sin r, z - mx sin r + mz cos r
            // (the firing range's target, 14.25 m down its -z, is ahead of a figure facing its rotation).
            double WorldX(double mx) => x + mx * Math.Cos(rotation);
            double WorldZ(double mx) => BootcampTrainingOfficer.LineZ - mx * Math.Sin(rotation);

            var ends = new[] { WorldX(BootcampAmbientGuards.PaceToPlusX), WorldX(BootcampAmbientGuards.PaceToMinusX) };

            Assert.AreEqual(BootcampAmbientGuards.BeatMiddleX, ends.Average(), 0.001, "the middle of his line on the middle of the beat");
            Assert.IsTrue(ends.All(end => end > BootcampTrainingOfficer.NorthEndX && end < BootcampTrainingOfficer.SouthEndX), "within the beat");
            Assert.AreEqual(BootcampTrainingOfficer.LineZ, WorldZ(BootcampAmbientGuards.PaceToMinusX), 0.001, "along the beat's line");

            // He stands facing his +z: (sin r, cos r) in the world, the yaw atan2(-dx, -dz) of which is the officer's at his stops.
            var yaw = Math.Atan2(-Math.Sin(rotation), -Math.Cos(rotation));

            Assert.AreEqual(BootcampTrainingOfficer.StopFacing, yaw, 0.001);
        }

        [TestMethod]
        public void TheGuardsAndTheDrillSergeantStandAtTheProvingGroundsAsFiguresAndNoRiflemanIsMade()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);

            var rows = new AmbientNpcRepository(harness.WorldContext).Get();
            var classes = harness.WorldContext.EntityClassEntries.AsNoTracking()
                .Where(entry => entry.Id == BootcampAmbientGuards.MaleGuardClass || entry.Id == BootcampAmbientGuards.DrillSergeantClass).ToList();

            var loaded = EntityClassManager.Instance.LoadedEntityClasses;
            var had = classes.Where(entry => loaded.ContainsKey((EntityClasses)entry.Id))
                .ToDictionary(entry => (EntityClasses)entry.Id, entry => loaded[(EntityClasses)entry.Id]);

            foreach (var entry in classes)
                loaded[(EntityClasses)entry.Id] =
                    new EntityClass(entry.Id, entry.ClassName, entry.MeshId, entry.ClassCollisionRole, new() { AugmentationType.StatelessSwitch }, false);

            try
            {
                AmbientNpcs.Load(rows.Where(row => row.Id >= BootcampAmbientGuards.FirstId && row.Id <= BootcampAmbientGuards.LastId));

                Assert.AreEqual(9, AmbientNpcs.Place(harness.BootcampMap));

                SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

                var creatures = harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().ToList();

                Assert.IsFalse(creatures.Any(creature => creature.SpawnPool != null && BootcampAmbientGuards.PoolIds.Contains(creature.SpawnPool.DbId)));
                Assert.IsTrue(creatures.Any(creature => creature.SpawnPool?.DbId == BootcampPostedNpcs.InfantrymanPoolId), "the unarmed Infantryman is still at his post");

                // A recruit at the bridge is shown its two guards, and the drill sergeant up the road.
                var standing = AmbientNpcs.OnChannel(harness.BootcampMap);

                harness.MovePlayerTo(new Vector3(388.7f, 119.6f, 142f));
                harness.Drain();
                CellManager.Instance.UpdateVisibility(harness.Client);

                var made = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                    .Where(packet => standing.Any(figure => figure.EntityId == packet.EntityId))
                    .ToDictionary(packet => packet.EntityId);

                foreach (var id in new uint[] { 7, 8, 9 })
                {
                    var figure = standing.Single(obj => ((AmbientNpcs.Placement)obj.ObjectData).Id == id);
                    var packet = made[figure.EntityId];

                    Assert.AreEqual(rows.Single(row => row.Id == id).ClassId, (uint)packet.ClassId);
                    Assert.IsFalse(packet.EntityData.OfType<IsTargetablePacket>().Single().IsTargetable);
                    Assert.AreEqual(UseObjectState.SsState0, packet.EntityData.OfType<UsableInfoPacket>().Single().CurState);
                }
            }
            finally
            {
                foreach (var figure in AmbientNpcs.OnChannel(harness.BootcampMap))
                    CellManager.Instance.RemoveFromWorld(harness.BootcampMap, figure);

                AmbientNpcs.Load(null);

                foreach (var entry in classes)
                {
                    if (had.TryGetValue((EntityClasses)entry.Id, out var was))
                        loaded[(EntityClasses)entry.Id] = was;
                    else
                        loaded.Remove((EntityClasses)entry.Id);
                }
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "delete from spawnpool_patrol where pool_id = 400005;");
            StringAssert.Contains(up, "delete from spawnpool_pose where id in (400003, 400004, 400005, 400101, 400102, 400103, 400104, 400105, 400106);");
            StringAssert.Contains(up, "delete from spawnpool where id in (400003, 400004, 400005, 400101, 400102, 400103, 400104, 400105, 400106);");
            StringAssert.Contains(up, "INSERT INTO `ambient_npc`");
            // Doubles as MySQL is given them: 379.3246 as 379.32459999999998.
            StringAssert.Contains(up, "(7, 1985, 29422, 379.3245");
            StringAssert.Contains(up, ", 119.58, 148.59, 3.1415");
            StringAssert.Contains(up, "(8, 1985, 26397, 384.4805");
            StringAssert.Contains(up, "(15, 1985, 26397, 382.45");
            StringAssert.Contains(up, "'Proving Grounds: rifle guard at ease, west of the bridge'");
            StringAssert.Contains(up, $"'{Migration}'");

            StringAssert.Contains(down, "delete from ambient_npc where id between 7 and 15;");
            StringAssert.Contains(down, "INSERT INTO `spawnpool`");
            StringAssert.Contains(down, "INSERT INTO `spawnpool_pose`");
            StringAssert.Contains(down, "INSERT INTO `spawnpool_patrol`");
            Assert.IsFalse(down.Contains("(400001,"), "the posted NPCs that stay are not put back twice");
        }
    }
}
