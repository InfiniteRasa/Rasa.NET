using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader;
    using Rasa.Structures;

    /// <summary>Every creature class's flags as the monster flag editor sets them (Creature_flags, CreatureFlagEdits).</summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureFlagEditsTests
    {
        private const string Migration = "20270114000000_Creature_flags";
        private const string Previous = "20270113000000_Wilderness_reward_consumables";

        private const uint HominisMachina = 3868;
        private const uint HolographicForeanMachina = 25606;
        private const uint Necromite = 6029;
        private const uint Predator = 3902;
        private const uint AfsTurret = 4064;
        private const uint ThraxSoldierPistol = 3762;

        [TestMethod]
        public void EachClassGetsTheEditorsFlagsAndKeepsTheWrecks()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rows(database);
            migrator.Migrate(Migration);
            var after = Rows(database);

            Assert.AreEqual(3612, CreatureFlagEdits.Edits.Sum(edit => edit.Add.Length));
            Assert.AreEqual(14, CreatureFlagEdits.Edits.Sum(edit => edit.Removed.Length));
            Assert.AreEqual(340, CreatureFlagEdits.Edits.Length);
            Assert.AreEqual(before.Count + 3612 - 14, after.Count);

            foreach (var edit in CreatureFlagEdits.Edits)
            {
                foreach (var flag in edit.Add)
                    Assert.IsTrue(after.Contains((edit.ClassId, flag)), $"class {edit.ClassId} gains {(CreatureFlag)flag}");
                foreach (var flag in edit.Removed)
                    Assert.IsFalse(after.Contains((edit.ClassId, flag)), $"class {edit.ClassId} loses {(CreatureFlag)flag}");
            }

            // Nothing else changes: what the export did not know of stays, the wrecks' meshes among it.
            var touched = CreatureFlagEdits.Edits.SelectMany(edit => edit.Add.Concat(edit.Removed).Select(flag => (edit.ClassId, flag))).ToHashSet();
            CollectionAssert.AreEquivalent(before.Where(row => !touched.Contains(row)).ToList(), after.Where(row => !touched.Contains(row)).ToList());
            Assert.IsTrue(after.Contains((AfsTurret, (uint)CreatureFlag.AltMesh)));
            Assert.IsTrue(after.Contains((Predator, (uint)CreatureFlag.AltMeshDelayed3500)));

            // A Machina is MACHINA alone; the Necromite and the Predator are machines.
            Assert.IsTrue(after.Contains((HominisMachina, (uint)CreatureFlag.Machina)));
            Assert.IsFalse(after.Contains((HominisMachina, (uint)CreatureFlag.Mechanical)));
            foreach (var machine in new[] { Necromite, Predator })
            {
                Assert.IsTrue(after.Contains((machine, (uint)CreatureFlag.Mechanical)), $"{machine}");
                Assert.IsFalse(after.Contains((machine, (uint)CreatureFlag.Biological)), $"{machine}");
            }
        }

        [TestMethod]
        public void NoClassHasTwoOfAKindThatIsOneOnly()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate(Migration);
            var byClass = Rows(database).GroupBy(row => row.ClassId).ToDictionary(g => g.Key, g => g.Select(row => (CreatureFlag)row.FlagId).ToList());

            CreatureFlag[][] oneOnly =
            {
                Group("Powerlevel"), Group("XpModifier"), Group("ArmorLevel"), Group("ChitinArmor"), Group("Kingdom"), Group("Sentience"), Group("Origin"),
            };

            foreach (var (classId, flags) in byClass)
                foreach (var group in oneOnly)
                    Assert.IsTrue(flags.Count(group.Contains) <= 1, $"class {classId}: {string.Join(", ", flags.Where(group.Contains))}");

            // The one class that is two substances, as the editor has it.
            var substances = new[] { CreatureFlag.Biological, CreatureFlag.Machina, CreatureFlag.Mechanical };
            CollectionAssert.AreEqual(new[] { HolographicForeanMachina },
                byClass.Where(pair => pair.Value.Count(substances.Contains) > 1).Select(pair => pair.Key).ToArray());
        }

        [TestMethod]
        public void TheUpRunsTwiceAndTheDownPutsTheSeedBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            var before = Rows(database);

            migrator.Migrate(Migration);
            var once = Rows(database);
            Assert.AreNotEqual(before.Count, once.Count);

            foreach (var sql in CreatureFlagEdits.Up())
                database.Context.Database.ExecuteSqlRaw(sql.Replace("{", "{{").Replace("}", "}}"));
            CollectionAssert.AreEquivalent(once.ToList(), Rows(database).ToList(), "twice");

            migrator.Migrate(Previous);
            CollectionAssert.AreEquivalent(before.ToList(), Rows(database).ToList(), "Down puts the seed back");
        }

        [TestMethod]
        public void OnMySqlTheFlagsGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Migration);
            var down = migrator.GenerateScript(Migration, Previous);

            foreach (var sql in CreatureFlagEdits.Up())
                StringAssert.Contains(up, sql);
            StringAssert.Contains(up, $"'{Migration}'");
            foreach (var sql in CreatureFlagEdits.Down())
                StringAssert.Contains(down, sql);
        }

        [TestMethod]
        public void TheServerLoadsThemAndSendsThemToTheClient()
        {
            using var harness = Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create();
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            var machina = classes[(EntityClasses)HominisMachina].CreatureFlags;
            CollectionAssert.Contains(machina, CreatureFlag.Machina);
            CollectionAssert.DoesNotContain(machina, CreatureFlag.Mechanical);

            var thrax = classes[(EntityClasses)ThraxSoldierPistol].CreatureFlags;
            foreach (var flag in new[] { CreatureFlag.IsBane, CreatureFlag.OriginThrax, CreatureFlag.Powerlevel2Minion, CreatureFlag.XpModifier3Normal, CreatureFlag.CanBeRevived, CreatureFlag.Biological })
                CollectionAssert.Contains(thrax, flag, $"{flag}");

            // CreatureInfo carries the class's list as it is.
            var soldier = new Creature { EntityClass = (EntityClasses)ThraxSoldierPistol };
            CollectionAssert.AreEquivalent(thrax.ConvertAll(flag => (int)flag), CreatureManager.CreatureFlagsOf(soldier));
        }

        [TestMethod]
        public void AMachinaIsHarvestedEitherWayAndTheOthersOneWay()
        {
            static Creature Of(params CreatureFlag[] flags)
            {
                var creature = new Creature { EntityClass = EntityClasses.HumanBaseMale };
                creature.ExtraFlags.AddRange(flags);
                return creature;
            }

            var machina = Of(CreatureFlag.Machina);
            Assert.IsNull(ToolActionManager.SubstanceRefusal(machina, Harvest.SalvageArg));
            Assert.IsNull(ToolActionManager.SubstanceRefusal(machina, Harvest.TissueExtractionArg));

            var machine = Of(CreatureFlag.Mechanical);
            Assert.IsNull(ToolActionManager.SubstanceRefusal(machine, Harvest.SalvageArg));
            Assert.AreEqual(PlayerMessage.PmTargetInvalid, ToolActionManager.SubstanceRefusal(machine, Harvest.TissueExtractionArg));

            var beast = Of(CreatureFlag.Biological);
            Assert.AreEqual(PlayerMessage.PmTargetInvalid, ToolActionManager.SubstanceRefusal(beast, Harvest.SalvageArg));
            Assert.IsNull(ToolActionManager.SubstanceRefusal(beast, Harvest.TissueExtractionArg));

            var unknown = Of();
            Assert.AreEqual(PlayerMessage.PmHarvestFailNotHarvestable, ToolActionManager.SubstanceRefusal(unknown, Harvest.SalvageArg));
            Assert.AreEqual(PlayerMessage.PmHarvestFailNotHarvestable, ToolActionManager.SubstanceRefusal(unknown, Harvest.TissueExtractionArg));
        }

        private static CreatureFlag[] Group(string prefix) =>
            System.Enum.GetValues<CreatureFlag>().Where(flag => flag.ToString().StartsWith(prefix, System.StringComparison.Ordinal)).ToArray();

        private static HashSet<(uint ClassId, uint FlagId)> Rows(OrdinaryLootTestDatabase database) =>
            database.Context.Database.SqlQueryRaw<string>("SELECT class_id || ' ' || flag_id AS Value FROM creature_class_flag").AsEnumerable()
                .Select(row => row.Split(' '))
                .Select(parts => (uint.Parse(parts[0]), uint.Parse(parts[1])))
                .ToHashSet();
    }
}
