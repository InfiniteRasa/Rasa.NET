using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Data;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// The tool templates lose the guns' placeholder alternate attack and take their action's
    /// range (ToolTemplatesNoAltFire); the guns keep theirs.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ToolTemplatesNoAltFireTests
    {
        private const string Migration = "20270125000000_Tool_templates_no_alt_fire";
        private const string Before = "20270124000000_Weapon_windup_signed";

        private sealed record Row(uint Id, uint ToolType, uint AltAction, uint AltArg, uint AltDamage, uint AltType, uint AltRange, uint AltRadius, uint AltAeType, uint Range);

        [TestMethod]
        public void EveryToolLosesItsAltAttackAndTakesItsActionsRangeAndDownPutsThemBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Before);
            var before = Rows(database);
            var tools = before.Where(row => row.ToolType >= 1 && row.ToolType <= 6).ToList();
            Assert.HasCount(200, tools, "the 200 tool rows");
            Assert.IsTrue(tools.All(row => (row.AltAction, row.AltArg, row.AltDamage, row.Range) == (1u, 133u, 25u, 80u)), "all on the placeholder");

            migrator.Migrate(Migration);
            var after = Rows(database);

            foreach (var row in after.Where(row => row.ToolType >= 1 && row.ToolType <= 6))
            {
                Assert.AreEqual((0u, 0u, 0u, 0u, 0u, 0u, 0u), (row.AltAction, row.AltArg, row.AltDamage, row.AltType, row.AltRange, row.AltRadius, row.AltAeType), $"template {row.Id}");
                Assert.IsTrue(ToolTemplatesNoAltFire.Tools.Any(tool => tool.ToolType == row.ToolType && tool.Range == row.Range), $"template {row.Id}: range {row.Range}");
            }

            CollectionAssert.AreEquivalent(new[] { 5u, 10u, 20u, 30u },
                after.Where(row => row.ToolType >= 1 && row.ToolType <= 6).Select(row => row.Range).Distinct().ToList());
            CollectionAssert.AreEqual(before.Where(row => row.ToolType < 1 || row.ToolType > 6).ToList(),
                after.Where(row => row.ToolType < 1 || row.ToolType > 6).ToList(), "the guns as they were");

            foreach (var sql in ToolTemplatesNoAltFire.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(after, Rows(database), "a second run changes nothing");

            migrator.Migrate(Before);
            CollectionAssert.AreEqual(before, Rows(database));
        }

        [TestMethod]
        public void TheHealingDiscsAreThirtyDirectAndTenRadial()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);

            var byAction = database.Context.Database.SqlQueryRaw<string>(
                "SELECT (wc.attack_action_id || '/' || wc.attack_action_arg_id || '=' || w.`range`) AS Value FROM itemtemplate_weapon w " +
                "JOIN itemtemplate_itemclass ic ON ic.itemTemplateId = w.id JOIN weaponclass wc ON wc.id = ic.itemClassId " +
                "WHERE w.tool_type BETWEEN 1 AND 6").AsEnumerable().Distinct().OrderBy(value => value).ToList();

            CollectionAssert.AreEquivalent(new[] { "147/1=30", "147/5=10", "172/168=5", "172/169=5", "198/1=30", "198/3=10", "199/1=20", "258/14=5" }, byAction);
        }

        [TestMethod]
        public void OnMySqlTheRowsChangeAndComeBack()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "UPDATE itemtemplate_weapon SET alt_action_id = 0, alt_action_arg_id = 0, alt_max_damage = 0, alt_damage_type = 0, alt_range = 0, alt_ae_radius = 0, alt_ae_type = 0, `range` = 30 WHERE tool_type = 1");
            StringAssert.Contains(up, "WHERE wc.attack_action_id = 147 AND wc.attack_action_arg_id = 5");
            StringAssert.Contains(up, $"'{Migration}'");
            StringAssert.Contains(down, "SET alt_action_id = 1, alt_action_arg_id = 133, alt_max_damage = 25, alt_damage_type = 1, alt_range = 80, alt_ae_radius = 1, alt_ae_type = 1, `range` = 80 WHERE tool_type = 6");
        }

        /// <summary>A weapon with no alternate attack sends its alt pair as None, so the client builds no alt action; one with one sends it.</summary>
        [TestMethod]
        [DataRow(0u, 0u, false)]
        [DataRow(174u, 5u, true)]
        public void WeaponInfoSendsNoAltPairForAWeaponWithNone(uint altAction, uint altArg, bool sent)
        {
            var weaponClass = new EntityClass(7614, "fixture", 0, 1, new() { AugmentationType.Weapon }, true)
            {
                WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry { Id = 7614, ClipSize = 5, WeaponAnimConditionCode = 1 })
            };
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 630, ItemClass = 7614 })
            {
                WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry { Id = 630, AmmoPerShot = 1, ToolType = 1, AltActionId = altAction, AltActionArgId = altArg })
            };

            using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(new WeaponInfoPacket(new Item { ItemTemplate = template }, weaponClass)))));
            Assert.AreEqual(17, reader.ReadTuple());
            for (var i = 0; i < 5; i++)
                reader.SkipValue();

            if (sent)
            {
                Assert.AreEqual((int)altAction, reader.ReadInt());
                Assert.AreEqual((int)altArg, reader.ReadInt());
            }
            else
            {
                reader.ReadNoneStruct();    // altActionId: throws if anything else
                reader.ReadNoneStruct();    // altActionArg
            }
        }

        private static List<Row> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.ItemTemplateWeaponEntries.AsNoTracking().AsEnumerable()
                .Select(w => new Row(w.Id, w.ToolType, w.AltActionId, w.AltActionArgId, w.AltMaxDamage, w.AltDamageType, w.AltRange, w.AltAeRadius, w.AltAeType, w.Range))
                .OrderBy(row => row.Id).ToList();
    }
}
