using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Test.Database;

    /// <summary>The drill sergeant's two squads of recruits at the Proving Grounds (BootcampTrainees, Add_bootcamp_trainees).</summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampTraineeTests
    {
        private const string Before = "20261130000000_Add_bootcamp_ambient_guards";
        private const string Migration = "20261201000000_Add_bootcamp_trainees";

        [TestMethod]
        public void TheSeedIsTwoSquadsOfRecruitsAndDownTakesThemAway()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                var squads = new AmbientNpcRepository(world).Get().Where(row => row.ClassId == BootcampTrainees.TraineesClass).ToList();
                var trainees = world.EntityClassEntries.AsNoTracking().Single(entry => entry.Id == BootcampTrainees.TraineesClass);

                Assert.AreEqual("UsableStatelessNPCtraineesPTV01", trainees.ClassName);
                Assert.AreEqual("8", trainees.AugList, "a stateless switch and nothing else");
                CollectionAssert.AreEqual(new uint[] { 16, 17 }, squads.Select(row => row.Id).ToArray());

                foreach (var squad in squads)
                {
                    Assert.AreEqual(BootcampPostedNpcs.BootcampMapContextId, squad.MapContextId);
                    Assert.AreEqual(119.58, squad.PosY, 0.0001);
                    Assert.AreEqual(146.5, squad.PosZ, 0.0001);
                    Assert.AreEqual(3.1416, squad.Rotation, 0.0001);
                    StringAssert.StartsWith(squad.Comment, "Proving Grounds: six recruits");
                }

                Assert.AreEqual(377.0993, squads[0].PosX, 0.0001);
                Assert.AreEqual(381.4688, squads[1].PosX, 0.0001);

                world.GetService<IMigrator>().Migrate(Before);

                Assert.IsFalse(new AmbientNpcRepository(world).Get().Any(row => row.ClassId == BootcampTrainees.TraineesClass));
                Assert.HasCount(9, new AmbientNpcRepository(world).Get().Where(row => row.Id >= BootcampAmbientGuards.FirstId), "the guards and the drill sergeant stay");

                world.GetService<IMigrator>().Migrate(Migration);

                Assert.HasCount(2, new AmbientNpcRepository(world).Get().Where(row => row.ClassId == BootcampTrainees.TraineesClass));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheTwelveStandInSixEvenColumnsAndTwoRowsBeforeTheDrillSergeantFacingHim()
        {
            // A point on a squad's model, (mx, mz), is at x + mx cos r + mz sin r, z - mx sin r + mz cos r.
            var recruits = new List<(double X, double Z)>();

            foreach (var x in new[] { BootcampTrainees.WestX, BootcampTrainees.EastX })
                foreach (var mz in new[] { 0.0, BootcampTrainees.BackRowZ })
                    foreach (var mx in BootcampTrainees.ColumnsX)
                        recruits.Add((x + mx * Math.Cos(BootcampTrainees.Rotation) + mz * Math.Sin(BootcampTrainees.Rotation),
                            BootcampTrainees.FrontRowZ - mx * Math.Sin(BootcampTrainees.Rotation) + mz * Math.Cos(BootcampTrainees.Rotation)));

            var columns = recruits.Select(recruit => Math.Round(recruit.X, 2)).Distinct().OrderBy(x => x).ToList();
            var rows = recruits.Select(recruit => Math.Round(recruit.Z, 2)).Distinct().OrderBy(z => z).ToList();

            Assert.HasCount(6, columns);
            Assert.HasCount(2, rows);

            for (var i = 1; i < columns.Count; i++)
                Assert.AreEqual(BootcampTrainees.ColumnSpacing, columns[i] - columns[i - 1], 0.05, $"column {i}");

            // Its middle on the middle of the drill sergeant's line, between his line and the gate
            // wall, the back row clear of the wall's overhang (4.3 m up at z 144.5).
            Assert.AreEqual(BootcampAmbientGuards.DrillSergeantX, (columns.First() + columns.Last()) / 2, 0.01);
            Assert.IsTrue(rows.Last() < BootcampTrainingOfficer.LineZ - 1.5, "a stride short of his line");
            Assert.IsTrue(rows.First() - 0.36 > 144.5, "push-ups clear of the wall's overhang");

            // Facing him: a figure faces its -z, which at rotation r is (-sin r, -cos r).
            Assert.AreEqual(0.0, -Math.Sin(BootcampTrainees.Rotation), 0.001);
            Assert.AreEqual(1.0, -Math.Cos(BootcampTrainees.Rotation), 0.001);
        }

        [TestMethod]
        public void TheMigrationMakesTheSameRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "insert into ambient_npc (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, comment) values (16, 1985, 29418, 377.0993, 119.58, 146.5, 3.1416, 'Proving Grounds: six recruits drilling before the drill sergeant, west squad');");
            StringAssert.Contains(up, "values (17, 1985, 29418, 381.4688, 119.58, 146.5, 3.1416,");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "delete from ambient_npc where id in (16, 17);");
        }
    }
}
