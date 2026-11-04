using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Marshes
{
    using Rasa.Missions.Scenes;
    using Rasa.Services.Preloader.Missions.Marshes;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The fixes of the Marshes review list (real-client playtest, 2026-11-04), each a migration after
    /// Marshes_on_the_navmesh: each test checks the fix on the migrated World DB, rolls its migration back to the one
    /// before it (IMigrationsAssembly) and finds the old data, then migrates up again.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesReviewFixesTests
    {
        private const string BraziersOnDryFloor = "20271104003100_Marshes_braziers_on_dry_floor";

        private static string Previous(DbContext world, string migration)
        {
            var ids = world.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(migration);
            Assert.IsGreaterThan(0, index, $"{migration} has a migration before it");
            return ids[index - 1];
        }

        private static int Count(DbContext world, string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();

        /// <summary>How many of the mission's bindings hold <paramref name="text"/> (braces doubled for SqlQueryRaw's format).</summary>
        private static int Bound(DbContext world, uint mission, string text) => Count(world,
            $"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id = {mission} AND content_revision = '{MarshesMissionDataV1.Revision}' " +
            $"AND bindings LIKE '%{text.Replace("{", "{{").Replace("}", "}}")}%'");

        /// <summary>
        /// 864's braziers stood in the temple fountains' water (review list, 2026-11-04). Both stand on the temple floor now;
        /// rolled back, both are in the basins again.
        /// </summary>
        [TestMethod]
        public void SpiritHomesBraziersStandOutOfTheFountains()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            int Braziers(bool now) => MarshesBraziersOnDryFloorV1.Moves.Count(move => Bound(world, MarshesBringingTheSpiritHomeV1.MissionId,
                MarshesBindings.Actor(move.Role, SceneActorKind.Object, MarshesBringingTheSpiritHomeV1.BrazierClass, now ? move.Now : move.Before)) == 1);

            Assert.AreEqual((2, 0), (Braziers(true), Braziers(false)), "both braziers on the temple floor");
            Assert.IsTrue(MarshesBraziersOnDryFloorV1.Moves.All(move => move.Now.Y > 231.14f), "over the fountains' water (231.14)");
            migrator.Migrate(Previous(world, BraziersOnDryFloor));
            Assert.AreEqual((0, 2), (Braziers(true), Braziers(false)), "both in the basins as MarshesBringingTheSpiritHomeV1 put them");
            migrator.Migrate(BraziersOnDryFloor);
            Assert.AreEqual((2, 0), (Braziers(true), Braziers(false)), "back on the temple floor");
        }
    }
}
