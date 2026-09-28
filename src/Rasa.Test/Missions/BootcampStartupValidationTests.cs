using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Configuration;
    using Configuration.ConnectionStrings;
    using Configuration.ContextSetup;
    using Context.World;
    using Rasa;
    using Rasa.Game;
    using Rasa.Managers;
    using Repositories.World;
    using Services.DbContext;
    using Structures.Missions;

    [TestClass]
    [DoNotParallelize]
    public class BootcampStartupValidationTests
    {
        [TestMethod]
        [DynamicData(nameof(RequiredContentFailureCases))]
        public void RequiredContentDefectsLogActionableStartupDiagnosticsAndBlockReadyState(
            string _,
            Action<MissionContentFixture> mutate,
            string expectedCode)
        {
            var fixture = MissionContentFixture.CreateValid();
            mutate(fixture);
            var snapshot = new MissionContentLoader().Load(fixture.CreateRepository());
            var report = new MissionContentValidator().Validate(
                snapshot,
                fixture.CreateWorldUnitOfWork());

            Assert.IsTrue(report.BlocksReadiness);
            CollectionAssert.Contains(
                report.Diagnostics.Select(diagnostic => diagnostic.Code).ToArray(),
                expectedCode);

            var output = CaptureLogs(() =>
            {
                Assert.IsFalse(Server.LogMissionValidationAndCheckReadiness(report));
            });

            foreach (var diagnostic in report.Diagnostics)
                StringAssert.Contains(output, diagnostic.ToOperatorMessage());
            StringAssert.Contains(
                output,
                "Mission content validation failed for required content; the Game server will not report ready.");
            Assert.IsFalse(output.Contains("Server ready!", StringComparison.Ordinal));
        }

        [TestMethod]
        public void MissingRequiredBootcampNpcPackageLogsTheExactOperatorDiagnosticAndBlocksReadyState()
        {
            const string expected = "Mission 1995@deployment_11 objective 4 transition 1 trigger 1: " +
                                    "conversation trigger references missing npc_package.package_id 2564; " +
                                    "restore npc_package.package_id 2564 or update mission_trigger.npc_package_id to a valid package.";

            WithDisposableSqliteWorld(context =>
            {
                context.Database.Migrate();

                var package = context.NpcPackageEntries.Single(entry => entry.PackageId == 2564);
                context.NpcPackageEntries.Remove(package);
                context.SaveChanges();

                var snapshot = LoadSnapshot(context);
                var report = Validate(snapshot, context);
                var diagnostic = report.Diagnostics.Single(entry =>
                    entry.Code == "missing-npc-package" &&
                    entry.MissionId == 1995 &&
                    entry.ObjectiveId == 4 &&
                    entry.TransitionId == 1 &&
                    entry.TriggerId == 1);

                Assert.IsTrue(report.BlocksReadiness);
                Assert.AreEqual(expected, diagnostic.ToOperatorMessage());

                var output = CaptureLogs(() =>
                {
                    Assert.IsFalse(Server.LogMissionValidationAndCheckReadiness(report));
                });

                StringAssert.Contains(output, expected);
                StringAssert.Contains(
                    output,
                    "Mission content validation failed for required content; the Game server will not report ready.");
                Assert.IsFalse(output.Contains("Server ready!", StringComparison.Ordinal));
            });
        }

        [TestMethod]
        public void MissingRequiredBootcampRetryConversationPackageLogsTheExactOperatorDiagnosticAndBlocksReadyState()
        {
            const string expected = "Mission 2005@deployment_11 objective 4 transition 1 trigger 1: " +
                                    "conversation trigger references missing npc_package.package_id 999999; " +
                                    "restore npc_package.package_id 999999 or update mission_trigger.npc_package_id to a valid package.";

            WithDisposableSqliteWorld(context =>
            {
                context.Database.Migrate();

                var trigger = context.MissionTriggerEntries.Single(entry =>
                    entry.MissionId == 2005 &&
                    entry.ContentRevision == "deployment_11" &&
                    entry.ObjectiveId == 4 &&
                    entry.TransitionId == 1 &&
                    entry.TriggerId == 1);
                trigger.NpcPackageId = 999999;
                context.SaveChanges();

                var snapshot = LoadSnapshot(context);
                var report = Validate(snapshot, context);
                var diagnostic = report.Diagnostics.Single(entry =>
                    entry.Code == "missing-npc-package" &&
                    entry.MissionId == 2005 &&
                    entry.ObjectiveId == 4 &&
                    entry.TransitionId == 1 &&
                    entry.TriggerId == 1);

                Assert.IsTrue(report.BlocksReadiness);
                Assert.AreEqual(expected, diagnostic.ToOperatorMessage());

                var output = CaptureLogs(() =>
                {
                    Assert.IsFalse(Server.LogMissionValidationAndCheckReadiness(report));
                });

                StringAssert.Contains(output, expected);
                StringAssert.Contains(
                    output,
                    "Mission content validation failed for required content; the Game server will not report ready.");
                Assert.IsFalse(output.Contains("Server ready!", StringComparison.Ordinal));
            });
        }

        public static IEnumerable<object[]> RequiredContentFailureCases() =>
            MissionContentValidatorTests.GetFailureCases();

        private static MissionContentSnapshot LoadSnapshot(SqliteWorldContext context)
        {
            var repository = new MissionContentRepository(context);
            return new MissionContentLoader().Load(repository);
        }

        private static MissionValidationReport Validate(
            MissionContentSnapshot snapshot,
            SqliteWorldContext context)
        {
            using var unit = new RepositoryBackedWorldUnitOfWork(context);
            return new MissionContentValidator().Validate(snapshot, unit);
        }

        private static void WithDisposableSqliteWorld(Action<SqliteWorldContext> body)
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "TestDatabases",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var database = Path.Combine(path, "database");
                using var context = CreateContext(database);
                body(context);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(path, true);
            }
        }

        private static SqliteWorldContext CreateContext(string database)
        {
            var connection = new DatabaseConnectionConfiguration { Database = database };
            var options = Options.Create(new DatabaseConfiguration
            {
                Provider = "Sqlite",
                Auth = connection,
                Char = connection,
                World = connection
            });
            return new SqliteWorldContext(
                options,
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier());
        }

        private static string CaptureLogs(Action action)
        {
            Logger.UpdateConfig(new Logger.LoggerConfig
            {
                IsDebugMode = true,
                LogToFile = false,
                LogFilePath = null
            });

            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                action();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            return writer.ToString();
        }

        private sealed class RepositoryBackedWorldUnitOfWork : Repositories.World.IWorldUnitOfWork
        {
            public RepositoryBackedWorldUnitOfWork(SqliteWorldContext context)
            {
                Actions = null;
                Equipment = new EquipmentRepository(context);
                Creatures = new CreatureRepository(context);
                EntityClasses = new EntityClassRepository(context);
                Footlockers = null;
                Logoses = null;
                MapInfos = new MapInfoRepository(context);
                MapLinks = null;
                Kraftwerks = null;
                MapRegions = null;
                MapMarkers = null;
                Recipes = null;
                NpcMissions = null;
                NpcMissionRewards = null;
                MissionContent = new MissionContentRepository(context);
                NpcPackages = new NpcPackageRepository(context);
                RandomNames = null;
                Spawnpools = null;
                Teleporters = new TeleporterRepository(context);
            }

            public IActionRepository Actions { get; }
            public IEquipmentRepository Equipment { get; }
            public ICreatureRepository Creatures { get; }
            public IEntityClassRepository EntityClasses { get; }
            public IFootlockerRepository Footlockers { get; }
            public ILogosRepository Logoses { get; }
            public IMapInfoRepository MapInfos { get; }
            public IMapLinkRepository MapLinks { get; }
            public IKraftwerksRepository Kraftwerks { get; }
            public IMapRegionRepository MapRegions { get; }
            public IMapMarkerRepository MapMarkers { get; }
            public IRecipeRepository Recipes { get; }
            public INpcMissionRepository NpcMissions { get; }
            public INpcMissionRewardRepository NpcMissionRewards { get; }
            public IMissionContentRepository MissionContent { get; }
            public INpcPackageRepository NpcPackages { get; }
            public IPlayerRandomNameRepository RandomNames { get; }
            public ISpawnpoolRepository Spawnpools { get; }
            public ITeleporterRepository Teleporters { get; }
            public void Complete() { }
            public void Reject() { }
            public Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction BeginTransaction() =>
                throw new NotSupportedException();
            public void Dispose() { }
        }
    }
}
