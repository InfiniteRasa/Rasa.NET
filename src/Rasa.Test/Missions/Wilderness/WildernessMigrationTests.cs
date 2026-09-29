using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Missions.Content;
using Rasa.Services.Preloader.Missions;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessMigrationTests
    {
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
                targetWorldMigration: "20260928224705_WildernessAliaOpening");
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
            }, targetWorldMigration: "20260929065937_WildernessSpawnStatistics");

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
                targetWorldMigration: "20260929090436_WildernessLandingZone");
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
                targetWorldMigration: "20260928224705_WildernessAliaOpening");
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
                targetWorldMigration: "20260929121256_WildernessDaghdasUrn");
            var original = EvidenceRows(harness);
            var assembly = harness.World.GetService<IMigrationsAssembly>();
            var prior = assembly.CreateMigration(assembly.Migrations["20260929121256_WildernessDaghdasUrn"],
                harness.World.Database.ProviderName);
            var legacy = new MigrationBuilder(harness.World.Database.ProviderName);
            legacy.AlterColumn<string>("reconstruction_note", "mission_evidence",
                type: "varchar(256)", nullable: false, oldClrType: typeof(string), oldType: "text");
            var legacyModel = harness.World.GetService<IModelRuntimeInitializer>().Initialize(prior.TargetModel, designTime: true);
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
            const string openingMigration = "20260928224705_WildernessAliaOpening";
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
