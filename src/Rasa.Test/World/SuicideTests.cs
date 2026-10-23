using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Client;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// /suicide (Suicide): a player's own command, asked twice, then a three-minute countdown that
    /// moving, a fight, damage, an ability or PvP calls off, and a death as a fall's. Refused when
    /// dead, in combat, in PvP and on an arena or battleground map, and for an hour after the last
    /// death it gave. The way back is only the nearest hospital, with the full Rez Trauma.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SuicideTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map

        // As PlayerDeathTests has them: Alia Das (103, graveyard 4) at x 100, Twin Pillars (105,
        // graveyard 6, a safe zone, free) at x -300, Ranja Gorge (106) at 40, 40.
        private static readonly (uint, uint, Vector3, string)[] TestHospitals =
        {
            (103, MapId, new Vector3(100, 0, 0), "Hospital: Alia Das"),
            (105, MapId, new Vector3(-300, 0, 0), "Hospital: Twin Pillars"),
            (106, MapId, new Vector3(40, 0, 40), "Hospital: Ranja Gorge")
        };

        private Func<long> _now;
        private Func<long> _pvpNow;
        private Func<IEnumerable<(uint, uint, Vector3, string)>> _source;
        private Func<uint, bool> _safe;
        private Action<Client, CharacterTeleporterEntry> _persist;
        private Func<Client, Dictionary<uint, MapWaypointInfoList>> _network;
        private long _tick;

        [TestInitialize]
        public void UseATestClockAndTestHospitals()
        {
            _now = Suicide.Now;
            _pvpNow = Pvp.Now;
            _tick = 10_000_000;
            Suicide.Now = () => _tick;
            Pvp.Now = () => _tick;
            Suicide.Reset();

            _source = Hospitals.Source;
            _safe = Hospitals.IsSafeZone;
            _persist = Hospitals.Persist;
            Hospitals.Source = () => TestHospitals;
            Hospitals.IsSafeZone = id => id == 105;
            Hospitals.Persist = (client, entry) => { };
            Hospitals.Reset();

            // One waypoint gained on the test map (PersonalWaypoints.Network).
            _network = PersonalWaypoints.Network;
            PersonalWaypoints.Network = client => Network(1);
        }

        [TestCleanup]
        public void PutItAllBack()
        {
            Suicide.Now = _now;
            Pvp.Now = _pvpNow;
            Suicide.Reset();
            Hospitals.Source = _source;
            Hospitals.IsSafeZone = _safe;
            Hospitals.Persist = _persist;
            Hospitals.Reset();
            PersonalWaypoints.Network = _network;
        }

        /// <summary>A waypoint network of the test map with this many waypoints gained.</summary>
        private static Dictionary<uint, MapWaypointInfoList> Network(int waypoints) => new()
        {
            [MapId] = new MapWaypointInfoList(MapId, new List<MapInstanceInfo>(),
                Enumerable.Range(1, waypoints).Select(id => new WaypointInfo((uint)id, false, WaypointType.Waypoint)).ToList())
        };

        #region Asking

        [TestMethod]
        public void TheFirstAsksASecondWithinTenSecondsStartsTheCountdown()
        {
            using var world = new WorldTestContext();
            var client = Player(world);

            Suicide.Run(client, "");
            CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));

            _tick += Suicide.ConfirmWindowMs;
            Suicide.Run(client, "");

            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
            CollectionAssert.AreEqual(new[] { Suicide.StartText }, Messages(client));
        }

        [TestMethod]
        public void ASecondAfterTenSecondsAsksAgain()
        {
            using var world = new WorldTestContext();
            var client = Player(world);

            Suicide.Run(client, "");
            _tick += Suicide.ConfirmWindowMs + 1;
            Suicide.Run(client, "");

            CollectionAssert.AreEqual(new[] { Suicide.AskText, Suicide.AskText }, Messages(client));
        }

        [TestMethod]
        public void APlayerHasItAsASlashCommand()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var commands = new ChatCommandsManager(null);

            commands.PrivilegedCommand(client, new PrivilegedCommandPacket { Command = "suicide", Args = "" });
            commands.PrivilegedCommand(client, new PrivilegedCommandPacket { Command = "Suicide", Args = "" });

            CollectionAssert.AreEqual(new[] { Suicide.AskText, Suicide.StartText }, Messages(client));
        }

        #endregion

        #region The countdown

        [TestMethod]
        public void TheCountdownWarnsAtTenSecondsAndKillsAtThreeMinutes()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            _tick += Suicide.CountdownMs - Suicide.WarningMs - 1;
            Suicide.Worker(world.Map);
            Assert.AreEqual(0, Messages(client).Length);

            _tick += 1;
            Suicide.Worker(world.Map);
            CollectionAssert.AreEqual(new[] { Suicide.WarningText }, Messages(client));
            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);

            _tick += Suicide.WarningMs - 1;
            Suicide.Worker(world.Map);
            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);

            _tick += 1;
            Suicide.Worker(world.Map);

            Assert.AreEqual(CharacterState.Dead, client.Player.State);
            Assert.AreEqual(0, client.Player.Attributes[Attributes.Health].Current);
            Assert.IsTrue(client.Player.DiedBySuicide);
            Assert.AreEqual(0UL, MissionTestContext.Drain(client).OfType<PlayerDeadPacket>().Single().SourceId, "no killer");
        }

        [TestMethod]
        public void ASuicideAskedForDuringTheCountdownSaysWhatIsLeft()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            _tick += Suicide.CountdownMs - 50_000;
            Suicide.Run(client, "");

            CollectionAssert.AreEqual(new[] { "You will die in 50 seconds." }, Messages(client));
        }

        [TestMethod]
        public void MovingCallsItOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            client.Player.PlaceAt(client.Player.Position + new Vector3(Suicide.MoveToleranceMetres / 2, 0, 0));
            Suicide.Worker(world.Map);
            Assert.AreEqual(0, Messages(client).Length, "a drift is no move");

            client.Player.PlaceAt(client.Player.Position + new Vector3(Suicide.MoveToleranceMetres, 0, 0));
            Suicide.Worker(world.Map);

            AssertCancelled(world, client, Suicide.MovedReason);
        }

        [TestMethod]
        public void AFightCallsItOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            client.Player.InCombat = true;
            Suicide.Worker(world.Map);

            AssertCancelled(world, client, Suicide.CombatReason);
        }

        [TestMethod]
        public void DamageCallsItOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            client.Player.Attributes[Attributes.Health].Current -= 1;
            Suicide.Worker(world.Map);

            AssertCancelled(world, client, Suicide.DamageReason);
        }

        [TestMethod]
        public void DamageToArmourAloneCallsItOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 200, 200, 200, 0, 0);
            Confirm(client);

            // A hit with no source - lava, a tesla coil - that the armour takes all of.
            ActorManager.Instance.Damage(world.Map, client.Player, 5, null);

            Assert.AreEqual(1000, client.Player.Attributes[Attributes.Health].Current, "health untouched");
            Assert.IsFalse(client.Player.InCombat, "no source: no fight");
            AssertCancelled(world, client, Suicide.DamageReason);
        }

        [TestMethod]
        public void AHitAfterHealingIsStillDamage()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var health = client.Player.Attributes[Attributes.Health];
            health.Current = 500;
            Confirm(client);

            health.Current = 800;   // healed while it counts down
            Suicide.Worker(world.Map);
            Assert.AreEqual(0, Messages(client).Length);

            health.Current = 700;   // still above where it started
            Suicide.Worker(world.Map);

            AssertCancelled(world, client, Suicide.DamageReason);
        }

        [TestMethod]
        public void AWeaponCallsItOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            // Fire held down (the client's StartAutoFire): with nothing in hand, the fists.
            ManifestationManager.Instance.StartAutoFire(client, 0);
            ManifestationManager.RemoveAutoFire(client);

            CollectionAssert.Contains(Messages(client), $"/suicide cancelled: {Suicide.WeaponReason}.");
            _tick += Suicide.CountdownMs;
            Suicide.Worker(world.Map);
            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
        }

        [TestMethod]
        public void BetweenMapsItIsNotAskedForAndLeavingForgetsIt()
        {
            using var world = new WorldTestContext();
            var client = Player(world);

            client.Player.MapChannel = null;
            Suicide.Run(client, "");
            Assert.AreEqual(0, Messages(client).Length);
            client.Player.MapChannel = world.Map;

            Confirm(client);
            Suicide.Forget(client);

            _tick += Suicide.CountdownMs;
            Suicide.Worker(world.Map);
            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);

            // Forgotten too is the ask: the next /suicide asks again.
            Suicide.Run(client, "");
            CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
        }

        [TestMethod]
        public void APvpHitCallsItOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var enemy = Player(world, 5);
            Confirm(client);

            Pvp.RecordEngagement(enemy.Player, client.Player);
            Suicide.Worker(world.Map);

            AssertCancelled(world, client, Suicide.PvpReason);
        }

        [TestMethod]
        public void AnAbilityCallsItOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            AbilityManager.Instance.RequestPerformAbility(client, new RequestPerformAbilityPacket { ActionId = 0, ActionArgId = 0 });

            CollectionAssert.Contains(Messages(client), $"/suicide cancelled: {Suicide.AbilityReason}.");
            _tick += Suicide.CountdownMs;
            Suicide.Worker(world.Map);
            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
        }

        [TestMethod]
        public void OneWhoLeftTheMapIsForgottenQuietly()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            Confirm(client);

            world.Map.ClientList.Remove(client);
            Suicide.Worker(world.Map);
            world.Map.ClientList.Add(client);

            _tick += Suicide.CountdownMs;
            Suicide.Worker(world.Map);

            Assert.AreEqual(0, Messages(client).Length);
            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
        }

        #endregion

        #region Refused

        [TestMethod]
        public void OneAlreadyDeadIsTold()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.State = CharacterState.Dead;

            Suicide.Run(client, "");

            CollectionAssert.AreEqual(new[] { Suicide.DeadText }, Messages(client));
        }

        [TestMethod]
        public void InAFightItIsRefusedAndTheAskIsCalledOff()
        {
            using var world = new WorldTestContext();
            var client = Player(world);

            Suicide.Run(client, "");
            client.Player.InCombat = true;
            Suicide.Run(client, "");

            CollectionAssert.AreEqual(new[] { Suicide.AskText, Suicide.CombatText }, Messages(client));

            // Out of the fight: it asks again, the earlier ask being off.
            client.Player.InCombat = false;
            Suicide.Run(client, "");

            CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
        }

        [TestMethod]
        public void InAPvpFightItIsRefused()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var enemy = Player(world, 5);

            Pvp.RecordEngagement(enemy.Player, client.Player);
            Suicide.Run(client, "");

            CollectionAssert.AreEqual(new[] { Suicide.PvpText }, Messages(client));

            // The PvP fight over (CombatRegen.CombatTimeoutMs): it may be asked.
            _tick += CombatRegen.CombatTimeoutMs;
            Suicide.Run(client, "");

            CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
        }

        [TestMethod]
        [DataRow(2259u)]        // adv_wargame_indoorarena
        [DataRow(2361u)]        // adv_wargame_provinggroundsv002
        [DataRow(20000009u)]    // adv_afs_arena
        [DataRow(2374u)]        // adv_wargame_edmundrange2, a battleground
        public void OnAnArenaOrBattlegroundMapItIsRefused(uint mapContextId)
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var own = world.Map.MapInfo.MapContextId;

            try
            {
                world.Map.MapInfo.MapContextId = mapContextId;
                Suicide.Run(client, "");
            }
            finally
            {
                world.Map.MapInfo.MapContextId = own;
            }

            CollectionAssert.AreEqual(new[] { Suicide.PvpText }, Messages(client));
        }

        [TestMethod]
        [DataRow(118781u)]  // One-Way Personal Waypoint
        [DataRow(118782u)]  // Two-Way Personal Waypoint
        [DataRow(118783u)]  // Two-Way Squad Waypoint
        public void OneWhoCarriesAPersonalWaypointIsToldToUseIt(uint templateId)
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var item = Carry(client, templateId);

            try
            {
                Suicide.Run(client, "");
                CollectionAssert.AreEqual(new[] { Suicide.WaypointText }, Messages(client));

                // Where a Personal Waypoint cannot be put down - an Operation, here Velon Hollow
                // (2136, PersonalWaypoints.IsInstance) - it is no way, and /suicide may be asked.
                var own = world.Map.MapInfo.MapContextId;
                world.Map.MapInfo.MapContextId = 2136;
                Suicide.Run(client, "");
                world.Map.MapInfo.MapContextId = own;
                CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
                Suicide.Reset();
            }
            finally
            {
                Drop(client, item);
            }

            Suicide.Run(client, "");
            CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
        }

        [TestMethod]
        public void APersonalWaypointWithNoWaypointGainedHereIsNoWay()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var item = Carry(client, 118781);
            PersonalWaypoints.Network = _ => Network(0);

            try
            {
                Suicide.Run(client, "");
                CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
            }
            finally
            {
                Drop(client, item);
            }
        }

        [TestMethod]
        public void APersonalWaypointNotTheirsOrUsedUpIsNoWay()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var foreign = Carry(client, 118782);
            foreign.OwnerId = client.Player.Id + 1;
            var empty = Carry(client, 118782);
            empty.StackSize = 0;

            try
            {
                Assert.IsFalse(Suicide.CarriesPersonalWaypoint(client.Player));
            }
            finally
            {
                Drop(client, foreign);
                Drop(client, empty);
            }
        }

        [TestMethod]
        public void AnotherItemIsNoWaypoint()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var item = Carry(client, 28);

            try
            {
                Assert.IsFalse(Suicide.CarriesPersonalWaypoint(client.Player));
                Suicide.Run(client, "");
                CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
            }
            finally
            {
                Drop(client, item);
            }
        }

        [TestMethod]
        public void AfterADeathItWaitsAnHour()
        {
            using var world = new WorldTestContext();
            var client = Player(world);

            Die(world, client);
            StandUp(client);

            Suicide.Run(client, "");
            CollectionAssert.AreEqual(new[] { "You can use /suicide again in 60 minutes." }, Messages(client));

            _tick += Suicide.CooldownMs - 1;
            Suicide.Run(client, "");
            CollectionAssert.AreEqual(new[] { "You can use /suicide again in 1 second." }, Messages(client));

            _tick += 1;
            Suicide.Run(client, "");
            CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(client));
        }

        [TestMethod]
        public void AGmWhoMayNotDieStandsUpAndHasNoCooldown()
        {
            using var world = new WorldTestContext();
            var gm = Player(world);
            PlayerDeathTests.GameMaster(gm);

            Confirm(gm);
            _tick += Suicide.CountdownMs;
            Suicide.Worker(world.Map);

            Assert.AreNotEqual(CharacterState.Dead, gm.Player.State);
            Assert.AreEqual(1000, gm.Player.Attributes[Attributes.Health].Current, "back on full");
            Assert.IsFalse(gm.Player.DiedBySuicide);

            Messages(gm);
            Suicide.Run(gm, "");
            CollectionAssert.AreEqual(new[] { Suicide.AskText }, Messages(gm));
        }

        #endregion

        #region No quick way home

        [TestMethod]
        public void TheWayBackIsOnlyTheNearestHospital()
        {
            using var world = new WorldTestContext();
            var client = Player(world, 90);
            Hospitals.Gain(client, Hospitals.OnMap(MapId).Single(h => h.TeleporterId == 103));
            Messages(client);

            Die(world, client, keepPackets: true);

            // Alia Das (x 100) is nearest; Twin Pillars, gained-free and far, is not offered.
            var dead = MissionTestContext.Drain(client).OfType<PlayerDeadPacket>().Single();
            Assert.AreEqual(1, dead.Graveyards.Count);
            Assert.AreEqual(100f, dead.Graveyards.Single().Position.X, 0.01f);

            // Asked for by hand, Twin Pillars (6) is still not where they go.
            PlayerDeath.ReviveMe(client, 6);

            Assert.AreNotEqual(CharacterState.Dead, client.Player.State);
            Assert.AreEqual(100f, client.Player.Position.X, 0.01f, "Alia Das, the nearest");
            Assert.IsFalse(client.Player.DiedBySuicide);

            // An ordinary death after it has the choice again.
            client.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(world.Map, client.Player, null);
            Assert.AreEqual(2, MissionTestContext.Drain(client).OfType<PlayerDeadPacket>().Single().Graveyards.Count);
        }

        [TestMethod]
        public void ItComesBackWithTheFullRezTrauma()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.Level = 10;

            Die(world, client);
            PlayerDeath.ReviveMe(client, null);

            var trauma = client.Player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessTypeId);
            Assert.AreEqual(PlayerDeath.RezSicknessMaxStack, trauma.Stacks);
            var left = trauma.ExpiresTick - Environment.TickCount64;
            Assert.IsTrue(Math.Abs(left - PlayerDeath.RezSicknessMaxSeconds * 1000L) < 1000, $"{left} ms left");
        }

        [TestMethod]
        public void NoKitClearsItsRezTraumaUntilItWouldRunOut()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.Level = 10;

            Die(world, client);
            PlayerDeath.ReviveMe(client, null);

            Assert.IsTrue(Suicide.IsKitProof(client.Player));
            _tick += PlayerDeath.RezSicknessMaxSeconds * 1000L - 1;
            Assert.IsTrue(Suicide.IsKitProof(client.Player), "a millisecond before it would run out");
            _tick += 1;
            Assert.IsFalse(Suicide.IsKitProof(client.Player), "when it would run out");
        }

        [TestMethod]
        public void ARezTraumaTakenOffIsKitProofNoMore()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.Level = 10;

            Die(world, client);
            PlayerDeath.ReviveMe(client, null);
            GameEffectManager.Instance.DettachEffect(world.Map, client.Player, PlayerDeath.CurrentRezTrauma(client.Player));

            Assert.IsFalse(Suicide.IsKitProof(client.Player));
        }

        [TestMethod]
        public void AnOrdinaryDeathOnItLeavesItKitProof()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.Level = 10;

            Die(world, client);
            PlayerDeath.ReviveMe(client, null);
            client.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(world.Map, client.Player, null);
            PlayerDeath.ReviveMe(client, null);

            Assert.IsTrue(Suicide.IsKitProof(client.Player));
        }

        [TestMethod]
        public void AnOrdinaryDeathOnNoRezTraumaIsNotKitProof()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.Level = 10;

            Die(world, client);
            PlayerDeath.ReviveMe(client, null);
            GameEffectManager.Instance.DettachEffect(world.Map, client.Player, PlayerDeath.CurrentRezTrauma(client.Player));
            client.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(world.Map, client.Player, null);
            PlayerDeath.ReviveMe(client, null);

            Assert.IsNotNull(PlayerDeath.CurrentRezTrauma(client.Player));
            Assert.IsFalse(Suicide.IsKitProof(client.Player));
        }

        [TestMethod]
        public void AnOrdinaryDeathStillComesBackWithOneStack()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            client.Player.Level = 10;

            client.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(world.Map, client.Player, null);
            PlayerDeath.ReviveMe(client, null);

            Assert.AreEqual(1, client.Player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessTypeId).Stacks);
        }

        #endregion

        [TestMethod]
        [DataRow(3_600_000L, "60 minutes")]
        [DataRow(60_001L, "2 minutes")]
        [DataRow(60_000L, "60 seconds")]
        [DataRow(1_001L, "2 seconds")]
        [DataRow(1L, "1 second")]
        public void TheTimeLeftIsRoundedUp(long ms, string expected) => Assert.AreEqual(expected, Suicide.Left(ms));

        private static Client Player(WorldTestContext world, float x = 0)
        {
            var client = PlayerDeathTests.Player(world, x, 0);
            client.Player.Id = (uint)client.Player.EntityId;
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(client,
                new GameAccountEntry { Id = client.Player.Id, Level = (byte)GmLevel.Player });
            return client;
        }

        /// <summary>An item of this template in the player's backpack, known to the entity manager.</summary>
        private static Item Carry(Client client, uint templateId)
        {
            var item = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = templateId, ItemClass = 3147 }),
                OwnerId = client.Player.Id,
                StackSize = 1
            };
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            client.Player.Inventory.PersonalInventory.Add(item.EntityId);
            return item;
        }

        private static void Drop(Client client, Item item)
        {
            client.Player.Inventory.PersonalInventory.Remove(item.EntityId);
            EntityManager.Instance.UnregisterItem(item.EntityId);
            EntityManager.Instance.UnregisterEntity(item.EntityId);
        }

        /// <summary>Asked twice: the countdown is running.</summary>
        private static void Confirm(Client client)
        {
            Suicide.Run(client, "");
            Suicide.Run(client, "");
            CollectionAssert.AreEqual(new[] { Suicide.AskText, Suicide.StartText }, Messages(client));
        }

        private void Die(WorldTestContext world, Client client, bool keepPackets = false)
        {
            Confirm(client);
            _tick += Suicide.CountdownMs;
            Suicide.Worker(world.Map);
            Assert.AreEqual(CharacterState.Dead, client.Player.State);

            if (!keepPackets)
                MissionTestContext.Drain(client);
        }

        private void AssertCancelled(WorldTestContext world, Client client, string reason)
        {
            CollectionAssert.AreEqual(new[] { $"/suicide cancelled: {reason}." }, Messages(client));

            _tick += Suicide.CountdownMs;
            Suicide.Worker(world.Map);
            Assert.AreNotEqual(CharacterState.Dead, client.Player.State, "no death once called off");
        }

        private static void StandUp(Client client)
        {
            client.Player.State = CharacterState.Idle;
            client.Player.DiedBySuicide = false;
            client.Player.Attributes[Attributes.Health].Current = client.Player.Attributes[Attributes.Health].CurrentMax;
        }

        private static string[] Messages(Client client) =>
            MissionTestContext.Drain(client).OfType<SystemMessagePacket>().Select(packet => packet.TextMessage).ToArray();
    }
}
