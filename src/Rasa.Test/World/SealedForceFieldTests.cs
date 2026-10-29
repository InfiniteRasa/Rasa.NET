using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// A force field a mission takes down (world_destructible.sealed, MissionActionKind.TakeDownField):
    /// a sealed row's field in every copy of its map takes nothing from shots; the action takes the
    /// one in the player's copy down for good and leaves the others'; an unsealed row is as it was;
    /// the validator, the transition, and the column and kind on both providers.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SealedForceFieldTests
    {
        private const uint Wilderness = 1220;
        private const EntityClasses BaneGate = (EntityClasses)9572;     // UsableForceFieldBaneOutpostWallMajorV01
        private const EntityClasses Barrel = (EntityClasses)9260;       // UsableInertDestBaneBarrelV01

        private const string BeforeSealed = "20270919000000_Npc_squad_tether";
        private const string Sealed = "20270921000000_World_destructible_sealed";
        private const string TakeDownAction = "20270921000100_Take_down_field_action";

        private long _now;
        private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _classes = new();

        [TestInitialize]
        public void Start()
        {
            _now = 9_000_000;
            PlacedDestructibles.Now = () => _now;
            PlacedDestructibles.Roll = (min, max) => 50_000;

            AddClass(BaneGate, "UsableForceFieldBaneOutpostWallMajorV01", AugmentationType.ForceField);
            AddClass(Barrel, "UsableInertDestBaneBarrelV01", AugmentationType.InertDestroyable);
        }

        [TestCleanup]
        public void Restore()
        {
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();

            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            for (var i = _classes.Count - 1; i >= 0; i--)
            {
                if (_classes[i].Had)
                    classes[_classes[i].Id] = _classes[i].Was;
                else
                    classes.Remove(_classes[i].Id);
            }
        }

        private void AddClass(EntityClasses id, string name, params AugmentationType[] augmentations)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            _classes.Add((id, classes.TryGetValue(id, out var was), was));
            classes[id] = new EntityClass((uint)id, name, 0, 1, augmentations.ToList(), true);
        }

        private static WorldDestructibleEntry Row(uint id, EntityClasses classId, bool @sealed, uint map = Wilderness) => new()
        {
            Id = id, MapContextId = map, ClassId = (uint)classId, PosX = 10 + id % 50 * 20, PosY = 2, PosZ = 20, Rotation = 0.5,
            Sealed = @sealed, Comment = "fixture"
        };

        /// <summary>The rows on two copies of the map; each copy's taken away when the test is done.</summary>
        private sealed class Copies : IDisposable
        {
            public WorldTestContext A { get; } = new WorldTestContext();
            public WorldTestContext B { get; } = new WorldTestContext();

            public Copies(params WorldDestructibleEntry[] rows)
            {
                // Two squads' copies of the map.
                A.Map.IsSquadInstance = true;
                B.Map.IsSquadInstance = true;
                PlacedDestructibles.Load(rows);
                PlacedDestructibles.Place(A.Map);
                PlacedDestructibles.Place(B.Map);
            }

            public void Dispose()
            {
                A.Map.SquadState = null;
                A.Map.IsSquadInstance = false;
                B.Map.IsSquadInstance = false;
                PlacedDestructibles.Forget(A.Map);
                PlacedDestructibles.Forget(B.Map);
                B.Dispose();
                A.Dispose();
            }
        }

        [TestMethod]
        public void ASealedRowsFieldTakesNothingFromShotsInEveryCopy()
        {
            using var copies = new Copies(Row(1, BaneGate, @sealed: true));
            var shooter = copies.A.CreateClient(30, 20).Player;

            foreach (var map in new[] { copies.A.Map, copies.B.Map })
            {
                var live = PlacedDestructibles.LiveOf(map, 1);
                Assert.IsTrue(live.Placement.Sealed);
                Assert.IsTrue(live.Field.Sealed, "sealed in each copy");
                Assert.AreEqual(ForceFields.Side.B, live.Field.Side, "a Bane field, which stops players");
                Assert.IsFalse(ForceFields.MayShoot(live.Field, shooter), "a player's shot is refused");
                Assert.IsFalse(live.IsDown);
            }

            Assert.AreNotSame(PlacedDestructibles.LiveOf(copies.A.Map, 1).Field, PlacedDestructibles.LiveOf(copies.B.Map, 1).Field);
        }

        [TestMethod]
        public void TheActionTakesItDownForGoodInOneCopyAndTheOtherKeepsIt()
        {
            using var copies = new Copies(Row(1, BaneGate, @sealed: true));
            var a = PlacedDestructibles.LiveOf(copies.A.Map, 1);
            var b = PlacedDestructibles.LiveOf(copies.B.Map, 1);

            Assert.IsTrue(PlacedDestructibles.TakeDown(copies.A.Map, 1, 0));

            Assert.IsTrue(a.IsDown);
            Assert.IsFalse(ForceFields.StopsPlayer(a.Field), "players go through");
            Assert.AreEqual(long.MaxValue, a.RespawnAt, "never due back");
            Assert.IsFalse(b.IsDown, "the other copy's stands");
            Assert.IsTrue(b.Field.Sealed, "and is still sealed");
            Assert.IsTrue(ForceFields.StopsPlayer(b.Field));

            // Well past an ordinary field's time back, and past a squad instance's own respawn.
            copies.A.Map.SquadState = new SquadInstanceState(0, 0, () => 1, () => 1_800_000, (_, _, _) => { }, (_, _) => { }, _ => { });
            for (var i = 0; i < 4; i++)
            {
                _now += PlacedDestructibles.RespawnMaxMs + 1_800_000;
                PlacedDestructibles.Worker(copies.A.Map);
                PlacedDestructibles.Worker(copies.B.Map);
            }

            Assert.IsTrue(a.IsDown, "down for the rest of the copy's life");
            Assert.AreEqual(long.MaxValue, a.RespawnAt);
            Assert.IsFalse(b.IsDown);

            // Again: nothing more to do, and still down for good.
            Assert.IsTrue(PlacedDestructibles.TakeDown(copies.A.Map, 1, 0));
            Assert.IsTrue(a.IsDown);
        }

        [TestMethod]
        public void AnUnsealedRowIsShotDownAndComesBackAsBefore()
        {
            using var copies = new Copies(Row(1, BaneGate, @sealed: false), Row(2, Barrel, @sealed: true));
            var shooter = copies.A.CreateClient(30, 20).Player;
            var field = PlacedDestructibles.LiveOf(copies.A.Map, 1);

            Assert.IsFalse(field.Placement.Sealed);
            Assert.IsFalse(field.Field.Sealed);
            Assert.IsTrue(ForceFields.MayShoot(field.Field, shooter), "players shoot it as before");
            Assert.IsFalse(PlacedDestructibles.LiveOf(copies.A.Map, 2).Placement.Sealed, "sealed is only a force field's");

            ForceFields.Damage(field.Field, field.Field.Health, shooter.EntityId);
            PlacedDestructibles.Worker(copies.A.Map);
            Assert.AreEqual(_now + 50_000, field.RespawnAt);
            _now += 50_000;
            PlacedDestructibles.Worker(copies.A.Map);
            Assert.IsFalse(field.IsDown, "back");
            Assert.IsFalse(field.Field.Sealed);
        }

        [TestMethod]
        public void TheActionDoesNothingForARowThatIsNoFieldOrNotOnThisMap()
        {
            using var copies = new Copies(Row(1, BaneGate, @sealed: true), Row(2, Barrel, @sealed: false), Row(3, BaneGate, @sealed: true, map: 9999));

            Assert.IsFalse(PlacedDestructibles.TakeDown(copies.A.Map, 2, 0), "a barrel");
            Assert.IsFalse(PlacedDestructibles.TakeDown(copies.A.Map, 3, 0), "another map's");
            Assert.IsFalse(PlacedDestructibles.TakeDown(copies.A.Map, 77, 0), "no such row");
            Assert.IsFalse(PlacedDestructibles.TakeDown(null, 1, 0), "no map");
            Assert.IsFalse(PlacedDestructibles.LiveOf(copies.A.Map, 2).IsDown);
            Assert.IsFalse(PlacedDestructibles.LiveOf(copies.A.Map, 1).IsDown);
        }

        [TestMethod]
        public void OnTheOpenWorldNothingIsTakenDown()
        {
            using var world = new WorldTestContext();
            PlacedDestructibles.Load(new[] { Row(1, BaneGate, @sealed: true) });
            PlacedDestructibles.Place(world.Map);

            try
            {
                Assert.IsFalse(world.Map.IsCopy);
                var log = Logged(() => Assert.IsFalse(PlacedDestructibles.TakeDown(world.Map, 1, 0)));

                var live = PlacedDestructibles.LiveOf(world.Map, 1);
                Assert.IsFalse(live.IsDown, "the open world's stands for everyone");
                Assert.IsTrue(live.Field.Sealed);
                StringAssert.Contains(log, "is no copy");
            }
            finally
            {
                PlacedDestructibles.Forget(world.Map);
            }
        }

        [TestMethod]
        public void ATransitionWhileThePlayerIsOnAnotherMapLogsNoErrorAndLeavesTheFieldUp()
        {
            using var copies = new Copies(Row(1, BaneGate, @sealed: true), Row(3, BaneGate, @sealed: true, map: 9999));

            var log = Logged(() => Assert.IsFalse(PlacedDestructibles.TakeDown(copies.A.Map, 3, 0), "another map's"));

            var lines = log.Split('\n').Where(line => line.Contains("world_destructible 3")).ToList();
            Assert.HasCount(1, lines, log);
            StringAssert.Contains(lines[0], "on map 9999, not 1220");
            Assert.IsFalse(lines[0].Contains("Error"), lines[0]);
            Assert.IsFalse(PlacedDestructibles.LiveOf(copies.A.Map, 1).IsDown, "this map's stands");
        }

        /// <summary>What the logger wrote while <paramref name="action"/> ran, Debug included.</summary>
        private static string Logged(Action action)
        {
            var previousOutput = Console.Out;
            var previousLogging = Logger.Config;
            using var output = new StringWriter();
            Logger.UpdateConfig(new Logger.LoggerConfig { IsDebugMode = true, LogToFile = false });
            Logger.Flush();
            Console.SetOut(output);

            try
            {
                action();
                Logger.Flush();
            }
            finally
            {
                Console.SetOut(previousOutput);
                Logger.UpdateConfig(previousLogging);
            }

            return output.ToString();
        }

        [TestMethod]
        public void AGameMastersBringingItBackSealsItAgain()
        {
            using var copies = new Copies(Row(1, BaneGate, @sealed: true));
            var live = PlacedDestructibles.LiveOf(copies.A.Map, 1);

            PlacedDestructibles.TakeDown(copies.A.Map, 1, 0);
            PlacedDestructibles.Respawn(live);

            Assert.IsFalse(live.IsDown);
            Assert.IsTrue(live.Field.Sealed);
        }

        #region The validator

        private static MissionContentFixture FixtureWith(uint destructibleId, params WorldDestructibleEntry[] rows)
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Destructibles.AddRange(rows);
            fixture.Actions.Add(new MissionActionEntry
            {
                MissionId = 321, ContentRevision = "deployment_11", ObjectiveId = 10, TransitionId = 20,
                ActionId = 5, Sequence = 5, Requirement = MissionContentRequirement.Required,
                Kind = MissionActionKind.TakeDownField,
                ItemIntentJson = JsonSerializer.Serialize<CharacterIntent>(new TakeDownFieldIntent("take-down-gate", destructibleId),
                    MissionContentCodec.Options),
                Comment = "Take the gate's field down"
            });
            return fixture;
        }

        private static string[] Codes(MissionContentFixture fixture) =>
            new MissionContentValidator().Validate(new MissionContentLoader().Load(fixture.CreateRepository()), fixture.CreateWorldUnitOfWork())
                .Diagnostics.Select(diagnostic => diagnostic.Code).ToArray();

        [TestMethod]
        public void TheValidatorTakesARowThatIsAForceFieldAndRejectsOneThatIsMissingOrNoField()
        {
            const uint torcastra = 1349;    // a squad's instance
            var valid = Codes(FixtureWith(1, Row(1, BaneGate, @sealed: true, map: torcastra)));
            Assert.AreEqual("", string.Join(", ", valid));
            CollectionAssert.Contains(Codes(FixtureWith(1, Row(1, BaneGate, @sealed: false, map: torcastra))), "destructible-not-sealed");

            // Sealed on a map that is no squad's instance: said, for the row and for the action.
            var open = Codes(FixtureWith(1, Row(1, BaneGate, @sealed: true)));
            CollectionAssert.Contains(open, "destructible-not-in-squad-instance");
            CollectionAssert.Contains(open, "sealed-destructible-not-in-squad-instance");

            CollectionAssert.Contains(Codes(FixtureWith(2, Row(1, BaneGate, @sealed: true))), "missing-destructible");
            CollectionAssert.Contains(Codes(FixtureWith(2, Row(2, Barrel, @sealed: false))), "destructible-not-force-field");
            CollectionAssert.Contains(Codes(FixtureWith(0, Row(1, BaneGate, @sealed: true))), "missing-destructible",
                "row 0 names nothing");

            // Another intent under the kind does not match it.
            var wrong = FixtureWith(1, Row(1, BaneGate, @sealed: true));
            wrong.Actions.Last().ItemIntentJson = JsonSerializer.Serialize<CharacterIntent>(new FailRelatedMissionIntent("x", 700),
                MissionContentCodec.Options);
            CollectionAssert.Contains(Codes(wrong), "invalid-item-action");
        }

        #endregion

        #region The transition

        [TestMethod]
        public void ATransitionTakesTheFieldDownInThePlayersCopyOnly()
        {
            using var harness = BootcampRuntimeTestHarness.CreateFromPendingSelection();
            const uint rowId = 990_001;

            harness.WorldContext.WorldDestructibleEntries.Add(Row(rowId, BaneGate, @sealed: true, map: BootcampRuntimeTestHarness.BootcampMapContextId));
            harness.WorldContext.MissionActionEntries.Add(new MissionActionEntry
            {
                MissionId = 1990, ContentRevision = "deployment_11", ObjectiveId = 1, TransitionId = 1,
                ActionId = 99, Sequence = 99, Kind = MissionActionKind.TakeDownField,
                ItemIntentJson = JsonSerializer.Serialize<CharacterIntent>(new TakeDownFieldIntent("take-down-gate", rowId),
                    MissionContentCodec.Options),
                Comment = "Take the gate's field down"
            });
            harness.WorldContext.SaveChanges();
            var report = harness.Manager.LoadMissions();
            Assert.IsFalse(report.BlocksReadiness);
            Assert.IsFalse(report.Diagnostics.Any(diagnostic => diagnostic.MissionId == 1990), "the action is valid");

            // The row read back from the table, sealed; on the player's copy and on the open map.
            var rows = new WorldDestructibleRepository(harness.WorldContext).Get().Where(row => row.Id == rowId).ToList();
            Assert.IsTrue(rows.Single().Sealed);
            PlacedDestructibles.Load(rows);
            var copy = harness.Client.Player.MapChannel;
            var open = harness.Maps.MapChannelArray[BootcampRuntimeTestHarness.BootcampMapContextId];
            Assert.AreNotSame(copy, open, "the player is in a copy");
            PlacedDestructibles.Place(copy);
            PlacedDestructibles.Place(open);

            try
            {
                Assert.IsTrue(PlacedDestructibles.LiveOf(copy, rowId).Field.Sealed);

                var giver = harness.AddNpc(BootcampRuntimeTestHarness.MajorMcAllisterCreatureId);
                Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, giver.EntityId, 1990));
                Assert.IsFalse(PlacedDestructibles.LiveOf(copy, rowId).IsDown, "nothing before the transition");

                // A player in the copy without the progress: the field stays up.
                PlacedDestructibles.MissionsOf = () => harness.Manager;
                var builds = PlacedDestructibles.TakeDownIndexBuilds;
                Assert.IsTrue(copy.IsCopy);
                PlacedDestructibles.Worker(copy);
                Assert.IsFalse(PlacedDestructibles.LiveOf(copy, rowId).IsDown, "no progress, no catch-up");

                Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Area(1990, 430)));

                var taken = PlacedDestructibles.LiveOf(copy, rowId);
                Assert.IsTrue(taken.IsDown, "the copy's field is down");
                Assert.AreEqual(long.MaxValue, taken.RespawnAt, "for good");
                Assert.IsFalse(PlacedDestructibles.LiveOf(open, rowId).IsDown, "the open map's stands");
                Assert.IsTrue(PlacedDestructibles.LiveOf(open, rowId).Field.Sealed);

                // The copy closed and made again: its field up and sealed, until a member who has
                // been through the transition is in it.
                PlacedDestructibles.Forget(copy);
                PlacedDestructibles.Place(copy);
                var again = PlacedDestructibles.LiveOf(copy, rowId);
                Assert.AreNotSame(taken, again);
                Assert.IsFalse(again.IsDown);
                Assert.IsTrue(again.Field.Sealed);

                PlacedDestructibles.Worker(copy);
                Assert.IsTrue(again.IsDown, "down again with the member in");
                Assert.AreEqual(builds + 1, PlacedDestructibles.TakeDownIndexBuilds, "the index, built once for the loaded missions");
                CollectionAssert.Contains(PlacedDestructibles.TakeDownsOf(harness.Manager.LoadedMissions)[rowId],
                    (1990u, 1u, MissionObjectiveState.Completed));
                Assert.AreEqual(long.MaxValue, again.RespawnAt);
                Assert.IsFalse(again.Field.Sealed);

                // The open map is no copy: a member there leaves its field alone.
                PlacedDestructibles.Worker(open);
                Assert.IsFalse(PlacedDestructibles.LiveOf(open, rowId).IsDown);
            }
            finally
            {
                PlacedDestructibles.Forget(copy);
                PlacedDestructibles.Forget(open);
            }
        }

        #endregion

        #region The table

        [TestMethod]
        public void OnSqliteTheColumnAndTheKindComeAndGo()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());
                    Assert.IsFalse(context.Database.HasPendingModelChanges());
                    context.Database.ExecuteSqlRaw("DELETE FROM world_destructible");

                    // A row put in without it is not sealed; a lot's SealedIds are.
                    context.Database.ExecuteSqlRaw(
                        "INSERT INTO world_destructible (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, hit_points, creature_id, creature_count, side, comment) " +
                        "VALUES (5, 1220, 9572, 0, 0, 0, 0, 0, 0, 0, 0, 'old')");
                    var builder = new MigrationBuilder(context.Database.ProviderName);
                    new SealedLot().Preload(builder);
                    Run(context, builder);

                    var rows = new WorldDestructibleRepository(context).Get().ToDictionary(row => row.Id);
                    Assert.IsFalse(rows[5].Sealed, "0 by default");
                    Assert.IsTrue(rows[1].Sealed, "sealed by the lot");
                    Assert.IsFalse(rows[2].Sealed);
                    StringAssert.Contains(ActionCheck(context), "kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)");
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var migrator = context.GetService<IMigrator>();

                    migrator.Migrate(Sealed);
                    StringAssert.Contains(ActionCheck(context), "kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13))", "the kind is out again");
                    Assert.IsTrue(new WorldDestructibleRepository(context).Get().Single(row => row.Id == 1).Sealed, "the rows kept");

                    migrator.Migrate(BeforeSealed);
                    Assert.IsFalse(Columns(context).Contains("sealed"), "the column is dropped");
                    Assert.IsTrue(Columns(context).Contains("side"));

                    migrator.Migrate(TakeDownAction);
                    Assert.IsTrue(Columns(context).Contains("sealed"), "and comes back");
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void OnMySqlTheColumnAndTheKindComeAndGo()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(BeforeSealed, TakeDownAction);
            var down = migrator.GenerateScript(TakeDownAction, BeforeSealed);

            StringAssert.Contains(up, "ALTER TABLE `world_destructible` ADD `sealed` tinyint(1) NOT NULL DEFAULT FALSE");
            StringAssert.Contains(up, "kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14)");
            StringAssert.Contains(down, "ALTER TABLE `world_destructible` DROP COLUMN `sealed`");
            StringAssert.Contains(down, "kind IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13))");
        }

        private static string ActionCheck(WorldContext context)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'mission_action'";
            return (string)command.ExecuteScalar();
        }

        private static List<string> Columns(WorldContext context)
        {
            var connection = context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM pragma_table_info('world_destructible')";
            using var reader = command.ExecuteReader();
            var names = new List<string>();
            while (reader.Read())
                names.Add(reader.GetString(0));
            return names;
        }

        private static void Run(WorldContext context, MigrationBuilder builder)
        {
            var sql = context.GetService<IMigrationsSqlGenerator>().Generate(builder.Operations, context.Model);

            foreach (var command in sql)
                context.Database.ExecuteSqlRaw(command.CommandText);
        }

        private sealed class SealedLot : WorldDestructiblePreloader
        {
            protected override IEnumerable<object[]> GetRows()
            {
                yield return Row(1, 1220, 9572, 10, 2, 20, 0.5, "the gate's field");
                yield return Row(2, 1220, 9572, 40, 2, 20, 0.5, "a field players shoot down");
            }

            protected override IEnumerable<uint> SealedIds => new uint[] { 1 };
        }

        #endregion
    }
}
