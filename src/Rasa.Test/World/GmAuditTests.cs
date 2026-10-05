using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Repositories.Char.GmCommandLog;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// The game master audit log (GmAudit, the gm_command_log table): what goes on it, for
    /// whom, and that it is there before the command runs.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GmAuditTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>The log kept in memory: the rows a store was given, as they were given.</summary>
        private sealed class MemoryStore : GmAudit.IStore
        {
            private static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

            public List<GmCommandLogEntry> Rows { get; } = new List<GmCommandLogEntry>();

            /// <summary>Set, every call throws it.</summary>
            public Exception Fails { get; set; }

            public uint Add(GmCommandLogEntry entry)
            {
                if (Fails != null)
                    throw Fails;

                var copy = (GmCommandLogEntry)Clone.Invoke(entry, null);

                copy.Id = (uint)Rows.Count + 1;
                Rows.Add(copy);

                return copy.Id;
            }

            public void SetResult(uint id, GmCommandResult result)
            {
                if (Fails != null)
                    throw Fails;

                Rows.Single(r => r.Id == id).Result = (byte)result;
            }
        }

        private DateTime _utc = Noon;
        private long _now = 1_000_000;

        private (GmAudit Audit, MemoryStore Store) Log()
        {
            var store = new MemoryStore();
            var audit = new GmAudit { UtcNow = () => _utc, Now = () => _now };

            audit.Load(store);

            return (audit, store);
        }

        private static Client As(Client client, uint accountId, GmLevel level)
        {
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = accountId, Level = (byte)level });

            return client;
        }

        #region Dot commands

        [TestMethod]
        public void ACommandIsOnTheLogBeforeItRunsWithWhoEnteredItAndWhere()
        {
            using var world = new WorldTestContext();
            var (audit, store) = Log();
            var master = As(world.CreateClient(12, 34), 7, GmLevel.Admin);
            var other = world.CreateClient(1, 1);

            master.Player.FamilyName = "Master";
            other.Player.FamilyName = "Other";
            master.Player.Target = other.Player.EntityId;

            var commands = new ChatCommandsManager(null) { Audit = audit };
            var seen = -1;
            string[] given = null;

            commands.RegisterCommand(".probe", GmLevel.GameMaster, parts =>
            {
                seen = store.Rows.Count;
                given = parts;
            }, "what");

            commands.ProcessCommand(master, ".probe one two");

            Assert.AreEqual(1, seen, "written before the command ran");
            CollectionAssert.AreEqual(new[] { ".probe", "one", "two" }, given);

            var row = store.Rows.Single();

            Assert.AreEqual(Noon, row.CreatedAt);
            Assert.AreEqual((byte)GmCommandSource.Chat, row.Source);
            Assert.AreEqual((byte)GmCommandResult.Executed, row.Result);
            Assert.AreEqual(7u, row.AccountId);
            Assert.AreEqual((byte)GmLevel.Admin, row.AccountLevel);
            Assert.AreEqual((byte)GmLevel.GameMaster, row.RequiredLevel, "what the command needs, not what the account has");
            Assert.AreEqual(master.Player.Id, row.CharacterId);
            Assert.AreEqual(master.Player.Name, row.Name);
            Assert.AreEqual("Master", row.FamilyName);
            Assert.AreEqual("", row.Address, "a client with no connection has no address");
            Assert.AreEqual(world.Map.MapInfo.MapContextId, row.MapContextId);
            Assert.AreEqual(world.Map.InstanceId, row.InstanceId);
            Assert.AreEqual((12d, 0d, 34d), (row.CoordX, row.CoordY, row.CoordZ));
            Assert.AreEqual(other.Player.Id, row.TargetCharacterId, "who they had selected");
            Assert.AreEqual("player Other", row.Target);
            Assert.AreEqual(".probe", row.Command);
            Assert.AreEqual(".probe one two", row.Text, "the whole line, as entered");

            // Nothing selected, and a line longer than the log keeps.
            master.Player.Target = 0;
            _utc = Noon.AddMinutes(1);
            commands.ProcessCommand(master, ".probe " + new string('x', 600));

            Assert.AreEqual(2, store.Rows.Count);
            Assert.AreEqual(Noon.AddMinutes(1), store.Rows[1].CreatedAt);
            Assert.AreEqual((0u, ""), (store.Rows[1].TargetCharacterId, store.Rows[1].Target));
            Assert.AreEqual(GmCommandLogEntry.MaxTextLength, store.Rows[1].Text.Length);
            StringAssert.StartsWith(store.Rows[1].Text, ".probe xxx");
        }

        [TestMethod]
        public void WhatAGameMasterIsRefusedOrMistypesIsOnTheLogToo()
        {
            using var world = new WorldTestContext();
            var (audit, store) = Log();
            var observer = As(world.CreateClient(), 8, GmLevel.Observer);
            var commands = new ChatCommandsManager(null) { Audit = audit };
            var ran = 0;

            commands.RegisterCommand(".grant", GmLevel.Admin, parts => ran++);

            commands.ProcessCommand(observer, ".grant 500");
            commands.ProcessCommand(observer, ".grnat 500");

            Assert.AreEqual(0, ran);
            Assert.AreEqual(2, store.Rows.Count);

            Assert.AreEqual((byte)GmCommandResult.Denied, store.Rows[0].Result);
            Assert.AreEqual(".grant", store.Rows[0].Command);
            Assert.AreEqual(".grant 500", store.Rows[0].Text);
            Assert.AreEqual(((byte)GmLevel.Observer, (byte)GmLevel.Admin), (store.Rows[0].AccountLevel, store.Rows[0].RequiredLevel));

            Assert.AreEqual((byte)GmCommandResult.Unknown, store.Rows[1].Result);
            Assert.AreEqual(".grnat", store.Rows[1].Command);
            Assert.AreEqual((byte)0, store.Rows[1].RequiredLevel);
        }

        [TestMethod]
        public void AnOrdinaryPlayerHasOnlyRefusalsRecordedAndOnlySoMany()
        {
            using var world = new WorldTestContext();
            var (audit, store) = Log();
            var player = As(world.CreateClient(), 9, GmLevel.Player);
            var another = As(world.CreateClient(), 10, GmLevel.Player);
            var commands = new ChatCommandsManager(null) { Audit = audit };
            var ran = 0;

            commands.RegisterCommand(".grant", GmLevel.Admin, parts => ran++);

            // A line that begins with a dot and is no command is chat that went nowhere.
            commands.ProcessCommand(player, ".hello there");
            commands.ProcessCommand(player, "...");
            Assert.AreEqual(0, store.Rows.Count);

            // A real command tried without the level for it is on the log - ten of them a minute.
            for (var i = 0; i < 25; i++)
                commands.ProcessCommand(player, $".grant {i}");

            Assert.AreEqual(0, ran);
            Assert.AreEqual(GmAudit.UnprivilegedPerMinute, store.Rows.Count);
            Assert.IsTrue(store.Rows.All(r => r.Result == (byte)GmCommandResult.Denied && r.AccountId == 9 && r.AccountLevel == 0));
            Assert.AreEqual(".grant 9", store.Rows.Last().Text, "the first ten");

            // Each account has its own count, and a minute later there is room again.
            commands.ProcessCommand(another, ".grant");
            Assert.AreEqual(11, store.Rows.Count);
            Assert.AreEqual(10u, store.Rows.Last().AccountId);

            _now += 59_000;
            commands.ProcessCommand(player, ".grant late");
            Assert.AreEqual(11, store.Rows.Count);

            _now += 1_000;
            commands.ProcessCommand(player, ".grant again");
            Assert.AreEqual(12, store.Rows.Count);
            Assert.AreEqual(".grant again", store.Rows.Last().Text);

            // A game master is never counted.
            var master = As(world.CreateClient(), 11, GmLevel.Observer);

            for (var i = 0; i < 25; i++)
                commands.ProcessCommand(master, ".grant");

            Assert.AreEqual(37, store.Rows.Count);
        }

        [TestMethod]
        public void ACommandThatThrowsIsMarkedFailedAndALogThatCannotBeWrittenStopsNothing()
        {
            using var world = new WorldTestContext();
            var (audit, store) = Log();
            var master = As(world.CreateClient(), 7, GmLevel.Admin);
            var commands = new ChatCommandsManager(null) { Audit = audit };
            var ran = 0;

            commands.RegisterCommand(".break", GmLevel.GameMaster, parts => throw new InvalidOperationException("broken"));
            commands.RegisterCommand(".work", GmLevel.GameMaster, parts => ran++);

            Assert.ThrowsExactly<InvalidOperationException>(() => commands.ProcessCommand(master, ".break it"));

            Assert.AreEqual((byte)GmCommandResult.Failed, store.Rows.Single().Result);
            Assert.AreEqual(".break it", store.Rows.Single().Text);

            // The database is away: the command is run all the same, and a failure is still the command's own.
            store.Fails = new InvalidOperationException("the database is away");

            commands.ProcessCommand(master, ".work");
            Assert.AreEqual(1, ran);
            Assert.ThrowsExactly<InvalidOperationException>(() => commands.ProcessCommand(master, ".break again"));
            commands.ProcessCommand(master, ".nothing");
            Assert.AreEqual(1, store.Rows.Count);

            // No store at all, and no log at all.
            commands.Audit = new GmAudit();
            commands.ProcessCommand(master, ".work");
            commands.Audit = null;
            commands.ProcessCommand(master, ".work");
            Assert.AreEqual(3, ran);

            // Nobody's command is nobody's row.
            Assert.AreEqual(0u, audit.Record(null, GmCommandSource.Chat, ".work", ".work", GmLevel.GameMaster, GmCommandResult.Executed));
            Assert.AreEqual(0u, audit.Record(world.CreateClient(), GmCommandSource.Chat, ".work", ".work", GmLevel.GameMaster, GmCommandResult.Executed));
        }

        #endregion

        #region Slash commands, requests and the console

        [TestMethod]
        public void SlashCommandsAreOnTheLogAsTheDotCommandsAre()
        {
            using var world = new WorldTestContext();
            var (audit, store) = Log();
            var master = As(world.CreateClient(), 7, GmLevel.GameMaster);
            var player = As(world.CreateClient(), 9, GmLevel.Player);
            var commands = new ChatCommandsManager(null) { Audit = audit };

            // Run: nobody of that name is in the world, which is the command's own answer.
            commands.PrivilegedCommand(master, new PrivilegedCommandPacket { Command = "UserMissions", Args = "Nobody" });
            // Needs more than a game master has.
            commands.PrivilegedCommand(master, new PrivilegedCommandPacket { Command = "killmap", Args = "" });
            // No such command.
            commands.PrivilegedCommand(master, new PrivilegedCommandPacket { Command = "frobnicate", Args = "1 2" });

            Assert.AreEqual(3, store.Rows.Count);
            Assert.IsTrue(store.Rows.All(r => r.Source == (byte)GmCommandSource.Slash && r.AccountId == 7));

            Assert.AreEqual(("/usermissions", "/UserMissions Nobody", (byte)GmCommandResult.Executed), (store.Rows[0].Command, store.Rows[0].Text, store.Rows[0].Result));
            Assert.AreEqual((byte)GmLevel.GameMaster, store.Rows[0].RequiredLevel);
            Assert.AreEqual(("/killmap", "/killmap", (byte)GmCommandResult.Denied), (store.Rows[1].Command, store.Rows[1].Text, store.Rows[1].Result));
            Assert.AreEqual((byte)GmLevel.Admin, store.Rows[1].RequiredLevel);
            Assert.AreEqual(("/frobnicate", "/frobnicate 1 2", (byte)GmCommandResult.Unknown), (store.Rows[2].Command, store.Rows[2].Text, store.Rows[2].Result));

            // An ordinary player: the refusal, and not the slash command the server never heard of.
            commands.PrivilegedCommand(player, new PrivilegedCommandPacket { Command = "killmap", Args = "now" });
            commands.PrivilegedCommand(player, new PrivilegedCommandPacket { Command = "dance", Args = "" });

            Assert.AreEqual(4, store.Rows.Count);
            Assert.AreEqual((9u, (byte)GmCommandResult.Denied, "/killmap now"), (store.Rows[3].AccountId, store.Rows[3].Result, store.Rows[3].Text));
        }

        [TestMethod]
        public void TheClientsOwnGmRequestsAreOnTheLog()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var previous = GmAudit.Instance.Store;
            var clock = GmAudit.Instance.UtcNow;

            GmAudit.Instance.Load(store);
            GmAudit.Instance.UtcNow = () => _utc;

            try
            {
                var master = As(world.CreateClient(), 7, GmLevel.GameMaster);
                var player = As(world.CreateClient(), 9, GmLevel.Player);

                // /gotomob: nothing of that name is on the map, which is the command's own answer.
                CommunicatorManager.Instance.GotoMob(master, new GotoMobPacket { ArgString = "Thrax Soldier" });
                CommunicatorManager.Instance.GotoMob(player, new GotoMobPacket { ArgString = "Thrax Soldier" });

                // The GM mission window's button, from one who may not press it.
                GmMissionCommands.ForceCompleteObjective(player, new ForceCompleteObjectivePacket { UserId = 44, MissionId = 300, ObjectiveId = 2 });

                // A line-of-sight report.
                LosReport.Answer(master, 0);
                LosReport.Answer(player, 0);

                Assert.AreEqual(5, store.Rows.Count);
                Assert.IsTrue(store.Rows.All(r => r.Source == (byte)GmCommandSource.Request));

                Assert.AreEqual(("/gotomob", "/gotomob Thrax Soldier", (byte)GmCommandResult.Executed, 7u),
                    (store.Rows[0].Command, store.Rows[0].Text, store.Rows[0].Result, store.Rows[0].AccountId));
                Assert.AreEqual(((byte)GmCommandResult.Denied, 9u, (byte)GmLevel.GameMaster), (store.Rows[1].Result, store.Rows[1].AccountId, store.Rows[1].RequiredLevel));

                Assert.AreEqual(("ForceCompleteObjective", "ForceCompleteObjective character 44 mission 300 objective 2", (byte)GmCommandResult.Denied),
                    (store.Rows[2].Command, store.Rows[2].Text, store.Rows[2].Result));

                Assert.AreEqual(("RequestLOSReport", "RequestLOSReport 0", (byte)GmCommandResult.Executed), (store.Rows[3].Command, store.Rows[3].Text, store.Rows[3].Result));
                Assert.AreEqual(((byte)GmCommandResult.Denied, (byte)GmLevel.Observer), (store.Rows[4].Result, store.Rows[4].RequiredLevel));
            }
            finally
            {
                GmAudit.Instance.Load(previous);
                GmAudit.Instance.UtcNow = clock;
            }
        }

        [TestMethod]
        public void WhatIsTypedAtTheConsoleIsOnTheLog()
        {
            var store = new MemoryStore();
            var previous = GmAudit.Instance.Store;
            var clock = GmAudit.Instance.UtcNow;
            var audited = typeof(Server).GetMethod("Audited", BindingFlags.Static | BindingFlags.NonPublic);

            GmAudit.Instance.Load(store);
            GmAudit.Instance.UtcNow = () => _utc;

            try
            {
                var seen = -1;
                var ran = (Action<string[]>)audited.Invoke(null, new object[] { (Action<string[]>)(parts => seen = store.Rows.Count) });
                var broke = (Action<string[]>)audited.Invoke(null, new object[] { (Action<string[]>)(parts => throw new FormatException("not a number")) });

                ran(new[] { "gm", "Ellimist", "admin" });

                Assert.AreEqual(1, seen, "written before the command ran");

                var row = store.Rows.Single();

                Assert.AreEqual((byte)GmCommandSource.Console, row.Source);
                Assert.AreEqual((byte)GmCommandResult.Executed, row.Result);
                Assert.AreEqual(("gm", "gm Ellimist admin"), (row.Command, row.Text));
                Assert.AreEqual((0u, 0u, "", ""), (row.AccountId, row.CharacterId, row.FamilyName, row.Address), "nobody's account: the console's");
                Assert.AreEqual(Noon, row.CreatedAt);

                Assert.ThrowsExactly<FormatException>(() => broke(new[] { "exit", "abc" }));

                Assert.AreEqual(2, store.Rows.Count);
                Assert.AreEqual(("exit abc", (byte)GmCommandResult.Failed), (store.Rows[1].Text, store.Rows[1].Result));
            }
            finally
            {
                GmAudit.Instance.Load(previous);
                GmAudit.Instance.UtcNow = clock;
            }
        }

        #endregion

        #region The table

        [TestMethod]
        public void TheServersLogIsTheCharacterDatabase()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var audit = new GmAudit { UtcNow = () => _utc };

            audit.Load(new GmAudit.ServerStore(context));
            context.Client.AccountEntry.Level = (byte)GmLevel.Admin;

            var commands = new ChatCommandsManager(null) { Audit = audit };

            commands.RegisterCommand(".work", GmLevel.GameMaster, parts => { });
            commands.RegisterCommand(".break", GmLevel.GameMaster, parts => throw new InvalidOperationException("broken"));

            commands.ProcessCommand(context.Client, ".work now");
            Assert.ThrowsExactly<InvalidOperationException>(() => commands.ProcessCommand(context.Client, ".break"));
            audit.RecordConsole("announce", "announce Restart in five minutes");

            using var unit = context.CreateChar();
            var rows = unit.GmCommandLogs.GetRecent(10);

            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual(("announce Restart in five minutes", (byte)GmCommandSource.Console, 0u), (rows[0].Text, rows[0].Source, rows[0].AccountId));
            Assert.AreEqual((".break", (byte)GmCommandResult.Failed), (rows[1].Text, rows[1].Result));
            Assert.AreEqual((".work now", (byte)GmCommandResult.Executed), (rows[2].Text, rows[2].Result));
            Assert.AreEqual(context.Client.AccountEntry.Id, rows[2].AccountId);
            Assert.AreEqual(context.Client.Player.Id, rows[2].CharacterId);
            Assert.AreEqual(Noon, rows[2].CreatedAt);
            Assert.AreEqual(2, unit.GmCommandLogs.GetByAccount(context.Client.AccountEntry.Id, 10).Count);
        }

        [TestMethod]
        public void TheTableKeepsTheRowsItIsGiven()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(directory);

            var database = Path.Combine(directory, "database");

            try
            {
                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database))
                    context.Database.Migrate();

                T Log<T>(Func<GmCommandLogRepository, T> action)
                {
                    using var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database);
                    return action(new GmCommandLogRepository(context));
                }

                var first = Log(r => r.Add(new GmCommandLogEntry
                {
                    CreatedAt = Noon,
                    Source = (byte)GmCommandSource.Chat,
                    Result = (byte)GmCommandResult.Executed,
                    AccountId = 7,
                    AccountLevel = 10,
                    RequiredLevel = 5,
                    CharacterId = 70,
                    Name = "Ray",
                    FamilyName = "Master",
                    Address = "203.0.113.9",
                    MapContextId = 1220,
                    InstanceId = 2,
                    CoordX = 12.5,
                    CoordY = -3.25,
                    CoordZ = 800,
                    TargetCharacterId = 90,
                    Target = "player Other",
                    Command = ".giveitem",
                    Text = ".giveitem 28066 1"
                }));
                var second = Log(r => r.Add(new GmCommandLogEntry
                {
                    CreatedAt = Noon.AddMinutes(1),
                    Source = (byte)GmCommandSource.Console,
                    Result = (byte)GmCommandResult.Executed,
                    Command = "gm",
                    Text = "gm Master admin",
                    Name = null,
                    Target = null
                }));
                var third = Log(r => r.Add(new GmCommandLogEntry
                {
                    CreatedAt = Noon.AddMinutes(2),
                    Source = (byte)GmCommandSource.Slash,
                    Result = (byte)GmCommandResult.Denied,
                    AccountId = 7,
                    AccountLevel = 5,
                    RequiredLevel = 10,
                    Command = "/killmap",
                    Text = "/killmap"
                }));

                Assert.AreEqual((1u, 2u, 3u), (first, second, third));

                CollectionAssert.AreEqual(new uint[] { 3, 2, 1 }, Log(r => r.GetRecent(10)).Select(e => e.Id).ToArray(), "newest first");
                CollectionAssert.AreEqual(new uint[] { 3, 2 }, Log(r => r.GetRecent(2)).Select(e => e.Id).ToArray());
                CollectionAssert.AreEqual(new uint[] { 3, 1 }, Log(r => r.GetByAccount(7, 10)).Select(e => e.Id).ToArray());
                Assert.AreEqual(0, Log(r => r.GetByAccount(8, 10)).Count);

                var read = Log(r => r.GetRecent(10)).Single(e => e.Id == first);

                Assert.AreEqual(Noon, read.CreatedAt);
                Assert.AreEqual(((byte)GmCommandSource.Chat, (byte)GmCommandResult.Executed), (read.Source, read.Result));
                Assert.AreEqual((7u, (byte)10, (byte)5), (read.AccountId, read.AccountLevel, read.RequiredLevel));
                Assert.AreEqual((70u, "Ray", "Master", "203.0.113.9"), (read.CharacterId, read.Name, read.FamilyName, read.Address));
                Assert.AreEqual((1220u, 2u, 12.5, -3.25, 800d), (read.MapContextId, read.InstanceId, read.CoordX, read.CoordY, read.CoordZ));
                Assert.AreEqual((90u, "player Other"), (read.TargetCharacterId, read.Target));
                Assert.AreEqual((".giveitem", ".giveitem 28066 1"), (read.Command, read.Text));

                Assert.AreEqual(("", ""), (Log(r => r.GetRecent(10)).Single(e => e.Id == second).Name, Log(r => r.GetRecent(10)).Single(e => e.Id == second).Target));

                // The one change a row takes: its result, when the command threw.
                Log(r => { r.SetResult(first, GmCommandResult.Failed); r.SetResult(404, GmCommandResult.Failed); return 0; });

                Assert.AreEqual((byte)GmCommandResult.Failed, Log(r => r.GetRecent(10)).Single(e => e.Id == first).Result);
                Assert.AreEqual(".giveitem 28066 1", Log(r => r.GetRecent(10)).Single(e => e.Id == first).Text);
                Assert.AreEqual(3, Log(r => r.GetRecent(10)).Count);
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        #endregion
    }
}
