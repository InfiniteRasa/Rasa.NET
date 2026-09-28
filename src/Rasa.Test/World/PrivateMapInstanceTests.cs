extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class PrivateMapInstanceTests
    {
        [TestMethod]
        public void GetOrCreateKeepsPublicLookupCompatibleAndStaysStablePerOwner()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap();
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);

            var ownerOne = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var ownerOneAgain = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var ownerTwo = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 8);

            Assert.AreSame(ownerOne, ownerOneAgain);
            Assert.AreSame(publicMap, maps.FindByContextId(publicMap.MapInfo.MapContextId));
            Assert.AreEqual(1U, publicMap.InstanceId);
            Assert.AreNotSame(ownerOne, ownerTwo);
            Assert.IsTrue(ownerOne.IsPrivateInstance);
            Assert.AreEqual(7U, ownerOne.OwnerCharacterId);
            Assert.AreSame(ownerOne,
                maps.FindByContextAndInstance(publicMap.MapInfo.MapContextId, ownerOne.InstanceId));
            Assert.AreSame(ownerTwo,
                maps.FindOwnedPrivateInstance(publicMap.MapInfo.MapContextId, 8));
        }

        [TestMethod]
        public void MapChannelWorkerTicksEveryPrivateInstanceExactlyOnce()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap();
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);
            var first = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var second = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 8);

            maps.MapChannelWorker(100);

            Assert.AreEqual(100L, publicMap.MapChannelElapsed);
            Assert.AreEqual(100L, first.MapChannelElapsed);
            Assert.AreEqual(100L, second.MapChannelElapsed);
        }

        [TestMethod]
        public void FreshPrivateInstancesCloneRequiredStaticContentWithoutSharingMutableState()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap();
            publicMap.SpawnPools.Add(new SpawnPool
            {
                DbId = 55,
                MapContextId = publicMap.MapInfo.MapContextId,
                Position = new Vector3(10, 0, 10),
                Rotation = 1.5f,
                RespawnTime = 3000,
                UpdateTimer = 3000,
                SpawnSlot = new List<SpawnPoolSlot> { new(77, 1, 2) }
            });
            publicMap.ControlPoints.Add(1, new DynamicObject
            {
                MapContextId = publicMap.MapInfo.MapContextId,
                Position = new Vector3(20, 0, 20),
                Rotation = 2,
                DynamicObjectType = DynamicObjectType.ControlPoint,
                Faction = Factions.AFS,
                StateId = UseObjectState.CpointStateFactionAOwned,
                ObjectData = new ControlPointStatus(1, 1, 1, 30000)
            });
            publicMap.FootLockers.Add(2, new DynamicObject
            {
                MapContextId = publicMap.MapInfo.MapContextId,
                Position = new Vector3(30, 0, 30),
                Rotation = 3,
                DynamicObjectType = DynamicObjectType.Lockbox,
                Comment = "public-footlocker"
            });
            publicMap.Teleporters.Add(3, new DynamicObject
            {
                MapContextId = publicMap.MapInfo.MapContextId,
                Position = new Vector3(40, 0, 40),
                Rotation = 4,
                DynamicObjectType = DynamicObjectType.Waypoint,
                ObjectData = new WaypointInfo(3, false, WaypointType.Waypoint),
                Comment = "public-waypoint"
            });
            publicMap.DynamicObjects.Add(new DynamicObject
            {
                MapContextId = publicMap.MapInfo.MapContextId,
                Position = new Vector3(50, 0, 50),
                Rotation = 5,
                DynamicObjectType = DynamicObjectType.Logos,
                Comment = "public-dynamic"
            });
            publicMap.Kraftwerks.Add(4, new DynamicObject
            {
                MapContextId = publicMap.MapInfo.MapContextId,
                Position = new Vector3(60, 0, 60),
                Rotation = 6,
                DynamicObjectType = DynamicObjectType.Kraftwerks,
                Comment = "public-station"
            });
            var publicTrigger = new MapTrigger(5, "public-trigger", new Vector3(70, 0, 70), 7,
                publicMap.MapInfo.MapContextId);
            CellManager.Instance.AddToWorld(publicMap, publicTrigger);
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);

            var owned = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);

            Assert.AreEqual(publicMap.SpawnPools.Count, owned.SpawnPools.Count);
            Assert.AreEqual(publicMap.ControlPoints.Count, owned.ControlPoints.Count);
            Assert.AreEqual(publicMap.FootLockers.Count, owned.FootLockers.Count);
            Assert.AreEqual(publicMap.Teleporters.Count, owned.Teleporters.Count);
            Assert.AreEqual(publicMap.DynamicObjects.Count,
                owned.DynamicObjects.Count(obj => obj.DynamicObjectType != DynamicObjectType.PracticeDummy));
            Assert.AreEqual(0, owned.DynamicObjects.Count(obj => obj.DynamicObjectType == DynamicObjectType.PracticeDummy),
                "The generic private-map cloner must not manufacture tutorial content; the experience host owns it.");
            Assert.AreEqual(publicMap.Kraftwerks.Count, owned.Kraftwerks.Count);
            Assert.AreEqual(
                publicMap.MapCellInfo.Cells.Values.Sum(cell => cell.MapTriggers.Count),
                owned.MapCellInfo.Cells.Values.Sum(cell => cell.MapTriggers.Count));

            var publicPool = publicMap.SpawnPools.Single();
            var ownedPool = owned.SpawnPools.Single();
            Assert.AreNotSame(publicPool, ownedPool);
            Assert.AreNotSame(publicPool.SpawnSlot, ownedPool.SpawnSlot);
            Assert.AreEqual(publicPool.DbId, ownedPool.DbId);

            var publicControlPoint = publicMap.ControlPoints[1];
            var ownedControlPoint = owned.ControlPoints[1];
            Assert.AreNotSame(publicControlPoint, ownedControlPoint);
            Assert.AreNotEqual(publicControlPoint.EntityId, ownedControlPoint.EntityId);
            Assert.AreNotSame(publicControlPoint.ObjectData, ownedControlPoint.ObjectData);

            var publicTeleporter = publicMap.Teleporters[3];
            var ownedTeleporter = owned.Teleporters[3];
            Assert.AreNotSame(publicTeleporter, ownedTeleporter);
            Assert.AreNotEqual(publicTeleporter.EntityId, ownedTeleporter.EntityId);
            Assert.AreNotSame(publicTeleporter.ObjectData, ownedTeleporter.ObjectData);

            ownedPool.UpdateTimer = 0;
            ownedControlPoint.Faction = Factions.Bane;
            ownedTeleporter.Comment = "owned-waypoint";

            Assert.AreEqual(3000L, publicPool.UpdateTimer);
            Assert.AreEqual(Factions.AFS, publicControlPoint.Faction);
            Assert.AreEqual("public-waypoint", publicTeleporter.Comment);
            maps.ReleaseOwnedPrivateInstances(7);
        }

        [TestMethod]
        public void MapLoadedCommitsTransferToTheExactPrivateDestinationInstance()
        {
            using var world = new WorldTestContext();
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, updateCharacter: (_, _, _) => { },
                disconnect: _ => Assert.Fail("Transfer should not disconnect."),
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { },
                privateInstances: service);
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            var client = world.CreateClient();
            var destination = maps.GetOrCreatePrivateInstance(world.Map.MapInfo.MapContextId, client.Player.Id);
            var objects = new DynamicObjectManager(null, maps, updateCharacter: (_, _, _) => { },
                disconnect: current => current.State = ClientState.Disconnected);
            using var singletons = new ManagerInstances(objects);

            client.PendingTransfer = new PlayerTransfer
            {
                OriginMap = world.Map,
                OriginPosition = Vector3.Zero,
                OriginRotation = 0,
                DestinationMap = destination,
                DestinationPosition = new Vector3(250, 5, 0),
                DestinationRotation = 1,
                Deadline = long.MaxValue,
                IsDropship = true,
                HasDeparted = true
            };
            client.State = ClientState.Teleporting;
            client.Player.MapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            client.SetWorldPosition(new Vector3(250, 5, 0), 1);
            client.LoadingMap = destination.MapInfo.MapContextId;
            client.AwaitingMapLoaded = true;
            world.Map.ClientList.Remove(client);

            maps.MapLoaded(client);

            Assert.AreSame(destination, client.Player.MapChannel);
            Assert.IsTrue(destination.ClientList.Contains(client));
            Assert.IsFalse(world.Map.ClientList.Contains(client));
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(ClientState.Teleporting, client.State);
        }

        [TestMethod]
        public void CrossInstanceBroadcastsUseExactMapIdentity()
        {
            using var world = new WorldTestContext();
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            var first = maps.GetOrCreatePrivateInstance(world.Map.MapInfo.MapContextId, 7);
            var second = maps.GetOrCreatePrivateInstance(world.Map.MapInfo.MapContextId, 8);
            var firstClient = CreateClient(first, 7);
            var secondClient = CreateClient(second, 8);
            CellManager.Instance.AddToWorld(firstClient);
            CellManager.Instance.AddToWorld(secondClient);
            var creature = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = first.MapInfo.MapContextId,
                Position = Vector3.Zero,
                State = CharacterState.Idle,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 50, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(first, creature);
            WorldTestContext.Drain(firstClient);
            WorldTestContext.Drain(secondClient);

            ActorManager.Instance.Heal(creature, 10);

            Assert.IsTrue(WorldTestContext.Drain(firstClient).Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Any(message => message.Packet is UpdateHealthPacket));
            Assert.AreEqual(0, WorldTestContext.Drain(secondClient).Count);

            CellManager.Instance.RemoveCreatureFromWorld(first, creature);
            CleanupClient(firstClient);
            CleanupClient(secondClient);
        }

        [TestMethod]
        public void CrossInstanceUseAndRecoveryRequireExactPrivateMapIdentity()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap(1985);
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);
            var first = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var second = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 8);
            var firstClient = CreateClient(first, 7);
            var secondClient = CreateClient(second, 8);
            var controlPoint = new DynamicObject
            {
                MapContextId = second.MapInfo.MapContextId,
                Position = Vector3.Zero,
                DynamicObjectType = DynamicObjectType.ControlPoint,
                Faction = Factions.AFS,
                StateId = UseObjectState.CpointStateFactionAOwned
            };
            second.ControlPoints.Add(1, controlPoint);
            CellManager.Instance.AddToWorld(second, controlPoint);
            controlPoint.IsInWorld = true;
            var objects = new DynamicObjectManager(null, maps, updateCharacter: (_, _, _) => { },
                disconnect: _ => Assert.Fail("Cross-instance use should be ignored."));
            using var singletons = new ManagerInstances(objects);

            objects.RequestUseObjectPacket(firstClient, new RequestUseObjectPacket
            {
                ActionId = ActionId.UseObject,
                ActionArgId = DynamicObjectManager.ControlPointUseArgId,
                EntityId = controlPoint.EntityId
            });

            Assert.AreEqual(0, controlPoint.TriggeredByPlayers.Count);
            Assert.AreEqual(0, WorldTestContext.Drain(firstClient).Count);

            controlPoint.TriggeredByPlayers.Add(firstClient);
            objects.CaptureControlPointRecovery(second, new ActionData(
                firstClient.Player,
                ActionId.UseObject,
                DynamicObjectManager.ControlPointUseArgId,
                0));

            Assert.AreEqual(Factions.AFS, controlPoint.Faction);
            Assert.AreEqual(UseObjectState.CpointStateFactionAOwned, controlPoint.StateId);
            Assert.AreEqual(0, controlPoint.TriggeredByPlayers.Count);

            CellManager.Instance.RemoveFromWorld(second, controlPoint);
            CleanupClient(firstClient);
            CleanupClient(secondClient);
        }

        [TestMethod]
        public void CrossInstanceDamageLootAndDespawnCannotReachOtherPrivateMaps()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap(1985);
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);
            var first = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var second = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 8);
            var firstClient = CreateClient(first, 7);
            var target = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = second.MapInfo.MapContextId,
                Position = Vector3.Zero,
                State = CharacterState.Normal,
                Faction = Factions.Bane,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0),
                    [Attributes.Armor] = new(Attributes.Armor, 0, 0, 0, 0, 0)
                }
            };
            CellManager.Instance.AddToWorld(second, target);

            MissileManager.Instance.MissileLaunch(first, new ActionData(
                firstClient.Player,
                ActionId.WeaponAttack,
                133,
                target.EntityId,
                0), 55);

            Assert.AreEqual(0, first.QueuedMissiles.Count);
            Assert.AreEqual(100, target.Attributes[Attributes.Health].Current);

            var loot = new LootDispenser
            {
                Owner = firstClient.Player.EntityId,
                OwnerClient = firstClient,
                Player = firstClient.Player,
                Map = second,
                Corpse = target,
                CharacterId = firstClient.Player.Id,
                IsLootable = true
            };
            target.State = CharacterState.Dead;
            target.Attributes[Attributes.Health].Current = 0;
            target.CorpseLootEntityId = loot.EntityId;
            second.LootDispensers.Add(loot.EntityId, loot);
            var looting = new LootDispenserManager(null, _ => 20);

            looting.RequestCorpseLooting(firstClient,
                new RequestCorpseLootingPacket { EntityId = loot.EntityId });

            Assert.AreEqual(0UL, loot.CurrentLooter);
            Assert.AreEqual(0, WorldTestContext.Drain(firstClient).Count);

            Assert.IsFalse(CellManager.Instance.RemoveCreatureFromWorld(first, target));
            Assert.IsTrue(EntityManager.Instance.Creatures.ContainsKey(target.EntityId));
            Assert.AreSame(second, target.RuntimeMapChannel);

            second.LootDispensers.Remove(loot.EntityId);
            target.CorpseLootEntityId = 0;
            CellManager.Instance.RemoveCreatureFromWorld(second, target);
            CleanupClient(firstClient);
        }

        [TestMethod]
        public void WaypointTravelRequiresTheDepartureStationOnTheCurrentMapInstance()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap(1985);
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);
            var owned = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            var addedClass = false;
            if (!classes.ContainsKey(EntityClasses.HumanBaseMale))
            {
                classes.Add(EntityClasses.HumanBaseMale,
                    new EntityClass((uint)EntityClasses.HumanBaseMale, "fixture", 0, 0,
                        new List<AugmentationType>(), true));
                addedClass = true;
            }
            var client = CreateClient(owned, 7);
            try
            {
                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);
                var objects = new DynamicObjectManager(null, maps, updateCharacter: (_, _, _) => { },
                    disconnect: current => current.State = ClientState.Disconnected);

                publicMap.Teleporters.Add(10, new DynamicObject
                {
                    MapContextId = publicMap.MapInfo.MapContextId,
                    Position = Vector3.Zero,
                    ObjectData = new WaypointInfo(10, false, WaypointType.Waypoint)
                });
                publicMap.Teleporters.Add(20, new DynamicObject
                {
                    MapContextId = publicMap.MapInfo.MapContextId,
                    Position = new Vector3(200, 0, 0),
                    ObjectData = new WaypointInfo(20, false, WaypointType.Waypoint)
                });
                objects.Teleporters.Add(10, publicMap.Teleporters[10]);
                objects.Teleporters.Add(20, publicMap.Teleporters[20]);
                client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(client.Player.Id, 20, (byte)WaypointType.Waypoint));

                objects.SelectWaypoint(client, new SelectWaypointPacket
                {
                    MapInstanceId = owned.InstanceId,
                    WaypointId = 20
                });

                Assert.IsNull(client.PendingTransfer);
                Assert.AreEqual(ClientState.Ingame, client.State);
                Assert.IsTrue(WorldTestContext.Drain(client).Any(packet =>
                    packet.Message is CallMethodMessage message &&
                    message.Packet is TeleportFailedPacket));
            }
            finally
            {
                CleanupClient(client);
                if (addedClass)
                    classes.Remove(EntityClasses.HumanBaseMale);
            }
        }

        [TestMethod]
        public void ReleasingOwnedInstanceUnregistersOwnedEntitiesQueuesAndMissilesAndReconnectCreatesANewRuntime()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap(1985);
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);
            var owned = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var loadingClient = CreateClient(owned, 7);
            loadingClient.State = ClientState.Loading;
            owned.ClientList.Add(loadingClient);
            owned.QueuedClients.Enqueue(loadingClient);
            owned.PerformRecovery.Add(new ActionData(loadingClient.Player, ActionId.UseObject, 0, 0));
            owned.QueuedMissiles.Add(new Missile());
            var creature = new Creature
            {
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = owned.MapInfo.MapContextId,
                Position = Vector3.Zero,
                State = CharacterState.Idle,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = new Dictionary<Attributes, ActorAttributes>
                {
                    [Attributes.Health] = new(Attributes.Health, 100, 100, 100, 0, 0)
                }
            };
            var dynamicObject = new DynamicObject
            {
                MapContextId = owned.MapInfo.MapContextId,
                Position = Vector3.Zero
            };
            CellManager.Instance.AddToWorld(owned, creature);
            CellManager.Instance.AddToWorld(owned, dynamicObject);

            maps.ReleaseOwnedPrivateInstances(7);

            Assert.IsNull(maps.FindOwnedPrivateInstance(publicMap.MapInfo.MapContextId, 7));
            Assert.IsNull(maps.FindByContextAndInstance(publicMap.MapInfo.MapContextId, owned.InstanceId));
            Assert.AreEqual(0, owned.ClientList.Count);
            Assert.AreEqual(0, owned.QueuedClients.Count);
            Assert.AreEqual(0, owned.PerformRecovery.Count);
            Assert.AreEqual(0, owned.QueuedMissiles.Count);
            Assert.AreEqual(0, owned.MapCellInfo.Cells.Count);
            Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(creature.EntityId));
            Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(creature.EntityId));
            Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(dynamicObject.EntityId));
            Assert.IsFalse(EntityManager.Instance.DynamicObjects.ContainsKey(dynamicObject.EntityId));

            var recreated = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);

            Assert.AreNotSame(owned, recreated);
            Assert.AreEqual(publicMap.MapInfo.MapContextId, recreated.MapInfo.MapContextId);
            Assert.AreEqual(7U, recreated.OwnerCharacterId);
            CleanupClient(loadingClient);
        }

        [TestMethod]
        public void SameContextDropshipsTickOnlyOnTheirExactRuntimeMapChannel()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap(1985);
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);
            var first = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var second = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 8);
            var objects = new DynamicObjectManager(null, maps);
            using var singletons = new ManagerInstances(objects);
            var publicDropship = AddDropship(objects, publicMap, 5001);
            var firstDropship = AddDropship(objects, first, 5002);
            var secondDropship = AddDropship(objects, second, 5003);
            try
            {
                maps.MapChannelWorker(1000);

                Assert.AreEqual(4000L, publicDropship.PhaseTimeleft);
                Assert.AreEqual(4000L, firstDropship.PhaseTimeleft);
                Assert.AreEqual(4000L, secondDropship.PhaseTimeleft);
                Assert.AreEqual((byte)0, publicDropship.Phase);
                Assert.AreEqual((byte)0, firstDropship.Phase);
                Assert.AreEqual((byte)0, secondDropship.Phase);
            }
            finally
            {
                CleanupDropships(objects);
                maps.ReleaseOwnedPrivateInstances(7);
                maps.ReleaseOwnedPrivateInstances(8);
            }
        }

        [TestMethod]
        public void ReleasingOwnedPrivateInstanceRemovesOnlyThatInstancesDropshipsFromEntitiesAndWorkers()
        {
            var service = new PrivateMapInstanceService();
            var maps = new MapChannelManager(null, privateInstances: service);
            var publicMap = CreateMap(1985);
            maps.MapChannelArray.Add(publicMap.MapInfo.MapContextId, publicMap);
            var first = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 7);
            var second = maps.GetOrCreatePrivateInstance(publicMap.MapInfo.MapContextId, 8);
            var objects = new DynamicObjectManager(null, maps);
            using var singletons = new ManagerInstances(objects);
            var publicDropship = AddDropship(objects, publicMap, 6001);
            var firstDropship = AddDropship(objects, first, 6002);
            var secondDropship = AddDropship(objects, second, 6003);
            try
            {
                maps.ReleaseOwnedPrivateInstances(7);

                Assert.IsFalse(objects.Dropships.ContainsKey(firstDropship.EntityId));
                Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(firstDropship.EntityId));
                Assert.IsFalse(EntityManager.Instance.DynamicObjects.ContainsKey(firstDropship.EntityId));
                Assert.IsTrue(objects.Dropships.ContainsKey(publicDropship.EntityId));
                Assert.IsTrue(objects.Dropships.ContainsKey(secondDropship.EntityId));
                Assert.IsTrue(EntityManager.Instance.RegisteredEntities.ContainsKey(publicDropship.EntityId));
                Assert.IsTrue(EntityManager.Instance.RegisteredEntities.ContainsKey(secondDropship.EntityId));

                maps.MapChannelWorker(1000);

                Assert.AreEqual(4000L, publicDropship.PhaseTimeleft);
                Assert.AreEqual(5000L, firstDropship.PhaseTimeleft);
                Assert.AreEqual(4000L, secondDropship.PhaseTimeleft);

                maps.ReleaseOwnedPrivateInstances(8);

                Assert.IsFalse(objects.Dropships.ContainsKey(secondDropship.EntityId));
                Assert.IsFalse(EntityManager.Instance.RegisteredEntities.ContainsKey(secondDropship.EntityId));
                Assert.IsFalse(EntityManager.Instance.DynamicObjects.ContainsKey(secondDropship.EntityId));
                Assert.IsTrue(objects.Dropships.ContainsKey(publicDropship.EntityId));
                Assert.IsTrue(EntityManager.Instance.RegisteredEntities.ContainsKey(publicDropship.EntityId));
            }
            finally
            {
                CleanupDropships(objects);
            }
        }

        private static MapChannel CreateMap(uint contextId = 1985) => new()
        {
            MapInfo = new MapInfo(contextId, "fixture", 1556, 0),
            ClientList = new List<Client>(),
            PlayerLimit = 128
        };

        private static Dropship AddDropship(DynamicObjectManager objects, MapChannel map, uint spawnPoolId)
        {
            var dropship = new Dropship(Factions.AFS, DropshipType.Spawner, new SpawnPool
            {
                DbId = spawnPoolId,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Position = Vector3.Zero,
                SpawnSlot = new List<SpawnPoolSlot>()
            });
            CellManager.Instance.AddToWorld(map, dropship);
            objects.Dropships.Add(dropship.EntityId, dropship);
            return dropship;
        }

        private static Client CreateClient(MapChannel map, uint characterId)
        {
            var client = new Client(null, new ClientPacketHandler())
            {
                State = ClientState.Ingame
            };
            client.Player.Id = characterId;
            client.Player.EntityId = EntityManager.Instance.AllocateUnrecycledEntityId();
            client.Player.Name = $"Player{characterId}";
            client.Player.FamilyName = "Fixture";
            client.Player.Level = 1;
            client.Player.EntityClass = EntityClasses.HumanBaseMale;
            client.Player.AppearanceData = new Dictionary<EquipmentData, AppearanceData>();
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 17).ToList();
            client.Player.MapChannel = map;
            client.Player.MapContextId = map.MapInfo.MapContextId;
            client.Player.Position = Vector3.Zero;
            EntityManager.Instance.RegisterEntity(client.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(client.Player.EntityId, client.Player);
            EntityManager.Instance.RegisterActor(client.Player.EntityId, client.Player);
            return client;
        }

        private static void CleanupClient(Client client)
        {
            if (EntityManager.Instance.RegisteredEntities.ContainsKey(client.Player.EntityId))
                EntityManager.Instance.UnregisterEntity(client.Player.EntityId);
            if (EntityManager.Instance.Players.ContainsKey(client.Player.EntityId))
                EntityManager.Instance.UnregisterPlayer(client.Player.EntityId);
            if (EntityManager.Instance.Actors.ContainsKey(client.Player.EntityId))
                EntityManager.Instance.UnregisterActor(client.Player.EntityId);
            EntityManager.Instance.FreeEntity(client.Player.EntityId);
        }

        private static void CleanupDropships(DynamicObjectManager objects)
        {
            foreach (var dropship in objects.Dropships.Values.ToArray())
            {
                if (dropship.RuntimeMapChannel != null &&
                    EntityManager.Instance.RegisteredEntities.ContainsKey(dropship.EntityId))
                {
                    CellManager.Instance.RemoveFromWorld(dropship.RuntimeMapChannel, dropship);
                }

                objects.Dropships.Remove(dropship.EntityId);
            }
        }

        private sealed class ManagerInstances : System.IDisposable
        {
            private readonly FieldInfo _objectsField = typeof(DynamicObjectManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly object _previousObjects;

            internal ManagerInstances(DynamicObjectManager objects)
            {
                _previousObjects = _objectsField.GetValue(null);
                _objectsField.SetValue(null, objects);
            }

            public void Dispose()
            {
                _objectsField.SetValue(null, _previousObjects);
            }
        }
    }
}
