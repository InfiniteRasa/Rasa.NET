using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// The usables the client's map files place are sent the state the client draws them in as a
    /// player arrives on their map (MapUsables): the Forean fire pits, lit. A player puts one out
    /// by using it, and lights it again the same way.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MapUsableTests
    {
        private static MapChannel Channel(uint mapContextId, string name) => new MapChannel
        {
            MapInfo = new MapInfo(mapContextId, name, 1556, 0),
            ClientList = new List<Rasa.Game.Client>(),
            PlayerLimit = 128
        };

        private static List<(ulong EntityId, ForceStatePacket Packet)> States(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.Packet is ForceStatePacket)
                .Select(message => (message.EntityId, (ForceStatePacket)message.Packet)).ToList();

        private static Rasa.Game.Client On(WorldTestContext world, MapChannel map)
        {
            var client = world.CreateClient();
            world.Map.ClientList.Remove(client);
            client.Player.MapChannel = map;
            client.Player.MapContextId = map.MapInfo.MapContextId;
            map.ClientList.Add(client);
            WorldTestContext.Drain(client);
            return client;
        }

        [TestMethod]
        public void EachFirePitIsLitForWhoeverArrivesOnItsMap()
        {
            using var world = new WorldTestContext();

            foreach (var (map, name, entityId) in new[]
            {
                (MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide", 132770324522631UL),
                (MapUsables.ValverdePlateau, "adv_foreas_valverde_plateau", 134269267723598UL),
                (MapUsables.ConcordiaPalisades, "adv_foreas_concordia_palisades", 133182640922310UL),
            })
            {
                var client = On(world, Channel(map, name));

                Assert.AreEqual(MapUsables.OnMap(map).Count(), MapUsables.PlayerEnteredMap(client), name);

                var sent = States(client).Single(state => MapUsables.Find(state.EntityId).ClassId is MapUsables.ForeanFirePitV01 or MapUsables.ForeanFirePitV03);
                Assert.AreEqual(entityId, sent.EntityId, $"{name}: the .map's own id for its fire pit");
                Assert.AreEqual(UseObjectState.TsState0, sent.Packet.State, $"{name}: USE_TS_STATE_0, whose effect is the fire");
                Assert.AreEqual(0, sent.Packet.WindupTimeMs);
            }
        }

        [TestMethod]
        public void AMapWithoutOneIsSentNothingAndACopyOfOneIsSentTheSame()
        {
            using var world = new WorldTestContext();
            var wilderness = On(world, world.Map);

            Assert.AreEqual(0, MapUsables.PlayerEnteredMap(wilderness));
            Assert.IsEmpty(States(wilderness));

            var copy = Channel(MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide");
            copy.IsPrivateInstance = true;
            var visitor = On(world, copy);

            MapUsables.PlayerEnteredMap(visitor);
            CollectionAssert.AreEquivalent(MapUsables.OnMap(MapUsables.ConcordiaDivide).Select(usable => usable.EntityId).ToList(),
                States(visitor).Select(state => state.EntityId).ToList(), "a copy loads the same .map");

            Assert.AreEqual(0, MapUsables.PlayerEnteredMap(null));
        }

        [TestMethod]
        public void TheFirePitsAreTheTwoStateClassesWhoseFirstStateIsLit()
        {
            var pits = MapUsables.All.Where(usable => usable.ClassId == MapUsables.ForeanFirePitV01 || usable.ClassId == MapUsables.ForeanFirePitV03).ToList();
            Assert.HasCount(3, pits);

            foreach (var usable in pits)
                Assert.AreEqual((UseObjectState.TsState0, (UseObjectState?)UseObjectState.TsState1), (usable.State, usable.UsedState), usable.ToString());
        }

        [TestMethod]
        public void EveryOneHasItsOwnClientIdOnItsMap()
        {
            Assert.AreEqual(MapUsables.All.Count, MapUsables.All.Select(usable => usable.EntityId).Distinct().Count());

            // The client makes a .map entity only above 32 bits (gamemap.py _LoadEntities3).
            foreach (var usable in MapUsables.All)
                Assert.IsGreaterThan(uint.MaxValue, usable.EntityId, usable.ToString());
        }

        [TestMethod]
        public void TheDissectionTablesAndStasisChambersAreSentIntactAndAreNotUsed()
        {
            var tables = MapUsables.All.Where(usable => usable.ClassId == MapUsables.BrannDissectionTable).ToList();
            var chambers = MapUsables.All.Where(usable => usable.ClassId == MapUsables.BaneStasisChamber).ToList();

            Assert.HasCount(19, tables);
            Assert.HasCount(2, chambers);
            Assert.HasCount(18, tables.Where(table => table.MapContextId == MapUsables.PenalResearch).ToList());
            Assert.HasCount(1, tables.Where(table => table.MapContextId == MapUsables.StaalJunkyard).ToList());
            CollectionAssert.AreEquivalent(new[] { MapUsables.PravusResearch, MapUsables.TestWeaponsCenter }, chambers.Select(chamber => chamber.MapContextId).ToArray());

            foreach (var usable in tables.Concat(chambers))
            {
                Assert.AreEqual(UseObjectState.IdesStateIntact, usable.State, usable.ToString());
                Assert.IsNull(usable.UsedState, usable.ToString());
            }

            using var world = new WorldTestContext();

            // Penal Research: all eighteen of its tables, intact, by their .map ids.
            var lab = On(world, Channel(MapUsables.PenalResearch, "adv_arieki_torden_plains_penalresearch"));
            Assert.AreEqual(18, MapUsables.PlayerEnteredMap(lab));
            var sent = States(lab);
            CollectionAssert.AreEquivalent(tables.Where(table => table.MapContextId == MapUsables.PenalResearch).Select(table => table.EntityId).ToList(),
                sent.Select(state => state.EntityId).ToList());
            Assert.IsTrue(sent.All(state => state.Packet.State == UseObjectState.IdesStateIntact));

            // The Pravus Research chamber, and a use of it refused as the client would put it.
            var pravus = On(world, Channel(MapUsables.PravusResearch, "adv_foreas_concordia_wilderness_pravusresearch"));
            // Its six Bane teleporters go with it.
            Assert.AreEqual(7, MapUsables.PlayerEnteredMap(pravus));
            Assert.AreEqual(UseObjectState.IdesStateIntact, States(pravus).Single(state => state.EntityId == 133981504835290UL).Packet.State);
            Assert.AreEqual(UseObjectState.IdesStateIntact, MapUsables.StateOf(pravus.Player.MapChannel, chambers[0]));

            pravus.Player.Position = new Vector3(224, 7.6f, 42);
            Assert.IsTrue(MapUsables.TryRequestUse(pravus, Use(133981504835290UL)));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, Refusal(pravus));
            Assert.IsEmpty(pravus.Player.MapChannel.PerformRecovery);
        }

        [TestMethod]
        public void TheBrannMonitorsAreSentOnAndAreNotUsed()
        {
            var monitors = MapUsables.All.Where(usable => usable.ClassId == MapUsables.BrannMonitorV01 || usable.ClassId == MapUsables.BrannMonitorV02).ToList();

            Assert.HasCount(68, monitors);
            Assert.HasCount(31, monitors.Where(monitor => monitor.MapContextId == MapUsables.CommTower && monitor.ClassId == MapUsables.BrannMonitorV01).ToList());
            Assert.HasCount(33, monitors.Where(monitor => monitor.MapContextId == MapUsables.CommTower && monitor.ClassId == MapUsables.BrannMonitorV02).ToList());
            Assert.HasCount(4, monitors.Where(monitor => monitor.MapContextId == MapUsables.BurningSteps && monitor.ClassId == MapUsables.BrannMonitorV01).ToList());

            foreach (var monitor in monitors)
            {
                Assert.AreEqual(UseObjectState.TsState1, monitor.State, $"{monitor}: USE_TS_STATE_1, whose animation is the screen");
                Assert.IsNull(monitor.UsedState, monitor.ToString());
            }

            using var world = new WorldTestContext();

            // Burning Steps: its four, on (with its two Brann teleporters).
            var steps = On(world, Channel(MapUsables.BurningSteps, "adv_arieki_ligo_burningsteps"));
            Assert.AreEqual(MapUsables.OnMap(MapUsables.BurningSteps).Count(), MapUsables.PlayerEnteredMap(steps));
            var sent = States(steps).Where(state => MapUsables.Find(state.EntityId).ClassId == MapUsables.BrannMonitorV01).ToList();
            CollectionAssert.AreEquivalent(monitors.Where(monitor => monitor.MapContextId == MapUsables.BurningSteps).Select(monitor => monitor.EntityId).ToList(),
                sent.Select(state => state.EntityId).ToList());
            Assert.IsTrue(sent.All(state => state.Packet.State == UseObjectState.TsState1));

            // The Comm Tower: all sixty-four.
            var tower = On(world, Channel(MapUsables.CommTower, "adv_arieki_torden_incline_commtower"));
            Assert.AreEqual(64, MapUsables.PlayerEnteredMap(tower));

            // A use of one, from a client that made it up, is refused.
            var first = monitors.First(monitor => monitor.MapContextId == MapUsables.CommTower);
            tower.Player.Position = first.Position;
            WorldTestContext.Drain(tower);
            Assert.IsTrue(MapUsables.TryRequestUse(tower, Use(first.EntityId)));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, Refusal(tower));
            Assert.IsEmpty(tower.Player.MapChannel.PerformRecovery);
        }

        [TestMethod]
        public void TheArticulatedDrillsAreSentIntactAndTheSonicTowersAreLeftAlone()
        {
            var drills = MapUsables.All.Where(usable => usable.ClassId == MapUsables.BaneArticulatedDrill).ToList();

            Assert.HasCount(3, drills);
            CollectionAssert.AreEquivalent(new[] { MapUsables.ConcordiaDivide, MapUsables.TreebackCamp, MapUsables.Thunderhead },
                drills.Select(drill => drill.MapContextId).ToArray());

            foreach (var drill in drills)
            {
                Assert.AreEqual(UseObjectState.IdesStateIntact, drill.State, drill.ToString());
                Assert.IsNull(drill.UsedState, drill.ToString());
            }

            // Nothing of UsableInertDestBaneSonicTowerV02 (21964): its intact states have nothing to show.
            Assert.IsFalse(MapUsables.All.Any(usable => usable.ClassId == 21964));

            using var world = new WorldTestContext();

            // Concordia Divide: its fire pit and its drill (and its tesla coils).
            var divide = On(world, Divide());
            Assert.AreEqual(MapUsables.OnMap(MapUsables.ConcordiaDivide).Count(), MapUsables.PlayerEnteredMap(divide));
            var sent = States(divide).ToDictionary(state => state.EntityId, state => state.Packet.State);
            Assert.AreEqual(UseObjectState.IdesStateIntact, sent[132770324750504UL]);
            Assert.AreEqual(UseObjectState.TsState0, sent[ThoriaDas]);

            // Used by a client that made it up: refused.
            divide.Player.Position = drills.Single(drill => drill.MapContextId == MapUsables.ConcordiaDivide).Position;
            Assert.IsTrue(MapUsables.TryRequestUse(divide, Use(132770324750504UL)));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, Refusal(divide));
        }

        [TestMethod]
        public void TheBrannTeleportersWormholeAndAlarmLightsAndTheMiningCoreDrillAreSentRunning()
        {
            var running = new[]
            {
                (Class: MapUsables.BrannTeleporter, Map: MapUsables.BurningSteps, Count: 2, State: UseObjectState.TsState1),
                (Class: MapUsables.BrannWormhole, Map: MapUsables.TahrendraBase, Count: 1, State: UseObjectState.TsState1),
                (Class: MapUsables.BaneMiningCoreDrill, Map: MapUsables.TheRefuge, Count: 1, State: UseObjectState.TsState0),
                (Class: MapUsables.BrannAlarmLight, Map: MapUsables.BrannWaterRefinery, Count: 10, State: UseObjectState.TsState1),
            };

            using var world = new WorldTestContext();

            foreach (var (classId, map, count, state) in running)
            {
                var usables = MapUsables.All.Where(usable => usable.ClassId == classId).ToList();
                Assert.HasCount(count, usables, classId.ToString());

                foreach (var usable in usables)
                {
                    Assert.AreEqual(map, usable.MapContextId, usable.ToString());
                    Assert.AreEqual(state, usable.State, $"{usable}: the state with its running animation and effect");
                    Assert.IsNull(usable.UsedState, usable.ToString());
                }

                var client = On(world, Channel(map, "fixture"));
                MapUsables.PlayerEnteredMap(client);
                var sent = States(client).Where(sentState => usables.Any(usable => usable.EntityId == sentState.EntityId)).ToList();
                Assert.HasCount(count, sent);
                Assert.IsTrue(sent.All(sentState => sentState.Packet.State == state));
            }
        }

        [TestMethod]
        public void ArrivingOnTheMapSendsThem()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var client = harness.Client;
            var map = client.Player.MapChannel;
            var bootcamp = map.MapInfo;

            try
            {
                WorldTestContext.Drain(client);
                ManifestationManager.Instance.AssignPlayer(client);
                Assert.IsEmpty(States(client), "Bootcamp has none");

                map.MapInfo = new MapInfo(MapUsables.ConcordiaPalisades, "adv_foreas_concordia_palisades", bootcamp.MapVersion, bootcamp.BaseRegionId);
                ManifestationManager.Instance.AssignPlayer(client);

                var sent = States(client).Single();
                Assert.AreEqual((133182640922310UL, UseObjectState.TsState0), (sent.EntityId, sent.Packet.State));
            }
            finally
            {
                map.MapInfo = bootcamp;
            }
        }

        private const ulong ThoriaDas = 132770324522631UL;

        private static MapUsables.Usable Pit => MapUsables.Find(ThoriaDas);

        private static MapChannel Divide() => Channel(MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide");

        /// <summary>A player on the map, standing <paramref name="metres"/> from the Thoria Das pit.</summary>
        private static Rasa.Game.Client Beside(WorldTestContext world, MapChannel map, float metres = 2f)
        {
            var client = On(world, map);
            client.Player.Position = Pit.Position + new Vector3(metres, 0, 0);
            return client;
        }

        private static RequestUseObjectPacket Use(ulong entityId = ThoriaDas) =>
            new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 1, EntityId = entityId };

        private static List<(ulong EntityId, UsePacket Packet)> Uses(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.Packet is UsePacket)
                .Select(message => (message.EntityId, (UsePacket)message.Packet)).ToList();

        private static List<CallMethodMessage> Sent(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();

        /// <summary>Runs the map's queued actions past their windups.</summary>
        private static void Recover(MapChannel map) => ActorActionManager.Instance.DoWork(map, MapUsables.UseWindupMs);

        [TestMethod]
        public void UsingALitPitPutsItOutForEveryoneOnTheMapAndUsingItAgainLightsIt()
        {
            using var world = new WorldTestContext();
            var map = Divide();
            var user = Beside(world, map);
            var faraway = Beside(world, map, 300f);
            var copy = Divide();
            copy.IsPrivateInstance = true;
            var elsewhere = Beside(world, copy);

            Assert.IsTrue(MapUsables.TryRequestUse(user, Use()));

            var windup = Sent(user).Select(sent => sent.Packet).OfType<PerformWindupPacket>().Single();
            Assert.AreEqual((ActionId.UseObject, 1u), (windup.ActionId, windup.ActionArgId));
            Assert.IsEmpty(Uses(faraway), "nothing until the use is over");

            Recover(map);

            Assert.AreEqual(UseObjectState.TsState1, MapUsables.StateOf(map, Pit), "out");
            foreach (var client in new[] { user, faraway })
            {
                var use = Uses(client).Single();
                Assert.AreEqual(ThoriaDas, use.EntityId);
                Assert.AreEqual((user.Player.EntityId, UseObjectState.TsState1), (use.Packet.PlayerEntityId, use.Packet.CurState), "everyone on the map has the pit");
            }

            Assert.IsEmpty(Uses(elsewhere), "a copy of the map has its own");
            Assert.AreEqual(UseObjectState.TsState0, MapUsables.StateOf(copy, Pit));

            // Whoever arrives now is shown it out.
            var later = On(world, map);
            MapUsables.PlayerEnteredMap(later);
            Assert.AreEqual(UseObjectState.TsState1, States(later).Single(state => state.EntityId == ThoriaDas).Packet.State);

            // And lit again.
            Assert.IsTrue(MapUsables.TryRequestUse(user, Use()));
            Recover(map);

            Assert.AreEqual(UseObjectState.TsState0, MapUsables.StateOf(map, Pit), "lit");
            Assert.AreEqual(UseObjectState.TsState0, Uses(later).Single().Packet.CurState);
        }

        [TestMethod]
        public void AUseFromTooFarAwayOrOfAnotherMapsPitOrWhileItIsInUseIsRefused()
        {
            using var world = new WorldTestContext();
            var map = Divide();

            var far = Beside(world, map, DynamicObjectManager.MaxUseDistance + 1f);
            Assert.IsTrue(MapUsables.TryRequestUse(far, Use()));
            Assert.AreEqual(PlayerMessage.PmTargetOutOfRange, Refusal(far));

            // The Palisades pit, from Concordia Divide: no client of this map has it.
            var near = Beside(world, map);
            Assert.IsTrue(MapUsables.TryRequestUse(near, Use(133182640922310UL)));
            Assert.IsEmpty(Sent(near));
            Assert.IsEmpty(map.PerformRecovery);

            // One use of the pit at a time.
            var second = Beside(world, map);
            Assert.IsTrue(MapUsables.TryRequestUse(near, Use()));
            WorldTestContext.Drain(near);
            Assert.IsTrue(MapUsables.TryRequestUse(second, Use()));
            Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, Refusal(second));
            Assert.HasCount(1, map.PerformRecovery);

            // Anything else is not this one's to answer.
            Assert.IsFalse(MapUsables.TryRequestUse(near, Use(12345)));
        }

        [TestMethod]
        public void AnInterruptedUseOrOneWhoseUserLeftChangesNothing()
        {
            using var world = new WorldTestContext();
            var map = Divide();
            var user = Beside(world, map);

            MapUsables.TryRequestUse(user, Use());
            map.PerformRecovery.Single().IsInrerrupted = true;
            Recover(map);

            Assert.AreEqual(UseObjectState.TsState0, MapUsables.StateOf(map, Pit));
            Assert.IsEmpty(Uses(user));

            MapUsables.TryRequestUse(user, Use());
            var elsewhere = Divide();
            user.Player.MapChannel = elsewhere;
            Recover(map);

            Assert.AreEqual(UseObjectState.TsState0, MapUsables.StateOf(map, Pit));
            Assert.IsEmpty(Uses(user));
        }

        [TestMethod]
        public void TheServersUseRequestHandsAPitToMapUsables()
        {
            using var world = new WorldTestContext();
            var map = Divide();
            var user = Beside(world, map);

            DynamicObjectManager.Instance.RequestUseObjectPacket(user, Use());
            Assert.HasCount(1, map.PerformRecovery);
            Assert.AreEqual(ThoriaDas, map.PerformRecovery.Single().SourceId);

            Recover(map);
            Assert.AreEqual(UseObjectState.TsState1, MapUsables.StateOf(map, Pit));
        }

        private static PlayerMessage? Refusal(Rasa.Game.Client client) =>
            Sent(client).Select(sent => sent.Packet).OfType<UserActionFailedPacket>().Single().MsgId;

        [TestMethod]
        public void TheLastTwoStateSwitchesAreSentTheirStates()
        {
            var expected = new[]
            {
                (Class: MapUsables.BaneTeleporter, Map: MapUsables.MinosCaverns, Count: 11, State: UseObjectState.TsState1),
                (Class: MapUsables.BaneTeleporter, Map: MapUsables.DevilsDen, Count: 14, State: UseObjectState.TsState1),
                (Class: MapUsables.BaneTeleporter, Map: MapUsables.PravusResearch, Count: 6, State: UseObjectState.TsState1),
                (Class: MapUsables.BaneTeleporter, Map: MapUsables.Thunderhead, Count: 1, State: UseObjectState.TsState0),
                (Class: MapUsables.BaneTeleporter, Map: MapUsables.Incline, Count: 1, State: UseObjectState.TsState0),
                (Class: MapUsables.HumanTeleporter, Map: MapUsables.Thunderhead, Count: 4, State: UseObjectState.TsState1),
                (Class: MapUsables.ElohTemplesSpawner, Map: MapUsables.ElohTemples, Count: 19, State: UseObjectState.TsState1),
                (Class: MapUsables.LogosStoneAnalyzer, Map: MapUsables.MinosCaverns, Count: 1, State: UseObjectState.TsState1),
                (Class: MapUsables.LogosStoneAnalyzer, Map: MapUsables.DevilsDen, Count: 1, State: UseObjectState.TsState1),
            };

            using var world = new WorldTestContext();

            foreach (var (classId, map, count, state) in expected)
            {
                var usables = MapUsables.All.Where(usable => usable.ClassId == classId && usable.MapContextId == map).ToList();
                Assert.HasCount(count, usables, $"{classId} on {map}");
                Assert.IsTrue(usables.All(usable => usable.State == state && usable.UsedState == null), $"{classId} on {map}");

                var client = On(world, Channel(map, "fixture"));
                MapUsables.PlayerEnteredMap(client);
                var sent = States(client).Where(sentState => usables.Any(usable => usable.EntityId == sentState.EntityId)).ToList();
                Assert.HasCount(count, sent);
                Assert.IsTrue(sent.All(sentState => sentState.Packet.State == state));
            }

            Assert.HasCount(33, MapUsables.All.Where(usable => usable.ClassId == MapUsables.BaneTeleporter));

            // The human teleporters disabled are the four on the deck at 383 m, none at a local
            // teleporter of the server's (teleporter 542-548, Thunderhead Base, 339-377 m); the six
            // that are stay unsent, in their working mesh.
            var localTeleporters = new[]
            {
                new Vector3(564.7417f, 342.2656f, -432.0273f), new Vector3(575.875f, 342.2656f, -421.0664f),
                new Vector3(636.8945f, 339.1836f, -506.9843f), new Vector3(650.9922f, 339.1836f, -493.1055f),
                new Vector3(690.0898f, 385.8398f, -399.0f), new Vector3(633.0703f, 376.6836f, -605.8008f),
                new Vector3(643.6367f, 376.6836f, -595.332f)
            };

            foreach (var disabled in MapUsables.All.Where(usable => usable.ClassId == MapUsables.HumanTeleporter))
            {
                Assert.AreEqual(383f, disabled.Position.Y, 0.01f, disabled.ToString());
                Assert.IsTrue(localTeleporters.All(at => Vector3.Distance(at, disabled.Position) > 5f), disabled.ToString());
            }
        }

        [TestMethod]
        public void TheDellComputersAreSentOnAndAUseTurnsOneOffForEveryoneAndOnAgain()
        {
            var computers = MapUsables.All.Where(usable => usable.ClassId is MapUsables.DellNotebook or MapUsables.DellTower).ToList();

            Assert.HasCount(16, computers);
            Assert.HasCount(14, computers.Where(usable => usable.MapContextId == MapUsables.Abyss));
            Assert.HasCount(1, computers.Where(usable => usable.MapContextId == MapUsables.Mires));
            Assert.HasCount(1, computers.Where(usable => usable.MapContextId == MapUsables.TordenPlains));
            Assert.IsTrue(computers.All(usable => usable.State == UseObjectState.TsState1 && usable.UsedState == UseObjectState.TsState0));

            using var world = new WorldTestContext();
            var abyss = Channel(MapUsables.Abyss, "adv_arieki_torden_abyss");
            var computer = computers.First(usable => usable.MapContextId == MapUsables.Abyss);
            var user = On(world, abyss);
            user.Player.Position = computer.Position + new Vector3(1.5f, 0, 0);
            var other = On(world, abyss);
            other.Player.Position = computer.Position + new Vector3(250f, 0, 0);

            MapUsables.PlayerEnteredMap(user);
            var arrived = States(user).Where(sent => computers.Any(usable => usable.EntityId == sent.EntityId)).ToList();
            Assert.HasCount(14, arrived);
            Assert.IsTrue(arrived.All(sent => sent.Packet.State == UseObjectState.TsState1), "screens on");

            Assert.IsTrue(MapUsables.TryRequestUse(user, Use(computer.EntityId)));
            Assert.AreEqual(ActionId.UseObject, Sent(user).Select(sent => sent.Packet).OfType<PerformWindupPacket>().Single().ActionId);
            Recover(abyss);

            Assert.AreEqual(UseObjectState.TsState0, MapUsables.StateOf(abyss, computer), "off");
            Assert.AreEqual((computer.EntityId, UseObjectState.TsState0), (Uses(other).Single().EntityId, Uses(user).Single().Packet.CurState));

            Assert.IsTrue(MapUsables.TryRequestUse(user, Use(computer.EntityId)));
            Recover(abyss);
            Assert.AreEqual(UseObjectState.TsState1, MapUsables.StateOf(abyss, computer), "on again");
            Assert.AreEqual(UseObjectState.TsState1, Uses(other).Single().Packet.CurState);

            // The rest, used by nobody, are left as they were sent.
            var bane = MapUsables.All.First(usable => usable.ClassId == MapUsables.BaneTeleporter);
            var minos = Channel(bane.MapContextId, "adv_foreas_concordia_divide_minoscaverns");
            var looker = On(world, minos);
            looker.Player.Position = bane.Position + new Vector3(1f, 0, 0);
            Assert.IsTrue(MapUsables.TryRequestUse(looker, Use(bane.EntityId)));
            Recover(minos);
            Assert.AreEqual(UseObjectState.TsState1, MapUsables.StateOf(minos, bane));
        }
    }
}
