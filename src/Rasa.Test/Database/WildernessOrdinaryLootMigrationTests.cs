using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Loot;
using Rasa.Managers;
using Rasa.Repositories.World;
using Rasa.Services.Preloader;
using Rasa.Structures.World;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Database
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessOrdinaryLootMigrationTests
    {
        internal const string ContentMigration = "20261203000200_Wilderness_ordinary_loot_v1";
        private const string ThraxRegular = "wilderness_v1_thrax_10_14";
        private static readonly uint[] Bosses =
        {
            75, 76, 77, 78, 79, 80, 82, 83, 84, 89, 90, 630040, 630130
        };
        private static readonly uint[] RequiredCreatures =
        {
            1, 2, 3, 33, 35, 41, 44, 47, 57, 58, 59, 60, 75, 76, 77, 78, 79, 80,
            82, 83, 84, 85, 87, 88, 89, 90, 630040, 630046, 630071, 630076, 630077,
            630078, 630079, 630100, 630120, 630130
        };

        [TestMethod]
        public void PairedContentMigrationsHaveIdenticalFrozenRowsAndExactKeyRollback()
        {
            using var sqlite = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), "unused");
            using var mysql = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var left = RequireMigration(sqlite);
            var right = RequireMigration(mysql);
            CollectionAssert.AreEqual(Describe(left.UpOperations), Describe(right.UpOperations));
            CollectionAssert.AreEqual(Describe(left.DownOperations), Describe(right.DownOperations));
            foreach (var migration in new[] { left, right })
            {
                Assert.IsTrue(migration.UpOperations.All(operation => operation is InsertDataOperation));
                var inserts = migration.UpOperations.Cast<InsertDataOperation>().ToArray();
                CollectionAssert.AreEqual(new[]
                {
                    "loot_profile", "ordinary_loot_group", "loot_item", "creature_loot_binding", "loot_map_policy"
                }, inserts.Select(operation => operation.Table).ToArray());
                CollectionAssert.AreEqual(new[] { 27, 83, 179, 36, 1 },
                    inserts.Select(operation => operation.Values.GetLength(0)).ToArray());
                var deletes = migration.DownOperations.Cast<DeleteDataOperation>().ToArray();
                CollectionAssert.AreEqual(new[]
                {
                    "creature_loot_binding", "loot_item", "loot_map_policy", "ordinary_loot_group", "loot_profile"
                }, deletes.Select(operation => operation.Table).ToArray());
                foreach (var delete in deletes)
                {
                    var insert = inserts.Single(operation => operation.Table == delete.Table);
                    CollectionAssert.AreEquivalent(Keys(insert, delete.KeyColumns), Rows(delete.KeyValues));
                }
            }
            Assert.IsFalse(sqlite.Database.HasPendingModelChanges());
            Assert.IsFalse(mysql.Database.HasPendingModelChanges());
        }

        [TestMethod]
        public void MySqlContentAndRollbackSqlGenerateOfflineWithoutNativeOrSchemaWrites()
        {
            using var mysql = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            RequireMigration(mysql);
            var migrator = mysql.GetService<IMigrator>();
            var up = migrator.GenerateScript(OrdinaryLootMigrationTests.SchemaMigration, ContentMigration);
            var down = migrator.GenerateScript(ContentMigration, OrdinaryLootMigrationTests.SchemaMigration);
            foreach (var table in new[] { "loot_profile", "ordinary_loot_group", "loot_item", "creature_loot_binding", "loot_map_policy" })
            {
                StringAssert.Contains(up, $"INSERT INTO `{table}`");
                StringAssert.Contains(down, $"DELETE FROM `{table}`");
            }
            StringAssert.Contains(up, "'wilderness_v1_thrax_10_14'");
            StringAssert.Contains(up, "44917");
            StringAssert.Contains(down, "`map_context_id` = 1220");
            foreach (var sql in new[] { up, down })
                foreach (var forbidden in new[] { "CREATE TABLE", "ALTER TABLE", "DROP TABLE", "UPDATE `", "INSERT INTO `itemtemplate`", "DELETE FROM `creature`" })
                    Assert.IsFalse(sql.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
            Assert.AreEqual(System.Data.ConnectionState.Closed, mysql.Database.GetDbConnection().State);
        }

        [TestMethod]
        public void FreshAndUpgradeRowsMatchAndRollbackPreservesUnownedContentAndNativeData()
        {
            using var fresh = new OrdinaryLootTestDatabase();
            using var upgrade = new OrdinaryLootTestDatabase();
            var context = upgrade.Context;
            var migrator = context.GetService<IMigrator>();
            RequireMigration(context);
            migrator.Migrate(OrdinaryLootMigrationTests.SchemaMigration);
            var native = NativeCounts(context);
            AddForeignContent(context);
            var foreign = Serialize(new OrdinaryLootRepository(context).Get());
            migrator.Migrate(ContentMigration);
            // Only what is dated after the content migration is still to come: this fork's own.
            Assert.IsFalse(context.Database.GetPendingMigrations()
                .Any(id => string.CompareOrdinal(id, ContentMigration) <= 0));
            fresh.Context.GetService<IMigrator>().Migrate();
            var expected = new OrdinaryLootRepository(fresh.Context).Get();
            var actual = new OrdinaryLootRepository(context).Get();
            Assert.AreEqual(Serialize(Owned(expected)), Serialize(Owned(actual)));
            CollectionAssert.AreEqual(native, NativeCounts(context));
            AssertFrozen(actual);

            migrator.Migrate(OrdinaryLootMigrationTests.SchemaMigration);
            context.ChangeTracker.Clear();
            Assert.AreEqual(foreign, Serialize(new OrdinaryLootRepository(context).Get()));
            CollectionAssert.AreEqual(native, NativeCounts(context));
            Assert.AreEqual(0L, context.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM pragma_foreign_key_check").Single());
            migrator.Migrate(ContentMigration);
            Assert.AreEqual(Serialize(Owned(expected)), Serialize(Owned(new OrdinaryLootRepository(context).Get())));
        }

        [TestMethod]
        public void FrozenContentCoversAllRequiredNativeSourcesAndCanActuallyMaterializeEveryCandidate()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            RequireMigration(harness.World);
            using var world = harness.CreateWorld();
            var content = world.OrdinaryLoot.Get();
            AssertFrozen(content);
            var catalog = OrdinaryLootCatalog.Load(world);
            Assert.IsNull(catalog.Resolve(1148, 3));
            foreach (var binding in content.Bindings)
                Assert.IsTrue(catalog.Resolve(binding.MapContextId, binding.CreatureId).Groups.Count > 0);
            Assert.ThrowsExactly<Rasa.Structures.GameplayRejectionException>(() => catalog.Resolve(1220, uint.MaxValue));

            var templates = world.Equipment.GetItemTemplates().ToDictionary(row => row.Id);
            var links = world.Equipment.GetItemTemplateClasses().ToDictionary(row => row.ItemTemplateId, row => row.ItemClass);
            var classes = world.Equipment.GetItemClasses().ToDictionary(row => row.Id);
            var requirements = world.Equipment.GetRequirementsGeneric().ToDictionary(row => row.Id);
            var skills = world.Equipment.GetRequirementsSkill().ToLookup(row => row.Id);
            var equipment = world.Equipment.GetEquipableClasses().Select(row => row.Id).ToHashSet();
            var creatures = harness.World.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var expectedClassLinks = new Dictionary<uint, uint>
            {
                [28] = 3147, [56] = 3807, [145] = 6048, [3057] = 11889, [3061] = 11893,
                [3072] = 11904, [11557] = 27120, [11561] = 6470, [11567] = 6495,
                [13066] = 15542, [13081] = 15557, [13716] = 27123, [13717] = 27626,
                [13736] = 16196, [13739] = 16201, [14763] = 27723, [17130] = 27121,
                [17132] = 27625, [17133] = 27122, [17149] = 27124, [17150] = 27627,
                [28548] = 16206, [41636] = 20277, [41639] = 20281, [41642] = 20284,
                [41643] = 20285, [41644] = 20286, [41645] = 20287, [41651] = 20293,
                [41652] = 20294, [41665] = 20306, [41666] = 20307, [41677] = 20318,
                [41678] = 20319, [42300] = 20881, [42301] = 20882, [42326] = 20907,
                [42327] = 20908, [42332] = 20913, [42333] = 20914, [42341] = 20922,
                [42342] = 20923, [42369] = 20950, [42370] = 20951, [44917] = 21969,
                [48545] = 27624, [50211] = 24579
            };
            CollectionAssert.AreEquivalent(expectedClassLinks.Keys.ToArray(),
                content.Items.Select(row => row.ItemTemplateId).Distinct().ToArray());
            foreach (var binding in Owned(content).Bindings)
            {
                var band = binding.ProfileKey.Replace("_boss", "").Split('_');
                var minimum = int.Parse(band[^2]);
                var maximum = int.Parse(band[^1]);
                Assert.IsTrue(creatures[binding.CreatureId].Level >= minimum && creatures[binding.CreatureId].Level <= maximum,
                    $"{binding.CreatureId}/{binding.ProfileKey}: explicit frozen native level band");
            }
            var medpack = harness.World.ItemTemplateActionEntries.AsNoTracking().Single(row => row.ItemTemplateId == 44917);
            Assert.AreEqual(419U, medpack.ActionId);
            Assert.AreEqual(1U, medpack.Level);
            Assert.AreEqual("CONSUMABLE_MEDPACK", harness.World.ActionEntries.AsNoTracking().Single(row => row.Id == 419).Name);
            Assert.AreEqual(1, (int)requirements[links[44917]].RequirementValue);
            foreach (var item in content.Items)
            {
                var native = templates[item.ItemTemplateId];
                var classId = links[item.ItemTemplateId];
                Assert.AreEqual(expectedClassLinks[item.ItemTemplateId], classId);
                Assert.AreNotEqual(item.ItemTemplateId, classId, "Template IDs must not masquerade as class IDs.");
                Assert.AreEqual(0, (int)native.HasCharacterUniqueFlag);
                Assert.AreEqual(0, (int)native.HasAccountUniqueFlag);
                Assert.AreEqual(0, (int)native.BoundToCharacterFlag);
                Assert.AreEqual(0, (int)native.NotTradableFlag);
                Assert.AreEqual(1, (int)native.HasSellableFlag);
                Assert.AreNotEqual((byte)LootQuality.Mission, native.QualityId);
                Assert.AreEqual(0, (int)classes[classId].HidenInventoryFlag);
                Assert.IsTrue(item.MaximumQuantity <= classes[classId].StackSize);
                var group = content.Groups.Single(row => row.ProfileKey == item.ProfileKey && row.GroupKey == item.GroupKey);
                if (group.Kind == LootGroupKind.Equipment)
                {
                    Assert.IsTrue(equipment.Contains(classId));
                    Assert.IsTrue(requirements.TryGetValue(classId, out var requirement));
                    Assert.AreEqual(1, (int)requirement.RequirementType);
                    foreach (var binding in content.Bindings.Where(row => row.ProfileKey == item.ProfileKey))
                        Assert.IsTrue(requirement.RequirementValue <= creatures[binding.CreatureId].Level,
                            $"{binding.CreatureId}/{item.ProfileKey}/{item.ItemTemplateId}: class {classId} level requirement");
                    Assert.IsTrue(skills[classId].All(row => row.SkillLevel == 1));
                }
            }
            foreach (var group in content.Groups.Where(row => row.Kind == LootGroupKind.Equipment))
            {
                var items = content.Items.Where(row => row.ProfileKey == group.ProfileKey && row.GroupKey == group.GroupKey).ToArray();
                var total = items.Sum(row => row.Weight);
                var boss = group.ProfileKey.EndsWith("_boss", StringComparison.Ordinal);
                foreach (var (quality, percent) in boss
                    ? new[] { (LootQuality.Normal, 0), (LootQuality.Uncommon, 65), (LootQuality.Rare, 35) }
                    : new[] { (LootQuality.Normal, 70), (LootQuality.Uncommon, 25), (LootQuality.Rare, 5) })
                    Assert.AreEqual(total * percent,
                        items.Where(row => templates[row.ItemTemplateId].QualityId == (byte)quality).Sum(row => row.Weight) * 100,
                        $"{group.ProfileKey}: aggregate {quality}");
                Assert.AreEqual(boss ? 3 : 4, items.Length);
            }
            foreach (var id in content.Items.Select(row => row.ItemTemplateId).Distinct())
            {
                var quantity = (uint)content.Items.Where(row => row.ItemTemplateId == id).Max(row => row.MaximumQuantity);
                var template = InventoryManager.InventoryPlan.RequireTemplate(id);
                Assert.IsTrue(EntityClassManager.Instance.GetClassInfo(template.Class).Augmentations.Contains(AugmentationType.Item));
                var staged = ItemManager.StageItem(template, quantity, "");
                try
                {
                    Assert.AreEqual(quantity, staged.StackSize);
                    Assert.AreEqual(id, staged.ItemTemplate.ItemTemplateId);
                    ItemManager.Instance.SendItemDataToClient(harness.Client, staged, false);
                    Assert.IsTrue(harness.Drain().Count > 0, $"Native template {id} must publish pickup item packets.");
                }
                finally
                {
                    EntityManager.Instance.FreeEntity(staged.EntityId);
                }
            }

            // Let the catalog enumerate every source itself, including encounter-created actors.
            harness.World.Database.ExecuteSqlRaw("DELETE FROM creature_loot_binding WHERE map_context_id = 1220");
            var error = Assert.ThrowsExactly<LootContentException>(() => OrdinaryLootCatalog.Load(world));
            var missing = error.Diagnostics.Where(message => message.Contains("required ordinary loot profile binding is missing"))
                .Select(message => uint.Parse(message.Split("creature ")[1].Split(':')[0])).Distinct().OrderBy(id => id).ToArray();
            // And this fork's Wilderness Bane turret, which Bind_control_point_turret_loot binds,
            // and its Wilderness Bane mortar, which Bane_mortars binds.
            CollectionAssert.AreEqual(RequiredCreatures.Append(ControlPointTurretLoot.Creature).Append(BaneMortars.LootCreature).OrderBy(id => id).ToArray(), missing);
            StringAssert.Contains(error.Message, "mission 682@");
            StringAssert.Contains(error.Message, "actor xanx-1, map 1220, creature 87");
            StringAssert.Contains(error.Message, "control point 3");
        }

        [TestMethod]
        public void EveryFrozenNativeSpeciesHasItsIndependentGoldenJunkCandidatesIncludingAbsentFamilies()
        {
            var golden = new (string Profile, uint[] Creatures, uint[] Junk)[]
            {
                ("amoeboid_5_9", new uint[] { 33 }, Array.Empty<uint>()),
                ("boargar_1_4", new uint[] { 35 }, new uint[] { 41636, 41639 }),
                ("boargar_5_9", new uint[] { 44, 58 }, new uint[] { 41636, 41639 }),
                ("caretaker_15_19_boss", new uint[] { 79 }, new uint[] { 42300, 42301 }),
                ("caretaker_20_24", new uint[] { 2 }, new uint[] { 42300, 42301 }),
                ("drone_5_9", new uint[] { 85 }, Array.Empty<uint>()),
                ("filcher_5_9", new uint[] { 60 }, new uint[] { 41642, 41643 }),
                ("fithik_5_9", new uint[] { 1 }, new uint[] { 41644, 41645 }),
                ("fithik_10_14_boss", new uint[] { 89 }, new uint[] { 41644, 41645 }),
                ("hunter_5_9", new uint[] { 41 }, new uint[] { 42326, 42327 }),
                ("lightbender_5_9", new uint[] { 630046, 630071 }, new uint[] { 42332, 42333 }),
                ("lightbender_15_19_boss", new uint[] { 76 }, new uint[] { 42332, 42333 }),
                ("linker_15_19_boss", new uint[] { 78 }, Array.Empty<uint>()),
                ("machina_5_9", new uint[] { 630100 }, new uint[] { 50211 }),
                ("miasma_5_9", new uint[] { 88 }, new uint[] { 41651, 41652, 42341, 42342 }),
                ("miasma_5_9_boss", new uint[] { 630040 }, new uint[] { 41651, 41652, 42341, 42342 }),
                ("mortar_5_9", new uint[] { 630076, 630077, 630078, 630079 }, Array.Empty<uint>()),
                ("mox_5_9", new uint[] { 59 }, Array.Empty<uint>()),
                ("predator_10_14", new uint[] { 630120 }, Array.Empty<uint>()),
                ("technician_5_9", new uint[] { 47 }, new uint[] { 42369, 42370 }),
                ("thrax_5_9", new uint[] { 57 }, new uint[] { 41665, 41666 }),
                ("thrax_5_9_boss", new uint[] { 82, 83, 84 }, new uint[] { 41665, 41666 }),
                ("thrax_10_14", new uint[] { 3 }, new uint[] { 41665, 41666 }),
                ("thrax_10_14_boss", new uint[] { 90 }, new uint[] { 41665, 41666 }),
                ("thrax_15_19_boss", new uint[] { 75, 80, 630130 }, new uint[] { 41665, 41666 }),
                ("xanx_5_9", new uint[] { 87 }, new uint[] { 41677, 41678 }),
                ("xanx_15_19_boss", new uint[] { 77 }, new uint[] { 41677, 41678 })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            using var world = harness.CreateWorld();
            var content = world.OrdinaryLoot.Get();
            CollectionAssert.AreEquivalent(golden.Select(row => "wilderness_v1_" + row.Profile).ToArray(),
                content.Profiles.Select(row => row.ProfileKey).ToArray());
            CollectionAssert.AreEquivalent(golden.SelectMany(row => row.Creatures).ToArray(),
                Owned(content).Bindings.Select(row => row.CreatureId).ToArray());
            foreach (var (suffix, creatures, junk) in golden)
            {
                var key = "wilderness_v1_" + suffix;
                CollectionAssert.AreEquivalent(creatures,
                    Owned(content).Bindings.Where(row => row.ProfileKey == key)
                        .Select(row => row.CreatureId).ToArray(), key);
                var groups = content.Groups.Where(row => row.ProfileKey == key && row.Kind == LootGroupKind.Junk).ToArray();
                Assert.AreEqual(junk.Length == 0 ? 0 : 1, groups.Length, key);
                CollectionAssert.AreEqual(junk, content.Items.Where(row => row.ProfileKey == key &&
                        groups.Select(group => group.GroupKey).Contains(row.GroupKey))
                    .OrderBy(row => row.SortOrder).Select(row => row.ItemTemplateId).ToArray(), key);
                foreach (var creature in creatures)
                {
                    var profile = OrdinaryLootCatalog.Current.Resolve(1220, creature);
                    CollectionAssert.AreEqual(junk, profile.Groups.Where(group => group.Kind == LootGroupKind.Junk)
                        .SelectMany(group => group.Candidates).Select(candidate => candidate.TemplateId).ToArray(),
                        $"Native creature {creature}: the installed runtime profile must preserve its frozen family.");
                }
            }
        }

        [TestMethod]
        public void FollowUpDataOnlyChanceAndWeightMigrationChangesReloadedRollsAndDownRestoresThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            RequireMigration(harness.World);
            using var world = harness.CreateWorld();
            var catalog = OrdinaryLootCatalog.Load(world);
            var profile = catalog.Resolve(1220, 3);
            var before = profile.Roll(Draw).ToArray();
            Assert.IsTrue(before.Any(drop => drop.TemplateId == 44917));
            Assert.IsTrue(before.Any(drop => drop.TemplateId == 11567));
            var change = new FollowUpOddsMigration();
            Apply(harness.World, change.UpOperations);
            var after = OrdinaryLootCatalog.Load(world).Resolve(1220, 3).Roll(Draw).ToArray();
            Assert.IsFalse(after.Any(drop => drop.TemplateId == 44917));
            Assert.IsFalse(after.Any(drop => drop.TemplateId == 11567));
            Assert.IsTrue(after.Any(drop => drop.TemplateId == 17133));
            CollectionAssert.AreEqual(before, profile.Roll(Draw).ToArray(), "Already loaded snapshots remain immutable.");
            Apply(harness.World, change.DownOperations);
            CollectionAssert.AreEqual(before, OrdinaryLootCatalog.Load(world).Resolve(1220, 3).Roll(Draw).ToArray());
        }

        private static int Draw(int minimum, int maximum) => minimum == 0 && maximum is 100 or 66 ? 36 : minimum;

        private sealed class FollowUpOddsMigration : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.UpdateData("ordinary_loot_group", new[] { "profile_key", "group_key" },
                    new object[] { ThraxRegular, "consumable" }, "chance_basis_points", 0);
                migrationBuilder.UpdateData("loot_item", new[] { "profile_key", "group_key", "item_template_id" },
                    new object[] { ThraxRegular, "equipment", 3061U }, "weight", 1);
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.UpdateData("ordinary_loot_group", new[] { "profile_key", "group_key" },
                    new object[] { ThraxRegular, "consumable" }, "chance_basis_points", 1000);
                migrationBuilder.UpdateData("loot_item", new[] { "profile_key", "group_key", "item_template_id" },
                    new object[] { ThraxRegular, "equipment", 3061U }, "weight", 35);
            }
        }

        private static void Apply(WorldContext context, IReadOnlyList<MigrationOperation> operations)
        {
            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(operations, context.Model);
            using var transaction = context.Database.BeginTransaction();
            foreach (var generated in commands)
            {
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.Transaction = transaction.GetDbTransaction();
                command.CommandText = generated.CommandText;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
            context.ChangeTracker.Clear();
        }

        private static Migration RequireMigration(DbContext context)
        {
            var assembly = context.GetService<IMigrationsAssembly>();
            Assert.IsTrue(assembly.Migrations.ContainsKey(ContentMigration), $"Missing frozen content migration {ContentMigration}.");
            return assembly.CreateMigration(assembly.Migrations[ContentMigration], context.Database.ProviderName);
        }

        private static void AssertFrozen(OrdinaryLootContentSnapshot content)
        {
            content = Owned(content);
            Assert.AreEqual(27, content.Profiles.Count);
            Assert.AreEqual(83, content.Groups.Count);
            Assert.AreEqual(179, content.Items.Count);
            CollectionAssert.AreEqual(RequiredCreatures, content.Bindings.Select(row => row.CreatureId).ToArray());
            CollectionAssert.AreEqual(Bosses, content.Bindings.Where(row => row.ProfileKey.EndsWith("_boss", StringComparison.Ordinal))
                .Select(row => row.CreatureId).ToArray());
            Assert.AreEqual(1, content.MapPolicies.Count);
            Assert.AreEqual(1220U, content.MapPolicies[0].MapContextId);
            Assert.IsTrue(content.MapPolicies[0].RequireProfiles);
            Assert.IsTrue(content.Profiles.All(row => row.ContentRevision == "wilderness_ordinary_loot_v1" &&
                row.Provenance.Contains("reconstructed", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(row.EvidenceNotes)));
            foreach (var group in content.Groups)
            {
                var boss = group.ProfileKey.EndsWith("_boss", StringComparison.Ordinal);
                Assert.AreEqual(group.Kind switch
                {
                    LootGroupKind.Junk => boss ? 9000 : 7000,
                    LootGroupKind.Ammunition => boss ? 4500 : 3000,
                    LootGroupKind.Consumable => boss ? 2500 : 1000,
                    LootGroupKind.Equipment => boss ? 6500 : 800,
                    _ => -1
                }, group.ChanceBasisPoints, group.ProfileKey);
                Assert.AreEqual(1, group.MinimumRolls);
                Assert.AreEqual(1, group.MaximumRolls);
            }
            foreach (var item in content.Items)
            {
                Assert.AreEqual(item.ItemTemplateId switch { 28 => 12, 56 => 8, _ => 1 }, item.MinimumQuantity);
                Assert.AreEqual(item.ItemTemplateId switch { 28 => 24, 56 => 16, _ => 1 }, item.MaximumQuantity);
            }
            foreach (var profile in content.Profiles)
            {
                var groups = content.Groups.Where(row => row.ProfileKey == profile.ProfileKey).ToArray();
                Assert.IsTrue(groups.Length is >= 2 and <= 4);
                Assert.IsTrue(groups.Count(row => row.Kind == LootGroupKind.Junk) <= 1);
                Assert.IsTrue(groups.Select(row => row.SortOrder).Distinct().Count() == groups.Length);
            }
            foreach (var id in new uint[] { 33, 59, 78, 85, 630076, 630077, 630078, 630079, 630120 })
            {
                var key = content.Bindings.Single(row => row.CreatureId == id).ProfileKey;
                Assert.IsFalse(content.Groups.Any(row => row.ProfileKey == key && row.Kind == LootGroupKind.Junk),
                    $"Creature {id}: deliberately no unverified species-specific junk.");
            }
        }

        private static OrdinaryLootContentSnapshot Owned(OrdinaryLootContentSnapshot rows) => new(
            rows.Profiles.Where(row => row.ProfileKey.StartsWith("wilderness_v1_", StringComparison.Ordinal)),
            rows.Groups.Where(row => row.ProfileKey.StartsWith("wilderness_v1_", StringComparison.Ordinal)),
            rows.Items.Where(row => row.ProfileKey.StartsWith("wilderness_v1_", StringComparison.Ordinal)),
            // The v1 content's own: not the bindings this fork adds after it (Bind_control_point_turret_loot,
            // Bane_mortars).
            rows.Bindings.Where(row => row.MapContextId == 1220 &&
                !(row.MapContextId == ControlPointTurretLoot.Map && row.CreatureId == ControlPointTurretLoot.Creature) &&
                !(row.MapContextId == BaneMortars.LootMap && row.CreatureId == BaneMortars.LootCreature)),
            rows.MapPolicies.Where(row => row.MapContextId == 1220));

        private static string Serialize(OrdinaryLootContentSnapshot rows) => JsonSerializer.Serialize(rows);

        private static long[] NativeCounts(WorldContext context) => new[] { "map_info", "creature", "itemtemplate", "itemclass", "spawnpool" }
            .Select(table => context.Database.SqlQueryRaw<long>($"SELECT COUNT(*) AS Value FROM {table}").Single()).ToArray();

        private static void AddForeignContent(WorldContext context)
        {
            context.LootProfileEntries.Add(new LootProfileEntry { ProfileKey = "unowned" });
            context.OrdinaryLootGroupEntries.Add(new OrdinaryLootGroupEntry
            {
                ProfileKey = "unowned", GroupKey = "ammo", Kind = LootGroupKind.Ammunition,
                ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1
            });
            context.LootItemEntries.Add(new LootItemEntry
            {
                ProfileKey = "unowned", GroupKey = "ammo", ItemTemplateId = 28,
                Weight = 1, MinimumQuantity = 1, MaximumQuantity = 1
            });
            context.CreatureLootBindingEntries.Add(new CreatureLootBindingEntry
            {
                MapContextId = 1148, CreatureId = 3, ProfileKey = "unowned"
            });
            context.LootMapPolicyEntries.Add(new LootMapPolicyEntry { MapContextId = 1148, RequireProfiles = false });
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        private static string[] Describe(IReadOnlyList<MigrationOperation> operations) => operations.Select(operation => operation switch
        {
            InsertDataOperation insert => $"{insert.Table}:{string.Join(",", insert.Columns)}:{string.Join(";", Rows(insert.Values))}",
            DeleteDataOperation delete => $"{delete.Table}:{string.Join(",", delete.KeyColumns)}:{string.Join(";", Rows(delete.KeyValues))}",
            _ => operation.GetType().Name
        }).ToArray();

        private static string[] Keys(InsertDataOperation insert, string[] columns)
        {
            var values = new object[insert.Values.GetLength(0), columns.Length];
            for (var row = 0; row < values.GetLength(0); row++)
                for (var column = 0; column < columns.Length; column++)
                    values[row, column] = insert.Values[row, Array.IndexOf(insert.Columns, columns[column])];
            return Rows(values);
        }

        private static string[] Rows(object[,] values) => Enumerable.Range(0, values.GetLength(0))
            .Select(row => JsonSerializer.Serialize(Enumerable.Range(0, values.GetLength(1))
                .Select(column => values[row, column]).ToArray())).ToArray();
    }
}
