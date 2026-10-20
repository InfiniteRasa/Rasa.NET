extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class DropshipTravelTests
    {
        [TestMethod]
        public void BoardingUsesDestinationMetadataAndDepartsFromTheSourceMap()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(1220, world.Map);
            maps.MapChannelArray.Add(1300, destination);
            var saved = 0;
            var manager = new DynamicObjectManager(null, maps, () => 1000, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    saved++;
            });
            WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero, type: WaypointType.Dropship);
            WaypointTravelTests.AddWaypoint(manager, destination, 20, new Vector3(400, 5, 0), type: WaypointType.Dropship);
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Dropship));

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            Assert.IsNotNull(client.PendingTransfer);
            Assert.IsTrue(client.PendingTransfer.IsDropship);
            Assert.AreEqual(1300U, client.LoadingMap);
            Assert.IsFalse(client.HandleMovement(new Rasa.Models.Movement(Vector3.One, Vector2.Zero)));
            var ship = manager.Dropships[client.PendingTransfer.DropshipId];
            try
            {
                for (var step = 0; step < 6; step++)
                    manager.DropshipsWorker(world.Map, Math.Max(0, ship.PhaseTimeleft));

                Assert.IsTrue(client.PendingTransfer.HasDeparted);
                Assert.AreSame(destination, client.Player.MapChannel);
                Assert.AreEqual(1300U, client.Player.MapContextId);
                Assert.AreEqual(new Vector3(400, 5, 0), client.Player.Position);
                Assert.AreEqual(ClientState.Teleporting, client.State);
                Assert.IsFalse(world.Map.ClientList.Contains(client));
                Assert.IsFalse(manager.Dropships.ContainsKey(ship.EntityId));
                Assert.IsTrue(manager.IsExpectedMapLoad(client));
                Assert.AreEqual(0, saved);

                destination.ClientList.Add(client);
                CellManager.Instance.AddToWorld(client);
                manager.CompleteMapLoadTransfer(client);
                Assert.IsNull(client.PendingTransfer);
                Assert.AreEqual(1, saved);
                var arrival = new Dropship(
                    TargetCategory.Friendly,
                    DropshipType.Teleporter,
                    client,
                    DropshipRole.Arrival);
                CellManager.Instance.AddToWorld(destination, arrival);
                manager.Dropships.Add(arrival.EntityId, arrival);
                for (var step = 0; step < 6; step++)
                    manager.DropshipsWorker(destination, Math.Max(0, arrival.PhaseTimeleft));
                Assert.AreEqual(ClientState.Ingame, client.State);
                Assert.AreEqual(1, destination.ClientList.Count);
                Assert.IsFalse(manager.IsExpectedMapLoad(client));
            }
            finally
            {
                foreach (var remaining in new List<Dropship>(manager.Dropships.Values))
                {
                    var map = remaining.MapContextId == 1220 ? world.Map : destination;
                    CellManager.Instance.RemoveFromWorld(map, remaining);
                    manager.Dropships.Remove(remaining.EntityId);
                }
                destination.ClientList.Clear();
                destination.MapCellInfo.Cells.Clear();
            }
        }

        [TestMethod]
        public void DepartedDropshipAcceptsMapLoadedThroughTheProductionHandler()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var saved = 0;
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    saved++;
            });
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);

            Depart(world.Map, manager, client);

            Assert.IsTrue(client.AwaitingMapLoaded);
            RouteMapLoaded(client);

            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(1, saved);
            Assert.AreEqual(1, destination.ClientList.Count(member => member == client));
            Assert.IsTrue(CellManager.Instance.IsInWorld(client));

            LandArrival(destination, manager);
            Assert.AreEqual(ClientState.Ingame, client.State);
        }

        // A ride off the map leaves the map as a map link does: it had a copy of its own that
        // left target, weapon, stance, kill streak and the map's rosters with the player.
        [TestMethod]
        public void ADropshipRideLeavesTheMapAsAMapLinkDoes()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            KillStreaks.Reset();

            try
            {
                var player = client.Player;
                KillStreaks.Set(client, 2);
                Assert.AreEqual(2, KillStreaks.LevelOf(client));
                StartDropshipTransfer(world, destination, client, manager);

                player.Target = 4242;
                player.WeaponReady = true;
                player.InCombatMode = true;
                player.RequestedCombatMode = true;
                player.AutoFireCombatMode = true;
                player.TrackingTargetEntityId = 4242;
                player.InsideMapLinks.Add(7);
                player.RegionIds = new List<uint> { 1 };
                player.RegionsHeld = true;

                Depart(world.Map, manager, client);

                Assert.IsTrue(client.PendingTransfer.HasDeparted);
                Assert.AreSame(destination, player.MapChannel);
                Assert.AreEqual(0UL, player.Target, "a target of the map left");
                Assert.IsFalse(player.WeaponReady);
                Assert.IsFalse(player.InCombatMode);
                Assert.IsFalse(player.RequestedCombatMode);
                Assert.IsFalse(player.AutoFireCombatMode);
                Assert.AreEqual(0UL, player.TrackingTargetEntityId);
                Assert.AreEqual(0, KillStreaks.LevelOf(client), "a kill streak is of its map");
                Assert.IsEmpty(player.InsideMapLinks);
                Assert.IsNull(player.RegionIds);
                Assert.IsFalse(player.RegionsHeld);
                Assert.IsFalse(world.Map.ClientList.Contains(client));
            }
            finally
            {
                KillStreaks.Reset();
            }
        }

        // BR-166: a buff carried across a map change goes on again where the player's own client
        // is told of it - once they are in the world, not while they are still Teleporting.
        [TestMethod]
        public void ABuffCarriedByDropshipIsShownToItsOwnClientWhenTheShipSetsThemDown()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            var buff = CarriedBuff(world.Map, client.Player);
            StartDropshipTransfer(world, destination, client, manager);

            Depart(world.Map, manager, client);
            Assert.AreEqual(1, client.Player.CarriedEffects.Count, "kept aside for the ride");

            RouteMapLoaded(client);
            WorldTestContext.Drain(client);
            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.AreEqual(1, client.Player.CarriedEffects.Count, "not yet: their own client would not be told");

            LandArrival(destination, manager);

            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsEmpty(client.Player.CarriedEffects);
            Assert.IsTrue(client.Player.ActiveEffects.ContainsKey(buff.EffectId), "on again, under the new map's id");
            Assert.IsTrue(AttachesTo(client).Any(packet => packet.EffectTypeId == BuffTypeId && packet.Announced),
                "and their own client has it, announced");
        }

        [TestMethod]
        public void ABuffCarriedThroughAMapLinkIsShownToItsOwnClient()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var destination = CreateDestination();
            CellManager.Instance.AddToWorld(client);
            var maps = CreateMaps(world, destination);
            var buff = CarriedBuff(world.Map, client.Player);

            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId, new Vector3(400, 5, 0), 0));
            Assert.AreEqual(1, client.Player.CarriedEffects.Count);
            WorldTestContext.Drain(client);

            try
            {
                maps.MapLoaded(client);

                Assert.AreEqual(ClientState.Ingame, client.State);
                Assert.IsTrue(client.Player.ActiveEffects.ContainsKey(buff.EffectId));
                Assert.IsTrue(AttachesTo(client).Any(packet => packet.EffectTypeId == BuffTypeId && packet.Announced),
                    "their own client was left out while they were still Teleporting");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(client);
                destination.ClientList.Clear();
            }
        }

        [TestMethod]
        public void AClanMemberArrivingByDropshipIsGivenTheirOwnEntityWithTheirClan()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            client.Player.ClanId = 77;
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            WorldTestContext.Drain(client);

            RouteMapLoaded(client);

            Assert.AreEqual(77u, OwnClanId(client), "the entity the client makes again carries the clan");
        }

        [TestMethod]
        public void AClanMemberThroughAMapLinkIsGivenTheirOwnEntityWithTheirClan()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.Player.ClanId = 77;
            var destination = CreateDestination();
            CellManager.Instance.AddToWorld(client);
            var maps = CreateMaps(world, destination);

            // The map link tells the clan's feuds of the arrival, which asks the clan for the
            // member's rank: a member of it, in the cache, as a server has them.
            var members = ClanManager.Instance.ClanMembers;
            ClanManager.Instance.ClanMembers = new System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>>(
                new[]
                {
                    new KeyValuePair<uint, Lazy<List<ClanMemberEntry>>>(77, new Lazy<List<ClanMemberEntry>>(() =>
                        new List<ClanMemberEntry> { new() { ClanId = 77, CharacterId = client.Player.Id, Rank = 0, Note = "" } }))
                });

            Assert.IsTrue(maps.ChangeMap(client, destination.MapInfo.MapContextId, new Vector3(400, 5, 0), 0));
            WorldTestContext.Drain(client);

            try
            {
                maps.MapLoaded(client);

                Assert.AreEqual(77u, OwnClanId(client));
            }
            finally
            {
                ClanManager.Instance.ClanMembers = members;
                CellManager.Instance.RemoveFromWorld(client);
                destination.ClientList.Clear();
            }
        }

        [TestMethod]
        public void TheClanGoesWithThePlayersEntityOnlyWhenTheyHaveOne()
        {
            using var world = new WorldTestContext();
            var member = world.CreateClient();
            var other = world.CreateClient(5, 0);
            member.Player.ClanId = 77;

            var own = ManifestationManager.Instance.CreatePlayerEntityData(member, member);
            Assert.AreEqual(77u, own.OfType<ClanIdPacket>().Single().ClanId);
            Assert.AreEqual(77u, ManifestationManager.Instance.CreatePlayerEntityData(member, other).OfType<ClanIdPacket>().Single().ClanId,
                "to another's client too, for the tag over the name (ClanTagTests)");
            Assert.IsFalse(ManifestationManager.Instance.CreatePlayerEntityData(other, other).OfType<ClanIdPacket>().Any(),
                "no clan, nothing: every manifestation starts in none");
        }

        /// <summary>The clan id in the creation of the player's own entity on their own client, or null.</summary>
        private static uint? OwnClanId(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<Rasa.Packets.Game.Server.CreatePhysicalEntityPacket>()
                .Where(create => create.EntityId == client.Player.EntityId)
                .SelectMany(create => create.EntityData).OfType<ClanIdPacket>()
                .Select(packet => (uint?)packet.ClanId).SingleOrDefault();

        [TestMethod]
        public void BuffsStillAsideWhenThePlayerLeavesAgainGoWithThem()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            var first = CarriedBuff(world.Map, player);

            EffectCarry.Stash(player);
            GameEffectManager.Instance.ClearEffects(world.Map, player);

            // Off again before the ride set them down: what was aside stays aside, with what is on now.
            var second = CarriedBuff(world.Map, player, typeId: BuffTypeId + 1);
            EffectCarry.Stash(player);

            CollectionAssert.AreEquivalent(new[] { first, second }, player.CarriedEffects.Select(c => c.Effect).ToArray());

            // The dead keep nothing.
            player.State = CharacterState.Dead;
            EffectCarry.Stash(player);
            Assert.IsEmpty(player.CarriedEffects);
        }

        [TestMethod]
        public void AnArmedSelfDestructStaysOnTheMapItWasArmedOn()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient(x: 120, z: -40).Player;

            // As ArmSelfDestruct makes it: a buff with a duration, no callbacks, its own damage
            // tick, and the place its holder is sent back to when it goes off.
            var bomb = new GameEffect
            {
                TypeId = 10000032,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                EffectLevel = 1,
                SourceId = player.EntityId,
                Source = player,
                IsBuff = true,
                AllowDetach = true,
                TickDamageMin = 100,
                TickDamageMax = 200,
                TickRadius = 10,
                ReturnTo = player.Position,
                ExpiresTick = Environment.TickCount64 + 30_000
            };

            GameEffectManager.Instance.Attach(world.Map, player, bomb);

            // Carried, it went off on the next map and put the player at this map's coordinates.
            Assert.IsFalse(EffectCarry.Carries(player, bomb));

            EffectCarry.Stash(player);
            Assert.IsEmpty(player.CarriedEffects);

            // The same effect with nowhere to return to is an ordinary buff of theirs.
            bomb.ReturnTo = null;
            Assert.IsTrue(EffectCarry.Carries(player, bomb));
        }

        /// <summary>RAGE: a buff of a minute, which a map change carries.</summary>
        private const int BuffTypeId = 74;

        private static GameEffect CarriedBuff(MapChannel map, Manifestation player, int typeId = BuffTypeId)
        {
            var buff = new GameEffect
            {
                TypeId = typeId,
                EffectId = GameEffectManager.Instance.NextEffectId(map),
                EffectLevel = 1,
                SourceId = player.EntityId,
                Source = player,
                IsBuff = true,
                ExpiresTick = Environment.TickCount64 + 60_000
            };

            GameEffectManager.Instance.Attach(map, player, buff);
            Assert.IsTrue(EffectCarry.Carries(player, buff));

            return buff;
        }

        private static List<GameEffectAttachedPacket> AttachesTo(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<GameEffectAttachedPacket>().ToList();

        [TestMethod]
        public void ASquadMateWhoArrivesByDropshipIsAnnouncedToTheSquadThere()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var lead = PlayerDeathTests.Player(world, 10, 0);
            var mate = PlayerDeathTests.Player(world, 0, 0);
            var third = world.CreateClient(400, 0);
            var destination = CreateDestination();

            // Already over there, by the pad.
            world.Map.ClientList.Remove(third);
            destination.ClientList.Add(third);
            third.Player.MapChannel = destination;
            third.Player.MapContextId = destination.MapInfo.MapContextId;
            CellManager.Instance.AddToWorld(third);

            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate, third);
            var cast = new SquadAfterZoningTests.Cast { [lead] = "lead", [mate] = "mate", [third] = "third" };
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);

            StartDropshipTransfer(world, destination, mate, manager);
            foreach (var client in new[] { lead, mate, third })
                WorldTestContext.Drain(client);

            Depart(world.Map, manager, mate);

            cast.Expect(lead, "destroy mate", "squad- mate");
            cast.Expect(mate, "destroy lead", "squad- lead");
            cast.Expect(third);

            RouteMapLoaded(mate);

            Assert.IsTrue(CellManager.Instance.IsInWorld(mate));
            cast.Expect(third, "create mate", "squad- mate", "squad+ mate");
            cast.Expect(mate, "create third", "squad- third", "squad+ third", "squad- lead", "squad+ lead");
            cast.Expect(lead, "squad- mate", "squad+ mate");    // on the map they left, the id alone

            WorldTestContext.Drain(third);
            destination.ClientList.Clear();
            destination.MapCellInfo.Cells.Clear();
        }

        [TestMethod]
        public void StaleMapLoadedDoesNotPublishASecondDropshipArrival()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            RouteMapLoaded(client);
            var arrivals = manager.Dropships.Count;
            WorldTestContext.Drain(client);

            RouteMapLoaded(client);

            Assert.AreEqual(arrivals, manager.Dropships.Count);
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
            Assert.AreEqual(1, destination.ClientList.Count(member => member == client));
        }

        [TestMethod]
        public void MismatchedMapLoadedDoesNotPublishDropshipArrival()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            Assert.IsTrue(client.AwaitingMapLoaded);
            client.LoadingMap = world.Map.MapInfo.MapContextId;
            WorldTestContext.Drain(client);

            RouteMapLoaded(client);

            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.AreEqual(0, destination.ClientList.Count);
            Assert.IsFalse(CellManager.Instance.IsInWorld(client));
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
        }

        [TestMethod]
        public void DropshipPersistenceFailurePublishesNoDestinationArrival()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var observer = world.CreateClient();
            world.Map.ClientList.Remove(observer);
            observer.Player.MapChannel = destination;
            observer.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(observer);
            CellManager.Instance.AddToWorld(observer);
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps,
                (_, _, _) => throw new DbUpdateException("Fixture persistence failure."),
                current => current.State = ClientState.Disconnected);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            WorldTestContext.Drain(client);
            WorldTestContext.Drain(observer);

            RouteMapLoaded(client);

            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreSame(world.Map, client.Player.MapChannel);
            Assert.AreEqual(Vector3.Zero, client.Player.Position);
            Assert.IsFalse(destination.ClientList.Contains(client));
            Assert.IsFalse(destination.MapCellInfo.Cells.Values.Any(cell => cell.ClientList.Contains(client)));
            Assert.IsFalse(WorldTestContext.Drain(client).Any(packet =>
                packet.Message is CallMethodMessage call && call.Packet is TeleportArrivalPacket));
            Assert.AreEqual(0, WorldTestContext.Drain(observer).Count);
        }

        // BR-203: the ship that set the player down lingers five seconds after they are free to
        // go. It is not a flight: a destination picked from the pad in that time is taken.
        [TestMethod]
        public void ADestinationPickedWhileTheArrivalShipLiftsOffIsTaken()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            client.Player.GainedWaypoints.Add(
                new CharacterTeleporterEntry(client.Player.Id, 10, (byte)WaypointType.Dropship));
            Depart(world.Map, manager, client);
            RouteMapLoaded(client);

            var arrival = SetDown(destination, manager, client);
            Assert.IsTrue(manager.Dropships.ContainsKey(arrival.EntityId), "still lifting off");
            Assert.IsTrue(manager.CanBeginDropshipTravel(client, world.Map, Vector3.Zero, 0));
            WorldTestContext.Drain(client);

            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 10 });

            Assert.IsNotNull(client.PendingTransfer);
            Assert.IsTrue(client.PendingTransfer.IsDropship);
            Assert.AreEqual(world.Map.MapInfo.MapContextId, client.LoadingMap);
            var departure = manager.Dropships[client.PendingTransfer.DropshipId];
            Assert.AreEqual(DropshipRole.Departure, departure.Role);
            Assert.AreNotSame(arrival, departure);
        }

        [TestMethod]
        public void ADepartingShipStillCountsAsAnActiveFlight()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            CellManager.Instance.AddToWorld(client);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            Depart(world.Map, manager, client);
            RouteMapLoaded(client);
            SetDown(destination, manager, client);

            var departure = new Dropship(
                TargetCategory.Friendly,
                DropshipType.Teleporter,
                client,
                DropshipRole.Departure);
            CellManager.Instance.AddToWorld(destination, departure);
            manager.Dropships.Add(departure.EntityId, departure);

            Assert.IsNull(client.PendingTransfer);
            Assert.IsFalse(manager.CanBeginDropshipTravel(client, world.Map, Vector3.Zero, 0));
        }

        // BR-165: the ship setting a player down brings their client and their neighbours' in
        // line with the cells, which passed them over while they were Teleporting.
        [TestMethod]
        public void WhoCameNearWhileTheShipHeldThePlayerIsSeenBothWaysWhenItSetsThemDown()
        {
            using var world = new WorldTestContext();
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            var walker = PlayerDeathTests.Player(world, 600, 0);
            var client = world.CreateClient();
            client.State = ClientState.Teleporting;
            client.Player.MapContextId = world.Map.MapInfo.MapContextId;
            client.LoadingMap = world.Map.MapInfo.MapContextId;
            CellManager.Instance.AddToWorld(client);
            var arrival = new Dropship(TargetCategory.Friendly, DropshipType.Teleporter, client, DropshipRole.Arrival);
            CellManager.Instance.AddToWorld(world.Map, arrival);
            manager.Dropships.Add(arrival.EntityId, arrival);

            walker.Player.Position = new Vector3(4, 0, 0);
            CellManager.Instance.UpdateVisibility(walker);
            WorldTestContext.Drain(client);
            WorldTestContext.Drain(walker);
            Assert.IsNotNull(client.PlacedView);

            for (var step = 0; step < 6 && client.State != ClientState.Ingame; step++)
                manager.DropshipsWorker(world.Map, Math.Max(0, arrival.PhaseTimeleft));

            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNull(client.PlacedView);
            Assert.IsTrue(Made(client).Contains(walker.Player.EntityId));
            Assert.IsTrue(Made(walker).Contains(client.Player.EntityId));
        }

        private static List<ulong> Made(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>()
                .Select(m => m.Packet).OfType<Rasa.Packets.Game.Server.CreatePhysicalEntityPacket>().Select(p => p.EntityId).ToList();

        [TestMethod]
        public void AMapTickDoesNotAdvanceAnotherMapsDropship()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var ship = new Dropship(
                TargetCategory.Friendly,
                DropshipType.Teleporter,
                client,
                DropshipRole.Departure);
            var manager = new DynamicObjectManager(null);
            manager.Dropships.Add(ship.EntityId, ship);
            try
            {
                manager.DropshipsWorker(CreateDestination(), 1000);

                Assert.AreEqual(5000L, ship.PhaseTimeleft);
                Assert.AreEqual((byte)0, ship.Phase);
            }
            finally
            {
                manager.Dropships.Remove(ship.EntityId);
                EntityManager.Instance.FreeEntity(ship.EntityId);
            }
        }

        // Those who saw the traveller board see the flash where they were as the ship takes them
        // (PostTeleport, after the PreTeleport fade), before the body goes off the map. Nothing
        // sent it: the body vanished with no departure effect.
        [TestMethod]
        public void ThoseAroundSeeTheDepartureEffectBeforeTheBodyGoesOffTheMap()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            var watcher = world.CreateClient(5, 0);
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(watcher);
            var destination = CreateDestination();
            var maps = CreateMaps(world, destination);
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            StartDropshipTransfer(world, destination, client, manager);
            WorldTestContext.Drain(watcher);

            Depart(world.Map, manager, client);

            var seen = Departure(watcher, client.Player.EntityId);
            Assert.AreEqual(1, seen.Count(kind => kind == "pre"), "the fade as they board");
            Assert.AreEqual(1, seen.Count(kind => kind == "post"), "the flash as they go");
            Assert.IsTrue(seen.IndexOf("pre") < seen.IndexOf("post"));
            Assert.IsTrue(seen.IndexOf("post") < seen.IndexOf("destroy"), "where the body was");
            Assert.IsFalse(Departure(client, client.Player.EntityId).Contains("post"), "not to the traveller, on their way to a loading screen");
        }

        [TestMethod]
        public void ARideOnTheSameMapShowsTheDepartureEffectBeforeTheBodyMoves()
        {
            using var world = new WorldTestContext();
            world.AddClass(EntityClasses.UsableCrSpawnerHumDropshipV01);
            var client = world.CreateClient();
            var watcher = world.CreateClient(5, 0);
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(watcher);
            var maps = CreateMaps(world, CreateDestination());
            var manager = CreateDropshipManager(maps);
            using var managers = new ManagerInstances(maps, manager);
            WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero, type: WaypointType.Dropship);
            WaypointTravelTests.AddWaypoint(manager, world.Map, 20, new Vector3(30, 5, 0), type: WaypointType.Dropship);
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Dropship));
            manager.SelectWaypoint(client, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
            Assert.IsTrue(client.PendingTransfer?.IsDropship == true, "a ride, on the same map");
            WorldTestContext.Drain(watcher);

            Depart(world.Map, manager, client);

            var seen = Departure(watcher, client.Player.EntityId);
            Assert.AreEqual(1, seen.Count(kind => kind == "post"));
            Assert.IsTrue(seen.IndexOf("post") < seen.IndexOf("move"), "where they boarded, before the body moves");
            Assert.IsFalse(Departure(client, client.Player.EntityId).Contains("post"), "their own client plays it from the Teleport");
        }

        /// <summary>What a client was sent about the traveller, in order: their fade, the departure, a move of the body, its removal.</summary>
        internal static List<string> Departure(Rasa.Game.Client client, ulong traveller)
        {
            var seen = new List<string>();

            foreach (var message in WorldTestContext.Drain(client).Select(p => p.Message))
            {
                if (message is MoveObjectMessage move && move.EntityId == traveller)
                    seen.Add("move");
                else if (message is CallMethodMessage call)
                {
                    if (call.EntityId == traveller && call.Packet is PreTeleportPacket)
                        seen.Add("pre");
                    else if (call.EntityId == traveller && call.Packet is PostTeleportPacket)
                        seen.Add("post");
                    else if (call.Packet is DestroyPhysicalEntityPacket destroy && destroy.EntityId == traveller)
                        seen.Add("destroy");
                }
            }

            return seen;
        }

        internal static MapChannel CreateDestination()
        {
            return new MapChannel
            {
                MapInfo = new MapInfo(1300, "adv_foreas_fixture_destination", 1, 0),
                ClientList = new List<Rasa.Game.Client>(),
                PlayerLimit = 128
            };
        }

        private static MapChannelManager CreateMaps(WorldTestContext world, MapChannel destination,
            Action<Rasa.Game.Client, CharacterUpdate, object> update = null)
        {
            var maps = new MapChannelManager(null, () => 1000,
                updateCharacter: update ?? ((_, _, _) => { }),
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);
            return maps;
        }

        private static DynamicObjectManager CreateDropshipManager(MapChannelManager maps,
            Action<Rasa.Game.Client, CharacterUpdate, object> update = null,
            Action<Rasa.Game.Client> disconnect = null) =>
            new(null, maps, () => 1000, update ?? ((_, _, _) => { }),
                disconnect ?? (current => current.State = ClientState.Disconnected));

        private static void StartDropshipTransfer(WorldTestContext world, MapChannel destination,
            Rasa.Game.Client client, DynamicObjectManager manager)
        {
            WaypointTravelTests.AddWaypoint(manager, world.Map, 10, Vector3.Zero, type: WaypointType.Dropship);
            WaypointTravelTests.AddWaypoint(manager, destination, 20, new Vector3(400, 5, 0),
                type: WaypointType.Dropship);
            client.Player.GainedWaypoints.Add(
                new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Dropship));
            manager.SelectWaypoint(client,
                new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });
        }

        private static void Depart(MapChannel origin, DynamicObjectManager manager, Rasa.Game.Client client)
        {
            var ship = manager.Dropships[client.PendingTransfer.DropshipId];
            for (var step = 0; step < 6; step++)
                manager.DropshipsWorker(origin, Math.Max(0, ship.PhaseTimeleft));
        }

        private static void LandArrival(MapChannel destination, DynamicObjectManager manager)
        {
            var arrival = manager.Dropships.Values.Single();
            for (var step = 0; step < 6; step++)
                manager.DropshipsWorker(destination, Math.Max(0, arrival.PhaseTimeleft));
        }

        // Runs the arrival ship until it frees the player (phase 4), leaving it to lift off.
        private static Dropship SetDown(MapChannel destination, DynamicObjectManager manager,
            Rasa.Game.Client client)
        {
            var arrival = manager.Dropships.Values.Single();
            Assert.AreEqual(DropshipRole.Arrival, arrival.Role);
            for (var step = 0; step < 6 && client.State != ClientState.Ingame; step++)
                manager.DropshipsWorker(destination, Math.Max(0, arrival.PhaseTimeleft));
            Assert.AreEqual(ClientState.Ingame, client.State);
            return arrival;
        }

        private static void RouteMapLoaded(Rasa.Game.Client client)
        {
            var handler = new ClientPacketHandler();
            handler.RegisterClient(client);
            new PacketRouter<ClientPacketHandler, GameOpcode>().RoutePacket(handler, new MapLoadedPacket());
        }

        private sealed class ManagerInstances : IDisposable
        {
            private readonly FieldInfo _mapsField = typeof(MapChannelManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly FieldInfo _objectsField = typeof(DynamicObjectManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly object _previousMaps;
            private readonly object _previousObjects;
            private readonly MapChannelManager _maps;
            private readonly DynamicObjectManager _objects;

            internal ManagerInstances(MapChannelManager maps, DynamicObjectManager objects)
            {
                _maps = maps;
                _objects = objects;
                _previousMaps = _mapsField.GetValue(null);
                _previousObjects = _objectsField.GetValue(null);
                _mapsField.SetValue(null, maps);
                _objectsField.SetValue(null, objects);
            }

            public void Dispose()
            {
                foreach (var dropship in _objects.Dropships.Values.ToArray())
                {
                    if (_maps.MapChannelArray.TryGetValue(dropship.MapContextId, out var map))
                        CellManager.Instance.RemoveFromWorld(map, dropship);
                    else
                        EntityManager.Instance.FreeEntity(dropship.EntityId);
                    _objects.Dropships.Remove(dropship.EntityId);
                }
                _mapsField.SetValue(null, _previousMaps);
                _objectsField.SetValue(null, _previousObjects);
            }
        }
    }
}
