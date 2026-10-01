using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Missions.Content;
using Rasa.Services.Preloader.Missions;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Test.Database;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessMigrationTests
    {
        private const string Pr105WorldBoundary = "20261103000000_Snowball_stacks_not_unique";
        private const string WildernessWorldBoundary = "20261104001600_WildernessEvidenceCapacity";

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void WildernessMigrationPairsFollowPr105InTheirApprovedLogicalOrder(Type contextType)
        {
            var names = new[]
            {
                "WildernessOpeningWorld", "NativeMissionCategory", "WildernessAliaOpening",
                "WildernessHubWorld", "RelatedMissionFailureAction", "WildernessRewardEquipment",
                "WildernessAdditionalWorld", "WildernessSpawnStatistics", "WildernessAliaBranches",
                "WildernessSniperPlacement", "WildernessElohPinhole", "WildernessLandingZone",
                "WildernessSupportedRewards", "WildernessTwinPillars", "WildernessRanjaGorge",
                "WildernessDaghdasUrn", "WildernessEvidenceCapacity"
            };
            var expected = names.Select((name, minute) => $"2026110400{minute:D2}00_{name}").ToArray();
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var migrations = context.Database.GetMigrations().ToArray();

            CollectionAssert.Contains(migrations, Pr105WorldBoundary);
            CollectionAssert.AreEqual(expected, migrations.Where(id =>
                names.Any(name => id.EndsWith("_" + name, StringComparison.Ordinal))).ToArray());
            Assert.IsTrue(expected.All(id => string.CompareOrdinal(id, Pr105WorldBoundary) > 0));
        }

        [TestMethod]
        [DataRow(typeof(Rasa.Migrations.SqliteWorld.WildernessRewardEquipment))]
        [DataRow(typeof(Rasa.Migrations.MySqlWorld.WildernessRewardEquipment))]
        public void WildernessEquipmentMarkerDoesNotOwnPr105Armor(Type migrationType)
        {
            var migration = (Migration)Activator.CreateInstance(migrationType);
            Assert.IsEmpty(migration.UpOperations, "PR105 already owns the class-sourced armor rows.");
            Assert.IsEmpty(migration.DownOperations, "Removing Wilderness must not remove PR105 armor.");
        }

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void FirstWildernessRollbackGeneratesSqlWithoutPr105TailEntityMappings(Type contextType)
        {
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var assembly = context.GetService<IMigrationsAssembly>();
            var prior = assembly.CreateMigration(assembly.Migrations[Pr105WorldBoundary], context.Database.ProviderName);
            var model = context.GetService<IModelRuntimeInitializer>().Initialize(prior.TargetModel, designTime: true);
            var opening = assembly.CreateMigration(
                assembly.Migrations["20261104000000_WildernessOpeningWorld"], context.Database.ProviderName);
            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(opening.DownOperations, model);

            Assert.IsTrue(commands.Count > 0);
            var sql = string.Join(Environment.NewLine, commands.Select(command => command.CommandText));
            StringAssert.Contains(sql, "spawnpool");
            StringAssert.Contains(sql, "creature");
            StringAssert.Contains(sql, "npc_package");
            StringAssert.Contains(sql, "630001");
        }

        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void EveryWildernessTargetModelRetainsPr105AndItsExactMissionSchemaPhase(Type contextType)
        {
            using var context = PersistenceIntegrationTests.CreateContext(contextType, "unused");
            var assembly = context.GetService<IMigrationsAssembly>();
            var latest = assembly.ModelSnapshot.Model;
            var expectedEntities = latest.GetEntityTypes().Select(entity => entity.Name).ToArray();
            var expectedProperties = latest.GetEntityTypes().SelectMany(entity =>
                entity.GetProperties().Select(property => entity.Name + "." + property.Name)).ToArray();
            var finalConstraint = latest.FindEntityType("Rasa.Structures.World.MissionActionEntry")
                .FindCheckConstraint("CK_mission_action_kind_parameter_set").Sql;
            var migrations = assembly.Migrations.Where(entry =>
                entry.Key.StartsWith("20261104", StringComparison.Ordinal)).OrderBy(entry => entry.Key).ToArray();

            Assert.AreEqual(17, migrations.Length);
            Assert.AreEqual(66, expectedEntities.Length);
            Assert.AreEqual(550, expectedProperties.Length);
            for (var index = 0; index < migrations.Length; index++)
            {
                var migration = assembly.CreateMigration(migrations[index].Value, context.Database.ProviderName);
                var model = context.GetService<IModelRuntimeInitializer>().Initialize(migration.TargetModel, designTime: true);
                CollectionAssert.AreEquivalent(expectedEntities, model.GetEntityTypes().Select(entity => entity.Name).ToArray(),
                    migrations[index].Key);
                CollectionAssert.AreEquivalent(expectedProperties, model.GetEntityTypes().SelectMany(entity =>
                    entity.GetProperties().Select(property => entity.Name + "." + property.Name)).ToArray(),
                    migrations[index].Key);

                var category = model.FindEntityType("Rasa.Structures.World.MissionContentDefinitionEntry")
                    .FindProperty("CategoryId");
                Assert.AreEqual(index == 0 ? typeof(byte) : typeof(uint), category.ClrType, migrations[index].Key);
                var categoryType = index == 0 ? "tinyint(3)" : "int(11)";
                if (contextType == typeof(MySqlWorldContext))
                    categoryType += " unsigned";
                Assert.AreEqual(categoryType, category.GetColumnType(), migrations[index].Key);
                var constraint = model.FindEntityType("Rasa.Structures.World.MissionActionEntry")
                    .FindCheckConstraint("CK_mission_action_kind_parameter_set").Sql;
                Assert.AreEqual(index < 4 ? finalConstraint.Replace(
                    "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13))",
                    "(kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12))") : finalConstraint,
                    constraint, migrations[index].Key);
                var evidence = model.FindEntityType("Rasa.Structures.World.MissionEvidenceEntry")
                    .FindProperty("ReconstructionNote");
                Assert.AreEqual(index < 8 ? "varchar(256)" : "text", evidence.GetColumnType(), migrations[index].Key);
                Assert.AreEqual(typeof(uint), model.FindEntityType("Rasa.Structures.World.CreatureActionEntry")
                    .FindProperty("DamageType").ClrType, migrations[index].Key);
                Assert.AreEqual(typeof(double), model.FindEntityType("Rasa.Structures.World.SpawnPoolEntry")
                    .FindProperty("Radius").ClrType, migrations[index].Key);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FreshAndExistingPr105WorldsPreserveUpstreamDataAndInstallWilderness(bool existingPr105)
        {
            WithDisposableWorld(context =>
            {
                var migrator = context.GetService<IMigrator>();
                string existingRows = null;
                if (existingPr105)
                {
                    migrator.Migrate(Pr105WorldBoundary);
                    Assert.AreEqual(Pr105WorldBoundary, context.Database.GetAppliedMigrations().Last());
                    existingRows = Pr105SeedRows(context);
                }

                migrator.Migrate();
                AssertMergedWorld(context);
                var upstreamRows = Pr105SeedRows(context);
                if (existingRows != null)
                    Assert.AreEqual(existingRows, upstreamRows, "Wilderness must preserve PR105-owned World rows.");

                migrator.Migrate(Pr105WorldBoundary);
                Assert.AreEqual(upstreamRows, Pr105SeedRows(context),
                    "Removing Wilderness must not delete or revert PR105 armor, Divide, or boss rows.");
                Assert.IsFalse(context.CreatureEntries.Any(row => row.Id >= 630001 && row.Id <= 630199));
                Assert.IsFalse(context.SpawnPoolEntries.Any(row => row.Id >= 630001 && row.Id <= 630199));

                migrator.Migrate();
                AssertMergedWorld(context);
                Assert.AreEqual(upstreamRows, Pr105SeedRows(context));
            });
        }

        private static void AssertMergedWorld(SqliteWorldContext context)
        {
            Assert.AreEqual(WildernessWorldBoundary, context.Database.GetAppliedMigrations().Last());
            Assert.IsFalse(context.Database.GetPendingMigrations().Any());

            var divide = context.CreatureEntries.AsNoTracking().Single(row => row.Id == 530001);
            Assert.AreEqual(20757U, divide.ClassId);
            Assert.AreEqual(0U, divide.Faction);
            Assert.AreEqual(15U, divide.Level);
            Assert.AreEqual(723U, divide.MaxHitPoints);
            Assert.AreEqual(70015U, divide.Action1);
            Assert.AreEqual(9, context.CreatureEntries.Count(row => row.Id >= 530001 && row.Id <= 530009));
            Assert.AreEqual(9, context.CreatureStatEntries.Count(row => row.Id >= 530001 && row.Id <= 530009));
            var divideStats = context.CreatureStatEntries.AsNoTracking().Single(row => row.Id == 530001);
            Assert.AreEqual(723, divideStats.Health);
            Assert.AreEqual(65, divideStats.Armor);
            var dividePools = context.SpawnPoolEntries.AsNoTracking()
                .Where(row => row.Id >= 530001 && row.Id <= 530079).ToArray();
            CollectionAssert.AreEquivalent(Enumerable.Range(530001, 79).Select(id => (uint)id).ToArray(),
                dividePools.Select(row => row.Id).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 1148, 1348, 1349, 1806 },
                dividePools.Select(row => row.MapContextId).Distinct().ToArray());
            Assert.AreEqual(1148U, dividePools.Single(row => row.Id == 530001).MapContextId);
            Assert.AreEqual(1349U, dividePools.Single(row => row.Id == 530070).MapContextId);

            var expectedCreatures = new uint[]
            {
                630001, 630010, 630040, 630043, 630046, 630070, 630071,
                630076, 630077, 630078, 630079, 630100, 630120, 630130
            };
            var creatures = context.CreatureEntries.AsNoTracking()
                .Where(row => row.Id >= 630001 && row.Id <= 630199).ToArray();
            CollectionAssert.AreEquivalent(expectedCreatures, creatures.Select(row => row.Id).ToArray());
            CollectionAssert.AreEquivalent(expectedCreatures, context.CreatureStatEntries.AsNoTracking()
                .Where(row => row.Id >= 630001 && row.Id <= 630199).Select(row => row.Id).ToArray());
            Assert.AreEqual(1U, creatures.Single(row => row.Id == 630001).Faction);
            Assert.AreEqual(600U, creatures.Single(row => row.Id == 630001).MaxHitPoints);
            Assert.AreEqual(630052U, creatures.Single(row => row.Id == 630046).Action1);
            Assert.AreEqual(630103U, creatures.Single(row => row.Id == 630100).Action1);
            CollectionAssert.AreEquivalent(new uint[] { 630052, 630103 }, context.CreatureActionEntries
                .Where(row => row.Id >= 630001 && row.Id <= 630199).Select(row => row.Id).ToArray());
            Assert.AreEqual(595U, context.NpcPackageEntries.Single(row => row.Id == 630130).PackageId);

            var expectedPools = new (uint Pool, uint Creature)[]
            {
                (630001, 630001), (630010, 630010),
                (630040, 630040), (630041, 630040), (630042, 630040),
                (630043, 630043), (630044, 630043), (630045, 630043),
                (630046, 630046), (630047, 630046), (630048, 630046),
                (630049, 630046), (630050, 630046), (630051, 630046),
                (630070, 630070), (630071, 630071), (630072, 630071),
                (630073, 630071), (630074, 630071),
                (630076, 630076), (630077, 630077), (630078, 630078), (630079, 630079),
                (630100, 630100), (630101, 630100), (630102, 630100),
                (630120, 630120), (630121, 630120), (630130, 630130)
            };
            var pools = context.SpawnPoolEntries.AsNoTracking()
                .Where(row => row.Id >= 630001 && row.Id <= 630199).ToDictionary(row => row.Id);
            CollectionAssert.AreEquivalent(expectedPools.Select(row => row.Pool).ToArray(), pools.Keys.ToArray());
            foreach (var (poolId, creatureId) in expectedPools)
            {
                Assert.AreEqual(1220U, pools[poolId].MapContextId, $"Mission pool {poolId} must stay in Wilderness.");
                Assert.AreEqual(creatureId, pools[poolId].Creature1Id);
                Assert.AreEqual((byte)1, pools[poolId].Creature1MinCount);
                Assert.AreEqual((byte)1, pools[poolId].Creature1MaxCount);
            }

            var expectedMissions = WildernessMissionCases.All
                .Where(row => row.Disposition == WildernessDisposition.OutdoorRelease)
                .Select(row => row.MissionId).Concat(WildernessMissionCases.ProtectedBootcampMissionIds).ToArray();
            var enabled = context.MissionContentDefinitionEntries.AsNoTracking().Where(row => row.Enabled).ToArray();
            Assert.AreEqual(69, enabled.Length);
            CollectionAssert.AreEquivalent(expectedMissions, enabled.Select(row => row.MissionId).ToArray());
            Assert.IsTrue(enabled.Where(row => row.ContentRevision == WildernessMissionCases.ContentRevision)
                .All(row => row.CategoryId == 10000044));
            Assert.AreEqual<uint?>(630070U, enabled.Single(row => row.MissionId == 666).GiverId);
            Assert.IsTrue(context.MissionActionEntries.Any(row => (byte)row.Kind == 13));
            Assert.AreEqual("TEXT", context.Database.SqlQueryRaw<string>(
                "SELECT type AS Value FROM pragma_table_info('mission_evidence') WHERE name = 'reconstruction_note'")
                .Single().ToUpperInvariant());

            var expectedArmor = new (uint Template, int Value)[]
            {
                (20250, 71), (35486, 95), (26996, 54), (20697, 118), (20399, 47),
                (20846, 142), (20548, 95), (36083, 189), (12827, 91), (12855, 61),
                (11567, 84), (11568, 139), (12887, 188), (12831, 141), (12943, 281),
                (12915, 234), (13388, 250), (35784, 126), (35933, 158),
                (13744, 167), (28692, 67), (13739, 100),
                (13126, 47), (13156, 59), (13186, 70)
            };
            var templateIds = expectedArmor.Select(row => row.Template).ToArray();
            var armor = context.ItemTemplateArmorEntries.AsNoTracking()
                .Where(row => templateIds.Contains(row.Id)).ToDictionary(row => row.Id, row => row.ArmorValue);
            CollectionAssert.AreEquivalent(templateIds, armor.Keys.ToArray());
            foreach (var (template, value) in expectedArmor)
                Assert.AreEqual(value, armor[template], $"Template {template} must retain PR105's class-sourced armor.");
        }

        private static string Pr105SeedRows(SqliteWorldContext context) => JsonSerializer.Serialize(new
        {
            DivideCreatures = context.CreatureEntries.AsNoTracking()
                .Where(row => row.Id >= 530001 && row.Id <= 530009).OrderBy(row => row.Id).ToArray(),
            DivideStats = context.CreatureStatEntries.AsNoTracking()
                .Where(row => row.Id >= 530001 && row.Id <= 530009).OrderBy(row => row.Id).ToArray(),
            DividePools = context.SpawnPoolEntries.AsNoTracking()
                .Where(row => row.Id >= 530001 && row.Id <= 530079).OrderBy(row => row.Id).ToArray(),
            WildernessPools = context.SpawnPoolEntries.AsNoTracking()
                .Where(row => row.Id >= 580001 && row.Id <= 580075).OrderBy(row => row.Id).ToArray(),
            MovedBosses = context.SpawnPoolEntries.AsNoTracking()
                .Where(row => row.Id == 520020 || row.Id == 520023 || row.Id == 520038 ||
                    row.Id == 520048 || row.Id == 520062).OrderBy(row => row.Id).ToArray(),
            Armor = context.ItemTemplateArmorEntries.AsNoTracking().OrderBy(row => row.Id).ToArray()
        });

        private static void WithDisposableWorld(Action<SqliteWorldContext> action)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                using var context = (SqliteWorldContext)PersistenceIntegrationTests.CreateContext(
                    typeof(SqliteWorldContext), Path.Combine(directory, "world"));
                action(context);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void AuthoringFixtureLoadsAdditionalDataThroughTheRealCatalog()
        {
            using var harness = WildernessRuntimeTestHarness.Create(migration =>
                migration.UpdateData("mission_content_definition",
                    new[] { "mission_id", "content_revision" },
                    new object[] { 1407U, "wilderness_1_16_5" },
                    "comment", "Additional authoring fixture data"));
            Assert.AreEqual("Additional authoring fixture data", harness.Manager.LoadedMissions[1407].Name);
            Assert.IsTrue(harness.Manager.LoadedMissions[1407].IsOperational);
            Assert.AreEqual(10000044U, harness.Manager.LoadedMissions[1407].CategoryId);
        }

        [TestMethod]
        public void AuthoringFixturePreservesLiteralBracesInProviderGeneratedSql()
        {
            using var harness = WildernessRuntimeTestHarness.Create(migration =>
                migration.UpdateData("mission_content_definition",
                    new[] { "mission_id", "content_revision" },
                    new object[] { 1407U, "wilderness_1_16_5" },
                    "comment", "Literal {scene} braces, not format arguments"));

            Assert.AreEqual("Literal {scene} braces, not format arguments",
                harness.Manager.LoadedMissions[1407].Name);
        }

        [TestMethod]
        public void FirstHubMigrationsEnableExactlyBootcampAndTheFourOpeningMissions()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20261104000200_WildernessAliaOpening");
            CollectionAssert.AreEquivalent(
                new uint[] { 1990, 1992, 1994, 1995, 2005, 1407, 1069, 479, 1449 },
                harness.World.MissionContentDefinitionEntries.Where(entry => entry.Enabled)
                    .Select(entry => entry.MissionId).ToArray());
        }

        [TestMethod]
        public void AliaBranchDataDownRemovesOnlyItsOwnDependencyGraph()
        {
            using var harness = WildernessRuntimeTestHarness.Create(migration =>
            {
                WildernessAliaBranchesV1.Up(migration);
                WildernessAliaBranchesV1.Down(migration);
            }, targetWorldMigration: "20261104000700_WildernessSpawnStatistics");

            CollectionAssert.AreEquivalent(
                new uint[] { 1990, 1992, 1994, 1995, 2005, 1407, 1069, 479, 1449 },
                harness.World.MissionContentDefinitionEntries.Where(entry => entry.Enabled)
                    .Select(entry => entry.MissionId).ToArray());
            Assert.IsTrue(harness.Manager.LoadedMissions[1407].IsOperational);
            Assert.IsTrue(harness.Manager.LoadedMissions[1449].IsOperational);
        }

        [TestMethod]
        public void WaveBProvidersActivateInOrderWithoutChangingTheWaveABoundary()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20261104001100_WildernessLandingZone");
            var expected = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled).Select(entry => entry.MissionId).ToHashSet();
            Assert.AreEqual(44, expected.Count);
            foreach (var (migration, missions) in new[]
            {
                ("WildernessTwinPillars", new uint[] { 442, 444, 623, 791, 570, 574, 912, 1634, 1635, 908 }),
                ("WildernessRanjaGorge", new uint[] { 425, 679, 451, 697, 860, 758, 787, 769 }),
                ("WildernessDaghdasUrn", new uint[] { 696, 682, 695, 698, 700, 820, 701 })
            })
            {
                harness.World.GetService<IMigrator>().Migrate(migration);
                Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
                expected.UnionWith(missions);
                CollectionAssert.AreEquivalent(expected.ToArray(),
                    harness.World.MissionContentDefinitionEntries.AsNoTracking()
                        .Where(entry => entry.Enabled).Select(entry => entry.MissionId).ToArray());
                Assert.IsTrue(expected.All(id => harness.Manager.LoadedMissions[id].IsOperational),
                    $"{migration} must activate complete operational content, not just enabled rows.");
            }
            Assert.AreEqual(69, expected.Count);
            harness.World.Initialize();
            CollectionAssert.AreEquivalent(expected.ToArray(),
                harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.Enabled).Select(entry => entry.MissionId).ToArray());
            Assert.IsFalse(harness.World.Database.GetPendingMigrations().Any());
        }

        [TestMethod]
        public void EvidenceCapacityPreservesFreshW1UpgradesAndEveryAuthoredNoteAcrossRollback()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20261104000200_WildernessAliaOpening");
            var opening = EvidenceRows(harness);
            harness.World.GetService<IMigrator>().Migrate("WildernessAliaBranches");
            var waveA = EvidenceRows(harness);
            CollectionAssert.IsSubsetOf(opening, waveA);
            Assert.IsTrue(harness.World.MissionEvidenceEntries.AsNoTracking()
                .AsEnumerable().Any(entry => entry.ReconstructionNote.Length > 256));

            harness.World.Initialize();
            var latest = EvidenceRows(harness);
            CollectionAssert.IsSubsetOf(waveA, latest);
            Assert.AreEqual("TEXT", EvidenceColumnType(harness).ToUpperInvariant());
            harness.World.GetService<IMigrator>().Migrate("WildernessDaghdasUrn");
            CollectionAssert.AreEqual(latest, EvidenceRows(harness));
            Assert.AreEqual("TEXT", EvidenceColumnType(harness).ToUpperInvariant());
            harness.World.GetService<IMigrator>().Migrate("WildernessAliaOpening");
            CollectionAssert.AreEqual(opening, EvidenceRows(harness));
            harness.World.Initialize();
            CollectionAssert.AreEqual(latest, EvidenceRows(harness));
        }

        [TestMethod]
        public void ForwardCapacityMigrationPreservesAnAlreadyAppliedLateSchema()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20261104001500_WildernessDaghdasUrn");
            var original = EvidenceRows(harness);
            var assembly = harness.World.GetService<IMigrationsAssembly>();
            var prior = assembly.CreateMigration(assembly.Migrations["20261104001500_WildernessDaghdasUrn"],
                harness.World.Database.ProviderName);
            var legacy = new MigrationBuilder(harness.World.Database.ProviderName);
            legacy.AlterColumn<string>("reconstruction_note", "mission_evidence",
                type: "varchar(256)", nullable: false, oldClrType: typeof(string), oldType: "text");
            // SQLite rebuilds from the target model, so the legacy fixture needs its own narrow-column model.
            var legacySnapshot = (ModelSnapshot)Activator.CreateInstance(assembly.ModelSnapshot.GetType(), nonPublic: true);
            var legacyDefinition = (IMutableModel)legacySnapshot.Model;
            legacyDefinition.FindEntityType("Rasa.Structures.World.MissionEvidenceEntry")
                .FindProperty("ReconstructionNote").SetColumnType("varchar(256)");
            var legacyModel = harness.World.GetService<IModelRuntimeInitializer>()
                .Initialize(legacyDefinition.FinalizeModel(), designTime: true);
            Assert.AreEqual("text", prior.TargetModel.FindEntityType("Rasa.Structures.World.MissionEvidenceEntry")
                .FindProperty("ReconstructionNote").GetColumnType(),
                "Constructing the legacy fixture must not change the real Daghda target model.");
            var commands = harness.World.GetService<IMigrationsSqlGenerator>().Generate(legacy.Operations, legacyModel);
            harness.World.Database.OpenConnection();
            try
            {
                foreach (var command in commands)
                    harness.World.Database.ExecuteSqlRaw(command.CommandText);
            }
            finally
            {
                harness.World.Database.CloseConnection();
            }
            Assert.AreEqual("VARCHAR(256)", EvidenceColumnType(harness).ToUpperInvariant());
            CollectionAssert.AreEqual(original, EvidenceRows(harness));
            var pending = harness.World.Database.GetPendingMigrations().ToArray();
            Assert.AreEqual(1, pending.Length);
            StringAssert.EndsWith(pending[0], "_WildernessEvidenceCapacity");

            harness.World.Initialize();

            Assert.AreEqual("TEXT", EvidenceColumnType(harness).ToUpperInvariant());
            CollectionAssert.AreEqual(original, EvidenceRows(harness));
            Assert.IsFalse(harness.World.Database.GetPendingMigrations().Any());
            harness.World.Initialize();
            CollectionAssert.AreEqual(original, EvidenceRows(harness));
        }

        [TestMethod]
        public void GeneratedSqliteUpgradeAndRollbackScriptsPreserveEvidenceThroughTableRebuilds()
        {
            const string openingMigration = "20261104000200_WildernessAliaOpening";
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: openingMigration);
            var migrator = harness.World.GetService<IMigrator>();
            var latestMigration = harness.World.Database.GetMigrations().Last();
            var opening = EvidenceRows(harness);
            var up = migrator.GenerateScript(openingMigration, latestMigration);
            var down = migrator.GenerateScript(latestMigration, openingMigration);

            Execute(up);

            var latest = EvidenceRows(harness);
            CollectionAssert.IsSubsetOf(opening, latest);
            Assert.AreEqual(69, harness.World.MissionContentDefinitionEntries.Count(entry => entry.Enabled));
            Assert.AreEqual("TEXT", EvidenceColumnType(harness).ToUpperInvariant());
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            Assert.IsFalse(harness.World.Database.GetPendingMigrations().Any());

            Execute(down);

            CollectionAssert.AreEqual(opening, EvidenceRows(harness));
            Assert.AreEqual(9, harness.World.MissionContentDefinitionEntries.Count(entry => entry.Enabled));
            Assert.AreEqual(openingMigration, harness.World.Database.GetAppliedMigrations().Last());
            Execute(up);
            CollectionAssert.AreEqual(latest, EvidenceRows(harness));
            Assert.IsFalse(harness.World.Database.GetPendingMigrations().Any());

            void Execute(string script)
            {
                harness.World.Database.OpenConnection();
                try
                {
                    using var command = harness.World.Database.GetDbConnection().CreateCommand();
                    command.CommandText = script;
                    command.ExecuteNonQuery();
                }
                finally
                {
                    harness.World.Database.CloseConnection();
                }
            }
        }

        private static string[] EvidenceRows(WildernessRuntimeTestHarness harness) =>
            harness.World.MissionEvidenceEntries.AsNoTracking().AsEnumerable()
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision).ThenBy(entry => entry.EvidenceId)
                .Select(entry => JsonSerializer.Serialize(new
                {
                    entry.MissionId, entry.ContentRevision, entry.EvidenceId, entry.OwnerKind, entry.OwnerId,
                    entry.SourceKind, entry.SourceUri, entry.LocalClientPath, entry.Confidence, entry.ReconstructionNote
                })).ToArray();

        private static string EvidenceColumnType(WildernessRuntimeTestHarness harness) =>
            harness.World.Database.SqlQueryRaw<string>(
                "SELECT type AS Value FROM pragma_table_info('mission_evidence') WHERE name = 'reconstruction_note'").Single();

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void AbsentProgressMetadataDoesNotRewriteFrozenBootcampScenePayloads(bool useContentCodec)
        {
            var json = JsonSerializer.Serialize(BootcampMissionDataV1.Mission1990(),
                useContentCodec ? MissionContentCodec.Options : null);
            using var document = JsonDocument.Parse(json);
            foreach (var name in new[]
            {
                "ObjectiveAggregations", "objectiveAggregations",
                "HiddenObjectiveIds", "hiddenObjectiveIds",
                "ExistingFactObjectiveIds", "existingFactObjectiveIds"
            })
                Assert.IsFalse(document.RootElement.TryGetProperty(name, out _),
                    $"Default metadata {name} changes the output of a previously fixed migration.");
        }
    }
}
