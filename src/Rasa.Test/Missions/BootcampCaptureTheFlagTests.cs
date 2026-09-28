extern alias RasaGame;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class BootcampCaptureTheFlagTests
    {
        private const uint CaptureTheFlagMissionId = 1994;
        private const uint CaveInAreaId = 439;
        private const uint PromotionRewardExperience = 43000;
        private const uint YoungbloodRewardExperience = 5000;
        private static readonly System.TimeSpan YoungbloodDelay = System.TimeSpan.FromSeconds(7);

        [TestMethod]
        public void CaptureTheFlagPromotionKeepsRecruitAndUsesNormalPointFormulas()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            SeedCaptureTheFlagPrerequisites(harness, seedLightning: true);
            var deSimone = harness.AddNpc(
                BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId,
                BootcampRuntimeTestHarness.CorporalDeSimonePackageId);

            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                deSimone.EntityId,
                CaptureTheFlagMissionId));
            harness.Context.Drain();
            var before = harness.Context.ReadRewardTotals();
            var previousCloneCredits = harness.Client.Player.CloneCredits;

            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                deSimone.EntityId,
                CaptureTheFlagMissionId,
                4,
                1));

            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[CaptureTheFlagMissionId],
                (4U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Incomplete),
                (1U, MissionObjectiveState.Inactive),
                (3U, MissionObjectiveState.Inactive));
            Assert.AreEqual((uint)CharacterClass.Recruit, harness.Client.Player.Class);
            Assert.AreEqual((byte)5, harness.Client.Player.Level);
            Assert.AreEqual(previousCloneCredits + 1, harness.Client.Player.CloneCredits);
            using (var unit = harness.Context.CreateChar())
                Assert.AreEqual(harness.Client.Player.CloneCredits,
                    unit.Characters.Get(harness.Client.Player.Id).CloneCredits);
            Assert.AreEqual(
                before.Experience + PromotionRewardExperience,
                harness.Context.ReadRewardTotals().Experience);
            Assert.AreEqual(3, CountScenarioCreatures(harness.BootcampMap, 1994, 1));
            Assert.AreEqual(0, CountScenarioCreatures(harness.BootcampMap, 1994, 2));

            var packets = harness.Context.Drain();
            Assert.AreEqual(0, packets.OfType<CharacterClassPacket>().Count());
            CollectionAssert.AreEqual(
                new byte[] { 2, 3, 4, 5 },
                packets.OfType<LevelUpPacket>().Select(packet => packet.Level).ToArray());

            var available = packets.OfType<AvailableAllocationPointsPacket>().Last();
            var manifestation = new ManifestationManager(harness.Context);
            Assert.AreEqual(
                manifestation.GetAvailableAttributePoints(harness.Client.Player),
                available.AvailableAttributePoints);
            Assert.AreEqual(
                manifestation.GetSkillPointsAvailable(harness.Client.Player),
                available.AvailableSkillPoints);
        }

        [TestMethod]
        public void CaptureTheFlagCaveInStartsAssaultOnceAndKeepsCompanionsPrivate()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            PromoteToCaveObjective(harness);

            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(CaptureTheFlagMissionId, CaveInAreaId)));
            Assert.IsFalse(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(CaptureTheFlagMissionId, CaveInAreaId)));

            BootcampRuntimeTestHarness.AssertObjectiveStates(
                harness.Client.Player.Missions[CaptureTheFlagMissionId],
                (4U, MissionObjectiveState.Completed),
                (2U, MissionObjectiveState.Completed),
                (1U, MissionObjectiveState.Incomplete),
                (3U, MissionObjectiveState.Inactive));
            Assert.AreEqual(3, CountScenarioCreatures(harness.BootcampMap, 1994, 1));
            Assert.AreEqual(1, CountScenarioCreatures(harness.BootcampMap, 1994, 2));
            Assert.AreEqual(0, CountScenarioCreatures(harness.BootcampMap, 1994, 3));

            var foreignMap = harness.Maps.GetOrCreatePrivateInstance(
                BootcampRuntimeTestHarness.BootcampMapContextId,
                999);
            Assert.AreEqual(3, CountScenarioCreatures(foreignMap, 1994, 1));
            Assert.IsTrue(foreignMap.SpawnPools.Where(pool => pool.ScenarioMissionId == 1994)
                .All(pool => pool.FollowOwnerCharacterId == 0));
        }

        [TestMethod]
        public void CaptureTheFlagTizzikKillCreditsOnlyOwnedInstanceAndYoungbloodArrivesOnceAfterDelay()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            PromoteAndStartAssault(harness);
            var ownedMap = harness.BootcampMap;
            var foreignMap = harness.Maps.GetOrCreatePrivateInstance(
                BootcampRuntimeTestHarness.BootcampMapContextId,
                999);
            var foreignTizzik = CreateScenarioTizzik(harness, foreignMap, 999);

            MoveClientToMap(harness.Client, ownedMap, foreignMap);
            KillScenarioCreature(harness, foreignMap, foreignTizzik);
            Assert.AreEqual(
                MissionObjectiveState.Incomplete,
                harness.Client.Player.Missions[CaptureTheFlagMissionId].Objectives[1].State);

            MoveClientToMap(harness.Client, foreignMap, ownedMap);
            var ownedTizzik = BootcampRuntimeTestHarness.FindCreature(
                ownedMap,
                BootcampRuntimeTestHarness.TizzikGiCreatureId);
            Assert.IsNotNull(ownedTizzik);

            KillScenarioCreature(harness, ownedMap, ownedTizzik);

            var mission = harness.Client.Player.Missions[CaptureTheFlagMissionId];
            Assert.AreEqual(MissionObjectiveState.Completed, mission.Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Inactive, mission.Objectives[3].State);
            Assert.AreEqual(0, CountScenarioCreatures(ownedMap, 1994, 3));

            harness.UtcNow += YoungbloodDelay - System.TimeSpan.FromMilliseconds(1);
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Client));
            Assert.AreEqual(0, CountScenarioCreatures(ownedMap, 1994, 3));

            harness.UtcNow += System.TimeSpan.FromMilliseconds(1);
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Client));

            Assert.AreEqual(1, CountScenarioCreatures(ownedMap, 1994, 3));
            Assert.AreEqual(MissionObjectiveState.Incomplete, mission.Objectives[3].State);
        }

        [TestMethod]
        public void CaptureTheFlagEscortDeathsDoNotBlockProgressOrRespawnAcrossTicksAndReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            PromoteAndStartAssault(harness);

            var escort = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .First(creature =>
                    creature.SpawnPool?.ScenarioMissionId == 1994 && creature.SpawnPool.ScenarioGroupId == 1);
            KillScenarioCreature(harness, harness.BootcampMap, escort);
            BootcampRuntimeTestHarness.AdvanceScenarioCorpseAndRespawn(
                harness,
                escort,
                corpseMilliseconds: 1000,
                respawnMilliseconds: 1000);

            Assert.AreEqual(2, CountScenarioCreatures(harness.BootcampMap, 1994, 1));

            harness.ReconnectFresh();

            Assert.AreEqual(2, CountScenarioCreatures(harness.BootcampMap, 1994, 1));

            var tizzik = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.TizzikGiCreatureId);
            Assert.IsNotNull(tizzik);
            KillScenarioCreature(harness, harness.BootcampMap, tizzik);
            harness.UtcNow += YoungbloodDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            Assert.AreEqual(MissionObjectiveState.Completed, harness.Client.Player.Missions[CaptureTheFlagMissionId].Objectives[1].State);
            Assert.AreEqual(1, CountScenarioCreatures(harness.BootcampMap, 1994, 3));
        }

        [TestMethod]
        public void CaptureTheFlagTizzikDoesNotRespawnOrDoubleScheduleYoungbloodAcrossTicksAndReconnect()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            PromoteAndStartAssault(harness);

            var tizzik = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.TizzikGiCreatureId);
            Assert.IsNotNull(tizzik);
            KillScenarioCreature(harness, harness.BootcampMap, tizzik);
            BootcampRuntimeTestHarness.AdvanceScenarioCorpseAndRespawn(
                harness,
                tizzik,
                corpseMilliseconds: LootDispenserManager.LootableCorpseMs,
                respawnMilliseconds: 1000);

            Assert.AreEqual(0, CountScenarioCreatures(harness.BootcampMap, 1994, 2));

            harness.ReconnectFresh();
            Assert.AreEqual(0, CountScenarioCreatures(harness.BootcampMap, 1994, 2));

            harness.UtcNow += YoungbloodDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Client));
            Assert.AreEqual(1, CountScenarioCreatures(harness.BootcampMap, 1994, 3));

            harness.ReconnectFresh();

            Assert.AreEqual(0, CountScenarioCreatures(harness.BootcampMap, 1994, 2));
            Assert.AreEqual(1, CountScenarioCreatures(harness.BootcampMap, 1994, 3));
            Assert.IsFalse(harness.Manager.TickScenarios(harness.Client));
        }

        [TestMethod]
        public void CaptureTheFlagReconnectsAroundTizzikAndYoungbloodTurnInRewardsExactlyFiveThousandXp()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            PromoteAndStartAssault(harness);

            harness.ReconnectFresh();
            Assert.AreEqual(3, CountScenarioCreatures(harness.BootcampMap, 1994, 1));
            Assert.AreEqual(1, CountScenarioCreatures(harness.BootcampMap, 1994, 2));

            var tizzik = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.TizzikGiCreatureId);
            Assert.IsNotNull(tizzik);
            KillScenarioCreature(harness, harness.BootcampMap, tizzik);

            harness.ReconnectFresh();
            Assert.AreEqual(0, CountScenarioCreatures(harness.BootcampMap, 1994, 3));
            harness.UtcNow += YoungbloodDelay;
            Assert.IsTrue(harness.Manager.TickScenarios(harness.Client));

            harness.ReconnectFresh();
            Assert.AreEqual(1, CountScenarioCreatures(harness.BootcampMap, 1994, 3));
            var youngblood = BootcampRuntimeTestHarness.FindCreature(
                harness.BootcampMap,
                BootcampRuntimeTestHarness.CaptainYoungbloodCreatureId);
            Assert.IsNotNull(youngblood);

            var before = harness.Context.ReadRewardTotals();
            Assert.IsTrue(harness.Manager.TryGetRewardInfo(CaptureTheFlagMissionId, out var authoredReward));
            Assert.AreEqual(200U, authoredReward.FixedReward.Credits[CurencyType.Credits]);
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                youngblood.EntityId,
                CaptureTheFlagMissionId,
                3,
                1));
            Assert.IsTrue(harness.Client.Player.Missions[CaptureTheFlagMissionId].Completeable);
            Assert.IsTrue(harness.Manager.CompleteOfferedMission(
                harness.Client,
                youngblood.EntityId,
                CaptureTheFlagMissionId,
                selectionIndex: null,
                rating: null));
            var after = harness.Context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + YoungbloodRewardExperience, after.Experience);
            Assert.AreEqual(before.Credits + 200, after.Credits);
            Assert.AreEqual(before.Prestige, after.Prestige);
            Assert.AreEqual(before.ItemCount, after.ItemCount);
            Assert.AreEqual((uint)CharacterClass.Recruit, harness.Client.Player.Class);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(
                harness.Client, youngblood.EntityId, CaptureTheFlagMissionId, null, null));
            Assert.AreEqual(after, harness.Context.ReadRewardTotals());
            harness.ReconnectFresh();
            Assert.AreEqual(after, harness.Context.ReadRewardTotals());
            Assert.AreEqual(after.Experience, harness.Client.Player.Experience);
            Assert.AreEqual(after.Credits, harness.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(MissionState.Completed, harness.Client.Player.Missions[CaptureTheFlagMissionId].State);
        }

        private static void SeedCaptureTheFlagPrerequisites(
            BootcampRuntimeTestHarness.Harness harness,
            bool seedLightning = false)
        {
            harness.SeedMission(
                harness.Client.Player.Id,
                BootcampRuntimeTestHarness.MissionGearingUp,
                (uint)MissionState.Completed,
                false);
            if (!seedLightning)
                return;

            using var unit = harness.Context.CreateChar();
            unit.CharacterSkills.AddOrUpdate(
                harness.Client.Player.Id,
                (uint)SkillId.Lightning,
                (int)ActionId.AaRecruitLightning,
                1);
            unit.CharacterAbilityDrawers.AddOrUpdate(
                harness.Client.Player.Id,
                0,
                (int)ActionId.AaRecruitLightning,
                1);
            harness.Client.Player.Skills[SkillId.Lightning] = new SkillsData(
                SkillId.Lightning,
                (int)ActionId.AaRecruitLightning,
                1);
            harness.Client.Player.Abilities[0] = new AbilityDrawerData(
                0,
                (int)ActionId.AaRecruitLightning,
                1);
        }

        private static void PromoteToCaveObjective(BootcampRuntimeTestHarness.Harness harness)
        {
            SeedCaptureTheFlagPrerequisites(harness, seedLightning: true);
            var deSimone = harness.AddNpc(
                BootcampRuntimeTestHarness.CorporalDeSimoneCreatureId,
                BootcampRuntimeTestHarness.CorporalDeSimonePackageId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(
                harness.Client,
                deSimone.EntityId,
                CaptureTheFlagMissionId));
            harness.Context.Drain();
            Assert.IsTrue(harness.Manager.CompleteOfferedObjective(
                harness.Client,
                deSimone.EntityId,
                CaptureTheFlagMissionId,
                4,
                1));
            harness.Context.Drain();
        }

        private static void PromoteAndStartAssault(BootcampRuntimeTestHarness.Harness harness)
        {
            PromoteToCaveObjective(harness);
            Assert.IsTrue(harness.Manager.RecordProgress(
                harness.Client,
                MissionProgressEvent.Area(CaptureTheFlagMissionId, CaveInAreaId)));
            harness.Context.Drain();
        }

        private static int CountScenarioCreatures(MapChannel mapChannel, uint missionId, uint groupId) =>
            mapChannel.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Count(creature =>
                    creature.SpawnPool?.ScenarioMissionId == missionId &&
                    creature.SpawnPool.ScenarioGroupId == groupId);

        private static Creature CreateScenarioTizzik(
            BootcampRuntimeTestHarness.Harness harness,
            MapChannel mapChannel,
            uint ownerCharacterId)
        {
            var runtimeKey =
                $"owner:{ownerCharacterId}:mission:{CaptureTheFlagMissionId}:revision:deployment_11:attempt:-:spawn:2";
            var spawnPool = new SpawnPool
            {
                DbId = 1,
                ScenarioKey = runtimeKey,
                ScenarioGroupId = 2,
                MapContextId = mapChannel.MapInfo.MapContextId,
                RuntimeMapChannel = mapChannel,
                Position = new Vector3(95.1f, 109.25f, 150.8f),
                Rotation = 0,
                SpawnSlot = new List<SpawnPoolSlot>
                {
                    new(BootcampRuntimeTestHarness.TizzikGiCreatureId, 1, 1)
                }
            };
            mapChannel.SpawnPools.Add(spawnPool);

            var creatures = new CreatureManager(
                null,
                new ManifestationManager(harness.Context),
                harness.Manager);
            creatures.LoadedCreatures[BootcampRuntimeTestHarness.TizzikGiCreatureId] = new Creature
            {
                DbId = BootcampRuntimeTestHarness.TizzikGiCreatureId,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc(),
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Level = 6,
                MaxHitPoints = 100
            };
            var creature = creatures.CreateScenarioCreature(
                spawnPool,
                BootcampRuntimeTestHarness.TizzikGiCreatureId,
                spawnPool.Position,
                spawnPool.Rotation);
            CellManager.Instance.AddToWorld(mapChannel, creature);
            return creature;
        }

        private static void KillScenarioCreature(
            BootcampRuntimeTestHarness.Harness harness,
            MapChannel mapChannel,
            Creature creature)
        {
            new CreatureManager(
                    null,
                    new ManifestationManager(harness.Context),
                    harness.Manager)
                .HandleCreatureKill(mapChannel, creature, harness.Client.Player);
        }

        private static void MoveClientToMap(Client client, MapChannel origin, MapChannel destination)
        {
            CellManager.Instance.RemoveFromWorld(client);
            origin.ClientList.Remove(client);
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            CellManager.Instance.AddToWorld(client);
            destination.ClientList.Add(client);
            client.State = ClientState.Ingame;
        }
    }
}
