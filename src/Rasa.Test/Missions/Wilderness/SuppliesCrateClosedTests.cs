using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader.Missions.Wilderness;
    using Rasa.Test.Database;
    using Rasa.Test.World;

    /// <summary>
    /// Supplies On The Double (428)'s crate is set down closed, a state its class has, where it
    /// was set down in state 0, which it does not (Supplies_crate_closed).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SuppliesCrateClosedTests
    {
        private const string Before = "20261209000000_Ranja_egg_clusters_hatch";
        private const string Migration = "20261210000000_Supplies_crate_closed";

        [TestMethod]
        public void TheCrateIsSetDownClosedAndAClientIsShownItSo()
        {
            using var harness = WildernessAliaBranchesTests.CreateHarness();
            harness.SpawnWorld(510004);
            WildernessAliaBranchesTests.SeedCompletedHistory(harness, 1407, 1069, 479);
            var npcs = new NpcManager(harness, harness.Manager);
            WildernessAliaBranchesTests.Accept(harness, npcs, 510004, 428);

            var crate = harness.Map.DynamicObjects.Single(obj => obj.SceneMissionId == 428 && obj.SceneActorRole == "supplies");
            Assert.AreEqual(UseObjectState.TdStateClosed, crate.StateId);

            WorldTestContext.Drain(harness.Client);
            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(harness.Client, crate);
            var create = WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == crate.EntityId);
            Assert.AreEqual(UseObjectState.TdStateClosed, create.EntityData.OfType<UsableInfoPacket>().Single().CurState, "USE_TD_STATE_CLOSED, not 0");
        }

        [TestMethod]
        public void TheSceneHasTheCrateClosedAndDownPutsItBack()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);

                Assert.AreEqual(SuppliesCrateClosed.ClosedState, CrateState(world));

                world.GetService<IMigrator>().Migrate(Before);
                Assert.AreEqual(0u, CrateState(world));

                world.GetService<IMigrator>().Migrate(Migration);
                Assert.AreEqual(SuppliesCrateClosed.ClosedState, CrateState(world));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameChangeOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "\"initialObjectState\":200");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "\"initialObjectState\":0");
        }

        private static uint CrateState(WorldContext world)
        {
            using var command = world.Database.GetDbConnection().CreateCommand();
            world.Database.OpenConnection();
            command.CommandText = "select bindings from mission_scene_binding where mission_id = 428";
            var bindings = (string)command.ExecuteScalar();

            using var document = JsonDocument.Parse(bindings);
            var crate = document.RootElement.GetProperty("actors").GetProperty("supplies");
            Assert.AreEqual(26721u, crate.GetProperty("templateId").GetUInt32());
            return crate.GetProperty("initialObjectState").GetUInt32();
        }
    }
}
