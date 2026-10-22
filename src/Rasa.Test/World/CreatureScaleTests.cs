using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A creature's own body size (creature_scale, CreatureScaleEntry): read with the creature
    /// rows, kept by every creature made from the row, and sent as its BodyAttributes scale. A
    /// creature with no row, or a row that is no size, is its model's own size, 1.0.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureScaleTests
    {
        private const string Migration = "20270315000000_Add_creature_scale";
        private const string Before = "20270123000000_Thunderhead_base_teleporters";

        /// <summary>Three Wilderness creature rows, each alone on its own pool (pools 4, 5, 6).</summary>
        private const uint Sized = 4, NoSize = 5, Plain = 6;

        [TestMethod]
        public void ACreatureWithASizeOfItsOwnIsMadeAndSentAtIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create(migration =>
            {
                migration.Sql($"INSERT INTO {CreatureScaleEntry.TableName} (id, scale) VALUES ({Sized}, 1.5);");
                migration.Sql($"INSERT INTO {CreatureScaleEntry.TableName} (id, scale) VALUES ({NoSize}, 0);");
            });

            var templates = CreatureManager.Instance.LoadedCreatures;

            Assert.AreEqual(1.5, templates[Sized].Scale);
            Assert.AreEqual(1.0, templates[NoSize].Scale, "a scale of 0 is no size: the model's own");
            Assert.AreEqual(1.0, templates[Plain].Scale, "no row: the model's own");

            // Made from the row, as a pool makes it: the copy keeps it.
            var made = new Creature(templates[Sized]);

            Assert.AreEqual(1.5, made.Scale);

            // And the client is told it.
            made.MapContextId = harness.Map.MapInfo.MapContextId;
            made.RuntimeMapChannel = harness.Map;
            made.Position = harness.Client.Player.Position;
            EntityManager.Instance.RegisterEntity(made.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(made);
            EntityManager.Instance.RegisterActor(made.EntityId, made);

            WorldTestContext.Drain(harness.Client);
            CreatureManager.Instance.CreateCreatureOnClient(harness.Client, made);

            var created = WorldTestContext.Drain(harness.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet)
                .OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == made.EntityId);

            Assert.AreEqual(1.5, created.EntityData.OfType<BodyAttributesPacket>().Single().Scale);
        }

        [TestMethod]
        public void TheTableComesWithItsMigrationAndGoesWithIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Tables() => world.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = '{CreatureScaleEntry.TableName}'")
                .AsEnumerable().Single();

            Assert.AreEqual(1, Tables());
            Assert.AreEqual(0, world.Set<CreatureScaleEntry>().AsNoTracking().Count(), "no creature has a size of its own yet");

            world.GetService<IMigrator>().Migrate(Before);
            Assert.AreEqual(0, Tables());

            world.GetService<IMigrator>().Migrate(Migration);
            Assert.AreEqual(1, Tables());
        }
    }
}
