using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;
    using L = Rasa.Services.Preloader.MarshesLogosElement;

    /// <summary>
    /// The Valverde Marshes shrine of class 21271 teaches Logos Element (215), the Logos it shows,
    /// and Crucible's Everyone shrine is Everyone's class (Marshes_logos_element).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesLogosElementTests
    {
        private const string Migration = "20271007000000_Marshes_logos_element";
        private const string Previous = "20271006000000_Pools_turpis_refinery";

        [TestCleanup]
        public void ForgetTheShrines() => LogosTitles.Load(null);

        [TestMethod]
        public void TheMarshesShrineIsLogosElementAndEveryoneIsItsOwnShrine()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rows(database);
            var marshes = before[L.WasLogos];
            Assert.AreEqual((L.ShrineClass, L.Marshes, L.WasName), (marshes.ClassId, marshes.MapContextId, marshes.Name), "as the logos table had it");
            Assert.IsFalse(before.ContainsKey(L.NowLogos), "no shrine of Logos Element");
            Assert.AreEqual(L.WasEveryoneClass, before[L.Everyone].ClassId);

            migrator.Migrate(Migration);
            var after = Rows(database);
            Assert.IsFalse(after.ContainsKey(L.WasLogos), "no shrine of Element");
            var shrine = after[L.NowLogos];
            Assert.AreEqual((L.ShrineClass, L.Marshes, L.NowName), (shrine.ClassId, shrine.MapContextId, shrine.Name));
            Assert.AreEqual((marshes.PosX, marshes.PosY, marshes.PosZ), (shrine.PosX, shrine.PosY, shrine.PosZ), "where it stood");
            Assert.AreEqual(L.NowEveryoneClass, after[L.Everyone].ClassId);
            Assert.AreEqual(before.Count, after.Count);
            foreach (var id in before.Keys.Where(id => id != L.WasLogos && id != L.Everyone))
                Assert.AreEqual(Describe(before[id]), Describe(after[id]), $"logos {id} is as it was");

            // Twice is once.
            foreach (var sql in L.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEquivalent(after.Values.Select(Describe).ToList(), Rows(database).Values.Select(Describe).ToList());

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(before.Values.Select(Describe).ToList(), Rows(database).Values.Select(Describe).ToList(), "Down puts both back");
        }

        [TestMethod]
        public void OnMySqlBothChangeAndChangeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in L.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in L.Down())
                StringAssert.Contains(down, sql);
        }

        /// <summary>Used to the end, the Marshes shrine teaches Logos Element, and the client is told so.</summary>
        [TestMethod]
        public void UsingTheMarshesShrineTeachesLogosElement()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var map = player.MapChannel;
            var entry = harness.WorldContext.Set<LogosEntry>().AsNoTracking().Single(logos => logos.MapContextId == L.Marshes && logos.ClassId == L.ShrineClass);
            Assert.AreEqual(L.NowLogos, entry.Id);
            Assert.IsFalse(player.Logos.Contains(L.NowLogos));

            var shrine = new Logos(entry) { Position = player.Position, MapContextId = player.MapContextId, RuntimeMapChannel = map };
            var manager = new DynamicObjectManager(harness.Context, missionManager: harness.Manager,
                characterManager: new CharacterManager(harness.Context, harness.Manager));
            map.DynamicObjects.Add(shrine);

            try
            {
                harness.Drain();
                shrine.TriggeredByPlayers.Add(harness.Client);
                manager.LogosRecovery(map, new ActionData(player, ActionId.UseObject, DynamicObjectManager.LogosUseArgId, 0) { SourceId = shrine.EntityId });

                Assert.IsTrue(player.Logos.Contains(L.NowLogos), "Logos Element learned");
                Assert.IsFalse(player.Logos.Contains(L.WasLogos), "not Element");
                Assert.AreEqual(L.NowLogos, harness.Drain().OfType<LogosStoneAddedPacket>().Single().LogosId);
                Assert.IsTrue(LogosStones.IsKnownLogos(L.NowLogos), "a Logos of the client's Tabula");
            }
            finally
            {
                map.DynamicObjects.Remove(shrine);
            }
        }

        private static Dictionary<uint, LogosEntry> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.Set<LogosEntry>().AsNoTracking().ToDictionary(row => row.Id);

        private static string Describe(LogosEntry row) =>
            $"{row.Id} {row.ClassId} {row.MapContextId} {row.PosX} {row.PosY} {row.PosZ} {row.Name}";
    }
}
