extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.Char.Clan;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// A clan's name is one clan's whatever its case, on SQLite as on MySQL, and the database
    /// holds it so (the clan_index_name unique index): SQLite compared case, so "Wolves" and
    /// "wolves" could both be made there, and nothing in the database stopped a name checked
    /// free from being taken before it was written.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ClanNameTests
    {
        private const string Before = "20261213000000_Add_crafting_job";

        [TestMethod]
        public void ANameIsTakenInAnyCaseAndTheDatabaseRefusesASecond()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            using var unit = context.CreateChar();

            var wolves = unit.Clans.CreateClan("Wolves", false);
            Assert.AreEqual(wolves.Id, unit.Clans.GetClanByName("wolves")?.Id);
            Assert.AreEqual(wolves.Id, unit.Clans.GetClanByName("WOLVES")?.Id);
            Assert.IsNull(unit.Clans.GetClanByName("Wolf"));

            Assert.IsNull(unit.Clans.CreateClan("wOlVeS", false), "refused by the index, not thrown");

            var ravens = unit.Clans.CreateClan("Ravens", false);
            Assert.IsNotNull(ravens, "the failed insert left nothing behind to fail the next");
            Assert.IsFalse(unit.Clans.UpdateClanName(ravens.Id, "WOLVES"));
            Assert.AreEqual("Ravens", unit.Clans.GetClanById(ravens.Id).Name);

            Assert.IsTrue(unit.Clans.UpdateClanName(wolves.Id, "WOLVES"), "its own name in other capitals");
            CollectionAssert.AreEquivalent(new[] { "WOLVES", "Ravens" }, unit.Clans.GetClans().Select(clan => clan.Name).ToArray());
        }

        [TestMethod]
        public void ALeaderMayNotRenameIntoAnotherClansNameInOtherCapitalsButMayRecapitaliseTheirOwn()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var wolvesLeader = context.Client;
            var ravensLeader = context.CreateAdditionalClient(2);
            var wolves = Clan(context, "Wolves", wolvesLeader.Player.Id);
            var ravens = Clan(context, "Ravens", ravensLeader.Player.Id);

            wolvesLeader.Player.ClanId = wolves.Id;
            ravensLeader.Player.ClanId = ravens.Id;
            wolvesLeader.State = ClientState.Ingame;
            ravensLeader.State = ClientState.Ingame;
            WorldTestContext.Drain(wolvesLeader);
            WorldTestContext.Drain(ravensLeader);

            var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });
            clans.Clans[wolves.Id] = new Lazy<ClanEntry>(() => wolves);
            clans.Clans[ravens.Id] = new Lazy<ClanEntry>(() => ravens);

            clans.ChangeClanName(ravensLeader, new ChangeClanNamePacket { ClanName = "wolves" });

            var told = Messages(ravensLeader).Single();
            Assert.AreEqual((int)PlayerMessage.PmClanNameNotAvailable, told.MsgId);
            Assert.AreEqual("wolves", told.Args["clanname"]);
            Assert.AreEqual("Ravens", NameOf(context, ravens.Id));

            clans.ChangeClanName(wolvesLeader, new ChangeClanNamePacket { ClanName = "WOLVES" });

            Assert.IsFalse(Messages(wolvesLeader).Any(message => message.MsgId == (int)PlayerMessage.PmClanNameNotAvailable));
            Assert.AreEqual("WOLVES", NameOf(context, wolves.Id));
        }

        [TestMethod]
        public void TheMigrationRenamesTheLaterOfTwoNamesThatDifferOnlyInCase()
        {
            WithDatabase(characters =>
            {
                characters.GetService<IMigrator>().Migrate(Before);

                var clans = new ClanRepository(characters);
                var wolves = clans.CreateClan("Wolves", false);
                var shouting = clans.CreateClan("WOLVES", false);
                var ravens = clans.CreateClan("Ravens", true);
                var quiet = clans.CreateClan("wolves", false);
                characters.ChangeTracker.Clear();

                characters.Database.Migrate();

                var names = characters.ClanEntries.AsNoTracking().ToDictionary(clan => clan.Id, clan => clan.Name);
                Assert.AreEqual("Wolves", names[wolves.Id], "the oldest keeps it");
                Assert.AreEqual($"WOLVES ({shouting.Id})", names[shouting.Id]);
                Assert.AreEqual("Ravens", names[ravens.Id]);
                Assert.AreEqual($"wolves ({quiet.Id})", names[quiet.Id]);

                Assert.IsNull(new ClanRepository(characters).CreateClan("RAVENS", false), "the index is there");
                characters.ChangeTracker.Clear();

                // Back: the index goes, the names stay.
                characters.GetService<IMigrator>().Migrate(Before);
                Assert.IsNotNull(new ClanRepository(characters).CreateClan("RAVENS", false));
                Assert.AreEqual($"WOLVES ({shouting.Id})", characters.ClanEntries.AsNoTracking().Single(clan => clan.Id == shouting.Id).Name);
            });
        }

        [TestMethod]
        public void OnMySqlTheIndexIsTheColumnsOwnCollation()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlCharContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, "20261214000000_Clan_name_unique");
            var down = migrator.GenerateScript("20261214000000_Clan_name_unique", Before);

            StringAssert.Contains(up, "SET clan.name = CONCAT(clan.name, ' (', clan.id, ')');");
            StringAssert.Contains(up, "CREATE UNIQUE INDEX `clan_index_name` ON `clan` (`name`)");
            StringAssert.Contains(down, "DROP INDEX `clan_index_name`");
        }

        [TestMethod]
        public void ACreatedNameIsTrimmedBeforeItsRulesAndItsRow()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var founder = context.Client;
            var latecomer = context.CreateAdditionalClient(2);

            // The fee is taken from the row as well as the purse, and the two must agree.
            using (var database = context.Open())
            {
                foreach (var client in new[] { founder, latecomer })
                    database.CharacterEntries.Single(entry => entry.Id == client.Player.Id).Credit = 20000;

                database.SaveChanges();
            }

            foreach (var client in new[] { founder, latecomer })
            {
                client.State = ClientState.Ingame;
                client.Player.Credits[CurencyType.Credits] = 20000;
                WorldTestContext.Drain(client);
            }

            WithClans(context, clans =>
            {
                clans.CreateClan(founder, new CreateClanPacket { ClanName = "  Wolves  ", IsPvP = false });

                Assert.AreNotEqual(0U, founder.Player.ClanId, "created");
                Assert.AreEqual("Wolves", NameOf(context, founder.Player.ClanId), "stored without its spaces");

                // The same name with spaces around it is the same name.
                clans.CreateClan(latecomer, new CreateClanPacket { ClanName = " wolves ", IsPvP = false });

                var told = Messages(latecomer).Single();
                Assert.AreEqual((int)PlayerMessage.PmClanNameNotAvailable, told.MsgId);
                Assert.AreEqual("wolves", told.Args["clanname"]);
                Assert.AreEqual(0U, latecomer.Player.ClanId);
                Assert.AreEqual(20000, latecomer.Player.Credits[CurencyType.Credits], "nothing taken");

                // Spaces do not make up the length.
                clans.CreateClan(latecomer, new CreateClanPacket { ClanName = "  ab  ", IsPvP = false });

                Assert.AreEqual((int)PlayerMessage.PmClanNameRequired, Messages(latecomer).Single().MsgId);

                using var unit = context.CreateChar();
                Assert.HasCount(1, unit.Clans.GetClans());
            });
        }

        /// <summary>A clan manager over the context's database in place of the instance, and an inventory manager over it.</summary>
        private static void WithClans(MissionTestContext context, Action<ClanManager> body)
        {
            var instance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            var inventoryInstance = typeof(InventoryManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousInventory = inventoryInstance.GetValue(null);

            try
            {
                inventoryInstance.SetValue(null, new InventoryManager(context));

                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                instance.SetValue(null, clans);
                body(clans);
            }
            finally
            {
                instance.SetValue(null, previous);
                inventoryInstance.SetValue(null, previousInventory);
            }
        }

        private static ClanEntry Clan(MissionTestContext context, string name, uint leaderId)
        {
            using var unit = context.CreateChar();
            var clan = unit.Clans.CreateClan(name, false);

            Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, leaderId, ClanRank.Leader, ""));

            return clan;
        }

        private static string NameOf(MissionTestContext context, uint clanId)
        {
            using var unit = context.CreateChar();

            return unit.Clans.GetClanById(clanId).Name;
        }

        private static List<DisplayClanMessagePacket> Messages(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>()
                .Select(c => c.Packet).OfType<DisplayClanMessagePacket>().ToList();

        private static void WithDatabase(Action<SqliteCharContext> body)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);

            try
            {
                using var context = (SqliteCharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), Path.Combine(path, "database"));
                body(context);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                try
                {
                    Directory.Delete(path, true);
                }
                catch (IOException)
                {
                }
            }
        }
    }
}
