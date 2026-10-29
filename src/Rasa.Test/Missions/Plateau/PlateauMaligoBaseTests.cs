extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauInstanceMaps;

    /// <summary>
    /// Maligo Base (squad instance 1830) and the Ustor Bridge, played through as a player plays them: Showdown (876:
    /// the radio call on arrival, Watchman Zyrutha's keypass), Coup (878: the prisoners' supplies, the Bane wave, the
    /// cells' console) and One Way Trip (877: Bosley, Rapp, Ustor's west exit onto the bridge, Neuman, the beams,
    /// Overseer Kythura's keypass, the powercube, the socket, Maybauer, the terminal into the chamber, the bomb,
    /// Homburg, Bosley; and the 15-minute deadline).
    ///
    /// Why these tests: every place in Maligo is an estimate on a map the server had nothing on (PlateauMaligoBaseV1),
    /// and One Way Trip is taken on the Plateau but played in a squad copy of Maligo. A person or an object off the
    /// navmesh or out of reach, a scene that came up on the Plateau, a terminal that did not move the player inside
    /// the copy, or a deadline that did not fail the mission would each break the chain. So the places are checked on
    /// the client's navmesh, the doors are the real links, and the objects are used as Ustor Yard's are
    /// (PlateauUstorYardTests).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauMaligoBaseTests
    {
        private const uint Maligo = PlateauMaligoBaseV1.MapContextId;
        private const uint Ustor = PlateauUstorYardV1.MapContextId;
        private const uint Showdown = PlateauShowdownV1.MissionId;
        private const uint Coup = PlateauCoupV1.MissionId;
        private const uint OneWayTrip = PlateauOneWayTripV1.MissionId;
        private const uint SelfPreservation = PlateauSelfPreservationV1.MissionId;

        /// <summary>
        /// The NPCs of Ustor Yard and Maligo Base stand on safe ground: the server's map-load check
        /// (SpawnPoolManager.PoolsTooClose, which the client shows as "Server Map Initialization Errors!") finds no
        /// Bane pool within SafeClearance of Rapp, the Downed Prisoner or Altera (to-fix #7: upstream's 531659,
        /// 531631 and 531633 were).
        /// </summary>
        [TestMethod]
        public void UstorAndMaligoNpcsStandOnSafeGround()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var ours = new[]
            {
                PlateauUstorYardV1.RappPool, PlateauMaligoBaseV1.DownedPrisoner, PlateauMaligoBaseV1.Altera,
                PlateauMaligoBaseV1.Neuman
            };
            foreach (var pool in ours)
                Assert.IsTrue(SpawnPoolManager.Instance.LoadedSpawnPools.ContainsKey(pool), $"pool {pool} is loaded");
            var found = SpawnPoolManager.Instance.PoolsTooClose(new List<(uint, Vector3, string)>())
                .Where(entry => entry.Map is PlateauUstorYardV1.MapContextId or PlateauMaligoBaseV1.MapContextId)
                .Select(entry => entry.Message)
                .Where(message => ours.Any(pool => message.Contains($"pool {pool},")))
                .ToList();
            Assert.AreEqual(0, found.Count, string.Join("\n", found));
        }

        [TestMethod]
        public void UstorsWestExitLeadsOntoTheBridgeBeforeTheMaligoDoorAndBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            if (!harness.Maps.MapChannelArray.ContainsKey(Ustor))
                LoadMap(harness, Ustor);
            var ustor = harness.Maps.MapChannelArray[Ustor];
            var links = harness.World.MapLinkEntries.AsNoTracking().ToArray();
            var onto = links.Single(row => row.Id == PlateauMaligoBaseV1.OntoBridge);
            var back = links.Single(row => row.Id == PlateauMaligoBaseV1.BackIntoUstor);
            var maligoDoor = links.Single(row => row.MapContextId == PlateauMissionDataV1.MapContextId && row.DestMapContextId == Maligo);
            Assert.AreEqual((Ustor, PlateauMissionDataV1.MapContextId), (onto.MapContextId, onto.DestMapContextId));
            Assert.AreEqual((PlateauMissionDataV1.MapContextId, Ustor), (back.MapContextId, back.DestMapContextId));

            var onBridge = new Vector3((float)onto.DestPosX, (float)onto.DestPosY, (float)onto.DestPosZ);
            AssertStandsOnNavMesh(plateau, onBridge);
            Assert.IsTrue(Flat(onBridge, back) > back.Radius, "set down outside the bridge's way back");
            Assert.IsTrue(Flat(onBridge, maligoDoor) > maligoDoor.Radius, "set down outside the Maligo door");
            AssertConnected(plateau, onBridge, new Vector3((float)maligoDoor.PosX, (float)maligoDoor.PosY, (float)maligoDoor.PosZ), "the Maligo door");
            AssertConnected(plateau, onBridge, new Vector3((float)back.PosX, (float)back.PosY, (float)back.PosZ), "the way back");
            var bridge = PlateauOneWayTripV1.BridgeAt;
            AssertConnected(plateau, onBridge, new Vector3((float)bridge.X, (float)bridge.Y, (float)bridge.Z), "One Way Trip's area");

            var inUstor = new Vector3((float)back.DestPosX, (float)back.DestPosY, (float)back.DestPosZ);
            AssertStandsOnNavMesh(ustor, inUstor);
            Assert.IsTrue(Flat(inUstor, onto) > onto.Radius, "set down outside Ustor's west exit");
            AssertConnected(ustor, inUstor, new Vector3((float)onto.PosX, (float)onto.PosY, (float)onto.PosZ), "the west exit");
            var rapp = harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == PlateauUstorYardV1.RappPool);
            AssertConnected(ustor, inUstor, new Vector3((float)rapp.PosX, (float)rapp.PosY, (float)rapp.PosZ), "Rapp");
        }

        /// <summary>
        /// The people, the officers and the pads stand on Maligo's navmesh, each where the player can walk to it: Level 1
        /// from the door, Level 2 from its pad, the chamber from where the terminal sets the player down. Level 2 is
        /// not walked to from Level 1, nor the chamber from Level 2: the pads and the terminal are the ways in.
        /// </summary>
        [TestMethod]
        public void MaligosPeopleOfficersAndPadsStandWhereThePlayerWalks()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoMaligo(harness);
            var arrival = harness.Client.Player.Position;
            AssertStandsOnNavMesh(copy, arrival);

            var pads = harness.World.TeleporterEntries.AsNoTracking()
                .Where(row => row.Id >= PlateauMaligoBaseV1.Level1Pad && row.Id <= PlateauMaligoBaseV1.ChamberPad).ToDictionary(row => row.Id);
            Assert.HasCount(3, pads);
            Vector3 Pad(uint id) => new((float)pads[id].PosX, (float)pads[id].PosY, (float)pads[id].PosZ);
            foreach (var pad in pads.Values)
            {
                Assert.AreEqual((Maligo, PlateauMaligoBaseV1.PadClass, PlateauMaligoBaseV1.LocalTeleporter),
                    (pad.MapContextId, pad.ClassId, (uint)pad.Type), $"{pad.Id}");
                AssertStandsOnNavMesh(copy, Pad(pad.Id));
            }
            CollectionAssert.AreEquivalent(new[]
                {
                    (PlateauMaligoBaseV1.Level1Pad, PlateauMaligoBaseV1.Level2Pad), (PlateauMaligoBaseV1.Level2Pad, PlateauMaligoBaseV1.Level1Pad),
                    (PlateauMaligoBaseV1.ChamberPad, PlateauMaligoBaseV1.Level2Pad)
                },
                harness.World.LocalTeleporterLinkEntries.AsNoTracking().ToArray().Where(link => pads.ContainsKey(link.FromTeleporterId))
                    .Select(link => (link.FromTeleporterId, link.ToTeleporterId)).ToArray());

            var level1 = Pad(PlateauMaligoBaseV1.Level1Pad);
            var level2 = Pad(PlateauMaligoBaseV1.Level2Pad);
            var chamber = ToVector(PlateauOneWayTripV1.ChamberArrival);
            AssertConnected(copy, arrival, level1, "Level 1's pad");
            Assert.IsFalse(Connected(copy, arrival, level2), "Level 2 is not walked to from Level 1");
            Assert.IsFalse(Connected(copy, level2, chamber), "the chamber is not walked to from Level 2");
            AssertConnected(copy, chamber, Pad(PlateauMaligoBaseV1.ChamberPad), "the chamber's pad");

            Spawn(copy, PlateauMaligoBaseV1.Zyrutha, PlateauMaligoBaseV1.Kythura, PlateauMaligoBaseV1.DownedPrisoner,
                PlateauMaligoBaseV1.Altera, PlateauMaligoBaseV1.Neuman);
            var zyrutha = Here(copy).First(creature => creature.DbId == PlateauMaligoBaseV1.Zyrutha);
            AssertStandsOnNavMesh(copy, zyrutha.Position);
            AssertConnected(copy, arrival, zyrutha.Position, "Zyrutha");
            Assert.AreEqual(PlateauMaligoBaseV1.ZyruthaName, zyrutha.NameId);
            Assert.AreEqual(PlateauMaligoBaseV1.ZyruthaLevel, zyrutha.Level);
            Assert.AreEqual((int)PlateauMaligoBaseV1.ZyruthaHitPoints, (int)zyrutha.Attributes[Attributes.Health].Current);
            Assert.AreEqual(TargetCategory.Hostile, zyrutha.TargetCategory);
            var kythura = Here(copy).First(creature => creature.DbId == PlateauMaligoBaseV1.Kythura);
            Assert.AreEqual(PlateauMaligoBaseV1.KythuraName, kythura.NameId);
            Assert.AreEqual(TargetCategory.Hostile, kythura.TargetCategory);
            AssertStandsOnNavMesh(copy, kythura.Position);
            AssertConnected(copy, level2, kythura.Position, "Kythura");

            foreach (var (pool, name, package) in new (uint, uint, uint?)[]
                {
                    (PlateauMaligoBaseV1.DownedPrisoner, PlateauMaligoBaseV1.DownedPrisonerName, null),
                    (PlateauMaligoBaseV1.Altera, PlateauMaligoBaseV1.AlteraName, PlateauMaligoBaseV1.AlteraPackage),
                    (PlateauMaligoBaseV1.Neuman, PlateauMaligoBaseV1.NeumanName, PlateauMaligoBaseV1.NeumanPackage)
                })
            {
                var npc = Npc(copy, pool);
                Assert.AreEqual(name, npc.NameId, $"{pool}");
                if (package.HasValue)
                    Assert.AreEqual(package.Value, npc.Npc.NpcPackageId, $"{pool}");
                Assert.AreNotEqual(TargetCategory.Hostile, npc.TargetCategory, $"{pool}");
                AssertStandsOnNavMesh(copy, npc.Position);
                AssertConnected(copy, level2, npc.Position, $"{pool}");
            }

            foreach (var (place, what) in new[]
                {
                    (ToVector(PlateauOneWayTripV1.BeamsAt), "the beams"), (ToVector(PlateauOneWayTripV1.BankAt), "the computer bank"),
                    (ToVector(PlateauOneWayTripV1.SocketAt), "the socket"), (ToVector(PlateauOneWayTripV1.MaybauerAt), "Maybauer"),
                    (ToVector(PlateauOneWayTripV1.CrateAt), "the bomb crate"), (ToVector(PlateauOneWayTripV1.TerminalAt), "the terminal"),
                    (ToVector(PlateauCoupV1.ConsoleAt), "the cells' console")
                }.Concat(PlateauCoupV1.SuppliesAt.Concat(PlateauCoupV1.AmmoAt).Concat(PlateauCoupV1.WaveAt).Select(at => (ToVector(at), $"Coup {at}"))))
                AssertConnected(copy, level2, place, what);
            foreach (var (place, what) in new[]
                {
                    (ToVector(PlateauOneWayTripV1.DeviceAt), "the device"), (ToVector(PlateauOneWayTripV1.ChargeAt), "the charge"),
                    (ToVector(PlateauOneWayTripV1.HomburgAt), "Homburg")
                }.Concat(Enumerable.Range(0, PlateauOneWayTripV1.PrisonerLanes.Length)
                    .SelectMany(index => PlateauOneWayTripV1.MarchOf(index).Select(point => (ToVector(point.Position), $"march {index + 1}")))))
                AssertConnected(copy, chamber, place, what);
        }

        [TestMethod]
        public void ShowdownIsCalledInOnArrivalAndEndsWithZyruthasKeypass()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoMaligo(harness);
            harness.Manager.OfferArrivalMissions(harness.Client);   // as ManifestationManager.MissionArrival does
            var offer = harness.Drain().OfType<DispenseRadioMissionPacket>().Single(packet => packet.MissionId == Showdown);
            Assert.IsNotNull(offer);
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Showdown));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Showdown, PlateauShowdownV1.Officer));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Showdown, PlateauShowdownV1.Keypass));

            var zyrutha = Officer(harness, copy, PlateauMaligoBaseV1.Zyrutha);
            // Markers: 1 and 2 at Zyrutha (the keypass is on his corpse).
            Assert.HasCount(2, Markers(harness, Showdown));
            AssertMarker(harness, Showdown, PlateauShowdownV1.Officer, zyrutha.Position, 3f, "Zyrutha");
            AssertMarker(harness, Showdown, PlateauShowdownV1.Keypass, zyrutha.Position, 3f, "Zyrutha's keypass");
            var corpse = Kill(harness, copy, zyrutha);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Showdown, PlateauShowdownV1.Officer));
            Assert.IsNotNull(corpse, "Zyrutha leaves a corpse");
            Assert.IsTrue(corpse.LootItems.Any(item => item.ItemTemplateId == PlateauShowdownV1.Pass), "the keypass on Zyrutha");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauShowdownV1.Pass));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Showdown, PlateauShowdownV1.Keypass));
            Assert.IsTrue(harness.Client.Player.Missions[Showdown].Completeable);

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, Showdown, 1, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[Showdown].State);
            Assert.AreEqual(before.Experience + PlateauShowdownV1.Experience, harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(before.Credits + (int)PlateauShowdownV1.Credits, harness.Context.ReadRewardTotals().Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauShowdownV1.GravitonHelmet), "the second choice");
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauShowdownV1.Pass), "the keypass is kept");

            harness.Manager.OfferArrivalMissions(harness.Client);
            Assert.IsFalse(harness.Drain().OfType<DispenseRadioMissionPacket>().Any(packet => packet.MissionId == Showdown), "not called in again");
        }

        /// <summary>
        /// Self Preservation comes by radio on arrival with Showdown; the three Security CPUs in the north of Level 1 are
        /// shot down one by one (the counter), and it ends by radio.
        /// </summary>
        [TestMethod]
        public void SelfPreservationIsCalledInOnArrivalAndEndsWithTheThreeCpusDown()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoMaligo(harness);
            var arrival = harness.Client.Player.Position;
            harness.Manager.OfferArrivalMissions(harness.Client);
            var offered = harness.Drain().OfType<DispenseRadioMissionPacket>().Select(packet => packet.MissionId).ToArray();
            CollectionAssert.IsSubsetOf(new[] { Showdown, SelfPreservation }, offered, "both called in on arrival");
            Assert.IsNull(FindSceneObject(harness, copy, SelfPreservation, PlateauSelfPreservationV1.CpuRole(1)), "no CPU before the mission");
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, SelfPreservation));
            harness.Drain();
            Assert.IsTrue(harness.Manager.Scenes.TryGetSceneMap(SelfPreservation, out var bound));
            Assert.AreEqual(Maligo, bound);
            Tick(harness, copy, 250);

            var fields = PlateauUstorYardTests.PlaceFields(harness, copy, PlateauSelfPreservationFieldsV1.Fields.Select(field => field.Id).ToArray());
            try
            {
            var cpus = PlateauSelfPreservationV1.Cpus.Select(cpu => (cpu, obj: SceneObject(harness, copy, SelfPreservation,
                PlateauSelfPreservationV1.CpuRole(cpu.Sequence)))).ToArray();
            foreach (var (cpu, obj) in cpus)
            {
                Assert.AreEqual(cpu.Class, (uint)obj.EntityClassId);
                Assert.AreEqual(PlateauSelfPreservationV1.IntactState, (uint)obj.StateId);
                AssertStandsOnNavMesh(copy, obj.Position, 0.5f);
                AssertConnected(copy, arrival, obj.Position, $"CPU {cpu.Sequence}");
                Assert.IsGreaterThan(250f, obj.Position.Z, $"CPU {cpu.Sequence} in the north of Level 1");
                Assert.IsFalse(obj.IsEnabled, $"CPU {cpu.Sequence} behind its field, not to be shot yet");
            }
            var switches = Enumerable.Range(0, PlateauSelfPreservationV1.Switches.Length)
                .Select(index => SceneObject(harness, copy, SelfPreservation, PlateauSelfPreservationV1.SwitchRole(index))).ToArray();
            for (var index = 0; index < switches.Length; index++)
            {
                Assert.AreEqual(PlateauSelfPreservationV1.SwitchClass, (uint)switches[index].EntityClassId);
                Assert.AreEqual(PlateauSelfPreservationV1.SwitchOff, (uint)switches[index].StateId);
                AssertStandsOnNavMesh(copy, switches[index].Position, 0.5f);
                AssertConnected(copy, arrival, switches[index].Position, $"switch {index + 1}");
                // The switch outside the field, its CPU inside: the step from one to the other meets the field.
                Assert.IsTrue(ForceFields.StopsPlayer(copy, switches[index].Position + new Vector3(0, 1, 0), cpus[index].obj.Position + new Vector3(0, 1, 0)),
                    $"field {index + 1} between its switch and its CPU");
            }
            PlateauUstorYardTests.AssertFieldsUp(copy, fields);

            // Markers on objective 1: each CPU and each switch.
            var markers = harness.World.MissionIndicatorEntries.AsNoTracking().Where(row => row.MissionId == SelfPreservation).ToList();
            Assert.HasCount(6, markers);
            Assert.IsTrue(markers.All(row => row.ObjectiveId == PlateauSelfPreservationV1.Destroy));
            foreach (var obj in cpus.Select(cpu => cpu.obj).Concat(switches))
                Assert.IsTrue(markers.Any(row => Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new(obj.Position.X, obj.Position.Z)) < 0.5f),
                    $"a marker at {obj.SceneActorRole}");

            // A CPU shot before its switch: no target, nothing counted.
            Damage(harness, copy, cpus[0].obj, (int)PlateauSelfPreservationV1.CpuHitPoints);
            Assert.AreEqual(PlateauSelfPreservationV1.IntactState, (uint)cpus[0].obj.StateId, "not shot through the field");

            for (var index = 0; index < cpus.Length; index++)
            {
                Use(harness, copy, switches[index], PlateauSelfPreservationV1.UseArgument);
                Tick(harness, copy, 250);
                Assert.AreEqual(PlateauSelfPreservationV1.SwitchOn, (uint)switches[index].StateId, $"switch {index + 1} flipped");
                Assert.AreEqual(MissionObjectiveState.Completed,
                    Objective(harness, SelfPreservation, PlateauSelfPreservationV1.Switches[index].Objective), $"switch {index + 1}'s objective");
                PlateauUstorYardTests.AssertFieldsDown(copy, new[] { fields[index] });
                PlateauUstorYardTests.AssertFieldsUp(copy, fields.Skip(index + 1).ToArray());
                Assert.IsTrue(cpus[index].obj.IsEnabled, $"CPU {index + 1} open to fire");
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, SelfPreservation, PlateauSelfPreservationV1.Destroy), $"before CPU {index + 1}");
                Damage(harness, copy, cpus[index].obj, (int)PlateauSelfPreservationV1.CpuHitPoints);
                Assert.AreEqual(PlateauSelfPreservationV1.DestroyedState, (uint)cpus[index].obj.StateId, $"CPU {index + 1} down");
            }
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, SelfPreservation, PlateauSelfPreservationV1.Destroy));
            Assert.IsTrue(harness.Client.Player.Missions[SelfPreservation].Completeable);

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryCompleteRadioMission(harness.Client, SelfPreservation, 3, null));
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[SelfPreservation].State);
            Assert.AreEqual(before.Experience + PlateauSelfPreservationV1.Experience, harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(before.Credits + (int)PlateauSelfPreservationV1.Credits, harness.Context.ReadRewardTotals().Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauSelfPreservationV1.StealthBoots), "the fourth choice");
            }
            finally
            {
                PlateauUstorYardTests.ForgetFields(copy);
            }
        }

        [TestMethod]
        public void CoupGathersTheSuppliesHoldsOffTheWaveAndOpensTheCells()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoMaligo(harness);
            var prisoner = Npc(copy, PlateauMaligoBaseV1.DownedPrisoner);
            var altera = Npc(copy, PlateauMaligoBaseV1.Altera);
            var neuman = Npc(copy, PlateauMaligoBaseV1.Neuman);

            Assert.IsFalse(Offers(harness, prisoner, Coup), "it waits for Showdown");
            SeedCompleted(harness, Showdown);
            var offer = WildernessRanjaGorgeTests.Offer(harness, prisoner, Coup);
            Assert.AreEqual(0, offer.SelectableReward.Count);
            Accept(harness, prisoner, Coup);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Coup, PlateauCoupV1.MeetAltera));

            Talk(harness, altera, Coup, PlateauCoupV1.MeetAltera);
            Assert.IsNull(FindSceneObject(harness, copy, Coup, PlateauCoupV1.SupplyRole(0)), "no crate before Neuman");
            Talk(harness, neuman, Coup, PlateauCoupV1.ReportNeuman);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Coup, PlateauCoupV1.Gather));
            Tick(harness, copy, 250);

            var roles = Enumerable.Range(0, PlateauCoupV1.SuppliesAt.Length).Select(PlateauCoupV1.SupplyRole)
                .Concat(Enumerable.Range(0, PlateauCoupV1.AmmoAt.Length).Select(PlateauCoupV1.AmmoRole)).ToArray();
            foreach (var role in roles)
            {
                Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Coup, PlateauCoupV1.Gather), $"before {role}");
                var crate = SceneObject(harness, copy, Coup, role);
                Assert.AreEqual(PlateauCoupV1.CrateClass, (uint)crate.EntityClassId);
                AssertStandsOnNavMesh(copy, crate.Position, 0.5f);
                Use(harness, copy, crate, PlateauCoupV1.CrateUseArgument);
            }
            Assert.AreEqual((uint)PlateauCoupV1.SuppliesAt.Length, WildernessRanjaGorgeTests.Held(harness, PlateauCoupV1.SupplyItem));
            Assert.AreEqual((uint)PlateauCoupV1.AmmoAt.Length, WildernessRanjaGorgeTests.Held(harness, PlateauCoupV1.AmmoItem));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Coup, PlateauCoupV1.Gather));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Coup, PlateauCoupV1.Regroup));
            Assert.IsNull(SceneCreature(harness, copy, Coup, PlateauCoupV1.WaveRole(0)), "no wave before Altera");

            Talk(harness, altera, Coup, PlateauCoupV1.Regroup);
            Tick(harness, copy, 250);
            for (var index = 0; index < PlateauCoupV1.WaveAt.Length; index++)
            {
                var bane = SceneCreature(harness, copy, Coup, PlateauCoupV1.WaveRole(index));
                Assert.IsNotNull(bane, $"Bane {index + 1} comes");
                Assert.AreEqual(PlateauUstorYardV1.Security, bane.DbId);
                Assert.AreEqual(TargetCategory.Hostile, bane.TargetCategory);
                AssertStandsOnNavMesh(copy, bane.Position);
                Kill(harness, copy, bane);
            }

            var console = SceneObject(harness, copy, Coup, PlateauCoupV1.ConsoleRole);
            Assert.AreEqual(PlateauCoupV1.ConsoleClass, (uint)console.EntityClassId);
            AssertStandsOnNavMesh(copy, console.Position, 0.5f);

            // Markers: 1 and 5 at Altera, 6 at Neuman, 2 at each crate, 4 at the console.
            Assert.HasCount(4 + roles.Length, Markers(harness, Coup));
            AssertMarker(harness, Coup, PlateauCoupV1.MeetAltera, altera.Position, 2f, "Altera");
            AssertMarker(harness, Coup, PlateauCoupV1.Regroup, altera.Position, 2f, "Altera");
            AssertMarker(harness, Coup, PlateauCoupV1.ReportNeuman, neuman.Position, 2f, "Neuman");
            foreach (var role in roles)
                AssertMarker(harness, Coup, PlateauCoupV1.Gather, SceneObject(harness, copy, Coup, role).Position, 0.5f, role);
            AssertMarker(harness, Coup, PlateauCoupV1.Release, console.Position, 0.5f, "the console");
            Assert.IsFalse(harness.Client.Player.Missions[Coup].Completeable);
            Use(harness, copy, console, 1);
            Assert.AreEqual(PlateauCoupV1.ConsoleSetState, (uint)console.StateId, "the cells open");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Coup, PlateauCoupV1.Release));
            Assert.IsTrue(harness.Client.Player.Missions[Coup].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, altera, Coup, null, PlateauCoupV1.Experience, PlateauCoupV1.Credits);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauCoupV1.SupplyItem), "the supplies handed over");
        }

        /// <summary>
        /// To-fix #16: the player is at the crates, far from Altera, when Regroup (5) opens. Back near her, she offers
        /// its talk: the client is told her status, ObjectivComplete for Coup, on the way or on arrival.
        /// </summary>
        [TestMethod]
        public void CoupsRegroupIsOfferedByAlteraWhenThePlayerComesBackFromTheCrates()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoMaligo(harness);
            var prisoner = Npc(copy, PlateauMaligoBaseV1.DownedPrisoner);
            var altera = Npc(copy, PlateauMaligoBaseV1.Altera);
            var neuman = Npc(copy, PlateauMaligoBaseV1.Neuman);
            SeedCompleted(harness, Showdown);
            Accept(harness, prisoner, Coup);
            Talk(harness, altera, Coup, PlateauCoupV1.MeetAltera);
            Talk(harness, neuman, Coup, PlateauCoupV1.ReportNeuman);
            Tick(harness, copy, 250);

            var roles = Enumerable.Range(0, PlateauCoupV1.SuppliesAt.Length).Select(PlateauCoupV1.SupplyRole)
                .Concat(Enumerable.Range(0, PlateauCoupV1.AmmoAt.Length).Select(PlateauCoupV1.AmmoRole)).ToArray();
            foreach (var role in roles.Take(roles.Length - 1))
                Use(harness, copy, SceneObject(harness, copy, Coup, role), PlateauCoupV1.CrateUseArgument);

            // The last crate, from where it stands: far from Altera.
            var last = SceneObject(harness, copy, Coup, roles.Last());
            harness.MoveTo(last.Position + new Vector3(0, 0, 1));
            harness.Drain();
            Assert.IsGreaterThan(64f, Vector3.Distance(harness.Client.Player.Position, altera.Position), "far from Altera");
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = last.EntityId, ActionId = ActionId.UseObject, ActionArgId = PlateauCoupV1.CrateUseArgument
            });
            if (copy.PerformRecovery.Count > 0)
            {
                var recovery = copy.PerformRecovery.Last();
                copy.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(copy, recovery);
            }
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Coup, PlateauCoupV1.Regroup));
            var told = StatusesOf(harness, altera);

            // A few seconds at the crates (MissionContacts.Worker's check), then back to Altera, and a check there.
            MissionContacts.Sync(harness.Client);
            told.AddRange(StatusesOf(harness, altera));
            harness.MoveTo(altera.Position + new Vector3(0, 0, 6));
            MissionContacts.Sync(harness.Client);
            told.AddRange(StatusesOf(harness, altera));

            Assert.IsNotEmpty(told, "Altera's status was sent");
            var status = told.Last();
            Assert.AreEqual(ConversationStatus.ObjectivComplete, status.ConvoStatusId,
                string.Join(", ", told.Select(each => $"{each.ConvoStatusId} [{string.Join(" ", each.Data)}]")));
            CollectionAssert.Contains(status.Data, Coup);
            Talk(harness, altera, Coup, PlateauCoupV1.Regroup);
        }

        /// <summary>
        /// To-fix #16 and #17: the people the player talks to in a squad copy of Maligo or Ustor - the Downed Prisoner,
        /// Altera and Neuman (Coup), Rapp (Silent But Deadly, Interception!, Monkey Wrench) - outlive the copy's Bane. They
        /// are AFS (FRIENDLY), and to-fix #7 had put them only SafeClearance (15 m) from the Bane's ground, inside the aggro
        /// range (18 m): the Bane killed them, and a squad copy's pool does not come back, so Coup's Regroup with Altera and
        /// the hand-ins of 1187 and 1189 at Rapp had nobody left to talk to. They stand out of reach now
        /// (PlateauContactsOutOfAggroV1). The player here is a GM out of sight, as the playtest bot was: the Bane notice
        /// only the NPCs.
        /// </summary>
        [TestMethod]
        public void MaligoAndUstorContactsOutliveTheCopysBane()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            harness.Client.Player.GmInvisible = true;

            var maligo = IntoMaligo(harness);
            AssertOutlivesTheBane(harness, maligo, PlateauMaligoBaseV1.Altera, PlateauMaligoBaseV1.DownedPrisoner, PlateauMaligoBaseV1.Neuman);

            Leave(harness, maligo, plateau);
            var ustor = IntoUstor(harness);
            AssertOutlivesTheBane(harness, ustor, PlateauUstorYardV1.RappPool);
            var offered = Told(harness, Npc(ustor, PlateauUstorYardV1.RappPool));
            Assert.AreEqual(ConversationStatus.Available, offered.ConvoStatusId);
            CollectionAssert.Contains(offered.Data, PlateauSilentButDeadlyV1.MissionId);
        }

        /// <summary>
        /// The NPCs of these pools and every hostile pool whose ground comes within 60 m of them: a Bane of each pool, gone
        /// wandering to the edge of its ground nearest the NPC (its circle, or BehaviorManager.WanderZoneFallbackRadius
        /// around a point pool), does not pick the NPC; and after a minute of the copy's thinking the NPCs stand.
        /// </summary>
        internal static void AssertOutlivesTheBane(WildernessRuntimeTestHarness harness, MapChannel copy, params uint[] pools)
        {
            var npcs = pools.Select(pool => Npc(copy, pool)).ToArray();
            static float Ground(Rasa.Structures.SpawnPool pool) => pool.Radius > 0 ? pool.Radius : BehaviorManager.WanderZoneFallbackRadius;
            static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new(a.X, a.Z), new(b.X, b.Z));
            var bane = copy.SpawnPools.Where(pool => !pools.Contains(pool.DbId) && pool.SpawnSlot.Any(slot =>
                    CreatureManager.Instance.LoadedCreatures.TryGetValue(slot.CreatureId, out var template) &&
                    template.TargetCategory == TargetCategory.Hostile) &&
                npcs.Any(npc => Flat(pool.Position, npc.Position) - Ground(pool) <= 60))
                .ToArray();
            var missing = bane.Select(pool => pool.DbId).Where(pool => !Here(copy).Any(creature => creature.SpawnPool?.DbId == pool)).ToArray();
            if (missing.Length > 0)
                Spawn(copy, missing);
            Assert.IsNotEmpty(bane, "Bane pools near them");
            foreach (var npc in npcs)
                foreach (var pool in bane)
                {
                    var one = Here(copy).First(creature => creature.SpawnPool?.DbId == pool.DbId && creature.TargetCategory == TargetCategory.Hostile);
                    var home = pool.Position;
                    var toward = Vector3.Normalize(new Vector3(npc.Position.X - home.X, 0, npc.Position.Z - home.Z));
                    var edge = home + toward * Ground(pool);
                    one.Position = new Vector3(edge.X, npc.Position.Y, edge.Z);
                    Assert.AreNotEqual(npc.EntityId, BehaviorManager.FindAttackableInRange(copy, one, one.AggroRange),
                        $"Bane {one.DbId} of pool {pool.DbId}, {Vector3.Distance(one.Position, npc.Position):0.0} m away, goes for {npc.DbId}");
                }
            harness.MoveTo(npcs[0].Position + new Vector3(0, 0, 2));
            for (var second = 0; second < 60; second++)
                Tick(harness, copy, 1000);
            foreach (var npc in npcs)
            {
                Assert.AreNotEqual(CharacterState.Dead, npc.State, $"{npc.DbId} is alive");
                Assert.IsTrue(CreatureManager.IsLivingOnMap(copy, npc), $"{npc.DbId} stands in the copy");
            }
        }

        /// <summary>The NPC's conversation status as the client is told it now.</summary>
        private static NPCConversationStatusPacket Told(WildernessRuntimeTestHarness harness, Creature npc)
        {
            harness.Drain();
            NpcManager.Instance.UpdateConversationStatus(harness.Client, npc, harness.Manager);
            return StatusesOf(harness, npc).Single();
        }

        /// <summary>The conversation statuses the client was told for this NPC since the last drain.</summary>
        private static List<NPCConversationStatusPacket> StatusesOf(WildernessRuntimeTestHarness harness, Creature npc) =>
            Rasa.Test.World.WorldTestContext.Drain(harness.Client)
                .Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                .Where(call => call.EntityId == npc.EntityId)
                .Select(call => call.Packet).OfType<NPCConversationStatusPacket>().ToList();

        [TestMethod]
        public void OneWayTripGoesThroughUstorAndMaligoToTheWormholeAndBackToBosley()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var bosley = AcceptOneWayTrip(harness, plateau);
            var (copy, maybauer) = ToMaybauer(harness, plateau);

            Talk(harness, maybauer, OneWayTrip, PlateauOneWayTripV1.Maybauer);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Bomb));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Destroy));
            Tick(harness, copy, 250);
            var prisoners = Prisoners(harness, copy);
            var charge = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.ChargeRole);
            var device = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.DeviceRole);
            Assert.AreEqual(PlateauOneWayTripV1.DeviceClass, (uint)device.EntityClassId);
            Assert.AreEqual(PlateauOneWayTripV1.DeviceIntactState, (uint)device.StateId);
            AssertStandsOnNavMesh(copy, device.Position, 0.5f);
            Assert.AreEqual(PlateauOneWayTripV1.ChargeClass, (uint)charge.EntityClassId);

            IntoTheChamber(harness, copy);
            Use(harness, copy, charge, PlateauOneWayTripV1.UseArgument);
            Assert.AreEqual(PlateauOneWayTripV1.DeviceIntactState, (uint)device.StateId, "no charge without the bomb");

            var crate = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.CrateRole);
            Assert.AreEqual(PlateauOneWayTripV1.CrateClass, (uint)crate.EntityClassId);
            AssertStandsOnNavMesh(copy, crate.Position, 0.5f);
            Use(harness, copy, crate, PlateauOneWayTripV1.CrateUseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOneWayTripV1.BombItem));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Bomb));
            Tick(harness, copy, 250);

            Assert.IsNull(SceneCreature(harness, copy, OneWayTrip, PlateauOneWayTripV1.HomburgRole), "no Homburg before the bomb");
            Use(harness, copy, charge, PlateauOneWayTripV1.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauOneWayTripV1.BombItem), "the bomb set");
            Assert.AreEqual(PlateauOneWayTripV1.DeviceDestroyedState, (uint)device.StateId, "the device destroyed");
            Assert.IsNotNull(device.MissionDestruction, "a destruction, so the blow shows as one (DestroyableStates): the client ignores a 110 -> 2 use");
            Assert.AreEqual(0U, device.CurrentHitPoints, "shown as a destruction");
            Tick(harness, copy, 250);
            var homburg = SceneCreature(harness, copy, OneWayTrip, PlateauOneWayTripV1.HomburgRole);
            Assert.IsNotNull(homburg, "Homburg comes");
            Assert.AreEqual(PlateauMaligoBaseV1.HomburgName, homburg.NameId);
            Assert.AreEqual(PlateauMaligoBaseV1.HomburgPackage, homburg.Npc.NpcPackageId);
            AssertStandsOnNavMesh(copy, homburg.Position);
            Talk(harness, homburg, OneWayTrip, PlateauOneWayTripV1.Destroy);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Report));

            // A marker for every objective: on the objects and NPCs it is done at (12's at the device and the terminal).
            Assert.HasCount(13, Markers(harness, OneWayTrip));
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Maybauer, maybauer.Position, 2f, "Maybauer");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Bomb, crate.Position, 0.5f, "the bomb crate");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Destroy, device.Position, 0.5f, "the device");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Destroy,
                SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.TerminalRole).Position, 0.5f, "the chamber terminal");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.PowerDown,
                SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.SocketRole).Position, 0.5f, "the socket");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Powercube,
                SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.BankRole).Position, 0.5f, "the computer bank");
            var neuman = Npc(copy, PlateauMaligoBaseV1.Neuman);
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Massacre, neuman.Position, 2f, "Neuman");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.HowTo, neuman.Position, 2f, "Neuman");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Keypass, ToVector(PlateauMaligoBaseV1.KythuraAt), 0.5f, "Kythura's pool");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Locate, ToVector(PlateauOneWayTripV1.BeamsAt), 0.5f, "the beams");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Rapp, ToVector(PlateauContactsOutOfAggroV1.RappAt), 0.5f, "Rapp in Ustor Yard");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Bridge, ToVector(PlateauOneWayTripV1.BridgeAt), 0.5f, "the bridge");
            AssertMarker(harness, OneWayTrip, PlateauOneWayTripV1.Report, bosley.Position, 2f, "Bosley");

            PastTheDeadline(harness, copy);
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[OneWayTrip].State, "the deadline met");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Destroy));
            for (var index = 0; index < prisoners.Length; index++)
            {
                var end = ToVector(PlateauOneWayTripV1.MarchOf(index).Last().Position);
                Assert.IsLessThan(1.5f, Vector3.Distance(prisoners[index].Position, end), $"prisoner {index + 1} at the device ({prisoners[index].Position})");
            }

            Leave(harness, copy, plateau);
            Talk(harness, bosley, OneWayTrip, PlateauOneWayTripV1.Report);
            Assert.IsTrue(harness.Client.Player.Missions[OneWayTrip].Completeable);
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, bosley, OneWayTrip, 2,
                PlateauOneWayTripV1.Experience, PlateauOneWayTripV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOneWayTripV1.MechGloves), "the third choice");
        }

        /// <summary>
        /// The test bot's order (to-fix #24): Maybauer (16), the bomb from the crate (15), then the terminal. The
        /// terminal's use starts the passage into the chamber; uses again while its hold runs (the bot tried
        /// arguments 3 and 2) are refused, as the player is not in the world for a use then, and change nothing:
        /// the passage still sets the player down in the chamber, where the charge and Homburg complete 12.
        /// </summary>
        [TestMethod]
        public void OneWayTripsTerminalAfterTheBombTakesThePlayerIntoTheChamberAndRefusesUsesWhileOnTheWay()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            AcceptOneWayTrip(harness, plateau);
            var (copy, maybauer) = ToMaybauer(harness, plateau);

            Talk(harness, maybauer, OneWayTrip, PlateauOneWayTripV1.Maybauer);
            Tick(harness, copy, 250);
            var crate = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.CrateRole);
            Use(harness, copy, crate, PlateauOneWayTripV1.CrateUseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOneWayTripV1.BombItem));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Bomb));
            Tick(harness, copy, 250);

            var terminal = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.TerminalRole);
            Use(harness, copy, terminal, PlateauOneWayTripV1.UseArgument);
            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "the terminal takes the player");
            Assert.IsTrue(transfer.IsPassage);
            Assert.AreSame(copy, transfer.DestinationMap, "inside the squad's copy");
            Assert.IsLessThan(2f, Vector3.Distance(ToVector(PlateauOneWayTripV1.ChamberArrival), transfer.DestinationPosition), "to the chamber");

            foreach (var argument in new uint[] { 3, 2, PlateauOneWayTripV1.UseArgument })
            {
                harness.Drain();
                harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
                {
                    EntityId = terminal.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
                });
                var refused = Rasa.Test.World.WorldTestContext.Drain(harness.Client)
                    .Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                    .Select(call => call.Packet).OfType<UserActionFailedPacket>().ToList();
                Assert.IsTrue(refused.Any(packet => packet.ActionArgId == argument), $"a use with argument {argument} on the way is refused");
                Assert.AreSame(transfer, harness.Client.PendingTransfer, $"the passage goes on after the use with argument {argument}");
            }
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Destroy));

            // The passage's hold runs on the wall clock (DynamicObjectManager.PassageHoldMs): the player is set down as it lands.
            harness.Client.PendingTransfer = null;
            harness.MoveTo(ToVector(PlateauOneWayTripV1.ChamberArrival));
            harness.Drain();
            Assert.AreSame(copy, harness.Client.Player.MapChannel, "still in the squad's copy");

            var charge = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.ChargeRole);
            var device = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.DeviceRole);
            Use(harness, copy, charge, PlateauOneWayTripV1.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauOneWayTripV1.BombItem), "the bomb set");
            Assert.AreEqual(PlateauOneWayTripV1.DeviceDestroyedState, (uint)device.StateId, "the device destroyed");
            Assert.IsNotNull(device.MissionDestruction, "a destruction, so the blow shows as one (DestroyableStates): the client ignores a 110 -> 2 use");
            Assert.AreEqual(0U, device.CurrentHitPoints, "shown as a destruction");
            Tick(harness, copy, 250);
            var homburg = SceneCreature(harness, copy, OneWayTrip, PlateauOneWayTripV1.HomburgRole);
            Assert.IsNotNull(homburg, "Homburg comes");
            Talk(harness, homburg, OneWayTrip, PlateauOneWayTripV1.Destroy);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Destroy));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Report));
        }

        /// <summary>The 15 minutes run out before the bomb: the mission fails, and Bosley gives it again.</summary>
        [TestMethod]
        public void OneWayTripFailsWhenTheTimeRunsOutAndBosleyGivesItAgain()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            var bosley = AcceptOneWayTrip(harness, plateau);
            var (copy, maybauer) = ToMaybauer(harness, plateau);
            Talk(harness, maybauer, OneWayTrip, PlateauOneWayTripV1.Maybauer);
            Tick(harness, copy, 250);

            PastTheDeadline(harness, copy);
            Assert.AreEqual(MissionState.Failed, harness.Client.Player.Missions[OneWayTrip].State);
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Destroy));

            Leave(harness, copy, plateau);
            var offer = WildernessRanjaGorgeTests.Offer(harness, bosley, OneWayTrip);
            Assert.HasCount(4, offer.SelectableReward);
            Accept(harness, bosley, OneWayTrip);
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[OneWayTrip].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Rapp));
        }

        /// <summary>
        /// To-fix #18: Overseer Kythura stands on the way to Neuman, so she is killed before One Way Trip's 7 (her
        /// keypass) is open, and no keypass drops. In the squad's copy her pool comes back on its own 60 s, not the copy's
        /// 45 minutes (the scene names it a public spawn, PlateauMaligoOfficerSquadRespawnV1): 7 is done on her then.
        /// </summary>
        [TestMethod]
        public void OneWayTripsKythuraKilledBeforeHerKeypassIsDueComesBackInTheSquadCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            AcceptOneWayTrip(harness, plateau);
            var copy = ToMaligo(harness, plateau);
            Assert.IsNotNull(copy.SquadState, "a squad's copy");

            var early = Kill(harness, copy, Officer(harness, copy, PlateauMaligoBaseV1.Kythura));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Keypass));
            Assert.IsFalse(early?.LootItems.Any(item => item.ItemTemplateId == PlateauOneWayTripV1.Pass) == true, "no keypass before 7");

            var neuman = Npc(copy, PlateauMaligoBaseV1.Neuman);
            Talk(harness, neuman, OneWayTrip, PlateauOneWayTripV1.Massacre);
            Talk(harness, neuman, OneWayTrip, PlateauOneWayTripV1.HowTo);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Keypass));

            var kythura = BackAfterAMinute(harness, copy, PlateauMaligoBaseV1.Kythura);
            var corpse = Kill(harness, copy, kythura);
            Assert.IsNotNull(corpse, "Kythura leaves a corpse");
            Assert.IsTrue(corpse.LootItems.Any(item => item.ItemTemplateId == PlateauOneWayTripV1.Pass), "the keypass on Kythura");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Keypass));
        }

        /// <summary>
        /// To-fix #18: Watchman Zyrutha killed before Showdown is taken (a squad mate's kill, or the radio call not yet
        /// answered) comes back in the squad's copy on his own 60 s; Showdown is done on him then.
        /// </summary>
        [TestMethod]
        public void ShowdownsZyruthaKilledBeforeTheCallComesBackInTheSquadCopy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            EnterPlateau(harness);
            var copy = IntoMaligo(harness);
            Assert.IsNotNull(copy.SquadState, "a squad's copy");
            Kill(harness, copy, Officer(harness, copy, PlateauMaligoBaseV1.Zyrutha));

            harness.Manager.OfferArrivalMissions(harness.Client);
            harness.Drain();
            Assert.IsTrue(harness.Manager.TryAcceptRadioMission(harness.Client, Showdown));
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, Showdown, PlateauShowdownV1.Officer));

            var corpse = Kill(harness, copy, BackAfterAMinute(harness, copy, PlateauMaligoBaseV1.Zyrutha));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Showdown, PlateauShowdownV1.Officer));
            Assert.IsNotNull(corpse, "Zyrutha leaves a corpse");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, Showdown, PlateauShowdownV1.Keypass));
        }

        /// <summary>
        /// The officer's pool, run on its own a second at a time (the copy's other pools left out, as Spawn does): not back
        /// after 50 s, back by 70 s (its respawn is 60 s, PlateauMaligoOfficerSquadRespawnV1).
        /// </summary>
        private static Creature BackAfterAMinute(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool)
        {
            Creature Standing() => Here(copy).SingleOrDefault(creature => creature.DbId == pool && creature.State != CharacterState.Dead);
            var all = copy.SpawnPools.ToArray();
            try
            {
                copy.SpawnPools.Clear();
                copy.SpawnPools.Add(all.Single(candidate => candidate.DbId == pool));
                for (var second = 1; second <= 70; second++)
                {
                    harness.UtcNow = harness.UtcNow.AddSeconds(1);
                    SpawnPoolManager.Instance.SpawnPoolWorker(copy, 1000);
                    if (second == 50)
                        Assert.IsNull(Standing(), $"{pool} not back before its minute");
                }
            }
            finally
            {
                copy.SpawnPools.Clear();
                copy.SpawnPools.AddRange(all);
            }
            harness.Drain();
            var officer = Standing();
            Assert.IsNotNull(officer, $"{pool} back in the squad's copy after its 60 s");
            return officer;
        }

        /// <summary>Bosley at Camp Resistance; 877 offered after Blueprint For Victory, the four rewards, accepted.</summary>
        private static Creature AcceptOneWayTrip(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            Spawn(plateau, PlateauGiantKillerV1.Bosley);
            var bosley = RequireNpc(plateau, PlateauGiantKillerV1.Bosley);
            Assert.IsFalse(Offers(harness, bosley, OneWayTrip), "it waits for Blueprint For Victory");
            SeedCompleted(harness, PlateauBlueprintForVictoryV1.MissionId);
            var offer = WildernessRanjaGorgeTests.Offer(harness, bosley, OneWayTrip);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauOneWayTripV1.BioGloves, PlateauOneWayTripV1.GravitonHelmet,
                    PlateauOneWayTripV1.MechGloves, PlateauOneWayTripV1.StealthGloves
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Accept(harness, bosley, OneWayTrip);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Rapp));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Bridge));
            Assert.IsTrue(harness.Manager.Scenes.TryGetSceneMap(OneWayTrip, out var bound));
            Assert.AreEqual(Maligo, bound);
            return bosley;
        }

        /// <summary>
        /// Rapp in Ustor (10), the bridge (9), into Maligo: Neuman (6, 11), the beams (1), Kythura's keypass (7), the
        /// computer bank's powercube (14), the socket (4); Maybauer there.
        /// </summary>
        private static (MapChannel Copy, Creature Maybauer) ToMaybauer(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            var copy = ToMaligo(harness, plateau);
            var neuman = Npc(copy, PlateauMaligoBaseV1.Neuman);
            Talk(harness, neuman, OneWayTrip, PlateauOneWayTripV1.Massacre);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.HowTo));
            Talk(harness, neuman, OneWayTrip, PlateauOneWayTripV1.HowTo);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Keypass));

            var beams = ToVector(PlateauOneWayTripV1.BeamsAt);
            Enter(harness, beams + new Vector3(40, 0, 0), beams, "the second beams");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Locate));

            Tick(harness, copy, 250);
            Assert.IsNull(FindSceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.BankRole), "no computer bank before the keypass");
            var corpse = Kill(harness, copy, Officer(harness, copy, PlateauMaligoBaseV1.Kythura));
            Assert.IsNotNull(corpse, "Kythura leaves a corpse");
            Assert.IsTrue(corpse.LootItems.Any(item => item.ItemTemplateId == PlateauOneWayTripV1.Pass), "the keypass on Kythura");
            PickUp(harness, corpse);
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Keypass));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Powercube));
            Tick(harness, copy, 250);

            var bank = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.BankRole);
            Assert.AreEqual(PlateauOneWayTripV1.BankClass, (uint)bank.EntityClassId);
            AssertStandsOnNavMesh(copy, bank.Position, 0.5f);
            Assert.IsNull(FindSceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.SocketRole), "no socket before the powercube");
            Use(harness, copy, bank, PlateauOneWayTripV1.UseArgument);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauOneWayTripV1.Cube));
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Powercube));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.PowerDown));
            Tick(harness, copy, 250);

            var socket = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.SocketRole);
            Assert.AreEqual(PlateauOneWayTripV1.SocketClass, (uint)socket.EntityClassId);
            AssertStandsOnNavMesh(copy, socket.Position, 0.5f);
            Assert.IsNull(SceneCreature(harness, copy, OneWayTrip, PlateauOneWayTripV1.MaybauerRole), "no Maybauer before the socket");
            Use(harness, copy, socket, PlateauOneWayTripV1.UseArgument);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauOneWayTripV1.Cube), "the powercube in the socket");
            Assert.AreEqual(PlateauOneWayTripV1.SocketSetState, (uint)socket.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.PowerDown));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Maybauer));
            Assert.AreEqual(MissionObjectiveState.Inactive, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Destroy));
            Tick(harness, copy, 250);

            var maybauer = SceneCreature(harness, copy, OneWayTrip, PlateauOneWayTripV1.MaybauerRole);
            Assert.IsNotNull(maybauer, "Maybauer comes");
            Assert.AreEqual(PlateauMaligoBaseV1.MaybauerName, maybauer.NameId);
            Assert.AreEqual(PlateauMaligoBaseV1.MaybauerPackage, maybauer.Npc.NpcPackageId);
            AssertStandsOnNavMesh(copy, maybauer.Position);
            Assert.IsNull(FindSceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.TerminalRole), "no terminal before Maybauer's talk");
            return (copy, maybauer);
        }

        /// <summary>Rapp in Ustor (10), the bridge (9), into Maligo.</summary>
        private static MapChannel ToMaligo(WildernessRuntimeTestHarness harness, MapChannel plateau)
        {
            var ustor = IntoUstor(harness);
            if (!Here(ustor).Any(creature => creature.DbId == PlateauUstorYardV1.Rapp))
                Spawn(ustor, PlateauUstorYardV1.RappPool);
            var rapp = Here(ustor).First(creature => creature.SpawnPool?.DbId == PlateauUstorYardV1.RappPool && creature.Npc != null);
            Talk(harness, rapp, OneWayTrip, PlateauOneWayTripV1.Rapp);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Bridge));

            Return(harness, plateau);   // through the west exit onto the bridge
            var onto = harness.World.MapLinkEntries.AsNoTracking().Single(row => row.Id == PlateauMaligoBaseV1.OntoBridge);
            var onBridge = new Vector3((float)onto.DestPosX, (float)onto.DestPosY, (float)onto.DestPosZ);
            var bridge = PlateauOneWayTripV1.BridgeAt;
            Enter(harness, onBridge, new Vector3((float)bridge.X, (float)bridge.Y, (float)bridge.Z), "the bridge's Maligo end");
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Bridge));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Locate));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(harness, OneWayTrip, PlateauOneWayTripV1.Massacre));

            var copy = IntoMaligo(harness);
            Assert.IsFalse(Here(plateau).Concat(Here(ustor)).Any(creature => creature.DbId is PlateauMaligoBaseV1.Maybauer or PlateauMaligoBaseV1.Homburg),
                "nothing of the scene outside Maligo");
            return copy;
        }

        /// <summary>The four Mind Controlled Soldiers in the chamber, on their walk to the device; none hostile.</summary>
        private static Creature[] Prisoners(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            var prisoners = Enumerable.Range(0, PlateauOneWayTripV1.PrisonerLanes.Length)
                .Select(index => SceneCreature(harness, copy, OneWayTrip, PlateauOneWayTripV1.PrisonerRole(index))).ToArray();
            for (var index = 0; index < prisoners.Length; index++)
            {
                Assert.IsNotNull(prisoners[index], $"prisoner {index + 1} comes");
                Assert.AreEqual(PlateauMaligoBaseV1.MindControlledName, prisoners[index].NameId);
                Assert.AreNotEqual(TargetCategory.Hostile, prisoners[index].TargetCategory);
                AssertStandsOnNavMesh(copy, prisoners[index].Position);
            }
            return prisoners;
        }

        /// <summary>The terminal by the socket: a passage inside the squad's copy (SceneWorldAdapter), to the chamber.</summary>
        private static void IntoTheChamber(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            var terminal = SceneObject(harness, copy, OneWayTrip, PlateauOneWayTripV1.TerminalRole);
            Assert.AreEqual(PlateauOneWayTripV1.TerminalClass, (uint)terminal.EntityClassId);
            AssertStandsOnNavMesh(copy, terminal.Position, 0.5f);
            Use(harness, copy, terminal, PlateauOneWayTripV1.UseArgument);
            Tick(harness, copy, 250);
            var transfer = harness.Client.PendingTransfer;
            Assert.IsNotNull(transfer, "the terminal takes the player");
            Assert.IsTrue(transfer.IsPassage);
            Assert.AreSame(copy, transfer.DestinationMap, "inside the squad's copy");
            Assert.IsLessThan(2f, Vector3.Distance(ToVector(PlateauOneWayTripV1.ChamberArrival), transfer.DestinationPosition), "to the chamber");
            // The passage's hold runs on the wall clock (DynamicObjectManager.PassageHoldMs): the player is set down as it lands.
            harness.Client.PendingTransfer = null;
            harness.MoveTo(ToVector(PlateauOneWayTripV1.ChamberArrival));
            harness.Drain();
        }

        /// <summary>
        /// The deadline's 15 minutes and a little more run on the harness clock a second at a time, as the map's
        /// think walks the prisoners (one second at most a tick), the deadlines evaluated.
        /// </summary>
        private static void PastTheDeadline(WildernessRuntimeTestHarness harness, MapChannel copy)
        {
            for (var second = 0; second < PlateauOneWayTripV1.DeadlineSeconds + 5; second++)
                Tick(harness, copy, 1000);
            harness.Manager.EvaluateDeadlines(harness.Client);
            harness.Drain();
            Tick(harness, copy, 250);
        }

        private static MapChannel IntoMaligo(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Maligo))
                LoadMap(harness, Maligo);
            var copy = ThroughTheDoor(harness, Maligo);
            Assert.IsTrue(copy.IsSquadInstance);
            harness.Manager.Scenes.Resume(harness.Client);   // as MapChannelManager.MapLoaded does (ResumeMissionScenes)
            harness.Drain();
            return copy;
        }

        private static MapChannel IntoUstor(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Ustor))
                LoadMap(harness, Ustor);
            var copy = ThroughTheDoor(harness, Ustor);
            Assert.IsTrue(copy.IsSquadInstance);
            return copy;
        }

        /// <summary>Out of the copy to the Plateau.</summary>
        private static void Leave(WildernessRuntimeTestHarness harness, MapChannel copy, MapChannel plateau)
        {
            harness.Manager.Scenes.Detach(harness.Client, copy);
            Return(harness, plateau);
        }

        /// <summary>The NPC of the pool in this copy, spawned if it is not there.</summary>
        private static Creature Npc(MapChannel copy, uint pool)
        {
            if (!Here(copy).Any(creature => creature.SpawnPool?.DbId == pool))
                Spawn(copy, pool);
            var npc = Here(copy).FirstOrDefault(creature => creature.SpawnPool?.DbId == pool && creature.Npc != null);
            Assert.IsNotNull(npc, $"{pool} stands in the copy");
            return npc;
        }

        /// <summary>
        /// The officer of the pool in this copy. An earlier test can have killed it in the squad's copy: its pool is
        /// back as after its respawn.
        /// </summary>
        private static Creature Officer(WildernessRuntimeTestHarness harness, MapChannel copy, uint pool)
        {
            var officer = Here(copy).FirstOrDefault(creature => creature.DbId == pool);
            if (officer != null)
                return officer;
            // Its respawn over: by the copy's clock, and by its own (the officers come back on their own time, to-fix #18).
            var spawnPool = copy.SpawnPools.Single(candidate => candidate.DbId == pool);
            spawnPool.ClearedAtUtcMs = 0;
            spawnPool.UpdateTimer = spawnPool.RespawnTime;
            Spawn(copy, pool);
            officer = Here(copy).FirstOrDefault(creature => creature.DbId == pool);
            Assert.IsNotNull(officer, $"{pool} stands in the copy");
            return officer;
        }

        /// <summary>The NPC hears the objective (only that one), and it is done.</summary>
        private static void Talk(WildernessRuntimeTestHarness harness, Creature npc, uint mission, uint objective)
        {
            AssertHandInTopic(Conversation(harness, npc), mission, objective);
            harness.MoveTo(npc.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, npc.EntityId, mission, objective, 1), $"{mission}.{objective}");
            harness.Drain();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(harness, mission, objective));
        }

        /// <summary>A step into a mission area, as MissionAreaService takes it.</summary>
        private static void Enter(WildernessRuntimeTestHarness harness, Vector3 from, Vector3 to, string what)
        {
            harness.MoveTo(to);
            Assert.IsTrue(new MissionAreaService(() => harness.Manager).RecordAcceptedMovement(harness.Client, from, to), $"{what} reached");
            harness.Drain();
        }

        private static IEnumerable<Creature> Here(MapChannel map) =>
            Living(map).Where(creature => ReferenceEquals(creature.RuntimeMapChannel, map));

        private static bool Offers(WildernessRuntimeTestHarness harness, Creature npc, uint mission) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered) &&
            ((Dictionary<uint, MissionInfo>)offered).ContainsKey(mission);

        private static void Accept(WildernessRuntimeTestHarness harness, Creature npc, uint mission)
        {
            harness.MoveTo(npc.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, npc.EntityId, mission));
            harness.Drain();
        }

        private static MissionObjectiveState Objective(WildernessRuntimeTestHarness harness, uint mission, uint objective) =>
            harness.Client.Player.Missions[mission].Objectives[objective].State;

        private static DynamicObject SceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var obj = FindSceneObject(harness, map, mission, role);
            Assert.IsNotNull(obj, $"{role} is there");
            return obj;
        }

        /// <summary>The object of this test's own run: a squad copy can still hold an earlier test's (see Here).</summary>
        private static DynamicObject FindSceneObject(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return map.DynamicObjects.SingleOrDefault(candidate => candidate.SceneMissionId == mission && candidate.SceneActorRole == role &&
                candidate.IsInWorld && runs.Contains(candidate.SceneRunId));
        }

        /// <summary>The scene creature of this test's own run (a squad copy can still hold an earlier test's, see Here).</summary>
        private static Creature SceneCreature(WildernessRuntimeTestHarness harness, MapChannel map, uint mission, string role)
        {
            var runs = Runs(harness, mission);
            return Here(map).SingleOrDefault(creature => creature.SpawnPool?.SceneActorRole == role && runs.Contains(creature.SpawnPool.SceneRunId));
        }

        private static HashSet<string> Runs(WildernessRuntimeTestHarness harness, uint mission)
        {
            using var unit = harness.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(harness.Client.Player.Id, mission).Select(scene => scene.RunId).ToHashSet();
        }

        /// <summary>Kills the creature as the player and runs the map on; returns its corpse, if it has one.</summary>
        private static LootDispenser Kill(WildernessRuntimeTestHarness harness, MapChannel map, Creature creature)
        {
            harness.MoveTo(creature.Position);
            creature.Attributes[Attributes.Health].Current = 0;
            harness.Creatures.HandleCreatureKill(map, creature, harness.Client.Player);
            Tick(harness, map, 250);
            return map.LootDispensers.TryGetValue(creature.CorpseLootEntityId, out var loot) ? loot : null;
        }

        private static Vector3 ToVector(Rasa.Missions.Scenes.ScenePosition at) => new(at.X, at.Y, at.Z);

        private static Vector3 ToVector((double X, double Y, double Z) at) => new((float)at.X, (float)at.Y, (float)at.Z);

        private static System.Collections.Generic.List<Rasa.Structures.World.MissionIndicatorEntry> Markers(WildernessRuntimeTestHarness harness, uint mission) =>
            harness.World.MissionIndicatorEntries.AsNoTracking().Where(row => row.MissionId == mission).ToList();

        /// <summary>A marker of <paramref name="objective"/> within <paramref name="tolerance"/> m (flat) of <paramref name="at"/>.</summary>
        private static void AssertMarker(WildernessRuntimeTestHarness harness, uint mission, uint objective, Vector3 at, float tolerance, string what) =>
            Assert.IsTrue(Markers(harness, mission).Any(row => row.ObjectiveId == objective &&
                Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new(at.X, at.Z)) < tolerance),
                $"{mission} objective {objective}'s marker at {what} ({at})");

        private static float Flat(Vector3 position, Rasa.Structures.World.MapLinkEntry link) =>
            Vector2.Distance(new(position.X, position.Z), new((float)link.PosX, (float)link.PosZ));

        private static bool Connected(MapChannel map, Vector3 from, Vector3 to) =>
            map.NavMesh.FindPath(from, to, out var complete) != null && complete;

        private static void AssertConnected(MapChannel map, Vector3 from, Vector3 to, string what) =>
            Assert.IsTrue(Connected(map, from, to), $"{what} ({to}) is walked to from {from}");

        private static void AssertStandsOnNavMesh(MapChannel map, Vector3 position, float tolerance = 1f)
        {
            var ground = map.NavMesh.GroundHeight(position);
            Assert.IsNotNull(ground, $"navmesh under {position}");
            Assert.AreEqual(ground.Value, position.Y, tolerance, $"{position} on the ground");
        }

        /// <summary>As PlateauUstorYardTests.Tick.</summary>
        private static void Tick(WildernessRuntimeTestHarness harness, MapChannel map, long milliseconds)
        {
            harness.UtcNow = harness.UtcNow.AddMilliseconds(milliseconds);
            BehaviorManager.Instance.MapChannelThink(map, System.Math.Min(milliseconds, 1000));
            harness.Manager.Scenes.Tick(map);
            harness.Drain();
        }

        /// <summary>A weapon hit on the object, as PlateauUstorYardTests.Damage.</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Drain();
        }

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauUstorYardTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, uint argument)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = argument
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }
    }
}
