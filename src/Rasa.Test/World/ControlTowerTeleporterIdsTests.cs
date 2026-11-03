using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Database;

namespace Rasa.Test.World
{
    /// <summary>The control tower pads moved under the wormholes' ids, their links with them (ControlTowerTeleporterIds).</summary>
    [TestClass]
    [DoNotParallelize]
    public class ControlTowerTeleporterIdsTests
    {
        private const string Migration = "20271018000000_Control_tower_teleporter_ids";
        private const string Previous = "20271017000000_Kardash_waypoints_mysteries";

        [TestMethod]
        public void EveryPadMovesUnderAThousandWithItsLinksAndDownMovesThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var rowsBefore = Rows(database);
            var linksBefore = Links(database);
            var moves = ControlTowerTeleporterIds.Moves().ToList();
            Assert.HasCount(57, moves);
            Assert.IsTrue(moves.All(m => rowsBefore.ContainsKey(m.From) && !rowsBefore.ContainsKey(m.To)), "the old ids there, the new ones free");

            migrator.Migrate(Migration);
            var rows = Rows(database);
            var links = Links(database);
            Assert.IsLessThan(1000u, database.Context.TeleporterEntries.AsNoTracking().Where(t => t.MapContextId != 0).Max(t => t.Id));
            var to = moves.ToDictionary(m => m.From, m => m.To);
            foreach (var m in moves)
                Assert.AreEqual(rowsBefore[m.From], rows[m.To], $"{m.From} as {m.To}");
            CollectionAssert.AreEquivalent(rowsBefore.Keys.Select(id => to.GetValueOrDefault(id, id)).ToList(), rows.Keys.ToList());
            CollectionAssert.AreEquivalent(
                linksBefore.Select(l => (to.GetValueOrDefault(l.From, l.From), to.GetValueOrDefault(l.To, l.To))).ToList(), links);
            Assert.IsTrue(links.All(l => rows.ContainsKey(l.From) && rows.ContainsKey(l.To)), "every link's ends are there");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(rowsBefore.ToList(), Rows(database).ToList());
            CollectionAssert.AreEquivalent(linksBefore, Links(database));
        }

        [TestMethod]
        public void OnMySqlThePadsMoveAndMoveBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var (from, to, _, _, _) = ControlTowerTeleporterIds.Moves().First();

            StringAssert.Contains(migrator.GenerateScript(Previous, Migration), $"UPDATE teleporter SET id = {to} WHERE id = {from} AND");
            StringAssert.Contains(migrator.GenerateScript(Migration, Previous), $"UPDATE teleporter SET id = {from} WHERE id = {to} AND");
        }

        private static Dictionary<uint, string> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.TeleporterEntries.AsNoTracking().ToList()
                .ToDictionary(t => t.Id, t => $"{t.ClassId} {t.Type} {t.MapContextId} {t.PosX:F3} {t.PosY:F3} {t.PosZ:F3} {t.Rotation:F2} {t.Description}");

        private static List<(uint From, uint To)> Links(OrdinaryLootTestDatabase database) =>
            database.Context.LocalTeleporterLinkEntries.AsNoTracking().ToList().Select(l => (l.FromTeleporterId, l.ToTeleporterId)).ToList();
    }
}
