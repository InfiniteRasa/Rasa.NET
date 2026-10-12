extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Navigation;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    // The world wormholes as one network (Wormholes): a wormhole's window lists every other one,
    // on any map, and picking one goes there, unless a row of wormhole_lock shuts the trip.
    [TestClass]
    [DoNotParallelize]
    public class WorldWormholeTests
    {
        private const uint Divide = WorldWormholesSeed.DivideWormholeId;           // 343, on the player's map
        private const uint Plains = WorldWormholesSeed.TordenPlainsWormholeId;     // 348, Arieki
        private const uint Crucible = WorldWormholesSeed.CrucibleWormholeId;       // 358, Arieki
        private const uint Abyss = WorldWormholesSeed.TordenAbyssWormholeId;       // 400, Arieki
        private const uint Plateau = 356;                                          // Foreas
        private const uint Arieki = WorldWormholesSeed.AriekiMissionId;

        [TestMethod]
        public void AWormholesWindowListsEveryOtherWormholeMapByMapGainedOrNot()
        {
            using var f = new Fixture(locked: false);
            var player = f.Player();

            // Gained already: only the one they stand in, and none of where they may go.
            player.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(player.Player.Id, Divide, (byte)WaypointType.Wormhole));
            f.Manager.PlayerEnterWaypoint(f.Here);

            var window = Packets(player).OfType<EnteredWaypointPacket>().Single();
            Assert.AreEqual(WaypointType.Wormhole, window.WaypointTypeId);
            Assert.AreEqual(Divide, window.CurrentWaypointId);
            Assert.IsNull(window.TempWormholes);

            // Not the one they stand in; the others under their own maps, where they are.
            CollectionAssert.AreEquivalent(new[] { f.Foreas.MapInfo.MapContextId, f.Arieki.MapInfo.MapContextId }, window.MapWaypointInfoList.Keys.ToArray());
            var foreas = window.MapWaypointInfoList[f.Foreas.MapInfo.MapContextId];
            Assert.AreEqual(Plateau, foreas.Waypoints.Single().WaypointId);
            Assert.AreEqual(f.Teleporter(Plateau).Position, foreas.Waypoints.Single().Position);
            Assert.AreEqual(f.Foreas.MapInfo.MapContextId, foreas.MapInstanceList.Single().MapContextId);
            CollectionAssert.AreEquivalent(new[] { Plains, Crucible, Abyss },
                window.MapWaypointInfoList[f.Arieki.MapInfo.MapContextId].Waypoints.Select(waypoint => waypoint.WaypointId).ToArray());

        }

        [TestMethod]
        public void FromDivideArriekiIsShutUntilMission1038HasBeenTaken()
        {
            using var f = new Fixture();
            var player = f.Player();

            CollectionAssert.AreEquivalent(new[] { Plateau }, f.Listed(player, Divide));

            // Taken and in the log.
            player.Player.Missions.Add(Arieki, new MissionLog(Arieki, MissionState.Active, false));
            CollectionAssert.AreEquivalent(new[] { Plateau, Plains, Crucible, Abyss }, f.Listed(player, Divide));

            // Taken and given up: it is in their history, and that is enough.
            player.Player.Missions.Remove(Arieki);
            Assert.IsEmpty(f.Listed(player, Divide).Where(id => id != Plateau).ToArray());
            player.Player.MissionHistory[Arieki] = MissionState.NotAssigned;
            CollectionAssert.AreEquivalent(new[] { Plateau, Plains, Crucible, Abyss }, f.Listed(player, Divide));
        }

        [TestMethod]
        public void TheLockIsDividesOnlyArriekiIsOpenFromEverywhereElse()
        {
            using var f = new Fixture();
            var player = f.Player();

            Assert.IsTrue(f.Manager.Wormholes.IsOpen(player.Player, Plateau, Plains));
            Assert.IsTrue(f.Manager.Wormholes.IsOpen(player.Player, Plains, Divide));
            Assert.IsTrue(f.Manager.Wormholes.IsOpen(player.Player, Divide, Plateau));
            Assert.IsFalse(f.Manager.Wormholes.IsOpen(player.Player, Divide, Plains));
            Assert.IsFalse(f.Manager.Wormholes.IsOpen(player.Player, Divide, Crucible));
            Assert.IsFalse(f.Manager.Wormholes.IsOpen(player.Player, Divide, Abyss));
        }

        [TestMethod]
        public void PickingAWormholeOnAnotherMapTakesThePlayerThere()
        {
            using var f = new Fixture();
            var player = f.Player();

            f.Manager.SelectWaypoint(player, new SelectWaypointPacket { MapInstanceId = f.Foreas.MapInfo.MapContextId, WaypointId = Plateau });

            var transfer = player.PendingTransfer;
            Assert.IsNotNull(transfer);
            Assert.AreSame(f.Foreas, transfer.DestinationMap);
            Assert.AreEqual(f.Teleporter(Plateau).Position + new Vector3(0, 1, 0), transfer.DestinationPosition);
            Assert.AreEqual((float)f.Teleporter(Plateau).Rotation, (float)transfer.DestinationRotation, 0.0001f);
            Assert.IsFalse(Packets(player).OfType<TeleportFailedPacket>().Any());
        }

        [TestMethod]
        public void AShutTripIsRefusedAndOpensOnceTheMissionIsTaken()
        {
            using var f = new Fixture();
            var player = f.Player();

            f.Manager.SelectWaypoint(player, new SelectWaypointPacket { MapInstanceId = f.Arieki.MapInfo.MapContextId, WaypointId = Plains });

            Assert.IsNull(player.PendingTransfer);
            Assert.IsTrue(Packets(player).OfType<TeleportFailedPacket>().Any());

            player.Player.MissionHistory[Arieki] = MissionState.Completed;
            f.Manager.SelectWaypoint(player, new SelectWaypointPacket { MapInstanceId = f.Arieki.MapInfo.MapContextId, WaypointId = Plains });

            Assert.AreSame(f.Arieki, player.PendingTransfer?.DestinationMap);
        }

        [TestMethod]
        public void OnlyAPlayerStandingInAWormholeMayTravel()
        {
            using var f = new Fixture();
            var player = f.Player(new Vector3(30, 0, 30));

            f.Manager.SelectWaypoint(player, new SelectWaypointPacket { MapInstanceId = f.Foreas.MapInfo.MapContextId, WaypointId = Plateau });

            Assert.IsNull(player.PendingTransfer);
            Assert.IsTrue(Packets(player).OfType<TeleportFailedPacket>().Any());

            // Standing at a waypoint is not standing at a wormhole.
            var waypoint = WaypointTravelTests.AddWaypoint(f.Manager, f.World.Map, 77, new Vector3(30, 0, 30));
            waypoint.RuntimeMapChannel = f.World.Map;
            f.Manager.SelectWaypoint(player, new SelectWaypointPacket { MapInstanceId = f.Foreas.MapInfo.MapContextId, WaypointId = Plateau });
            Assert.IsNull(player.PendingTransfer);
        }

        [TestMethod]
        public void ARequiredLevelIsReadAndNotEnforcedYetAndAKindNobodyKnowsShuts()
        {
            using var f = new Fixture(locked: false);
            var player = f.Player();
            player.Player.Level = 1;

            f.Manager.Wormholes.Load(new[]
            {
                new WormholeLockEntry { Id = 1, FromTeleporterId = 0, ToTeleporterId = Plateau, Kind = WormholeLockEntry.KindRequiredLevel, Value = 30 },
                new WormholeLockEntry { Id = 2, FromTeleporterId = 0, ToTeleporterId = Abyss, Kind = 99, Value = 1 }
            });

            Assert.IsTrue(f.Manager.Wormholes.IsOpen(player.Player, Divide, Plateau), "a level is not asked for yet");
            Assert.IsFalse(f.Manager.Wormholes.IsOpen(player.Player, Divide, Abyss));
            Assert.IsFalse(f.Manager.Wormholes.IsOpen(player.Player, Plains, Abyss), "0 is from anywhere");
            CollectionAssert.AreEquivalent(new[] { Plateau, Plains, Crucible }, f.Listed(player, Divide));
        }

        [TestMethod]
        public void TheMigrationAddsTheCrucibleWormholeTheLocksAndThePersonalWormholesClass()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                List<string> Rows(string sql)
                {
                    var rows = new List<string>();
                    var connection = context.Database.GetDbConnection();

                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;

                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                        rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i).ToString())));

                    return rows;
                }

                migrator.Migrate();

                CollectionAssert.AreEqual(new[] { "343|1148", "346|2051", "348|1764", "356|1497", "358|1993", "400|2028", "406|2047", "425|1416" },
                    Rows("select id, map_context_id from teleporter where type = 3 order by id"));
                Assert.AreEqual("25408|Wormhole: Ligo Crucible|966.44|140|59.95", Rows("select class_id, description, pos_x, pos_y, pos_z from teleporter where id = 358").Single());
                Assert.AreEqual("28474", Rows("select class_id from teleporter where id = 425").Single());
                CollectionAssert.AreEqual(new[] { "343|348|1|1038", "343|358|1|1038", "343|400|1|1038" },
                    Rows("select from_teleporter_id, to_teleporter_id, kind, value from wormhole_lock order by id"));

                // What the server reads.
                var locks = new TeleporterRepository((WorldContext)context).GetWormholeLocks();
                Assert.HasCount(3, locks);
                Assert.IsTrue(locks.All(row => row.Kind == WormholeLockEntry.KindMissionAccepted && row.Value == Arieki));

                CollectionAssert.Contains(Rows("select MigrationId from __EFMigrationsHistory"), "20261208000000_Add_world_wormholes");

                migrator.Migrate(BeforeWormholes);

                Assert.AreEqual("0|0|0|Wormhole: Ligo Crucible Dupe see 86", Rows("select class_id, type, map_context_id, description from teleporter where id = 358").Single());
                Assert.AreEqual("28478", Rows("select class_id from teleporter where id = 425").Single());
                Assert.AreEqual("0", Rows("select count(*) from sqlite_master where type = 'table' and name = 'wormhole_lock'").Single());
                Assert.AreEqual("7", Rows("select count(*) from teleporter where type = 3").Single());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TheMigrationIsTheSameStatementsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var script = context.GetService<IMigrator>()
                .GenerateScript(BeforeWormholes, "20261208000000_Add_world_wormholes");

            StringAssert.Contains(script, Rasa.Migrations.MySqlWorld.Add_world_wormholes.TableIfMissing);
            StringAssert.Contains(script, "CREATE TABLE IF NOT EXISTS `wormhole_lock`");
            StringAssert.Contains(script, WorldWormholesSeed.ForgetFirstIdStatement);

            foreach (var statement in WorldWormholesSeed.InsertStatements)
                StringAssert.Contains(script, statement);
        }

        // The migration was numbered 20261207000000 at first, as Add_control_point_turrets is. A
        // world that ran it under that id sees 20261208000000_Add_world_wormholes as pending: it
        // runs without an error, leaves the rows as they were, and takes the first id off the
        // history.
        [TestMethod]
        public void AWorldThatRanItUnderItsFirstIdRunsItAgainUnchanged()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                List<string> Rows(string sql)
                {
                    var rows = new List<string>();
                    var connection = context.Database.GetDbConnection();

                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;

                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                        rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i).ToString())));

                    return rows;
                }

                migrator.Migrate();

                // As such a world stands: everything the migration does, under the first id.
                Rows($"update __EFMigrationsHistory set MigrationId = '{WorldWormholesSeed.FirstMigrationId}' where MigrationId = '20261208000000_Add_world_wormholes'");
                CollectionAssert.AreEqual(new[] { "20261208000000_Add_world_wormholes" }, context.Database.GetPendingMigrations().ToArray());

                var before = new[]
                {
                    "select id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id from teleporter where id in (358, 425) order by id",
                    "select id, from_teleporter_id, to_teleporter_id, kind, value, comment from wormhole_lock order by id",
                    "select sql from sqlite_master where name = 'wormhole_lock'"
                }.Select(Rows).ToList();

                migrator.Migrate();

                Assert.IsEmpty(context.Database.GetPendingMigrations().ToArray());
                var history = Rows("select MigrationId from __EFMigrationsHistory");
                CollectionAssert.Contains(history, "20261208000000_Add_world_wormholes");
                CollectionAssert.DoesNotContain(history, WorldWormholesSeed.FirstMigrationId);
                CollectionAssert.Contains(history, "20261207000000_Add_control_point_turrets");

                var after = new[]
                {
                    "select id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id from teleporter where id in (358, 425) order by id",
                    "select id, from_teleporter_id, to_teleporter_id, kind, value, comment from wormhole_lock order by id",
                    "select sql from sqlite_master where name = 'wormhole_lock'"
                }.Select(Rows).ToList();

                for (var i = 0; i < before.Count; i++)
                    CollectionAssert.AreEqual(before[i], after[i]);

                Assert.HasCount(3, after[1]);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        // The table the migration makes, when it is missing, is the one it made under its first
        // id, with CreateTable - which is what a world that ran it then has - on either provider.
        [TestMethod]
        [DataRow(typeof(SqliteWorldContext))]
        [DataRow(typeof(MySqlWorldContext))]
        public void TheTableIsTheOneCreateTableWouldMake(Type contextType)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(contextType, Path.Combine(directory, "database"));
                var sqlite = contextType == typeof(SqliteWorldContext);
                var integer = sqlite ? "INTEGER" : "int unsigned";
                var first = new MigrationBuilder(context.Database.ProviderName);

                // The first version's Up, as it was.
                first.CreateTable(
                    name: WormholeLockEntry.TableName,
                    columns: table => new
                    {
                        id = sqlite
                            ? table.Column<uint>(type: integer, nullable: false).Annotation("Sqlite:Autoincrement", true)
                            : table.Column<uint>(type: integer, nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                        from_teleporter_id = table.Column<uint>(type: integer, nullable: false),
                        to_teleporter_id = table.Column<uint>(type: integer, nullable: false),
                        kind = table.Column<uint>(type: integer, nullable: false),
                        value = table.Column<uint>(type: integer, nullable: false),
                        comment = table.Column<string>(type: "varchar(128)", nullable: true)
                    },
                    constraints: table => table.PrimaryKey("PK_wormhole_lock", x => x.id));

                var made = context.GetService<IMigrationsSqlGenerator>()
                    .Generate(first.Operations, context.GetService<IDesignTimeModel>().Model).Single().CommandText;

                var ours = contextType == typeof(SqliteWorldContext)
                    ? Rasa.Migrations.SqliteWorld.Add_world_wormholes.TableIfMissing
                    : Rasa.Migrations.MySqlWorld.Add_world_wormholes.TableIfMissing;

                Assert.AreEqual(Flat(made).Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS "), Flat(ours));
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }

            static string Flat(string sql) => string.Join(" ", sql.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>The migration before this one, now that it is numbered after Add_control_point_turrets.</summary>
        private const string BeforeWormholes = "20261207000000_Add_control_point_turrets";

        [TestMethod]
        public void TheCrucibleWormholeStandsOnTheFloorOfOutpostIntrepid()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Repository root not found.");

            var nav = TestNavMeshes.Query(NavMeshFile.PathFor(Path.Combine(directory.FullName, "navmesh"), "adv_arieki_ligo_burningsteps"));
            var row = WorldWormholesSeed.CrucibleWormhole;
            var position = new Vector3((float)(double)row[4], (float)(double)row[5], (float)(double)row[6]);

            Assert.IsTrue(nav.IsOnMesh(position));
            Assert.AreEqual(nav.GroundHeight(position).Value, position.Y, 0.35f);

            // Walkable to the outpost's waypoint (teleporter 191), whose hall it is in.
            nav.FindPath(position, new Vector3(943.6836f, 144.0039f, 60.527344f), out var reached);
            Assert.IsTrue(reached);
        }

        #region Helpers

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        /// <summary>
        /// The player's map has the Divide wormhole (343) at the origin; a Foreas map has
        /// Valverde Plateau's (356); an Arieki one has Torden Plains', Ligo Crucible's and Torden
        /// Abyss' - the network in small, with the seed's lock unless told otherwise.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            internal WorldTestContext World { get; } = new WorldTestContext();
            internal MapChannel Foreas { get; } = Map(1497, "adv_foreas_fixture_plateau");
            internal MapChannel Arieki { get; } = Map(1764, "adv_arieki_fixture_plains");
            internal DynamicObjectManager Manager { get; }
            internal DynamicObject Here { get; }

            internal Fixture(bool locked = true)
            {
                var maps = new MapChannelManager(null, () => 1000,
                    updateCharacter: (_, _, _) => { },
                    refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });
                maps.MapChannelArray.Add(World.Map.MapInfo.MapContextId, World.Map);
                maps.MapChannelArray.Add(Foreas.MapInfo.MapContextId, Foreas);
                maps.MapChannelArray.Add(Arieki.MapInfo.MapContextId, Arieki);

                Manager = new DynamicObjectManager(null, maps, () => 1000, (_, _, _) => { },
                    client => client.State = ClientState.Disconnected);

                Here = Wormhole(World.Map, Divide, Vector3.Zero);
                Wormhole(Foreas, Plateau, new Vector3(-283, 421, 848), 4.19);
                Wormhole(Arieki, Plains, new Vector3(316, 430, -221), 3.15);
                Wormhole(Arieki, Crucible, new Vector3(96, 144, 59));
                Wormhole(Arieki, Abyss, new Vector3(170, 542, -558));

                if (locked)
                    Manager.Wormholes.Load(WorldWormholesSeed.Locks.Select(row => new WormholeLockEntry
                    {
                        Id = (uint)row[0],
                        FromTeleporterId = (uint)row[1],
                        ToTeleporterId = (uint)row[2],
                        Kind = (uint)row[3],
                        Value = (uint)row[4]
                    }));
            }

            internal DynamicObject Teleporter(uint id) => Manager.Teleporters[id];

            /// <summary>A player standing in the Divide wormhole, with nothing waiting to be read.</summary>
            internal Client Player(Vector3? at = null)
            {
                var client = World.CreateClient((at ?? Vector3.Zero).X, (at ?? Vector3.Zero).Z);

                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);

                return client;
            }

            /// <summary>The wormholes the player's window at <paramref name="fromId"/> lists.</summary>
            internal uint[] Listed(Client client, uint fromId) =>
                Manager.CreateListOfWormholes(client, fromId).Values.SelectMany(list => list.Waypoints).Select(waypoint => waypoint.WaypointId).ToArray();

            private DynamicObject Wormhole(MapChannel map, uint id, Vector3 position, double rotation = 0)
            {
                var wormhole = WaypointTravelTests.AddWaypoint(Manager, map, id, position, type: WaypointType.Wormhole);

                wormhole.Rotation = rotation;
                wormhole.RuntimeMapChannel = map;

                return wormhole;
            }

            private static MapChannel Map(uint contextId, string name) => new MapChannel
            {
                MapInfo = new MapInfo(contextId, name, 1, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128
            };

            public void Dispose()
            {
                World.Dispose();
            }
        }

        #endregion
    }
}
