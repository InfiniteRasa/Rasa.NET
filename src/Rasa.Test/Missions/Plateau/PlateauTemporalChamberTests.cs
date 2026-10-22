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
using Rasa.Packets.Mission.Server;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Plateau
{
    using static PlateauCampResistanceTests;
    using static PlateauTrinityBridgeLogosTests;

    /// <summary>
    /// The Temporal Chamber's first three missions, played through as a player plays them: Array Of Hope
    /// (935: the six Logos Conduits on Trinity Bridge, then Veryin beyond the portal), Future Sacrifice
    /// (1171: through the chamber's teleporter to Colonel Hudson in Alia Das) and The Final Assault (1167:
    /// three stations, then the detonator on the power core, turned in to Veryin).
    ///
    /// Why these tests: the conduits are personal objects on the public Plateau, but Veryin and Hudson are
    /// the server's NPCs of the squad instance 2029, which MapChannelManager copies as the player goes
    /// through the portal (map_link 49). A talk to Veryin that did not wait for the conduits, a copy without
    /// the teleporter pair, or a core that took the detonator before the three stations would each break the
    /// chain; so the door is walked as MapLinkManager walks it, the teleporter as LocalTeleporterWorker takes
    /// a player, and the stations are shot as Toxic Detox's pumps (PlateauMartyrsCanyonTests).
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauTemporalChamberTests
    {
        private const uint ArrayOfHope = PlateauArrayOfHopeV1.MissionId;
        private const uint FutureSacrifice = PlateauFutureSacrificeV1.MissionId;
        private const uint FinalAssault = PlateauFinalAssaultV1.MissionId;
        private const uint Chamber = PlateauTemporalChamberNpcsV1.MapContextId;

        [TestMethod]
        public void ArrayOfHopeWaitsForTheSixLogosAndVeryinWaitsForTheSixConduits()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, PlateauVoiceFromThePastV1.MissionId, PlateauTrinityLogosPastV1.MissionId, 1047, 1048, 1049, 1051);
            var hologram = Hologram(plateau);
            Assert.IsFalse(Offered(harness, hologram).Contains(ArrayOfHope), "waits for the sixth Logos mission");
            SeedCompleted(harness, 1057);

            var offer = WildernessRanjaGorgeTests.Offer(harness, hologram, ArrayOfHope);
            CollectionAssert.AreEqual(new[]
                {
                    (PlateauArrayOfHopeV1.StandardMedPack, 3U), (PlateauArrayOfHopeV1.IncendiaryGrenade, 3U),
                    (PlateauArrayOfHopeV1.AdvancedMedPack, 2U), (PlateauArrayOfHopeV1.EmpGrenade, 3U)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            Assert.IsFalse(plateau.DynamicObjects.Any(obj => obj.SceneMissionId == ArrayOfHope), "no conduit before the mission");
            harness.MoveTo(hologram.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hologram.EntityId, ArrayOfHope));
            harness.Drain();

            var conduits = Conduits(plateau);
            for (var index = 0; index < conduits.Length; index++)
            {
                Assert.AreEqual(PlateauArrayOfHopeV1.ConduitClass, (uint)conduits[index].EntityClassId);
                Assert.AreEqual(PlateauArrayOfHopeV1.ReadyState, (uint)conduits[index].StateId);
                var ground = plateau.NavMesh.GroundHeight(conduits[index].Position);
                Assert.IsNotNull(ground, $"{conduits[index].SceneActorRole} has no navmesh under it");
                Assert.AreEqual(ground.Value, conduits[index].Position.Y, 0.5f, $"{conduits[index].SceneActorRole} on the bridge");
            }

            // Through the portal before the conduits: Veryin has nothing to say yet.
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            harness.MoveTo(veryin.Position);
            harness.Drain();
            Assert.IsFalse(harness.Manager.CompleteOfferedObjective(harness.Client, veryin.EntityId, ArrayOfHope, 1, 1),
                "Veryin waits for the conduits");
            Return(harness, plateau);

            for (var index = 0; index < conduits.Length; index++)
            {
                Assert.AreNotEqual(MissionObjectiveState.Completed,
                    harness.Client.Player.Missions[ArrayOfHope].Objectives[PlateauArrayOfHopeV1.Tally].State, $"before conduit {index + 1}");
                Use(harness, plateau, conduits[index]);
                Assert.AreEqual(PlateauArrayOfHopeV1.UsedState, (uint)conduits[index].StateId, $"conduit {index + 1} on");
            }
            Use(harness, plateau, conduits[0]);
            Assert.AreEqual(MissionObjectiveState.Completed,
                harness.Client.Player.Missions[ArrayOfHope].Objectives[PlateauArrayOfHopeV1.Tally].State, "the six conduits");
            Assert.IsFalse(harness.Client.Player.Missions[ArrayOfHope].Completeable);

            copy = IntoTheChamber(harness);
            veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            AssertHandInTopic(Conversation(harness, veryin), ArrayOfHope, 1);
            harness.MoveTo(veryin.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, veryin.EntityId, ArrayOfHope, 1, 1), "Who comes here?");
            harness.Drain();
            Assert.IsTrue(harness.Client.Player.Missions[ArrayOfHope].Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, veryin, ArrayOfHope, 1,
                PlateauArrayOfHopeV1.Experience, PlateauArrayOfHopeV1.Credits);
            Assert.AreEqual(3U, WildernessRanjaGorgeTests.Held(harness, PlateauArrayOfHopeV1.IncendiaryGrenade), "the second choice");
        }

        [TestMethod]
        public void VeryinSendsThePlayerThroughTheTeleporterToHudsonWhoGivesTheFinalAssault()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            var hudson = Npc(copy, PlateauTemporalChamberNpcsV1.Hudson, PlateauTemporalChamberNpcsV1.HudsonPackage);

            var offer = WildernessRanjaGorgeTests.Offer(harness, veryin, FutureSacrifice);
            CollectionAssert.AreEqual(new[]
                {
                    (PlateauFutureSacrificeV1.ResKit, 1U), (PlateauFutureSacrificeV1.ConcussionGrenade, 3U),
                    (PlateauFutureSacrificeV1.AdvancedMedPack, 6U), (PlateauFutureSacrificeV1.FragGrenade, 3U)
                },
                offer.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
            harness.MoveTo(veryin.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, veryin.EntityId, FutureSacrifice));
            harness.Drain();

            // The pad on Veryin's right takes the player to Hudson's.
            var pad = copy.Teleporters[PlateauTemporalChamberNpcsV1.ChamberPad];
            var far = copy.Teleporters[PlateauTemporalChamberNpcsV1.AliaDasPad];
            Assert.AreNotSame(harness.Objects.Teleporters[PlateauTemporalChamberNpcsV1.ChamberPad], pad, "the copy's own pad");
            Teleport(harness, copy, pad);
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, far.Position) < 2f, "at the Alia Das pad");
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, hudson.Position) < 15f, "by Hudson");

            AssertHandInTopic(Conversation(harness, hudson), FutureSacrifice, 1);
            harness.MoveTo(hudson.Position);
            harness.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(harness.Client, hudson.EntityId, FutureSacrifice, 1, 1));
            harness.Drain();
            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, hudson, FutureSacrifice, 2,
                PlateauFutureSacrificeV1.Experience, PlateauFutureSacrificeV1.Credits);
            Assert.AreEqual(6U, WildernessRanjaGorgeTests.Held(harness, PlateauFutureSacrificeV1.AdvancedMedPack), "the third choice");

            WildernessRanjaGorgeTests.Offer(harness, hudson, FinalAssault);
            harness.Objects.DynamicObjectProximityWorker(copy, far, 0);   // off the pad they arrived on, by Hudson
            Teleport(harness, copy, far);
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, pad.Position) < 2f, "and back to the chamber");
        }

        [TestMethod]
        public void TheFinalAssaultTakesThreeStationsBeforeTheCoreTakesTheDetonator()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var plateau = EnterPlateau(harness);
            SeedCompleted(harness, ArrayOfHope, FutureSacrifice);
            var copy = IntoTheChamber(harness);
            var veryin = Npc(copy, PlateauTemporalChamberNpcsV1.Veryin, PlateauTemporalChamberNpcsV1.VeryinPackage);
            var hudson = Npc(copy, PlateauTemporalChamberNpcsV1.Hudson, PlateauTemporalChamberNpcsV1.HudsonPackage);
            Assert.IsFalse(copy.DynamicObjects.Any(obj => obj.SceneMissionId == FinalAssault), "nothing before the mission");

            var offer = WildernessRanjaGorgeTests.Offer(harness, hudson, FinalAssault);
            CollectionAssert.AreEqual(new[]
                {
                    PlateauFinalAssaultV1.RocketLauncher, PlateauFinalAssaultV1.Rifle,
                    PlateauFinalAssaultV1.IncendiaryNetGun, PlateauFinalAssaultV1.PulsePistol
                },
                offer.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            harness.MoveTo(hudson.Position);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, hudson.EntityId, FinalAssault));
            harness.Drain();
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.Detonator), "Hudson's detonator");

            var stations = PlateauFinalAssaultV1.Stations.Select(station => copy.DynamicObjects.Single(obj =>
                obj.SceneMissionId == FinalAssault && obj.SceneActorRole == PlateauFinalAssaultV1.StationRole(station.Objective))).ToArray();
            var core = copy.DynamicObjects.Single(obj => obj.SceneMissionId == FinalAssault && obj.SceneActorRole == PlateauFinalAssaultV1.CoreRole);
            Assert.AreEqual(PlateauFinalAssaultV1.CoreClass, (uint)core.EntityClassId);
            foreach (var station in stations)
            {
                Assert.AreEqual(PlateauFinalAssaultV1.IntactState, (uint)station.StateId);
                var ground = copy.NavMesh.GroundHeight(station.Position);
                Assert.IsNotNull(ground, $"{station.SceneActorRole} has no navmesh under it");
                Assert.AreEqual(ground.Value, station.Position.Y, 0.5f, $"{station.SceneActorRole} on the ground");
            }

            var mission = harness.Client.Player.Missions[FinalAssault];
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[PlateauFinalAssaultV1.Detonate].State);
            Use(harness, copy, core);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.Detonator), "the core is locked");
            Assert.AreEqual(PlateauFinalAssaultV1.CoreReadyState, (uint)core.StateId);

            var objectives = new[] { PlateauFinalAssaultV1.Alpha, PlateauFinalAssaultV1.Bravo, PlateauFinalAssaultV1.Charlie };
            for (var index = 0; index < stations.Length; index++)
            {
                Damage(harness, copy, stations[index], (int)PlateauFinalAssaultV1.StationHitPoints);
                Assert.AreEqual(PlateauFinalAssaultV1.DestroyedState, (uint)stations[index].StateId, $"station {index + 1} down");
                mission = harness.Client.Player.Missions[FinalAssault];
                Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[objectives[index]].State, $"objective {objectives[index]}");
                Assert.AreEqual(index == 2 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive,
                    mission.Objectives[PlateauFinalAssaultV1.Detonate].State, $"the core after station {index + 1}");
            }

            Use(harness, copy, core);
            Assert.AreEqual(0U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.Detonator), "the detonator on the core");
            Assert.AreEqual(PlateauFinalAssaultV1.CoreSetState, (uint)core.StateId);
            mission = harness.Client.Player.Missions[FinalAssault];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[PlateauFinalAssaultV1.Detonate].State);
            Assert.IsTrue(mission.Completeable);

            WildernessRanjaGorgeTests.CompleteAndCheckReward(harness, veryin, FinalAssault, 0,
                PlateauFinalAssaultV1.Experience, PlateauFinalAssaultV1.Credits);
            Assert.AreEqual(1U, WildernessRanjaGorgeTests.Held(harness, PlateauFinalAssaultV1.RocketLauncher), "the first choice");
        }

        /// <summary>
        /// The chamber as the server loads it (LoadMap), with its teleporters as InitTeleporters reads them and
        /// the links as InitLocalTeleporters does, then the player through the portal into the squad's copy.
        /// </summary>
        private static MapChannel IntoTheChamber(WildernessRuntimeTestHarness harness)
        {
            if (!harness.Maps.MapChannelArray.ContainsKey(Chamber))
            {
                var template = LoadMap(harness, Chamber);
                foreach (var row in harness.World.TeleporterEntries.AsNoTracking().Where(row => row.MapContextId == Chamber && row.Type == 1).ToArray())
                {
                    var pad = new DynamicObject
                    {
                        Position = new Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                        Rotation = row.Rotation,
                        MapContextId = Chamber,
                        RuntimeMapChannel = template,
                        EntityClassId = (EntityClasses)row.ClassId,
                        Comment = row.Description,
                        ObjectData = new WaypointInfo(row.Id, false, WaypointType.LocalTeleporter),
                        DynamicObjectType = DynamicObjectType.LocalTeleporter
                    };
                    template.Teleporters.Add(row.Id, pad);
                    harness.Objects.Teleporters.Add(row.Id, pad);
                }
                harness.Objects.InitLocalTeleporters();
                harness.Objects.DynamicObjectWorker(template, 0);
            }
            var copy = ThroughTheDoor(harness, Chamber);
            Assert.IsTrue(copy.IsSquadInstance);
            return copy;
        }

        private static Creature Npc(MapChannel map, uint creature, uint package)
        {
            if (!Living(map).Any(candidate => candidate.DbId == creature))
                Spawn(map, creature == PlateauTemporalChamberNpcsV1.Veryin ? PlateauTemporalChamberNpcsV1.VeryinPool : PlateauTemporalChamberNpcsV1.HudsonPool);
            var npc = Living(map).Single(candidate => candidate.DbId == creature && candidate.Npc != null);
            Assert.AreEqual(package, npc.Npc.NpcPackageId);
            return npc;
        }

        private static HashSet<uint> Offered(WildernessRuntimeTestHarness harness, Creature npc) =>
            Conversation(harness, npc).TryGetValue(ConversationType.MissionDispense, out var offered)
                ? ((Dictionary<uint, MissionInfo>)offered).Keys.ToHashSet()
                : new HashSet<uint>();

        private static DynamicObject[] Conduits(MapChannel plateau) => Enumerable.Range(0, PlateauArrayOfHopeV1.Conduits.Length)
            .Select(index => plateau.DynamicObjects.Single(obj => obj.SceneMissionId == ArrayOfHope &&
                obj.SceneActorRole == PlateauArrayOfHopeV1.ConduitRole(index)))
            .ToArray();

        /// <summary>A use of the object from where it stands, and its recovery if one is due (PlateauChasmTests.Use).</summary>
        private static void Use(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            harness.Drain();
            var count = map.PerformRecovery.Count;
            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = obj.EntityId, ActionId = ActionId.UseObject, ActionArgId = 1
            });
            if (map.PerformRecovery.Count > count)
            {
                var recovery = map.PerformRecovery.Last();
                map.PerformRecovery.Remove(recovery);
                ActorActionManager.Instance.PerformRecovery(map, recovery);
            }
            harness.Drain();
        }

        /// <summary>A weapon hit on the object, as PlateauMartyrsCanyonTests.DamagePump.</summary>
        private static void Damage(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject obj, int damage)
        {
            harness.MoveTo(obj.Position + new Vector3(0, 0, 1));
            var action = new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, 0) { TargetId = obj.EntityId };
            MissileManager.Instance.MissileLaunch(map, action, damage);
            MissileManager.Instance.DoWork(map, 1000);
            harness.Drain();
        }

        /// <summary>Onto the pad, as LocalTeleporterWorker takes a player, and the passage through.</summary>
        private static void Teleport(WildernessRuntimeTestHarness harness, MapChannel map, DynamicObject pad)
        {
            harness.MoveTo(pad.Position);
            harness.Drain();
            harness.Objects.DynamicObjectProximityWorker(map, pad, 0);
            Assert.IsNotNull(harness.Client.PendingTransfer, "taken by the pad");
            System.Threading.Thread.Sleep((int)DynamicObjectManager.PassageHoldMs + 50);
            harness.Objects.PassagesWorker(map);
            harness.Objects.TeleportAcknowledge(harness.Client);
            harness.Drain();
            Assert.IsNull(harness.Client.PendingTransfer);
        }
    }
}
