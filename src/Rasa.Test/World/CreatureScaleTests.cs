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
    using Rasa.Navigation;
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

        /// <summary>
        /// Cover (Managers.Cover) lays its sample points on a creature's height and sees from its
        /// eyes at it, its size times Cover.CreatureHeight. A wall 1.75 m high, halfway between
        /// two actors 20 m apart: a creature of its model's size is wholly behind it; one half
        /// again as big shows its top two sample points of six over it (2.57 m and 2.03 m: the
        /// rays from a player's eyes at 1.62 m clear the wall at 2.09 m and 1.82 m), so a hit on
        /// it does a third of its damage, not the tenth that gets through full cover.
        /// </summary>
        [TestMethod]
        public void ABiggerCreatureShowsMoreOfItselfOverCover()
        {
            using var world = new WorldTestContext();
            world.Map.Cover = Wall(1.75f);
            var player = world.CreateClient().Player;
            player.Position = Vector3.Zero;

            var plain = new Creature { Position = new Vector3(0, 0, -20) };
            var big = new Creature { Position = new Vector3(0, 0, -20), Scale = 1.5 };

            Assert.AreEqual(0.0, Cover.Visible(world.Map.Cover, Cover.EyeOf(player), Cover.SamplePoints(plain, player.Position)));
            Assert.AreEqual(Cover.MinModifier, Cover.Modifier(world.Map, player, plain), 1e-9, "full cover");

            Assert.AreEqual(2.0 / 6.0, Cover.Visible(world.Map.Cover, Cover.EyeOf(player), Cover.SamplePoints(big, player.Position)), 1e-9);
            Assert.AreEqual(1.0 / 3.0, Cover.Modifier(world.Map, player, big), 1e-9, "two of six points in the clear");
            Assert.AreEqual(2.7f, Cover.HeightOf(big), 1e-5f);
        }

        /// <summary>
        /// A creature sees from its eyes (Cover.EyeOf, 0.9 of its height): of its model's size, at
        /// 1.62 m, it cannot see a player behind that wall (the top of the player, 1.71 m, is
        /// behind it at 1.67 m); half again as big, at 2.43 m, it sees the player's head over it.
        /// </summary>
        [TestMethod]
        public void ABiggerCreatureSeesOverCover()
        {
            using var world = new WorldTestContext();
            var wall = Wall(1.75f);
            var player = world.CreateClient().Player;
            player.Position = new Vector3(0, 0, -20);

            var plain = new Creature { Position = Vector3.Zero };
            var big = new Creature { Position = Vector3.Zero, Scale = 1.5 };

            Assert.IsFalse(BehaviorManager.Sees(wall, plain, player));
            Assert.IsTrue(BehaviorManager.Sees(wall, big, player));
            Assert.AreEqual(2.43f, Cover.EyeOf(big).Y, 1e-5f);
        }

        /// <summary>A wall across z = -10, 20 m wide, <paramref name="height"/> high.</summary>
        private static CoverMesh Wall(float height) =>
            CoverMesh.Build(new float[] { -10, 0, -10, -10, height, -10, 10, height, -10, 10, 0, -10 }, new[] { 0, 1, 2, 0, 2, 3 });

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
