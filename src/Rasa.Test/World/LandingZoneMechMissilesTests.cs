using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Landing Zone's AFS Mech Defenders fire their missiles as well as their minigun
    /// (Landing_zone_mech_missiles, LandingZoneMechMissiles). The report: watching the Mechs at
    /// the Landing Zone control point, they only ever used the minigun and never the rocket
    /// launchers - Landing_zone_garrison had given the defender the minigun alone.
    /// </summary>
    [TestClass, DoNotParallelize]
    public class LandingZoneMechMissilesTests
    {
        private const string Migration = "20270926000000_Landing_zone_mech_missiles";

        [TestMethod]
        public void UpGivesTheDefenderTheMechsMissilesOnceAndDownTakesThemOff()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var all = database.Context.Database.GetMigrations().ToList();
            var previous = all[all.IndexOf(Migration) - 1];

            migrator.Migrate(previous);
            Assert.AreEqual((13L, 0L), Slots(database, LandingZoneMechMissiles.Defender), "the minigun alone");
            Assert.AreEqual((13L, 15L), Slots(database, 40), "the world's own Mech, minigun and missiles");

            migrator.Migrate(Migration);
            Assert.AreEqual((13L, 15L), Slots(database, LandingZoneMechMissiles.Defender), "the Mech's minigun and missiles");

            foreach (var sql in LandingZoneMechMissiles.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual((13L, 15L), Slots(database, LandingZoneMechMissiles.Defender), "twice is once");

            migrator.Migrate(previous);
            Assert.AreEqual((13L, 0L), Slots(database, LandingZoneMechMissiles.Defender), "Down takes them off");
            Assert.AreEqual((13L, 15L), Slots(database, 40), "and leaves the world's own Mech alone");
        }

        [TestMethod]
        public void AWorldThatRanItUnderTheOldIdKeepsTheMissilesAndOnlyTheNewHistoryRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();
            var all = database.Context.Database.GetMigrations().ToList();
            var previous = all[all.IndexOf(Migration) - 1];

            migrator.Migrate(previous);
            foreach (var sql in LandingZoneMechMissiles.Up())
                database.Context.Database.ExecuteSqlRaw(sql);
            database.Context.Database.ExecuteSqlRaw(
                $"INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{LandingZoneMechMissiles.OldId}', '9.0.20')");

            CollectionAssert.Contains(database.Context.Database.GetPendingMigrations().ToList(), Migration);

            migrator.Migrate(Migration);

            Assert.AreEqual((13L, 15L), Slots(database, LandingZoneMechMissiles.Defender), "the missiles, once");
            var history = database.Context.Database.GetAppliedMigrations().ToList();
            CollectionAssert.Contains(history, Migration);
            CollectionAssert.DoesNotContain(history, LandingZoneMechMissiles.OldId);
        }

        [TestMethod]
        public void ADefenderFiresItsMissilesAtBaneBeyondItsMiniguns()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            harness.LoadAbilities();
            var player = harness.Client.Player;
            CellManager.Instance.AddToWorld(harness.Client);

            var thrax = harness.Creatures.CreateCreature(560002, null);
            thrax.Actions.Clear();
            thrax.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000000, 10000000, 10000000, 0, 0);
            harness.Creatures.SetLocation(thrax, player.Position + new Vector3(5, 0, 0), 0, player.MapContextId);
            CellManager.Instance.AddToWorld(harness.Map, thrax);

            var mech = harness.Creatures.CreateCreature(LandingZoneMechMissiles.Defender, null);
            CollectionAssert.AreEquivalent(new[] { (ActionId.WeaponAttack, 224u), (ActionId.WeaponRocketlauncher, 17u) },
                mech.Actions.Select(a => (a.ActionId, a.ActionArgId)).ToList());

            // Thirty metres off: past the minigun's 20.5, inside the missiles' 40.
            harness.Creatures.SetLocation(mech, thrax.Position + new Vector3(30, 0, 0), 0, player.MapContextId);
            CellManager.Instance.AddToWorld(harness.Map, mech);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(mech, thrax.EntityId));
            mech.Controller.ActionFighting.Opened = true;

            var fired = new List<(ActionId, uint)>();

            try
            {
                for (var step = 0; step < 50; step++)
                {
                    BehaviorManager.Instance.MapChannelThink(harness.Map, 100);
                    MissileManager.Instance.DoWork(harness.Map, 100);

                    fired.AddRange(WorldTestContext.Drain(harness.Client).Select(p => p.Message).OfType<CallMethodMessage>()
                        .Where(m => m.EntityId == mech.EntityId).Select(m => m.Packet).OfType<PerformWindupPacket>()
                        .Select(w => (w.ActionId, w.ActionArgId)));
                }
            }
            finally
            {
                harness.Map.QueuedMissiles.Clear();
            }

            CollectionAssert.Contains(fired, (ActionId.WeaponRocketlauncher, 17u), string.Join(", ", fired));
        }

        private static (long, long) Slots(OrdinaryLootTestDatabase database, uint id)
        {
            var action1 = database.Context.Database.SqlQueryRaw<long>($"SELECT action1 AS Value FROM creature WHERE id = {id}").AsEnumerable().Single();
            var action2 = database.Context.Database.SqlQueryRaw<long>($"SELECT action2 AS Value FROM creature WHERE id = {id}").AsEnumerable().Single();
            return (action1, action2);
        }
    }
}
