extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Context;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    // The Personal Waypoints (PersonalWaypoints): CONSUMABLE_PORTABLE_WAYPOINT, the object it
    // puts down, the waypoint window at it, the way out by SelectWaypoint and the way back by
    // ReturnToWormhole, and the three items at the medical vendors.
    [TestClass]
    [DoNotParallelize]
    public class PersonalWaypointTests
    {
        private const uint OneWay = 1;
        private const uint TwoWay = 2;
        private const uint Squad = 3;

        private const long Start = 5_000_000;

        private const uint RedClan = 900011;
        private const uint BlueClan = 900012;

        #region The object

        [TestMethod]
        public void ItIsPutDownInServiceAndShownUsableToItsOwnerAlone()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);

            var obj = f.Deploy(owner, OneWay);

            Assert.AreEqual(PersonalWaypoints.WaypointClass, obj.EntityClassId);
            Assert.AreEqual(DynamicObjectType.PersonalWaypoint, obj.DynamicObjectType);
            Assert.AreEqual(UseObjectState.WhState0, obj.StateId);
            Assert.IsTrue(obj.IsEnabled, "a use of it is looked at");
            Assert.AreEqual(owner.Player.Position, obj.Position);

            // Each client around is shown it as it is for them.
            Assert.IsTrue(Created(owner, obj).Enabled);
            Assert.IsFalse(Created(stranger, obj).Enabled);
            Assert.IsTrue(PersonalWaypoints.MayUse(owner, obj));
            Assert.IsFalse(PersonalWaypoints.MayUse(stranger, obj));

            // And told nothing more while nothing changes.
            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Assert.AreEqual(0, Packets(owner).OfType<SetUsablePacket>().Count());
            Assert.AreEqual(0, Packets(stranger).OfType<SetUsablePacket>().Count());
        }

        [TestMethod]
        public void WithinReachItsOwnerGetsTheMapsWaypoints()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);
            f.Gain(owner, 10, 20);

            var obj = f.Deploy(owner, OneWay);
            Drain(owner);
            Drain(stranger);

            PersonalWaypoints.Worker(f.World.Map, Start + 500);

            var window = Packets(owner).OfType<EnteredWaypointPacket>().Single();
            Assert.AreEqual(WaypointType.Waypoint, window.WaypointTypeId);
            Assert.AreEqual(1220u, window.CurrentMapId, "the map's row, which its rows are listed under");
            Assert.AreEqual(PersonalWaypoints.WindowNameId, window.CurrentWaypointId);
            CollectionAssert.AreEquivalent(new uint[] { 10, 20 }, window.MapWaypointInfoList[1220].Waypoints.Select(w => w.WaypointId).ToArray());
            Assert.IsNull(window.TempWormholes, "not a way to itself");
            Assert.AreEqual(0, Packets(stranger).OfType<EnteredWaypointPacket>().Count());

            // Once: standing there does not open it again.
            PersonalWaypoints.Worker(f.World.Map, Start + 1000);
            Assert.AreEqual(0, Packets(owner).OfType<EnteredWaypointPacket>().Count());

            // Walking off closes it, and coming back opens it.
            f.Move(owner, obj.Position + new Vector3(PersonalWaypoints.Reach + 1, 0, 0));
            PersonalWaypoints.Worker(f.World.Map, Start + 1500);
            Assert.AreEqual(1, Packets(owner).OfType<ExitedWaypointPacket>().Count());
            Assert.IsFalse(PersonalWaypoints.IsNearUsable(owner, f.World.Map));

            f.Move(owner, obj.Position);
            PersonalWaypoints.Worker(f.World.Map, Start + 2000);
            Assert.AreEqual(1, Packets(owner).OfType<EnteredWaypointPacket>().Count());
            Assert.IsTrue(PersonalWaypoints.IsNearUsable(owner, f.World.Map));
        }

        [TestMethod]
        public void UsingItOpensTheWindowForItsOwnerAndNobodyElse()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);
            var obj = f.Deploy(owner, OneWay);
            Drain(owner);
            Drain(stranger);

            var use = new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 1, EntityId = obj.EntityId };

            PersonalWaypoints.Use(owner, obj, use);
            Assert.AreEqual(1, Packets(owner).OfType<EnteredWaypointPacket>().Count());

            PersonalWaypoints.Use(stranger, obj, use);
            Assert.AreEqual(0, Packets(stranger).OfType<EnteredWaypointPacket>().Count());
        }

        #endregion

        #region One way: out

        [TestMethod]
        public void SelectWaypointLeavesFromItForWhoeverMayUseIt()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(31, 30);
            f.Gain(owner, 20);
            f.Gain(stranger, 20);

            f.Deploy(owner, OneWay);
            Drain(owner);
            Drain(stranger);

            // No waypoint is near: the Personal Waypoint is the departure station, for its owner.
            f.Manager.SelectWaypoint(stranger, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.AreEqual(ClientState.Ingame, stranger.State);
            Assert.AreEqual(1, Packets(stranger).OfType<TeleportFailedPacket>().Count());

            f.Manager.SelectWaypoint(owner, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.AreEqual(ClientState.Teleporting, owner.State);
            Assert.AreEqual(f.Far.Position + new Vector3(0, 1, 0), owner.Player.Position);

            f.Manager.TeleportAcknowledge(owner);
            Assert.AreEqual(ClientState.Ingame, owner.State);
        }

        [TestMethod]
        public void ItIsNoWayOntoADropshipOrFromFurtherThanItsReach()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            f.Gain(owner, 20);
            WaypointTravelTests.AddWaypoint(f.Manager, f.World.Map, 30, new Vector3(250, 0, 0), type: WaypointType.Dropship);
            owner.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(owner.Player.Id, 30, (byte)WaypointType.Dropship));

            var obj = f.Deploy(owner, OneWay);
            Drain(owner);

            f.Manager.SelectWaypoint(owner, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 30 });
            Assert.AreEqual(ClientState.Ingame, owner.State);

            f.Move(owner, obj.Position + new Vector3(PersonalWaypoints.Reach + 1, 0, 0));
            f.Manager.SelectWaypoint(owner, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.AreEqual(2, Packets(owner).OfType<TeleportFailedPacket>().Count());
        }

        #endregion

        #region Two ways: back

        [TestMethod]
        public void ATwoWayOneIsListedAtAWaypointAndReturnedTo()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            f.Gain(owner, 10, 20);

            var obj = f.Deploy(owner, TwoWay);

            // At the waypoint at the origin: its window lists the way back.
            f.Move(owner, f.Near.Position);
            Drain(owner);
            f.Manager.PlayerEnterWaypoint(f.Near);

            var window = Packets(owner).OfType<EnteredWaypointPacket>().Single();
            var listed = window.TempWormholes.Single();
            Assert.AreEqual(obj.EntityId, listed.Id);
            Assert.AreEqual(obj.Position, listed.Position);
            Assert.AreEqual("Player1 Fixture", listed.OwnerName);
            Assert.IsFalse(f.Manager.Teleporters.ContainsKey((uint)listed.Id), "the window keeps waypoints and wormholes in one list by id");

            f.Manager.ReturnToWormhole(owner, obj.EntityId);

            Assert.AreEqual(ClientState.Teleporting, owner.State);
            Assert.AreEqual(obj.Position + new Vector3(0, 1, 0), owner.Player.Position);
            var packets = Packets(owner);
            Assert.AreEqual(owner.Player.Position, packets.OfType<TeleportPacket>().Single().Position);
            Assert.IsTrue(packets.FindIndex(p => p is BeginTeleportPacket) < packets.FindIndex(p => p is TeleportPacket), "or the client never acknowledges it");

            f.Manager.TeleportAcknowledge(owner);
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.IsNull(owner.PendingTransfer);
        }

        [TestMethod]
        public void AOneWayOneIsNotListedAndNotReturnedTo()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            f.Gain(owner, 10);

            var obj = f.Deploy(owner, OneWay);

            f.Move(owner, f.Near.Position);
            Drain(owner);
            f.Manager.PlayerEnterWaypoint(f.Near);
            Assert.IsNull(Packets(owner).OfType<EnteredWaypointPacket>().Single().TempWormholes);

            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.AreEqual(1, Packets(owner).OfType<TeleportFailedPacket>().Count());
        }

        [TestMethod]
        public void ReturningTakesAWaypointUnderfootAndAWormholeThatIsThere()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var stranger = f.Player(0, 0);
            var obj = f.Deploy(owner, TwoWay);
            Drain(owner);
            Drain(stranger);

            // From nowhere: the window that lists it opens at a waypoint.
            f.Move(owner, new Vector3(120, 0, 120));
            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);

            // Somebody else's, from the waypoint.
            f.Manager.ReturnToWormhole(stranger, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, stranger.State);

            // One that is not there, and no id at all.
            f.Move(owner, f.Near.Position);
            f.Manager.ReturnToWormhole(owner, obj.EntityId + 1000);
            f.Manager.ReturnToWormhole(owner, 0);
            Assert.AreEqual(ClientState.Ingame, owner.State);

            // A waypoint the Bane hold opens for nobody.
            ((WaypointInfo)f.Near.ObjectData).Contested = true;
            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);
            Assert.AreEqual(4, Packets(owner).OfType<TeleportFailedPacket>().Count());
            Assert.AreEqual(1, Packets(stranger).OfType<TeleportFailedPacket>().Count());

            // Gone: no way back.
            ((WaypointInfo)f.Near.ObjectData).Contested = false;
            PersonalWaypoints.Worker(f.World.Map, Start + 300_000);
            f.Manager.ReturnToWormhole(owner, obj.EntityId);
            Assert.AreEqual(ClientState.Ingame, owner.State);
        }

        [TestMethod]
        public void AnotherPersonalWaypointIsAWayToOne()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var mate = f.Player(60, 60);
            owner.Player.PartyId = 7;
            mate.Player.PartyId = 7;

            var theirs = f.Deploy(owner, Squad);
            var mine = f.Deploy(mate, TwoWay);
            Drain(mate);

            // At their own, the mate's window lists the squad's - and not the one they stand at.
            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            var window = Packets(mate).OfType<EnteredWaypointPacket>().Single();
            Assert.AreEqual(theirs.EntityId, window.TempWormholes.Single().Id);

            f.Manager.ReturnToWormhole(mate, mine.EntityId);
            Assert.AreEqual(ClientState.Ingame, mate.State, "the one underfoot is no way to itself");

            f.Manager.ReturnToWormhole(mate, theirs.EntityId);
            Assert.AreEqual(ClientState.Teleporting, mate.State);
            Assert.AreEqual(theirs.Position + new Vector3(0, 1, 0), mate.Player.Position);
        }

        #endregion

        #region Whose

        [TestMethod]
        public void TheSquadsOneIsTheSquadsAndTheOthersAreTheirOwnersAlone()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var mate = f.Player(31, 30);
            owner.Player.PartyId = 7;
            mate.Player.PartyId = 7;

            var own = f.Deploy(owner, TwoWay);
            Assert.IsFalse(Created(mate, own).Enabled);
            Assert.IsFalse(PersonalWaypoints.MayUse(mate, own));
            Assert.IsNull(PersonalWaypoints.ReturnPoints(mate, f.World.Map));

            var squads = f.Deploy(owner, Squad);
            Assert.IsTrue(Created(mate, squads).Enabled);
            Assert.IsTrue(PersonalWaypoints.MayUse(mate, squads));
            Assert.AreEqual(squads.EntityId, PersonalWaypoints.ReturnPoints(mate, f.World.Map).Single().Id);

            // Out of the squad while it stands, and in again: told each time, once.
            mate.Player.PartyId = 0;
            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Assert.IsFalse(Packets(mate).OfType<SetUsablePacket>().Single().IsEnabled);
            Assert.IsFalse(PersonalWaypoints.MayUse(mate, squads));

            mate.Player.PartyId = 7;
            PersonalWaypoints.Worker(f.World.Map, Start + 1000);
            Assert.IsTrue(Packets(mate).OfType<SetUsablePacket>().Single().IsEnabled);

            PersonalWaypoints.Worker(f.World.Map, Start + 1500);
            Assert.AreEqual(0, Packets(mate).OfType<SetUsablePacket>().Count());
        }

        [TestMethod]
        public void TheDeadDoNotUseIt()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var obj = f.Deploy(owner, TwoWay);

            owner.Player.State = CharacterState.Dead;

            Assert.IsFalse(PersonalWaypoints.MayUse(owner, obj));
            Assert.IsNull(PersonalWaypoints.ReturnPoints(owner, f.World.Map));
        }

        #endregion

        #region How long

        [TestMethod]
        public void ItGoesAfterFiveMinutes()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var obj = f.Deploy(owner, TwoWay);

            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Drain(owner);

            PersonalWaypoints.Worker(f.World.Map, Start + 299_999);
            Assert.AreEqual(1, f.Placed().Length);

            PersonalWaypoints.Worker(f.World.Map, Start + 300_000);

            Assert.AreEqual(0, f.Placed().Length);
            Assert.IsNull(obj.RuntimeMapChannel);
            Assert.IsFalse(obj.IsEnabled);

            var packets = Packets(owner);
            Assert.AreEqual(1, packets.OfType<ExitedWaypointPacket>().Count(), "the open window is closed");
            Assert.AreEqual(obj.EntityId, packets.OfType<DestroyPhysicalEntityPacket>().Single().EntityId);
            Assert.IsFalse(PersonalWaypoints.MayUse(owner, obj));
        }

        [TestMethod]
        public void PuttingDownAnotherTakesTheFirstAway()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var first = f.Deploy(owner, TwoWay);

            f.Move(owner, new Vector3(60, 0, 60));
            var second = f.Deploy(owner, TwoWay);

            Assert.AreSame(second, f.Placed().Single());
            Assert.IsNull(first.RuntimeMapChannel);
            Assert.AreEqual(second.EntityId, PersonalWaypoints.ReturnPoints(owner, f.World.Map).Single().Id);
        }

        [TestMethod]
        public void ItGoesWithItsOwner()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            var mate = f.Player(31, 30);
            owner.Player.PartyId = 7;
            mate.Player.PartyId = 7;
            f.Deploy(owner, Squad);

            PersonalWaypoints.Worker(f.World.Map, Start + 500);
            Assert.AreEqual(1, f.Placed().Length);

            // Off the map: another one, or out of the game.
            f.World.Map.ClientList.Remove(owner);
            PersonalWaypoints.Worker(f.World.Map, Start + 1000);

            Assert.AreEqual(0, f.Placed().Length);
            Assert.IsNull(PersonalWaypoints.ReturnPoints(mate, f.World.Map));
        }

        #endregion

        #region Where

        [TestMethod]
        public void ItIsNotPutDownInAnInstanceOrOnABattleground()
        {
            using var f = new Fixture();
            var player = f.Player(30, 30).Player;
            var map = f.World.Map;

            Assert.IsNull(PersonalWaypoints.Refusal(player), "the open world");

            // A further public copy of an open map is the open world still.
            map.IsSharedInstance = true;
            Assert.IsNull(PersonalWaypoints.Refusal(player));
            map.IsSharedInstance = false;

            // A squad's own copy of a map, and a player's own.
            map.IsSquadInstance = true;
            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, PersonalWaypoints.Refusal(player));
            map.IsSquadInstance = false;

            map.IsPrivateInstance = true;
            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, PersonalWaypoints.Refusal(player));
            map.IsPrivateInstance = false;

            // An Operation is an instance whether or not the server enters it as a squad's.
            var settings = SquadInstancePolicies.Current;

            try
            {
                SquadInstancePolicies.Apply(new Rasa.Config.SquadInstanceConfig { Enabled = false });

                player.MapChannel = new MapChannel { MapInfo = new MapInfo(1347, "adv_foreas_concordia_divide_minoscaverns", 1, 0), ClientList = new List<Client>() };
                Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, PersonalWaypoints.Refusal(player), "Minos Caverns");

                // And so is a map the settings name as one.
                player.MapChannel = new MapChannel { MapInfo = new MapInfo(999, "elsewhere", 1, 0), ClientList = new List<Client>() };
                Assert.IsNull(PersonalWaypoints.Refusal(player));

                SquadInstancePolicies.Apply(new Rasa.Config.SquadInstanceConfig { Enabled = true, Maps = new List<uint> { 999 } });
                Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, PersonalWaypoints.Refusal(player));
            }
            finally
            {
                SquadInstancePolicies.Apply(settings);
            }

            // Edmund Range: on a team or not.
            player.MapChannel = new MapChannel { MapInfo = new MapInfo(2374, "adv_wargame_edmundrange", 1, 0), ClientList = new List<Client>() };
            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, PersonalWaypoints.Refusal(player));

            player.MapChannel = map;
        }

        [TestMethod]
        public void InAnInstanceTheItemIsRefusedAndKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var instance = harness.BootcampMap.IsPrivateInstance;

            try
            {
                harness.BootcampMap.IsPrivateInstance = true;

                var item = ToyTests.Grant(harness, 118782);
                var manager = ToyTests.CreateManager(harness, 487, 2, 118782);
                harness.Drain();

                manager.RequestPerformAbility(harness.Client, ToyTests.Request(487, 2, item.EntityId));

                Assert.IsFalse(harness.BootcampMap.PerformRecovery.Any(action => action.ActionId == ActionId.ConsumablePortableWaypoint), "no windup");
                Assert.IsEmpty(Placed(harness.BootcampMap));

                var failed = harness.Drain().OfType<UserActionFailedPacket>().Single();
                Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, failed.MsgId);

                using var unit = harness.Context.CreateChar();
                Assert.IsNotNull(unit.Items.GetItem(item.Id), "kept");
            }
            finally
            {
                harness.BootcampMap.IsPrivateInstance = instance;
            }
        }

        #endregion

        #region Attacked

        [TestMethod]
        public void EachClientIsToldItsHitPointsAndWhatItIsToThem()
        {
            using var f = new Fixture();
            var owner = f.Fighter(RedClan, 30, 30);
            var enemy = f.Fighter(BlueClan, 31, 30);
            var stranger = f.Fighter(0, 32, 30);

            f.WithFeud(() =>
            {
                var obj = f.Deploy(owner, TwoWay);

                // Theirs: enabled, and the category the client starts it with, under which its
                // wormhole takes the mouse. Not theirs to damage.
                var met = f.Met(owner, obj);
                Assert.IsTrue(met.Usable);
                Assert.IsNull(met.Category);
                Assert.IsFalse(met.Damage.CanBeDamaged);
                Assert.AreEqual(PersonalWaypoints.MaxHealth, met.Damage.TotalHitPoints);
                Assert.AreEqual(PersonalWaypoints.MaxHealth, met.Damage.CurrentHitPoints);

                // An enemy's: not enabled, HOSTILE as it starts, and theirs to damage.
                met = f.Met(enemy, obj);
                Assert.IsFalse(met.Usable);
                Assert.IsNull(met.Category);
                Assert.IsTrue(met.Damage.CanBeDamaged);

                // Somebody else's: FRIENDLY has to be said, or the client reads its own start as HOSTILE.
                met = f.Met(stranger, obj);
                Assert.IsFalse(met.Usable);
                Assert.AreEqual(TargetCategory.Friendly, met.Category);
                Assert.IsFalse(met.Damage.CanBeDamaged);

                // Nothing more while nothing changes.
                PersonalWaypoints.Worker(f.World.Map, Start + 500);
                foreach (var client in new[] { enemy, stranger })
                    Assert.AreEqual(0, Packets(client).Count(p => p is TargetCategoryPacket || p is UsableDamageInfoPacket || p is SetUsablePacket));
            });
        }

        [TestMethod]
        public void AWargameBegunOrOverIsToldAgain()
        {
            using var f = new Fixture();
            var owner = f.Fighter(RedClan, 30, 30);
            var other = f.Fighter(BlueClan, 31, 30);
            var obj = f.Deploy(owner, TwoWay);

            Assert.AreEqual(TargetCategory.Friendly, f.Met(other, obj).Category, "no feud yet");

            f.WithFeud(() =>
            {
                PersonalWaypoints.Worker(f.World.Map, Start + 500);

                var packets = Packets(other);
                Assert.AreEqual(TargetCategory.Hostile, packets.OfType<TargetCategoryPacket>().Single().TargetCategory);
                Assert.IsTrue(packets.OfType<UsableDamageInfoPacket>().Single().CanBeDamaged);
                Assert.AreEqual(0, packets.OfType<SetUsablePacket>().Count(), "never theirs to use");
            });

            PersonalWaypoints.Worker(f.World.Map, Start + 1000);

            var after = Packets(other);
            Assert.AreEqual(TargetCategory.Friendly, after.OfType<TargetCategoryPacket>().Single().TargetCategory);
            Assert.IsFalse(after.OfType<UsableDamageInfoPacket>().Single().CanBeDamaged);
        }

        [TestMethod]
        public void AnEnemysShotLandsOnItsHitPointsAndNobodyElsesDoes()
        {
            using var f = new Fixture();
            var owner = f.Fighter(RedClan, 30, 30);
            var mate = f.Fighter(RedClan, 30, 31);
            var enemy = f.Fighter(BlueClan, 40, 30);
            var stranger = f.Fighter(0, 41, 30);

            f.WithFeud(() =>
            {
                var obj = f.Deploy(owner, Squad);
                Drain(owner);

                Assert.IsTrue(PersonalWaypoints.MayBeAttackedBy(enemy.Player, obj.EntityId));
                Assert.IsFalse(PersonalWaypoints.MayBeAttackedBy(owner.Player, obj.EntityId));
                Assert.IsFalse(PersonalWaypoints.MayBeAttackedBy(mate.Player, obj.EntityId));
                Assert.IsFalse(PersonalWaypoints.MayBeAttackedBy(stranger.Player, obj.EntityId));
                Assert.IsFalse(PersonalWaypoints.MayBeAttackedBy(enemy.Player, f.Near.EntityId), "a waypoint is no Personal Waypoint");

                // What it took is what the hit shows, and everyone around is told what is left.
                var hit = f.Shoot(enemy.Player, obj, 300);
                Assert.AreEqual(300, hit.DamageA);
                Assert.AreEqual(PersonalWaypoints.MaxHealth - 300, PersonalWaypoints.HealthOf(obj));
                Assert.AreEqual(PersonalWaypoints.MaxHealth - 300, Packets(owner).OfType<UpdateHitPointsPacket>().Single().CurrentHitPoints);

                foreach (var harmless in new[] { owner, mate, stranger })
                    Assert.AreEqual(0, f.Shoot(harmless.Player, obj, 300).DamageA);

                Assert.AreEqual(PersonalWaypoints.MaxHealth - 300, PersonalWaypoints.HealthOf(obj));

                // A shot is let go at it only by who may harm it.
                var before = f.World.Map.QueuedMissiles.Count;

                MissileManager.Instance.MissileLaunch(f.World.Map, new ActionData(stranger.Player, ActionId.WeaponAttack, 1, obj.EntityId, 0), 55);
                Assert.AreEqual(before, f.World.Map.QueuedMissiles.Count);

                MissileManager.Instance.MissileLaunch(f.World.Map, new ActionData(enemy.Player, ActionId.WeaponAttack, 1, obj.EntityId, 0), 55);
                Assert.AreEqual(before + 1, f.World.Map.QueuedMissiles.Count);
                Assert.AreEqual(obj.EntityId, f.World.Map.QueuedMissiles.Last().TargetEntityId);
                f.World.Map.QueuedMissiles.Clear();

                // A constant-fire weapon's pulse.
                Assert.AreEqual(120, PersonalWaypoints.TakeDamage(enemy.Player, obj.EntityId, 120));
                Assert.IsNull(PersonalWaypoints.TakeDamage(stranger.Player, obj.EntityId, 120));
                Assert.IsNull(PersonalWaypoints.TakeDamage(enemy.Player, f.Near.EntityId, 120));
                Assert.AreEqual(PersonalWaypoints.MaxHealth - 420, PersonalWaypoints.HealthOf(obj));
            });
        }

        [TestMethod]
        public void AtNoHitPointsItIsGoneAndItsDeathPlaysWhereItStood()
        {
            using var f = new Fixture();
            var owner = f.Fighter(RedClan, 30, 30);
            var enemy = f.Fighter(BlueClan, 40, 30);
            f.Gain(owner, 10);

            f.WithFeud(() =>
            {
                var obj = f.Deploy(owner, TwoWay);
                var id = obj.EntityId;
                var position = obj.Position;

                PersonalWaypoints.Worker(f.World.Map, Start + 500);
                Drain(owner);

                Assert.AreEqual(PersonalWaypoints.MaxHealth - 1, f.Shoot(enemy.Player, obj, PersonalWaypoints.MaxHealth - 1).DamageA);
                Assert.AreEqual(1, f.Placed().Length);

                // More than it has left: it takes what it has.
                Assert.AreEqual(1, f.Shoot(enemy.Player, obj, 500).DamageA);

                Assert.AreEqual(0, f.Placed().Length);
                Assert.IsNull(obj.RuntimeMapChannel);
                Assert.AreEqual(0, PersonalWaypoints.HealthOf(obj));
                Assert.IsNull(PersonalWaypoints.ReturnPoints(owner, f.World.Map), "no way back to it");
                Assert.IsFalse(PersonalWaypoints.TryGetPosition(id, out _));

                var packets = Packets(owner);
                Assert.AreEqual(0, packets.OfType<UpdateHitPointsPacket>().Last().CurrentHitPoints);
                Assert.AreEqual(1, packets.OfType<ExitedWaypointPacket>().Count(), "the open window is closed");
                Assert.AreEqual(id, packets.OfType<DestroyPhysicalEntityPacket>().Single().EntityId);

                // vfx_ability_wormhole_death, for a while.
                var death = f.Deaths().Single();
                Assert.AreEqual(PersonalWaypoints.DeathPackage, ((MapEmitter)death.ObjectData).PackageId);
                Assert.AreEqual(position, death.Position);

                var now = Environment.TickCount64;
                PersonalWaypoints.Worker(f.World.Map, now + PersonalWaypoints.DeathMs - 100);
                Assert.AreEqual(1, f.Deaths().Length);

                PersonalWaypoints.Worker(f.World.Map, now + PersonalWaypoints.DeathMs + 100);
                Assert.AreEqual(0, f.Deaths().Length);
            });
        }

        [TestMethod]
        public void ItsDeathPlaysWhenItsTimeIsUpToo()
        {
            using var f = new Fixture();
            var owner = f.Player(30, 30);
            f.Deploy(owner, OneWay);

            PersonalWaypoints.Worker(f.World.Map, Start + 300_000);

            Assert.AreEqual(1, f.Deaths().Length);

            PersonalWaypoints.Worker(f.World.Map, Start + 300_000 + PersonalWaypoints.DeathMs);
            Assert.AreEqual(0, f.Deaths().Length);
        }

        [TestMethod]
        public void AnAttackOnAnEnemysEndsTheAttackersSafety()
        {
            using var f = new Fixture();
            var owner = f.Fighter(RedClan, 30, 30);
            var enemy = f.Fighter(BlueClan, 40, 30);

            f.WithFeud(() =>
            {
                var obj = f.Deploy(owner, TwoWay);

                enemy.Player.ActiveEffects[900] = new GameEffect { EffectId = 900, TypeId = Pvp.SafetyTypeId };
                Assert.IsTrue(Pvp.IsSafe(enemy.Player));

                f.Shoot(enemy.Player, obj, 10);

                Assert.IsFalse(Pvp.IsSafe(enemy.Player));
            });
        }

        [TestMethod]
        public void AHostileCreatureMayFightItAndAFriendlyOneMayNot()
        {
            using var f = new Fixture();
            var owner = f.Fighter(RedClan, 30, 30);
            var enemy = f.Fighter(BlueClan, 60, 60);
            var obj = f.Deploy(owner, TwoWay);

            var bane = f.Creature(TargetCategory.Hostile, 33, 30);
            var soldier = f.Creature(TargetCategory.Friendly, 34, 30);
            var neutral = f.Creature(TargetCategory.Neutral, 35, 30);

            Assert.IsTrue(BehaviorManager.MayFight(bane, obj.EntityId));
            Assert.IsTrue(Threat.CanFight(bane, obj.EntityId));
            Assert.IsFalse(BehaviorManager.MayFight(soldier, obj.EntityId));
            Assert.IsFalse(BehaviorManager.MayFight(neutral, obj.EntityId));
            Assert.IsFalse(BehaviorManager.MayFight(bane, f.Near.EntityId), "a waypoint is nothing to fight");

            // A player's creature: for its master's wargames, not for what it is.
            var turret = f.Creature(TargetCategory.Friendly, 36, 30, master: enemy);
            Assert.IsFalse(BehaviorManager.MayFight(turret, obj.EntityId));

            f.WithFeud(() =>
            {
                Assert.IsTrue(BehaviorManager.MayFight(turret, obj.EntityId));
                Assert.IsTrue(Threat.CanFight(turret, obj.EntityId));
            });

            // Its hit lands as a player's does.
            Assert.AreEqual(250, f.Shoot(bane, obj, 250).DamageA);
            Assert.AreEqual(0, f.Shoot(soldier, obj, 250).DamageA);
            Assert.AreEqual(PersonalWaypoints.MaxHealth - 250, PersonalWaypoints.HealthOf(obj));

            // Gone: nothing to fight.
            PersonalWaypoints.Worker(f.World.Map, Start + 300_000);
            Assert.IsFalse(BehaviorManager.MayFight(bane, obj.EntityId));
            Assert.IsFalse(Threat.CanFight(bane, obj.EntityId));
        }

        [TestMethod]
        public void AHostileCreatureInRangePicksItWalksUpToItAndBringsItDown()
        {
            using var f = new Fixture();
            var owner = f.Fighter(RedClan, 30, 30);
            var obj = f.Deploy(owner, TwoWay);

            // The owner is out of its sight; the waypoint is not.
            f.Move(owner, new Vector3(150, 0, 150));

            var bane = f.Creature(TargetCategory.Hostile, 40, 30);
            bane.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponMelee, ActionArgId = 1, RangeMin = 0, RangeMax = 5, MinDamage = 400, MaxDamage = 400, Cooldown = 1000 });
            bane.HomePos.Position = bane.Position;
            bane.RunSpeed = 4;
            bane.WalkSpeed = 2;
            BehaviorManager.StartWandering(bane, false);

            // Past the pause a creature takes before it looks about again.
            bane.LastAgression = 3000;

            f.Think(1);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, bane.Controller.CurrentAction);
            Assert.AreEqual(obj.EntityId, bane.Controller.ActionFighting.TargetEntityId);

            bane.Controller.ActionFighting.Opened = true;

            // Up to it and at it, until it is gone - and then the creature has nothing to fight.
            for (var tick = 0; tick < 400 && f.Placed().Length > 0; tick++)
                f.Think(1);

            Assert.AreEqual(0, f.Placed().Length, $"left with {PersonalWaypoints.HealthOf(obj)} hit points");
            Assert.IsLessThan(5f, Vector3.Distance(bane.Position, new Vector3(30, 0, 30)), "it walked up to within its reach of it");

            f.Think(2);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, bane.Controller.CurrentAction);
        }

        #endregion

        #region The item

        [TestMethod]
        [DataRow(118781u, 1u)]
        [DataRow(118782u, 2u)]
        [DataRow(118783u, 3u)]
        public void TheItemPutsOneDownAndIsUsedUp(uint template, uint level)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var instance = harness.BootcampMap.IsPrivateInstance;

            // The harness's map as open ground: an instance takes none (InAnInstanceTheItemIsRefusedAndKept).
            harness.BootcampMap.IsPrivateInstance = false;

            var entry = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == (uint)PersonalWaypoints.WaypointClass);
            EntityClassManager.Instance.LoadedEntityClasses[PersonalWaypoints.WaypointClass] = new EntityClass(entry.Id, entry.ClassName, entry.MeshId,
                entry.ClassCollisionRole, entry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), entry.TargetFlag != 0);

            try
            {
                Assert.AreEqual("UsableAbilityTemporaryWormhole", entry.ClassName);
                Assert.AreEqual(((uint)AugmentationType.Wormhole).ToString(), entry.AugList);

                var item = ToyTests.Grant(harness, template);
                var manager = ToyTests.CreateManager(harness, 487, level, template);
                harness.Drain();

                manager.RequestPerformAbility(harness.Client, ToyTests.Request(487, level, item.EntityId));

                var pending = harness.BootcampMap.PerformRecovery.Single(action => action.ActionId == ActionId.ConsumablePortableWaypoint);
                Assert.AreEqual(8000, pending.WaitTime, "the level's windup");
                Assert.IsEmpty(Placed(harness.BootcampMap), "not until it has run");

                ToyTests.Land(harness, manager, pending);

                var obj = Placed(harness.BootcampMap).Single();
                Assert.AreEqual(PersonalWaypoints.WaypointClass, obj.EntityClassId);
                Assert.IsLessThan(0.5f, Vector3.Distance(new Vector3(harness.Client.Player.Position.X, 0, harness.Client.Player.Position.Z),
                    new Vector3(obj.Position.X, 0, obj.Position.Z)));
                Assert.IsTrue(PersonalWaypoints.MayUse(harness.Client, obj));
                Assert.AreEqual(level >= PersonalWaypoints.TwoWayLevel,
                    PersonalWaypoints.ReturnPoints(harness.Client, harness.BootcampMap) != null);
                Assert.HasCount(1, harness.Drain().OfType<AbilityRecoveryPacket>().ToArray());

                using var unit = harness.Context.CreateChar();
                Assert.IsNull(unit.Items.GetItem(item.Id), "used up");
                Assert.IsFalse(harness.Client.Player.ActionReuseUntil.ContainsKey(ActionId.ConsumablePortableWaypoint), "it has no reuse");
            }
            finally
            {
                PersonalWaypoints.Worker(harness.BootcampMap, long.MaxValue - PersonalWaypoints.DeathMs);
                PersonalWaypoints.Worker(harness.BootcampMap, long.MaxValue);
                EntityClassManager.Instance.LoadedEntityClasses.Remove(PersonalWaypoints.WaypointClass);
                harness.BootcampMap.IsPrivateInstance = instance;
            }
        }

        #endregion

        #region The packets

        [TestMethod]
        public void EnteredWaypointListsTheWormholesAsTheClientReadsThem()
        {
            var list = new Dictionary<uint, MapWaypointInfoList>();

            Read(new EnteredWaypointPacket(1220, 1220, list, WaypointType.Waypoint, 10,
                new[] { new TempWormhole(0x1_0000_0005UL, new Vector3(1, 2, 3), "Sarah Morgan") }), r =>
            {
                Assert.AreEqual(6, r.ReadTuple());
                Assert.AreEqual(1220, r.ReadInt());
                Assert.AreEqual(1220, r.ReadInt());
                Assert.AreEqual(0, r.ReadList());

                // for (wormholeId, pos, ownername) in tempWormholes
                Assert.AreEqual(1, r.ReadList());
                Assert.AreEqual(3, r.ReadTuple());
                Assert.AreEqual(0x1_0000_0005UL, r.ReadULong());
                Assert.AreEqual(3, r.ReadTuple());
                CollectionAssert.AreEqual(new[] { 1.0, 2.0, 3.0 }, new[] { r.ReadDouble(), r.ReadDouble(), r.ReadDouble() });
                Assert.AreEqual("Sarah Morgan", r.ReadUnicodeString());

                Assert.AreEqual(1, r.ReadInt(), "the client's LOCALWAYPOINT");
                Assert.AreEqual(10, r.ReadInt());
            });

            // None is what it was: "if tempWormholes is not None".
            foreach (var none in new IReadOnlyList<TempWormhole>[] { null, Array.Empty<TempWormhole>() })
                Read(new EnteredWaypointPacket(1220, 1220, list, WaypointType.Waypoint, 10, none), r =>
                {
                    Assert.AreEqual(6, r.ReadTuple());
                    r.ReadInt();
                    r.ReadInt();
                    Assert.AreEqual(0, r.ReadList());
                    r.ReadNoneStruct();
                    Assert.AreEqual(1, r.ReadInt());
                });
        }

        [TestMethod]
        public void ReturnToWormholeReadsTheIdAsAnIntOrALong()
        {
            Assert.AreEqual(GameOpcode.ReturnToWormhole, new ReturnToWormholePacket().Opcode);
            Assert.AreEqual(686, (int)GameOpcode.ReturnToWormhole);

            Assert.AreEqual(77UL, Sent(w => w.WriteInt(77)).WormholeId);
            Assert.AreEqual(0x1_0000_0005UL, Sent(w => w.WriteULong(0x1_0000_0005UL)).WormholeId);
        }

        [TestMethod]
        public void TheWindowsNameAndTheWormholesIdsAreNoWaypointOfTheWorld()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var teleporters = harness.WorldContext.Set<TeleporterEntry>().AsNoTracking().ToList();

            // The window greys out the row whose id is the current one's.
            var row = teleporters.Single(teleporter => teleporter.Id == PersonalWaypoints.WindowNameId);
            Assert.AreEqual("Portable Waypoint", row.Description);
            Assert.AreEqual(0u, row.MapContextId, "a name with no place");

            // And keeps waypoints and wormholes in one list by id: a wormhole's is its entity id,
            // and those begin where the waypoints' have long ended.
            Assert.IsLessThan(1000u, teleporters.Where(teleporter => teleporter.MapContextId != 0).Max(teleporter => teleporter.Id));
            Assert.IsGreaterThanOrEqualTo(1000UL, EntityManager.Instance.GetEntityId);
        }

        #endregion

        #region The medical vendors

        private const string Before = "20261109000000_Use_client_waypoint_ids";
        private const string Migration = "20261110000000_Stock_personal_waypoints";

        private static readonly string Medical = string.Join(", ", PersonalWaypointStock.MedicalPackages);
        private static readonly string Items = string.Join(", ", PersonalWaypointStock.Items);

        [TestMethod]
        public void TheMigrationStocksEveryMedicalVendorAndTakesItOffAgain()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                migrator.Migrate(Before);

                var before = Rows(context, "select id, item_template_id from vendor_item order by rowid");
                var vendors = Rows(context, $"select id from vendor where package_id in ({Medical}) order by id");
                Assert.AreEqual(182, vendors.Count);
                Assert.AreEqual(0, Rows(context, $"select id from vendor_item where item_template_id in ({Items})").Count, "nobody sold one");

                migrator.Migrate();

                // Each of the three, once, at each medical vendor and nowhere else, after what it sold.
                var stocked = Rows(context, $"select id, item_template_id from vendor_item where item_template_id in ({Items}) order by id, item_template_id");
                CollectionAssert.AreEqual(
                    vendors.SelectMany(vendor => PersonalWaypointStock.Items.Select(item => $"{vendor}|{item}")).ToArray(), stocked);
                CollectionAssert.AreEqual(before, Rows(context, $"select id, item_template_id from vendor_item where item_template_id not in ({Items}) order by rowid"));

                // What they cost is the templates' own.
                CollectionAssert.AreEqual(new[] { "118781|1000", "118782|2500", "118783|5000" },
                    Rows(context, $"select id, buy_price from itemtemplate where id in ({Items}) order by id"));

                // Run again, as the script for a MySQL server may be: nothing is doubled.
                var after = Rows(context, "select id, item_template_id from vendor_item order by rowid");

                foreach (var statement in PersonalWaypointStock.Up)
                    context.Database.ExecuteSqlRaw(statement);

                CollectionAssert.AreEqual(after, Rows(context, "select id, item_template_id from vendor_item order by rowid"));

                migrator.Migrate(Before);
                CollectionAssert.AreEqual(before, Rows(context, "select id, item_template_id from vendor_item order by rowid"));
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
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            foreach (var statement in PersonalWaypointStock.Up)
                StringAssert.Contains(up, statement);

            foreach (var statement in PersonalWaypointStock.Down)
                StringAssert.Contains(down, statement);

            StringAssert.Contains(up, $"'{Migration}'");
        }

        #endregion

        #region Helpers

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static void Drain(Client client) => WorldTestContext.Drain(client);

        /// <summary>The UsableInfo the client was sent with the object: how it is for them.</summary>
        private static UsableInfoPacket Created(Client client, DynamicObject obj) =>
            Packets(client).OfType<CreatePhysicalEntityPacket>().Single(created => created.EntityId == obj.EntityId)
                .EntityData.OfType<UsableInfoPacket>().Single();

        private static DynamicObject[] Placed(MapChannel map) =>
            map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.DynamicObjectType == DynamicObjectType.PersonalWaypoint)
                .Distinct()
                .ToArray();

        private static List<string> Rows(RasaDbContextBase context, string sql)
        {
            var rows = new List<string>();
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));

            return rows;
        }

        private static void Read(ServerPythonPacket packet, Action<PythonReader> read)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                packet.Write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            read(reader);
        }

        private static ReturnToWormholePacket Sent(Action<PythonWriter> id)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, Encoding.UTF8, true)))
            {
                writer.WriteTuple(1);
                id(writer);
            }

            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            var packet = new ReturnToWormholePacket();
            packet.Read(reader);

            return packet;
        }

        /// <summary>
        /// The fixture map with two waypoints - 10 at the origin and 20 two hundred metres off -
        /// the object's class, and the window's waypoint list taken from the manager under test.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            private readonly Func<Client, Dictionary<uint, MapWaypointInfoList>> _network = PersonalWaypoints.Network;
            private readonly List<Creature> _creatures = new List<Creature>();

            internal WorldTestContext World { get; } = new WorldTestContext();
            internal DynamicObjectManager Manager { get; }
            internal DynamicObject Near { get; }
            internal DynamicObject Far { get; }

            internal Fixture()
            {
                World.AddClass(PersonalWaypoints.WaypointClass);
                Manager = WaypointTravelTests.CreateManager(World);
                Near = WaypointTravelTests.AddWaypoint(Manager, World.Map, 10, Vector3.Zero);
                Far = WaypointTravelTests.AddWaypoint(Manager, World.Map, 20, new Vector3(200, 5, 0));
                Near.RuntimeMapChannel = World.Map;
                Far.RuntimeMapChannel = World.Map;
                PersonalWaypoints.Network = client => Manager.CreateListOfWaypoints(client, WaypointType.Waypoint);
            }

            /// <summary>A player in the world, with nothing waiting to be read.</summary>
            internal Client Player(float x, float z)
            {
                var client = World.CreateClient(x, z);

                client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
                client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);

                return client;
            }

            internal void Gain(Client client, params uint[] waypoints)
            {
                foreach (var id in waypoints)
                    client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, id, (byte)WaypointType.Waypoint));
            }

            internal void Move(Client client, Vector3 position)
            {
                client.SetWorldPosition(position, client.Player.Rotation);
                CellManager.Instance.UpdateVisibility(client);
            }

            internal DynamicObject Deploy(Client owner, uint level) =>
                PersonalWaypoints.Deploy(World.Map, owner.Player, level, null, Start);

            internal DynamicObject[] Placed() => PersonalWaypointTests.Placed(World.Map);

            /// <summary>The deaths playing on the map: the emitters of ones that have gone.</summary>
            internal DynamicObject[] Deaths() =>
                World.Map.MapCellInfo.Cells.Values
                    .SelectMany(cell => cell.DynamicObjectList)
                    .Where(obj => obj.DynamicObjectType == DynamicObjectType.Emitter)
                    .Distinct()
                    .ToArray();

            /// <summary>A player of a clan, with health to be fought for.</summary>
            internal Client Fighter(uint clan, float x, float z)
            {
                var client = Player(x, z);

                client.Player.ClanId = clan;

                return client;
            }

            /// <summary>The Red clan and the Blue at feud for as long as the body runs; the players online for its messages.</summary>
            internal void WithFeud(Action body)
            {
                var online = World.Map.ClientList.ToList();

                lock (Server.Clients)
                    Server.Clients.AddRange(online);

                ClanFeuds.Feud feud = null;

                try
                {
                    feud = ClanFeuds.Instance.Start(
                        new ClanEntry { Id = RedClan, Name = "Red", IsPvP = true },
                        new ClanEntry { Id = BlueClan, Name = "Blue", IsPvP = true });
                    Assert.IsNotNull(feud);

                    foreach (var client in online)
                        WorldTestContext.Drain(client);

                    body();
                }
                finally
                {
                    if (feud != null)
                        ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                    lock (Server.Clients)
                        foreach (var client in online)
                            Server.Clients.Remove(client);

                    foreach (var client in online)
                        WorldTestContext.Drain(client);
                }
            }

            /// <summary>What a client has been told of the object since it was last read: whether it met it enabled, a category if one was said, and its hit points.</summary>
            internal (bool Usable, TargetCategory? Category, UsableDamageInfoPacket Damage) Met(Client client, DynamicObject obj)
            {
                var messages = WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();
                var created = messages.Select(message => message.Packet).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == obj.EntityId);
                var told = messages.Where(message => message.EntityId == obj.EntityId).Select(message => message.Packet).ToList();

                return (created.EntityData.OfType<UsableInfoPacket>().Single().Enabled,
                    told.OfType<TargetCategoryPacket>().SingleOrDefault()?.TargetCategory,
                    told.OfType<UsableDamageInfoPacket>().Single());
            }

            /// <summary>A creature on the map, of a player's if it has a master.</summary>
            internal Creature Creature(TargetCategory category, float x, float z, Client master = null)
            {
                var creature = new Creature
                {
                    Name = "Fixture",
                    MasterEntityId = master?.Player.EntityId ?? 0,
                    TargetCategory = category,
                    MapContextId = World.Map.MapInfo.MapContextId,
                    Position = new Vector3(x, 0, z),
                    EntityClass = EntityClasses.HumanBaseMale,
                    State = CharacterState.Idle,
                    Level = 1,
                    AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
                };

                creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
                creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
                CellManager.Instance.AddToWorld(World.Map, creature);
                _creatures.Add(creature);

                foreach (var client in World.Map.ClientList)
                    WorldTestContext.Drain(client);

                return creature;
            }

            /// <summary>A hit on the object that lands now.</summary>
            internal Missile Shoot(Actor source, DynamicObject obj, int damage)
            {
                var missile = new Missile
                {
                    Source = source,
                    TargetEntityId = obj.EntityId,
                    DamageA = damage,
                    DamageType = DamageType.Physical,
                    ActionId = ActionId.WeaponAttack,
                    ActionArgId = 1,
                    CritChance = 0
                };

                MissileManager.Instance.MissileTrigger(World.Map, missile);

                return missile;
            }

            /// <summary>The creatures think and what they have let go lands, a quarter second a tick.</summary>
            internal void Think(int ticks)
            {
                for (var tick = 0; tick < ticks; tick++)
                {
                    BehaviorManager.Instance.MapChannelThink(World.Map, 250);
                    MissileManager.Instance.DoWork(World.Map, 250);
                }
            }

            public void Dispose()
            {
                foreach (var creature in _creatures)
                    CellManager.Instance.RemoveCreatureFromWorld(World.Map, creature);

                World.Map.QueuedMissiles.Clear();

                // Whatever is still out is taken away with the map, and its death after it.
                foreach (var client in World.Map.ClientList.ToList())
                    World.Map.ClientList.Remove(client);

                PersonalWaypoints.Worker(World.Map, long.MaxValue - PersonalWaypoints.DeathMs);
                PersonalWaypoints.Worker(World.Map, long.MaxValue);
                PersonalWaypoints.Network = _network;
                World.Dispose();
            }
        }

        #endregion
    }
}
