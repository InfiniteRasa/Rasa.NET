using System.Collections.Generic;
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
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// A creature's own body tints (creature_tint, CreatureTintEntry): read with the creature rows,
    /// kept by every creature made from the row, given to each one as it spawns and sent as its
    /// BodyAttributes hue and hue2. A row with no hue is no tint (None); a creature with no row
    /// is tinted as before (Creature.TintAtSpawn). Valverde_creature_tints writes the Valverde rows.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureTintRowTests
    {
        private const string Schema = "20271107000000_Add_creature_tint";
        private const string Data = "20271107000100_Valverde_creature_tints";

        /// <summary>
        /// Two FRIENDLY Wilderness creature rows (as CreatureScaleTests uses them), so the row's
        /// tints win over the FRIENDLY none, and a hostile one, Bane_Thrax_Soldier (as CreatureTintTests).
        /// </summary>
        private const uint Tinted = 4, Untinted = 5, Plain = 3;

        /// <summary>(10, 200, 30), alpha 255, as Color.Hue packs it: A B G R.</summary>
        private const uint Green = 0xFF1EC80A;

        [TestMethod]
        public void ACreatureWithATintRowSpawnsWithItsTintsAndIsSentThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create(migration =>
            {
                migration.Sql($"INSERT INTO {CreatureTintEntry.TableName} (id, hue, hue2) VALUES ({Tinted}, {Green}, {Green});");
                migration.Sql($"INSERT INTO {CreatureTintEntry.TableName} (id, hue, hue2) VALUES ({Untinted}, NULL, NULL);");
            });

            var templates = CreatureManager.Instance.LoadedCreatures;

            Assert.AreEqual(((uint?)Green, (uint?)Green), templates[Tinted].RowTint);
            Assert.AreEqual(((uint?)null, (uint?)null), templates[Untinted].RowTint);
            Assert.IsNull(templates[Plain].RowTint, "no row");
            Assert.IsNull(templates[Tinted].Hue, "the template has not spawned");

            var tinted = CreatureManager.Instance.CreateCreature(Tinted, null);
            var untinted = CreatureManager.Instance.CreateCreature(Untinted, null);
            var plain = CreatureManager.Instance.CreateCreature(Plain, null);

            Assert.AreEqual((byte)10, tinted.Hue.Red);
            Assert.AreEqual((byte)200, tinted.Hue.Green);
            Assert.AreEqual((byte)30, tinted.Hue.Blue);
            Assert.AreEqual((byte)255, tinted.Hue.Alpha);
            Assert.AreEqual(Green, tinted.Hue.Hue);
            Assert.AreEqual(Green, tinted.Hue2.Hue);
            Assert.AreEqual(Green, new Creature(tinted).RowTint?.Hue, "a copy keeps its row's tints, as its size");
            Assert.IsNull(new Creature(tinted).Hue, "a copy has not spawned");

            Assert.IsNull(untinted.Hue, "a row with no hue: stock colours");
            Assert.IsNull(untinted.Hue2);

            Assert.AreEqual(TargetCategory.Friendly, tinted.TargetCategory);
            Assert.AreNotEqual(TargetCategory.Friendly, plain.TargetCategory);
            Assert.IsNotNull(plain.Hue, "no row: two random tints, as before");
            Assert.IsNotNull(plain.Hue2);

            // And the client is told them.
            Assert.AreEqual(Green, Shown(harness, tinted).Hue.Hue);
            Assert.AreEqual(Green, Shown(harness, tinted).Hue2.Hue);
            Assert.IsNull(Shown(harness, untinted).Hue, "None: the client paints no tint over the model");
            Assert.IsNull(Shown(harness, untinted).Hue2);
        }

        [TestMethod]
        public void ARowWithAHueAndNoHue2IsTheHueTwice()
        {
            var creature = new Creature { RowTint = (Green, null) };
            creature.TintAtSpawn();

            Assert.AreEqual(Green, creature.Hue.Hue);
            Assert.AreEqual(Green, creature.Hue2.Hue);
            Assert.AreNotSame(creature.Hue, creature.Hue2);
        }

        [TestMethod]
        public void AFriendlyCreatureWithATintRowHasItsTints()
        {
            var creature = new Creature { TargetCategory = TargetCategory.Friendly, RowTint = (Green, Green) };
            creature.TintAtSpawn();

            Assert.AreEqual(Green, creature.Hue.Hue);
        }

        [TestMethod]
        public void AnAmoeboidChildOfATintedParentHasStockColoursAndOfAnUntintedOneRandomOnes()
        {
            var ofTinted = new Creature { RowTint = AmoeboidVomit.ChildRowTint(new Creature { RowTint = (Green, Green) }) };
            ofTinted.TintAtSpawn();

            Assert.IsNull(ofTinted.Hue, "Goriam's child: no tint, an amoeboid's stock colours");
            Assert.IsNull(ofTinted.Hue2);

            var ofPlain = new Creature { RowTint = AmoeboidVomit.ChildRowTint(new Creature()) };
            ofPlain.TintAtSpawn();

            Assert.IsNull(ofPlain.RowTint, "a parent with no row: the child has none either");
            Assert.IsNotNull(ofPlain.Hue, "two random tints, as before");
            Assert.IsNotNull(ofPlain.Hue2);
        }

        [TestMethod]
        public void TheTableComesWithItsMigrationAndGoesWithIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();

            Assert.AreEqual(1, Tables(world));

            migrator.Migrate(Previous(world, Schema));
            Assert.AreEqual(0, Tables(world));

            // Its migration makes the table and puts no tint in it: Valverde_creature_tints does.
            migrator.Migrate(Schema);
            Assert.AreEqual(1, Tables(world));
            Assert.AreEqual(0, world.Set<CreatureTintEntry>().AsNoTracking().Count());

            migrator.Migrate(Data);
            Assert.AreEqual(ValverdeCreatureTints.Rows.Count, world.Set<CreatureTintEntry>().AsNoTracking().Count());
        }

        [TestMethod]
        public void TheValverdeTintsAreWrittenAsListed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = Rows(harness.World);

            // Pinned from the colour table (tints.csv, 2026-11-06): 103 creatures, 57 of them stock (2026-11-06: officers 255 grey, Cainynix plum, the Boargar).
            Assert.AreEqual(103, ValverdeCreatureTints.Rows.Count);
            Assert.AreEqual(103, ValverdeCreatureTints.Rows.Select(row => row.CreatureId).Distinct().Count());
            Assert.AreEqual(57, ValverdeCreatureTints.Rows.Count(row => row.Hue == null));
            Assert.AreEqual(0xFF00086Eu, ValverdeCreatureTints.Rgb(110, 8, 0), "(110, 8, 0), alpha 255: A B G R");
            Assert.AreEqual(0xFF288C78u, rows[650248].Hue, "Grumble, (120, 140, 40)");
            Assert.AreEqual(0xFF00086Eu, rows[531082].Hue, "Thrax Soldier Mark III, Thrax red");
            Assert.AreEqual(0xFFFFFFFFu, rows[531086].Hue, "Thrax Commander, officer white-grey");
            Assert.AreEqual(0xFF5A0028u, rows[650247].Hue, "Cainynix, plum");
            Assert.IsNull(rows[44].Hue, "Young Forest Boargar, stock");
            Assert.AreEqual(0xFF1E003Cu, rows[520036].Hue, "Seymour, (60, 0, 30)");
            Assert.IsNull(rows[520053].Hue, "Mox Alpha Male, stock");

            Assert.AreEqual(ValverdeCreatureTints.Rows.Count, rows.Count);
            foreach (var (id, hue) in ValverdeCreatureTints.Rows)
            {
                Assert.AreEqual(hue, rows[id].Hue, $"creature {id} hue");
                Assert.AreEqual(hue, rows[id].Hue2, $"creature {id} hue2");
            }
        }

        [TestMethod]
        public void UpAndDownAreEachSafeTwiceAndDownLeavesAnEditedRow()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var migrator = world.GetService<IMigrator>();
            var before = Rows(world);

            // Up again: nothing more.
            Run(world, ValverdeCreatureTints.Up());
            CollectionAssert.AreEquivalent(before.ToList(), Rows(world).ToList());

            // A GM recolours one row; Down takes only the rows still as Up wrote them, twice.
            world.Database.ExecuteSqlRaw($"UPDATE {CreatureTintEntry.TableName} SET hue = {Green} WHERE id = 531082;");
            world.Database.ExecuteSqlRaw($"UPDATE {CreatureTintEntry.TableName} SET hue = {Green} WHERE id = 520053;");

            migrator.Migrate(Previous(world, Data));
            Run(world, ValverdeCreatureTints.Down());

            var left = Rows(world);
            CollectionAssert.AreEquivalent(new uint[] { 520053, 531082 }, left.Keys.ToList());
            Assert.AreEqual(Green, left[531082].Hue);

            // Up again keeps the GM's rows as they are and puts back the rest.
            migrator.Migrate(Data);
            Run(world, ValverdeCreatureTints.Up());

            var after = Rows(world);
            Assert.AreEqual(before.Count, after.Count);
            Assert.AreEqual(Green, after[531082].Hue);
            Assert.AreEqual(Green, after[520053].Hue);
            Assert.AreEqual(before[650248], after[650248]);
        }

        private static BodyAttributesPacket Shown(WildernessRuntimeTestHarness harness, Creature creature)
        {
            if (creature.RuntimeMapChannel == null)
            {
                creature.MapContextId = harness.Map.MapInfo.MapContextId;
                creature.RuntimeMapChannel = harness.Map;
                creature.Position = harness.Client.Player.Position;
                EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
                EntityManager.Instance.RegisterCreature(creature);
                EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            }

            WorldTestContext.Drain(harness.Client);
            CreatureManager.Instance.CreateCreatureOnClient(harness.Client, creature);

            return WorldTestContext.Drain(harness.Client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet)
                .OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == creature.EntityId)
                .EntityData.OfType<BodyAttributesPacket>().Single();
        }

        /// <summary>The migration directly before <paramref name="migration"/>, read from the assembly.</summary>
        private static string Previous(DbContext context, string migration)
        {
            var ids = context.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(migration);
            Assert.IsGreaterThan(0, index, "the migration is there, after another");
            return ids[index - 1];
        }

        private static int Tables(DbContext world) => world.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = '{CreatureTintEntry.TableName}'")
            .AsEnumerable().Single();

        private static Dictionary<uint, (uint? Hue, uint? Hue2)> Rows(DbContext world) =>
            world.Set<CreatureTintEntry>().AsNoTracking().ToList().ToDictionary(row => row.Id, row => (row.Hue, row.Hue2));

        private static void Run(DbContext world, IEnumerable<string> statements)
        {
            foreach (var sql in statements)
                world.Database.ExecuteSqlRaw(sql);
        }
    }
}
