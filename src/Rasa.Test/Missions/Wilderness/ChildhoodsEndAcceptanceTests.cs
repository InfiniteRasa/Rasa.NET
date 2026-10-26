using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass, DoNotParallelize]
    public class ChildhoodsEndAcceptanceTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CouncilHandoffRejectsMissingRemoteRangerOnlyWithLegacyBinding(bool legacy)
        {
            using var h = WildernessRuntimeTestHarness.Create(additionalContent: legacy ? migration =>
            {
                foreach (var sql in Rasa.Services.Preloader.ChildhoodsEndPersonalEncounter.Down()) migration.Sql(sql);
            } : null);
            WildernessRanjaGorgeTests.SetQualification(h, h.Client.Player.Id,
                Rasa.Structures.Char.CharacterStartingExperienceState.Completed);
            h.SpawnWorld(170, 174, 175);
            var nula = h.Npc(170); var todae = h.Npc(174); var doyan = h.Npc(175);
            WildernessDaghdasUrnTests.SeedHistory(h, 679);
            h.MoveTo(nula.Position);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, nula.EntityId, 451));
            h.MoveTo(doyan.Position);
            Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, doyan.EntityId, 451, 1, 1));
            h.MoveTo(todae.Position);
            Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, todae.EntityId, 451, 2, 1));
            Assert.IsTrue(h.Manager.CompleteOfferedMission(h.Client, todae.EntityId, 451, 0));
            Assert.IsNull(h.Npc(176), "Reproduce acceptance while the remote public ranger is missing.");
            var npcs = new NpcManager(h, h.Manager);
            npcs.RequestNpcConverse(h.Client, new RequestNPCConversePacket { EntityId = todae.EntityId });
            Assert.IsTrue(h.Drain().OfType<ConversePacket>().Any());
            npcs.AssignNPCMission(h.Client, new AssignNPCMissionPacket { NpcEntityId = todae.EntityId, MissionId = 682 });
            Assert.AreEqual(!legacy, h.Client.Player.Missions.ContainsKey(682),
                "The legacy remote reservation fails acceptance; personal Anjuhi must remove that dependency.");
            if (!legacy)
            {
                Assert.AreEqual(MissionState.Active, h.Client.Player.Missions[682].State);
                Assert.IsNotNull(Ranger(h, h.Client.Player.Id));
            }
        }

        private static Rasa.Structures.Creature Ranger(WildernessRuntimeTestHarness h, uint owner) =>
            WildernessDaghdasUrnTests.Creatures(h).Single(c => c.SpawnPool?.ScenarioMissionId == 682 &&
                c.SpawnPool.SceneActorRole == "anjuhi" && c.SpawnPool.ScenarioOwnerCharacterId == owner &&
                c.State != CharacterState.Dead);

        [TestMethod]
        public void ConcurrentPersonalDefensesCompleteIndependentlyAndRestoreThePublicContact()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            h.SpawnWorld(174, 175, 176, 177, 580053);
            var todae = h.Npc(174); var publicRanger = h.Npc(176);
            WildernessDaghdasUrnTests.SeedHistory(h, 451);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, todae.EntityId, 682));
            var first = Ranger(h, h.Client.Player.Id);
            var second = h.Context.CreateAdditionalClient(2, manager: h.Manager);
            try
            {
                using (var unit = h.CreateChar())
                {
                    unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Archive(new Rasa.Structures.Char.CharacterMissionEntry(
                        second.Player.Id, 451, (uint)MissionState.Completed)
                        { ContentRevision = Rasa.Services.Preloader.Missions.Wilderness.WildernessMissionDataV1.Revision }, h.UtcNow));
                    h.Manager.HydrateAndClearInvalid(second.Player, unit);
                }
                Assert.IsTrue(h.Manager.AcceptOfferedMission(second, todae.EntityId, 682));
                var other = Ranger(h, second.Player.Id);
                Assert.AreNotEqual(first.EntityId, other.EntityId);
                Assert.IsNull(h.Manager.PublicActors.Handle(h.Map, 176));
                Assert.IsFalse(MissionNpcVisibility.IsShownTo(second, first));
                Assert.IsFalse(MissionNpcVisibility.IsShownTo(h.Client, other));
                Assert.IsFalse(MissionNpcVisibility.IsShownTo(h.Client, publicRanger));
                Assert.IsFalse(MissionNpcVisibility.IsShownTo(second, publicRanger));
                Assert.IsFalse(h.Manager.CompleteOfferedObjective(second, first.EntityId, 682, 2, 1));
                Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, first.EntityId, 682, 2, 1));
                Assert.IsTrue(h.Manager.CompleteOfferedObjective(second, other.EntityId, 682, 2, 1));
                var attackers = WildernessDaghdasUrnTests.Creatures(h).Where(c => c.DbId == 87 &&
                    c.SpawnPool?.ScenarioMissionId == 682 && c.SpawnPool.ScenarioOwnerCharacterId == h.Client.Player.Id).ToArray();
                Assert.AreEqual(3, attackers.Length);
                Assert.IsTrue(attackers.All(c => !MissionNpcVisibility.IsShownTo(second, c)));
                WildernessDaghdasUrnTests.Kill(h, WildernessDaghdasUrnTests.LiveCreature(h, 87, 580053));
                Assert.AreEqual(0U, h.Client.Player.Missions[682].Objectives[3].Counters[0]);
                foreach (var attacker in attackers) WildernessDaghdasUrnTests.Kill(h, attacker);
                h.Tick();
                Assert.AreEqual(3U, h.Client.Player.Missions[682].Objectives[3].Counters[0]);
                Assert.AreEqual(0U, second.Player.Missions[682].Objectives[3].Counters[0]);
                Assert.IsTrue(MissionNpcVisibility.IsShownTo(h.Client, publicRanger));
                Assert.IsFalse(MissionNpcVisibility.IsShownTo(second, publicRanger));
                Assert.IsTrue(CreatureManager.IsLivingOnMap(h.Map, other));
                Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, publicRanger.EntityId, 682, 5, 1));
                Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, h.Npc(177).EntityId, 682, 4, 1));
                Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, h.Npc(175).EntityId, 682, 6, 1));
                Assert.IsTrue(h.Manager.CompleteOfferedMission(h.Client, h.Npc(175).EntityId, 682, 1));
                Assert.AreEqual(MissionState.Completed, h.Client.Player.Missions[682].State);
                Assert.IsTrue(MissionNpcVisibility.IsShownTo(h.Client, publicRanger), "Anjuhi is not a permanently rescued prisoner.");
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(second);
                h.Map.ClientList.Remove(second);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PersonalRangerDeathFailsTheCorrectStageAndRetryDoesNotNeedPublicAnjuhi(bool attackStarted)
        {
            using var h = WildernessRuntimeTestHarness.Create();
            h.SpawnWorld(174, 580053);
            WildernessDaghdasUrnTests.SeedHistory(h, 451);
            var todae = h.Npc(174);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, todae.EntityId, 682));
            var first = Ranger(h, h.Client.Player.Id);
            if (attackStarted) Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, first.EntityId, 682, 2, 1));
            var assignment = h.Client.Player.Missions[682].AssignmentId;
            WildernessDaghdasUrnTests.Kill(h, first);
            h.Tick();
            Assert.AreEqual(MissionState.Failed, h.Client.Player.Missions[682].State);
            Assert.AreEqual(MissionObjectiveState.Failed, h.Client.Player.Missions[682].Objectives[attackStarted ? 3U : 2U].State);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, todae.EntityId, 682));
            Assert.AreNotEqual(assignment, h.Client.Player.Missions[682].AssignmentId);
            Assert.AreNotSame(first, Ranger(h, h.Client.Player.Id));
            Assert.AreNotEqual(first.SpawnPool.SceneRunId, Ranger(h, h.Client.Player.Id).SpawnPool.SceneRunId);
        }

        [TestMethod]
        public void AuthoredEncounterMapRefusesWorldEffectsOnAnotherMap()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            var definition = new Rasa.Missions.Content.MissionSceneDefinition { MapContextId = 1148 };
            definition.Actors["anjuhi"] = new Rasa.Missions.Scenes.SceneActorDefinition("anjuhi",
                Rasa.Missions.Scenes.SceneActorKind.Creature, 98, new Rasa.Missions.Scenes.ScenePosition(0, 0, 0));
            var run = new Rasa.Missions.Scenes.SceneRun("wrong-map", "test", "data.sequence", 1, 1, 0, "{}",
                Rasa.Missions.Scenes.SceneStatus.Running, h.Client.Player.Id, 682);
            var world = new Rasa.Game.Missions.World.SceneWorldAdapter(h.Manager.PublicActors, (_, _) => { });
            world.Attach(run, definition.Bindings("test"), h.Client, h.Map);
            Assert.AreEqual(Rasa.Game.Missions.World.WorldEffectState.Failed,
                world.Apply(run, new Rasa.Missions.Scenes.EnsureActorIntent("spawn", "anjuhi")).State);
            Assert.IsFalse(h.Map.SpawnPools.Any(pool => pool.SceneRunId == run.Id));
        }

        [TestMethod]
        public void ReconnectAndObsoleteLeaseRecoveryKeepThePersonalEncounterAndDefeatCounter()
        {
            using var h = WildernessRuntimeTestHarness.Create();
            h.SpawnWorld(174);
            WildernessDaghdasUrnTests.SeedHistory(h, 451);
            Assert.IsTrue(h.Manager.AcceptOfferedMission(h.Client, h.Npc(174).EntityId, 682));
            var ranger = Ranger(h, h.Client.Player.Id); var run = ranger.SpawnPool.SceneRunId;
            var assignment = h.Client.Player.Missions[682].AssignmentId;
            Assert.IsTrue(h.Manager.CompleteOfferedObjective(h.Client, ranger.EntityId, 682, 2, 1));
            var attacker = WildernessDaghdasUrnTests.Creatures(h).First(c => c.SpawnPool?.SceneActorRole == "xanx-1");
            WildernessDaghdasUrnTests.Kill(h, attacker);
            using (var unit = h.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Add(new Rasa.Structures.Char.MissionActorLeaseEntry
                {
                    MapKey = Rasa.Game.Missions.World.PublicActorLeaseService.MapKey(h.Map), SpawnKey = "spawn:176",
                    RunId = run, ActorRole = "anjuhi", Generation = ranger.SpawnPool.SceneGeneration
                }));
            new Rasa.Game.Missions.World.PublicActorLeaseService(h, () => h.Manager).Tick(h.Map);
            using (var unit = h.CreateChar())
            {
                Assert.AreEqual(0, unit.CharacterMissions.Runtime.Leases(run).Count);
                Assert.AreEqual("Running", unit.CharacterMissions.Runtime.Scene(run).Status);
            }
            var reconnect = h.Context.CreateCompetingClient(h.Manager);
            h.Manager.PublishInitialState(reconnect);
            Assert.AreEqual(assignment, reconnect.Player.Missions[682].AssignmentId);
            Assert.AreEqual(1U, reconnect.Player.Missions[682].Objectives[3].Counters[0]);
            Assert.AreSame(ranger, Ranger(h, reconnect.Player.Id));
            Assert.AreEqual(2, WildernessDaghdasUrnTests.Creatures(h).Count(c => c.DbId == 87 &&
                c.SpawnPool?.ScenarioMissionId == 682 && c.State != CharacterState.Dead));
        }
    }
}
