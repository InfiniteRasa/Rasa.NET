using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions;
    using Microsoft.EntityFrameworkCore;
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Repositories.Char.Character;
    using Rasa.Services.DbContext;
    using Rasa.Missions.Definitions;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// What the database work on the game loop's hot paths costs, in milliseconds, against a
    /// real SQLite file (MissionTestContext's, in TestDatabases) and not an in-memory one: what
    /// is being measured is mostly the wait for the disk at each commit.
    ///
    /// Not part of the normal suite: the numbers depend on the machine, and the runs are long.
    /// Set RASA_DB_BENCH=1 to run them; otherwise each is inconclusive (skipped). They print
    /// their figures, so run them with a console logger:
    ///   set RASA_DB_BENCH=1
    ///   dotnet test src/Rasa.Test/Rasa.Test.csproj --no-build --filter "FullyQualifiedName~HotPathDatabaseBenchmarks" --logger "console;verbosity=detailed"
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class HotPathDatabaseBenchmarks
    {
        private const int Warmup = 10;
        private const int Runs = 100;

        private Func<Client, Vector3, List<Client>> _sharers;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Gate()
        {
            if (Environment.GetEnvironmentVariable("RASA_DB_BENCH") != "1")
                Assert.Inconclusive("Benchmark: set RASA_DB_BENCH=1 to run it.");

            _sharers = KillShares.SharersOf;
        }

        [TestCleanup]
        public void RestoreTheSquad()
        {
            if (_sharers != null)
                KillShares.SharersOf = _sharers;
        }

        /// <summary>A kill's experience paid to 1, 5 and 10 earners (KillShares.AwardExperience), per kill.</summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(5)]
        [DataRow(10)]
        public void KillExperience(int earners)
        {
            using var context = Start();
            var clients = Earners(context, earners);
            var manifestations = new ManifestationManager(context);

            KillShares.SharersOf = (killer, corpse) => clients.Skip(1).ToList();

            var perKill = Measure(() =>
            {
                var sharers = KillShares.SharersFor(context.Client, context.Client.Player.Position);

                KillShares.AwardExperience(manifestations, sharers, context.Client, 50);


                foreach (var client in clients)
                    MissionTestContext.Drain(client);
            });

            Report($"kill experience, {earners} earner(s)", perKill);
        }

        /// <summary>A corpse's three drops made into items (LootDispenserManager.MaterializeItems), and the corpse's unclaimed items deleted (Retire).</summary>
        [TestMethod]
        public void LootDropAndRetire()
        {
            using var context = Start();

            context.AddRewardTemplate(901, 2001);
            context.AddRewardTemplate(902, 2002);
            context.AddRewardTemplate(903, 2003);

            var manager = LootDispenserManager.Instance;
            var materialize = typeof(LootDispenserManager).GetMethod("MaterializeItems", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var retire = typeof(LootDispenserManager).GetMethod("Retire", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var drops = new List<(uint TemplateId, uint Quantity)> { (901, 1), (902, 3), (903, 1) };
            var made = new List<LootDispenser>();

            var perDrop = Measure(() =>
            {
                var loot = new LootDispenser { Map = context.Map, UnitOfWorkFactory = context };

                materialize.Invoke(manager, new object[] { context.Client, loot, drops, 0u, null });
                made.Add(loot);
            });

            var perRetire = Measure(() =>
            {
                var loot = new LootDispenser { Map = context.Map, UnitOfWorkFactory = context };

                materialize.Invoke(manager, new object[] { context.Client, loot, drops, 0u, null });
                made.Add(loot);
            }, () =>
            {
                var loot = made[^1];

                made.RemoveAt(made.Count - 1);
                retire.Invoke(manager, new object[] { context.Map, loot, false, true });
            });

            foreach (var loot in made)
                retire.Invoke(manager, new object[] { context.Map, loot, false, true });

            Report("loot drop, 3 items", perDrop);
            Report("loot retire, 3 unclaimed items", perRetire);
        }

        /// <summary>One player's autosave (AutoSave.Save): position, time played, vitals, cooldowns.</summary>
        [TestMethod]
        public void OneAutoSave()
        {
            using var context = Start();

            // Somewhere else each time: a save that changes nothing writes nothing.
            var perSave = Measure(() =>
            {
                context.Client.Player.Position += new Vector3(1, 0, 0);
                AutoSave.Save(context.Client);
            });

            Report("autosave, one player", perSave);
        }

        /// <summary>A reload's end (ManifestationManager.WeaponReload): the clip filled from a stack in the pack, saved.</summary>
        [TestMethod]
        public void OneReload()
        {
            using var context = new Rasa.Test.Gameplay.WeaponAmmoContext(clip: 20);
            context.AddAmmo(50000);
            var manager = new ManifestationManager(context);
            ActionData action = null;

            var perReload = Measure(() =>
            {
                // Fired empty, the way a shot spends a clip (WeaponClips), and a reload asked for.
                WeaponClips.Spend(context.Client, context.Weapon, 20, context);
                context.Client.Player.CurrentAction = 0;
                context.World.Map.PerformRecovery.Clear();
                manager.RequestWeaponReload(context.Client, true);
                action = context.World.Map.PerformRecovery.Single();
                Rasa.Test.World.WorldTestContext.Drain(context.Client);
            }, () => manager.WeaponReload(action));

            Assert.AreEqual(20u, context.Read(context.Weapon).AmmoCount, "the reloads were saved");
            Report("reload, one stack", perReload);
        }

        /// <summary>
        /// What one character's progression write costs under each journal and synchronous
        /// setting, and what of it is the commit: a context opened, a transaction begun, the row
        /// read and written, and committed - GainExperience's database work, without the game.
        /// </summary>
        [TestMethod]
        [DataRow(SqliteJournalMode.Default, SqliteSynchronous.Default)]
        [DataRow(SqliteJournalMode.Delete, SqliteSynchronous.Full)]
        [DataRow(SqliteJournalMode.Wal, SqliteSynchronous.Full)]
        [DataRow(SqliteJournalMode.Wal, SqliteSynchronous.Normal)]
        public void OneProgressionCommit(SqliteJournalMode journalMode, SqliteSynchronous synchronous)
        {
            using var context = Start();
            string database;

            using (var open = context.Open())
                database = System.IO.Path.ChangeExtension(
                    new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(open.Database.GetDbConnection().ConnectionString).DataSource, null);

            SqliteCharContext Open() => new(
                Microsoft.Extensions.Options.Options.Create(new DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    Char = new DatabaseConnectionConfiguration { Database = database, JournalMode = journalMode, Synchronous = synchronous }
                }),
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier());

            var experience = 0u;

            using (var db = Open())
            {
                db.Database.OpenConnection();
                using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "PRAGMA journal_mode";
                var mode = command.ExecuteScalar();
                command.CommandText = "PRAGMA synchronous";
                var sync = command.ExecuteScalar();
                command.CommandText = "PRAGMA compile_options";
                var options = new List<string>();
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        options.Add(reader.GetString(0));
                Console.WriteLine($"BENCH {journalMode}/{synchronous}: journal_mode {mode}, synchronous {sync}; {string.Join(" ", options.Where(o => o.Contains("SYNC") || o.Contains("WAL") || o.Contains("JOURNAL")))}");
            }

            var opened = Measure(() =>
            {
                using var db = Open();
                db.Database.OpenConnection();
            });
            var read = Measure(() =>
            {
                using var db = Open();
                using var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                new CharacterRepository(db).Find(1);
                transaction.Commit();
            });
            var written = Measure(() =>
            {
                using var db = Open();
                using var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                new CharacterRepository(db).Find(1);
                new CharacterRepository(db).UpdateCharacterProgression(1, ++experience, 1);
                transaction.Commit();
            });

            Report($"{journalMode}/{synchronous}: context opened", opened);
            Report($"{journalMode}/{synchronous}: read in a transaction", read);
            Report($"{journalMode}/{synchronous}: read, written and committed", written);
        }

        private static MissionTestContext Start() =>
            MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));

        private static List<Client> Earners(MissionTestContext context, int earners)
        {
            var clients = new List<Client> { context.Client };

            for (var i = 2; i <= earners; i++)
                clients.Add(context.CreateAdditionalClient((uint)i));

            // Levels are gained on the way: a level-up works out the player's stats.
            foreach (var client in clients)
                client.Player.Attributes = Enum.GetValues<Attributes>().ToDictionary(
                    attribute => attribute,
                    attribute => new ActorAttributes(attribute, 100, 100, 100, 0, 0));

            return clients;
        }

        /// <summary>The mean of <see cref="Runs"/> runs of what is measured, in ms, after <see cref="Warmup"/> not counted; setup runs before each, not timed.</summary>
        private static double[] Measure(Action measured) => Measure(() => { }, measured);

        private static double[] Measure(Action setup, Action measured)
        {
            for (var i = 0; i < Warmup; i++)
            {
                setup();
                measured();
            }

            var times = new double[Runs];

            for (var i = 0; i < Runs; i++)
            {
                setup();
                var watch = Stopwatch.StartNew();
                measured();
                times[i] = watch.Elapsed.TotalMilliseconds;
            }

            return times;
        }

        private void Report(string what, double[] times)
        {
            var sorted = times.OrderBy(time => time).ToArray();
            var line = $"BENCH {what}: mean {times.Average():F3} ms, median {sorted[sorted.Length / 2]:F3} ms, p95 {sorted[(int)(sorted.Length * 0.95)]:F3} ms ({times.Length} runs)";

            Console.WriteLine(line);
            TestContext?.WriteLine(line);
        }
    }
}
