extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionScenarioServiceTests
    {
        [TestMethod]
        public void RewardGrantPublicationFailureLeavesDurableScenarioRewardsCommittedOnce()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateRewardScenarioFixture();
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            var maps = new MapChannelManager(
                null,
                privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            using var managers = CreateManagers(maps);
            var manager = LoadManager(
                context,
                fixture,
                () => now,
                maps,
                beforeRewardItemPublication: _ => throw new InvalidOperationException("publish failed"));
            context.AddRewardTemplate(28, 3147);
            context.AddRewardTemplate(29, 3147);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            var before = context.ReadRewardTotals();

            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));
            Assert.IsFalse(manager.TryExecuteScenario(context.Client, 321, 60));

            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + 125U, after.Experience);
            Assert.AreEqual(before.Credits + 75, after.Credits);
            Assert.AreEqual(before.Prestige + 10, after.Prestige);
            Assert.AreEqual(before.ItemCount + 3, after.ItemCount);
            Assert.IsTrue(context.Client.AccountEntry.CanSkipBootcamp);
            using var unit = context.CreateChar();
            var learnedSkill = unit.CharacterSkills.GetCharacterSkills(context.Client.Player.Id)
                .Single(skill => skill.SkillId == 901);
            Assert.AreEqual(194, learnedSkill.AbilityId);
            Assert.AreEqual(2, learnedSkill.SkillLevel);
            var slotted = unit.CharacterAbilityDrawers.GetCharacterAbilities(context.Client.Player.Id)
                .Single(entry => entry.AbilitySlot == 3);
            Assert.AreEqual(194, slotted.AbilityId);
            Assert.AreEqual(2U, slotted.AbilityLevel);
            Assert.IsTrue(unit.CharacterFlags.HasValue(
                context.Client.Player.Id,
                CharacterFlagIds.BootcampComplete));
            var run = unit.CharacterMissions.Runtime.Scenes(context.Client.Player.Id, 321).Single();
            Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(run.RunId).Count(message => message.Status == "Handled"));
            using var verify = context.Open();
            Assert.AreEqual(4, verify.Set<MissionReceiptEntry>().Count(receipt => receipt.OwnerId == run.RunId));
        }

        [TestMethod]
        public void TickExecutesScheduledScenarioExactlyOnceWhenDelayElapses()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateScheduledScenarioFixture();
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            var maps = new MapChannelManager(
                null,
                privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            using var managers = CreateManagers(maps);
            MissionApplication manager = null;
            var service = new MissionSceneHost(
                () => context,
                () => manager,
                new ManifestationManager(context),
                () => maps,
                () => CreatureManager.Instance,
                () => DynamicObjectManager.Instance,
                () => CommunicatorManager.Instance,
                () => now);
            manager = LoadManager(context, fixture, () => now, maps, scenarioService: service);
            context.AddRewardTemplate(28, 3147);
            context.AddRewardTemplate(29, 3147);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            var before = context.ReadRewardTotals();

            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));
            Assert.IsFalse(manager.TickScenarios(context.Client));

            now = now.AddMilliseconds(5000);

            Assert.IsTrue(manager.TickScenarios(context.Client));
            Assert.IsFalse(manager.TickScenarios(context.Client));

            var after = context.ReadRewardTotals();
            Assert.AreEqual(before.Experience + 125U, after.Experience);
            Assert.AreEqual(before.Credits + 75, after.Credits);
            Assert.AreEqual(before.Prestige + 10, after.Prestige);
            Assert.AreEqual(before.ItemCount + 3, after.ItemCount);
        }

        [TestMethod]
        public void RebuildRestoresTransientScenarioRuntimeOnOwnedPrivateReconnect()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            PrepareScenarioCreatureClass();
            var fixture = CreateRuntimeActorFixture();
            context.Map.MapInfo = new MapInfo(1985, "bootcamp_fixture", 1556, 0);
            context.Client.Player.MapContextId = 1985;
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            MapChannelManager maps = null;
            var objects = new DynamicObjectManager(null, maps);
            var creatures = new CreatureManager(null, new ManifestationManager(context));
            creatures.LoadedCreatures[501] = new Creature
            {
                DbId = 501,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc { NpcPackageId = 501 },
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var service = new MissionSceneHost(
                () => context,
                () => manager,
                new ManifestationManager(context),
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => now);
            maps = new MapChannelManager(
                null,
                privateInstances: new PrivateMapInstanceService(),
                scenarioService: service);
            maps.MapChannelArray.Add(1985, context.Map);
            objects = new DynamicObjectManager(null, maps);
            manager = LoadManager(context, fixture, () => now, maps, objects, creatures, service);
            context.AddRewardTemplate(28, 3147);
            using var singletons = new ManagerInstances(maps, objects, creatures, manager);
            var owned = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);
            MoveClientToMap(context.Client, context.Map, owned);
            var giver = context.AddNpc(101, owned);
            EnsureTestCreatureAttributes(giver);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));

            Assert.AreEqual(1, CountScenarioCreatures(owned));
            Assert.AreEqual(1, CountScenarioObjects(owned));

            maps.ReleaseOwnedPrivateInstances(context.Client.Player.Id);
            var rebuilt = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);

            Assert.AreNotSame(owned, rebuilt);
            Assert.AreEqual(1, CountScenarioCreatures(rebuilt));
            Assert.AreEqual(1, CountScenarioObjects(rebuilt));
            Assert.IsFalse(GetScenarioObject(rebuilt, "bootcamp-crate").IsEnabled);
        }

        [TestMethod]
        public void RebuildRecreatesMissingRuntimeActorsWithoutRepeatingDurableSteps()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            PrepareScenarioCreatureClass();
            var fixture = CreateRuntimeActorFixture();
            context.Map.MapInfo = new MapInfo(1985, "bootcamp_fixture", 1556, 0);
            context.Client.Player.MapContextId = 1985;
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            MapChannelManager maps = null;
            var objects = new DynamicObjectManager(null, maps);
            var creatures = new CreatureManager(null, new ManifestationManager(context));
            creatures.LoadedCreatures[501] = new Creature
            {
                DbId = 501,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc { NpcPackageId = 501 },
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var service = new MissionSceneHost(
                () => context,
                () => manager,
                new ManifestationManager(context),
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => now);
            maps = new MapChannelManager(
                null,
                privateInstances: new PrivateMapInstanceService(),
                scenarioService: service);
            maps.MapChannelArray.Add(1985, context.Map);
            objects = new DynamicObjectManager(null, maps);
            manager = LoadManager(context, fixture, () => now, maps, objects, creatures, service);
            context.AddRewardTemplate(28, 3147);
            using var singletons = new ManagerInstances(maps, objects, creatures, manager);
            var owned = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);
            MoveClientToMap(context.Client, context.Map, owned);
            var giver = context.AddNpc(101, owned);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));

            var creature = GetScenarioCreature(owned);
            var dynamicObject = GetScenarioObject(owned, "bootcamp-crate");
            CellManager.Instance.RemoveCreatureFromWorld(owned, creature);
            CellManager.Instance.RemoveFromWorld(owned, dynamicObject);
            owned.DynamicObjects.Remove(dynamicObject);

            manager.RebuildScenarioRuntime(context.Client.Player.Id, owned);

            Assert.AreEqual(1, CountScenarioCreatures(owned));
            Assert.AreEqual(1, CountScenarioObjects(owned));
        }

        [TestMethod]
        public void EscortSpawnGroupStepMakesScenarioCreaturesFollowTheOwnerAcrossReconnect()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            context.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            PrepareScenarioCreatureClass();
            var fixture = CreateEscortRuntimeFixture();
            context.Map.MapInfo = new MapInfo(1985, "bootcamp_fixture", 1556, 0);
            context.Client.Player.MapContextId = 1985;
            context.Client.Player.Class = (uint)CharacterClass.Recruit;
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            MapChannelManager maps = null;
            var objects = new DynamicObjectManager(null, maps);
            var creatures = new CreatureManager(null, new ManifestationManager(context));
            creatures.LoadedCreatures[501] = new Creature
            {
                DbId = 501,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc { NpcPackageId = 501 },
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var service = new MissionSceneHost(
                () => context,
                () => manager,
                new ManifestationManager(context),
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => now);
            maps = new MapChannelManager(
                null,
                privateInstances: new PrivateMapInstanceService(),
                scenarioService: service);
            maps.MapChannelArray.Add(1985, context.Map);
            objects = new DynamicObjectManager(null, maps);
            manager = LoadManager(context, fixture, () => now, maps, objects, creatures, service);
            context.AddRewardTemplate(28, 3147);
            using var singletons = new ManagerInstances(maps, objects, creatures, manager);
            var owned = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);
            MoveClientToMap(context.Client, context.Map, owned);
            var giver = context.AddNpc(101, owned);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));

            var escort = GetScenarioCreature(owned);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, escort.Controller.CurrentAction);
            Assert.AreEqual(context.Client.Player.EntityId, escort.Controller.ActionFollow.FollowTargetId);
            Assert.AreEqual(context.Client.Player.Id, escort.SpawnPool.FollowOwnerCharacterId);

            maps.ReleaseOwnedPrivateInstances(context.Client.Player.Id);
            var rebuilt = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);
            MoveClientToMap(context.Client, owned, rebuilt);
            BehaviorManager.Instance.MapChannelThink(rebuilt, 250);

            var rebuiltEscort = GetScenarioCreature(rebuilt);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, rebuiltEscort.Controller.CurrentAction);
            Assert.AreEqual(context.Client.Player.EntityId, rebuiltEscort.Controller.ActionFollow.FollowTargetId);
            Assert.AreEqual(context.Client.Player.Id, rebuiltEscort.SpawnPool.FollowOwnerCharacterId);
        }

        [TestMethod]
        public void EscortSpawnGroupUsesFollowBehaviorToFightNearbyHostiles()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            PrepareScenarioCreatureClass();
            var fixture = CreateEscortRuntimeFixture();
            context.Map.MapInfo = new MapInfo(1985, "bootcamp_fixture", 1556, 0);
            context.Client.Player.MapContextId = 1985;
            context.Client.Player.Class = (uint)CharacterClass.Recruit;
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            MapChannelManager maps = null;
            var objects = new DynamicObjectManager(null, maps);
            var creatures = new CreatureManager(null, new ManifestationManager(context));
            creatures.LoadedCreatures[501] = new Creature
            {
                DbId = 501,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc { NpcPackageId = 501 },
                Faction = Factions.AFS,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creatures.LoadedCreatures[502] = new Creature
            {
                DbId = 502,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc { NpcPackageId = 502 },
                Faction = Factions.Bane,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var service = new MissionSceneHost(
                () => context,
                () => manager,
                new ManifestationManager(context),
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => now);
            maps = new MapChannelManager(
                null,
                privateInstances: new PrivateMapInstanceService(),
                scenarioService: service);
            maps.MapChannelArray.Add(1985, context.Map);
            objects = new DynamicObjectManager(null, maps);
            manager = LoadManager(context, fixture, () => now, maps, objects, creatures, service);
            context.AddRewardTemplate(28, 3147);
            using var singletons = new ManagerInstances(maps, objects, creatures, manager);
            var owned = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);
            MoveClientToMap(context.Client, context.Map, owned);
            var giver = context.AddNpc(101, owned);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));

            var escort = GetScenarioCreature(owned);
            var hostile = creatures.CreateScenarioCreature(
                new SpawnPool
                {
                    DbId = 777,
                    MapContextId = owned.MapInfo.MapContextId,
                    RuntimeMapChannel = owned,
                    Position = escort.Position + new Vector3(1, 0, 1),
                    Rotation = 0,
                    SpawnSlot = new List<SpawnPoolSlot> { new(502, 1, 1) }
                },
                502,
                escort.Position + new Vector3(1, 0, 1),
                0);
            Assert.IsNotNull(hostile);
            CellManager.Instance.AddToWorld(owned, hostile);
            foreach (var creature in owned.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).ToArray())
                EnsureTestCreatureAttributes(creature);
            context.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);

            BehaviorManager.Instance.MapChannelThink(owned, 3500);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, escort.Controller.CurrentAction);
            Assert.AreEqual(hostile.EntityId, escort.Controller.ActionFighting.TargetEntityId);
        }

        [TestMethod]
        public void ObjectiveDeadlineTransferAndScenarioEventStepsApplyThroughAuthoritativeMissionFlows()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateObjectiveTransferFixture();
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            var maps = new MapChannelManager(
                null,
                () => 1000,
                updateCharacter: (_, _, _) => { },
                disconnect: _ => Assert.Fail("Transfer should not disconnect."),
                refreshStats: (_, _) => { },
                assignPlayer: _ => { },
                enterMapChannels: _ => { },
                privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            maps.MapChannelArray.Add(1221, new MapChannel
            {
                MapInfo = new MapInfo(1221, "mission_fixture_target", 1556, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128
            });
            using var managers = CreateManagers(maps);
            var manager = LoadManager(context, fixture, () => now, maps);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();

            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));

            Assert.AreEqual(MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(MissionObjectiveState.Failed,
                context.Client.Player.Missions[321].Objectives[11].State);
            using var unit = context.CreateChar();
            var deadline = unit.CharacterMissionDeadlines.Get(context.Client.Player.Id, 321);
            Assert.IsNotNull(deadline);
            Assert.AreEqual(CharacterMissionDeadlineState.Satisfied, deadline.State);
            Assert.AreEqual(ClientState.Teleporting, context.Client.State);
            Assert.IsNotNull(context.Client.PendingTransfer);
            Assert.AreEqual(1221U, context.Client.PendingTransfer.DestinationMap.MapInfo.MapContextId);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count(packet => packet.ObjectiveId == 10));
            var scene = unit.CharacterMissions.Runtime.Scenes(context.Client.Player.Id, 321).Single();
            Assert.AreEqual(1, unit.CharacterMissions.Runtime.Messages(scene.RunId).Count(message =>
                message.SequenceId == 60 && message.Status == "Handled"));
        }

        [TestMethod]
        public void TransferPlayerUsesOwnedPrivateBootcampDestinationInsteadOfPublicMap()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateObjectiveTransferFixture(1985);
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            var maps = new MapChannelManager(
                null,
                () => 1000,
                updateCharacter: (_, _, _) => { },
                disconnect: _ => Assert.Fail("Transfer should not disconnect."),
                refreshStats: (_, _) => { },
                assignPlayer: _ => { },
                enterMapChannels: _ => { },
                privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            var publicBootcamp = new MapChannel
            {
                MapInfo = new MapInfo(1985, "bootcamp_fixture", 1556, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128
            };
            maps.MapChannelArray.Add(1985, publicBootcamp);
            using var managers = CreateManagers(maps);
            var manager = LoadManager(context, fixture, () => now, maps);
            using (var unit = context.CreateChar())
                unit.CharacterStartingExperience.Add(new CharacterStartingExperienceEntry(
                    context.Client.Player.Id,
                    "deployment_11",
                    CharacterStartingExperienceState.Bootcamp));
            var owned = maps.GetOrCreatePrivateInstance(1985, context.Client.Player.Id);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));

            Assert.IsNotNull(context.Client.PendingTransfer);
            Assert.AreSame(owned, context.Client.PendingTransfer.DestinationMap);
            Assert.AreNotSame(publicBootcamp, context.Client.PendingTransfer.DestinationMap);
            Assert.IsTrue(context.Client.PendingTransfer.DestinationMap.IsPrivateInstance);
            Assert.AreEqual(context.Client.Player.Id, context.Client.PendingTransfer.DestinationMap.OwnerCharacterId);
        }

        [TestMethod]
        public void TransferPlayerLeavesPublicDestinationPublicWhenNoPrivateOwnershipApplies()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            var fixture = CreateObjectiveTransferFixture(1221);
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            var maps = new MapChannelManager(
                null,
                () => 1000,
                updateCharacter: (_, _, _) => { },
                disconnect: _ => Assert.Fail("Transfer should not disconnect."),
                refreshStats: (_, _) => { },
                assignPlayer: _ => { },
                enterMapChannels: _ => { },
                privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            var publicAlia = new MapChannel
            {
                MapInfo = new MapInfo(1221, "alia_fixture", 1556, 0),
                ClientList = new List<Client>(),
                PlayerLimit = 128
            };
            maps.MapChannelArray.Add(1221, publicAlia);
            using var managers = CreateManagers(maps);
            var manager = LoadManager(context, fixture, () => now, maps);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));

            Assert.IsNotNull(context.Client.PendingTransfer);
            Assert.AreSame(publicAlia, context.Client.PendingTransfer.DestinationMap);
            Assert.IsFalse(context.Client.PendingTransfer.DestinationMap.IsPrivateInstance);
        }

        [TestMethod]
        public void RuntimeCachesScopeActorsByMissionForIndependentCleanupOnSharedMaps()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            PrepareScenarioCreatureClass();
            var fixture = CreateSharedRuntimeFixture(includeSecondMission: true);
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            var maps = new MapChannelManager(null, privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            var objects = new DynamicObjectManager(null, maps);
            var creatures = new CreatureManager(null, new ManifestationManager(context));
            creatures.LoadedCreatures[501] = new Creature
            {
                DbId = 501,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc { NpcPackageId = 501 },
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var service = new MissionSceneHost(
                () => context,
                () => manager,
                new ManifestationManager(context),
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => now);
            manager = LoadManager(context, fixture, () => now, maps, objects, creatures, service);
            using var singletons = new ManagerInstances(maps, objects, creatures, manager);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 322));
            context.Drain();

            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));
            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 322, 60));

            Assert.AreEqual(2, CountScenarioCreatures(context.Map));
            Assert.AreEqual(2, CountScenarioObjects(context.Map));
            Assert.AreEqual(1, CountScenarioCreatures(context.Map, "mission:321"));
            Assert.AreEqual(1, CountScenarioCreatures(context.Map, "mission:322"));
            Assert.AreEqual(1, CountScenarioObjects(context.Map, "mission:321"));
            Assert.AreEqual(1, CountScenarioObjects(context.Map, "mission:322"));

            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 61));

            Assert.AreEqual(1, CountScenarioCreatures(context.Map));
            Assert.AreEqual(1, CountScenarioObjects(context.Map));
            Assert.AreEqual(0, CountScenarioCreatures(context.Map, "mission:321"));
            Assert.AreEqual(1, CountScenarioCreatures(context.Map, "mission:322"));
            Assert.AreEqual(0, CountScenarioObjects(context.Map, "mission:321"));
            Assert.AreEqual(1, CountScenarioObjects(context.Map, "mission:322"));
        }

        [TestMethod]
        public void RuntimeCachesScopeActorsByOwnerAndRebuildMissingRuntimeOnSharedMaps()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            PrepareScenarioCreatureClass();
            var fixture = CreateSharedRuntimeFixture(includeSecondMission: false);
            var now = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
            MissionApplication manager = null;
            var maps = new MapChannelManager(null, privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            var objects = new DynamicObjectManager(null, maps);
            var creatures = new CreatureManager(null, new ManifestationManager(context));
            creatures.LoadedCreatures[501] = new Creature
            {
                DbId = 501,
                EntityClass = (EntityClasses)4001,
                Npc = new Npc { NpcPackageId = 501 },
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var service = new MissionSceneHost(
                () => context,
                () => manager,
                new ManifestationManager(context),
                () => maps,
                () => creatures,
                () => objects,
                () => CommunicatorManager.Instance,
                () => now);
            manager = LoadManager(context, fixture, () => now, maps, objects, creatures, service);
            using var singletons = new ManagerInstances(maps, objects, creatures, manager);
            var secondClient = context.CreateAdditionalClient(2, manager: manager);
            var giver = context.AddNpc(101);

            Assert.IsTrue(manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            Assert.IsTrue(manager.AcceptOfferedMission(secondClient, giver.EntityId, 321));
            context.Drain();
            MissionTestContext.Drain(secondClient);

            Assert.IsTrue(manager.TryExecuteScenario(context.Client, 321, 60));
            Assert.IsTrue(manager.TryExecuteScenario(secondClient, 321, 60));

            Assert.AreEqual(2, CountScenarioCreatures(context.Map));
            Assert.AreEqual(2, CountScenarioObjects(context.Map));
            Assert.AreEqual(1, CountScenarioCreatures(context.Map, "owner:1"));
            Assert.AreEqual(1, CountScenarioCreatures(context.Map, "owner:2"));
            Assert.AreEqual(1, CountScenarioObjects(context.Map, "owner:1"));
            Assert.AreEqual(1, CountScenarioObjects(context.Map, "owner:2"));

            var firstCreature = GetScenarioCreature(context.Map, "owner:1");
            var firstObject = GetScenarioObjectByTokens(context.Map, "owner:1");
            CellManager.Instance.RemoveCreatureFromWorld(context.Map, firstCreature);
            CellManager.Instance.RemoveFromWorld(context.Map, firstObject);
            context.Map.DynamicObjects.Remove(firstObject);

            manager.RebuildScenarioRuntime(context.Client.Player.Id, context.Map);

            Assert.AreEqual(2, CountScenarioCreatures(context.Map));
            Assert.AreEqual(2, CountScenarioObjects(context.Map));
            Assert.AreEqual(1, CountScenarioCreatures(context.Map, "owner:1"));
            Assert.AreEqual(1, CountScenarioCreatures(context.Map, "owner:2"));
            Assert.AreEqual(1, CountScenarioObjects(context.Map, "owner:1"));
            Assert.AreEqual(1, CountScenarioObjects(context.Map, "owner:2"));
        }

        private static MissionApplication LoadManager(
            MissionTestContext context,
            MissionContentFixture fixture,
            Func<DateTime> utcNow,
            MapChannelManager maps,
            DynamicObjectManager objects = null,
            CreatureManager creatures = null,
            IMissionSceneHost scenarioService = null,
            Action<Item> beforeRewardItemPublication = null,
            Action<PythonPacket> beforeMissionPacketPublication = null)
        {
            MissionApplication manager = null;
            var manifestation = new ManifestationManager(context);
            var deadlineService = new MissionDeadlineService(
                () => context,
                () => manager,
                utcNow);
            manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new Dictionary<uint, Mission>(),
                new Dictionary<uint, MissionRewardDefinition>(),
                manifestation,
                beforeRewardItemPublication,
                beforeMissionPacketPublication,
                deadlineService,
                scenarioService,
                utcNow);

            var report = manager.LoadMissions();
            Assert.IsFalse(
                report.BlocksReadiness,
                string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Content.SceneFixtureBindings.Bind(manager, fixture);
            return manager;
        }

        private static ManagerInstances CreateManagers(MapChannelManager maps)
        {
            var objects = new DynamicObjectManager(null, maps);
            var creatures = new CreatureManager(null, new ManifestationManager(null));
            return new ManagerInstances(maps, objects, creatures, null);
        }

        private static void MoveClientToMap(Client client, MapChannel origin, MapChannel destination)
        {
            CellManager.Instance.RemoveFromWorld(client);
            origin.ClientList.Remove(client);
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        private static void AttachClientToMap(Client client, MapChannel destination)
        {
            client.Player.MapChannel = destination;
            client.Player.RuntimeMapChannel = destination;
            client.Player.MapContextId = destination.MapInfo.MapContextId;
            destination.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
        }

        private static int CountScenarioCreatures(MapChannel map, params string[] requiredTokens) =>
            map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Count(creature =>
                    !string.IsNullOrWhiteSpace(creature.SpawnPool?.ScenarioKey) &&
                    Matches(creature.SpawnPool.ScenarioMissionId, creature.SpawnPool.ScenarioOwnerCharacterId, requiredTokens));

        private static int CountScenarioObjects(MapChannel map, params string[] requiredTokens) =>
            map.DynamicObjects.Count(dynamicObject =>
                !string.IsNullOrWhiteSpace(dynamicObject.ScenarioKey) &&
                Matches(dynamicObject.SceneMissionId, dynamicObject.SceneOwnerCharacterId, requiredTokens));

        private static Creature GetScenarioCreature(MapChannel map, params string[] requiredTokens) =>
            map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Single(creature =>
                    !string.IsNullOrWhiteSpace(creature.SpawnPool?.ScenarioKey) &&
                    Matches(creature.SpawnPool.ScenarioMissionId, creature.SpawnPool.ScenarioOwnerCharacterId, requiredTokens));

        private static DynamicObject GetScenarioObject(MapChannel map, string key) =>
            map.DynamicObjects.Single(dynamicObject =>
                dynamicObject.SceneActorRole == key ||
                string.Equals(dynamicObject.ScenarioKey, key, StringComparison.Ordinal) ||
                dynamicObject.ScenarioKey?.EndsWith($":object:{key}", StringComparison.Ordinal) == true);

        private static DynamicObject GetScenarioObjectByTokens(MapChannel map, params string[] requiredTokens) =>
            map.DynamicObjects.Single(dynamicObject =>
                !string.IsNullOrWhiteSpace(dynamicObject.ScenarioKey) &&
                Matches(dynamicObject.SceneMissionId, dynamicObject.SceneOwnerCharacterId, requiredTokens));

        private static bool Matches(uint mission, uint owner, params string[] requiredTokens)
        {
            if (requiredTokens == null || requiredTokens.Length == 0)
                return true;

            return requiredTokens.All(token => token switch
            {
                _ when token.StartsWith("mission:", StringComparison.Ordinal) => mission == uint.Parse(token.Substring(8)),
                _ when token.StartsWith("owner:", StringComparison.Ordinal) => owner == uint.Parse(token.Substring(6)),
                _ => throw new InvalidOperationException("Attempt-string selectors are obsolete; address a scene run explicitly.")
            });
        }

        private static void PrepareScenarioCreatureClass()
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            if (!classes.ContainsKey((EntityClasses)3147))
                classes.Add((EntityClasses)3147, new EntityClass(3147, "scenario_object", 0, 0,
                    new List<AugmentationType>(), true));

            if (!classes.TryGetValue((EntityClasses)4001, out var entityClass))
            {
                entityClass = new EntityClass(4001, "scenario_creature", 0, 0,
                    new List<AugmentationType>(), true);
                classes.Add((EntityClasses)4001, entityClass);
            }

            if (!entityClass.Augmentations.Contains(AugmentationType.Creature))
                entityClass.Augmentations.Add(AugmentationType.Creature);
        }

        private static void EnsureTestCreatureAttributes(Creature creature)
        {
            if (creature == null)
                return;

            creature.State = CharacterState.Normal;
            creature.Attributes[Attributes.Body] = new ActorAttributes(Attributes.Body, 10, 10, 10, 0, 0);
            creature.Attributes[Attributes.Mind] = new ActorAttributes(Attributes.Mind, 10, 10, 10, 0, 0);
            creature.Attributes[Attributes.Spirit] = new ActorAttributes(Attributes.Spirit, 10, 10, 10, 0, 0);
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            creature.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 0, 0, 0, 0, 0);
            creature.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 0, 0, 0, 0, 0);
            creature.Attributes[Attributes.Aware] = new ActorAttributes(Attributes.Aware, 0, 0, 0, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Attributes[Attributes.Speed] = new ActorAttributes(Attributes.Speed, 1, 1, 1, 0, 0);
            creature.Attributes[Attributes.Regen] = new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0);
        }

        private static MissionContentFixture CreateRewardScenarioFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            ReplaceScenarioRewardWithNoSelectionPackage(fixture, 41);
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.AddRange(
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.GrantRewardPackage,
                    Sequence = 1,
                    RewardId = 41,
                    Comment = "Grant authored reward"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.GrantSkillAbility,
                    Sequence = 2,
                    SkillId = 901,
                    AbilityId = 194,
                    SkillLevel = 2,
                    AbilitySlot = 3,
                    Comment = "Grant skill"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SetQualification,
                    Sequence = 3,
                    QualificationKey = CharacterQualificationKey.BootcampComplete,
                    QualificationValue = MissionScenarioStepEntry.GrantedQualificationValue,
                    Comment = "Grant qualification"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 4,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SetAccountSkipEntitlement,
                    Sequence = 4,
                    AccountSkipEntitlement = true,
                    Comment = "Grant skip entitlement"
                });
            return fixture;
        }

        private static MissionContentFixture CreateScheduledScenarioFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            ReplaceScenarioRewardWithNoSelectionPackage(fixture, 41);
            fixture.Scenarios.Add(new MissionScenarioEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 61,
                Requirement = MissionContentRequirement.Required,
                Name = "DelayedReward",
                Comment = "Delayed reward scenario"
            });
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.Add(new MissionScenarioStepEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 60,
                StepId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionScenarioStepKind.ScheduleScenario,
                Sequence = 1,
                TargetScenarioId = 61,
                DelayMilliseconds = 5000,
                Comment = "Schedule reward"
            });
            fixture.ScenarioSteps.Add(new MissionScenarioStepEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 61,
                StepId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionScenarioStepKind.GrantRewardPackage,
                Sequence = 1,
                RewardId = 41,
                Comment = "Delayed reward"
            });
            return fixture;
        }

        private static MissionContentFixture CreateRuntimeActorFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.MapContextIds.Add(1985);
            fixture.SpawnGroups.Clear();
            fixture.Spawns.Clear();
            fixture.SpawnGroups.Add(new MissionSpawnGroupEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                SpawnGroupId = 50,
                Requirement = MissionContentRequirement.Required,
                AreaId = null,
                MapContextId = 1985,
                Enabled = false,
                SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                RespawnSeconds = null,
                Comment = "Scenario spawn group"
            });
            fixture.Spawns.Add(new MissionSpawnEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                SpawnGroupId = 50,
                SpawnId = 1,
                CreatureId = 501,
                PosX = 8,
                PosY = 9,
                PosZ = 10,
                Rotation = 0.25,
                Quantity = 1
            });
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.AddRange(
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnGroup,
                    Sequence = 1,
                    SpawnGroupId = 50,
                    Comment = "Spawn creatures"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnDynamicObject,
                    Sequence = 2,
                    DynamicObjectKey = "bootcamp-crate",
                    EntityClassId = 3147,
                    PosX = 12,
                    PosY = 0,
                    PosZ = 6,
                    Orientation = 0.5,
                    InitialInteractionEnabled = true,
                    Comment = "Spawn crate"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.DisableInteraction,
                    Sequence = 3,
                    EntityClassId = 3147,
                    Comment = "Disable crate interaction"
                });
            return fixture;
        }

        private static MissionContentFixture CreateAttemptKeyRuntimeFixture()
        {
            var fixture = CreateRuntimeActorFixture();
            fixture.Scenarios.Clear();
            fixture.Scenarios.AddRange(
                new MissionScenarioEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    Requirement = MissionContentRequirement.Required,
                    Name = "ResetAttempt",
                    Comment = "Reset attempt scenario"
                },
                new MissionScenarioEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    Requirement = MissionContentRequirement.Required,
                    Name = "ScoutAttempt",
                    Comment = "Scout attempt"
                },
                new MissionScenarioEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 62,
                    Requirement = MissionContentRequirement.Required,
                    Name = "ScoutFollowUp",
                    Comment = "Scheduled scout follow-up"
                },
                new MissionScenarioEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 63,
                    Requirement = MissionContentRequirement.Required,
                    Name = "MedicAttempt",
                    Comment = "Medic attempt"
                });
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.AddRange(
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ResetAttempt,
                    Sequence = 1,
                    AttemptKey = "scout",
                    Comment = "Reset scout attempt"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnGroup,
                    Sequence = 1,
                    SpawnGroupId = 50,
                    AttemptKey = "scout",
                    Comment = "Spawn scout creatures"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnDynamicObject,
                    Sequence = 2,
                    DynamicObjectKey = "bootcamp-crate",
                    EntityClassId = 3147,
                    PosX = 12,
                    PosY = 0,
                    PosZ = 6,
                    Orientation = 0.5,
                    InitialInteractionEnabled = true,
                    AttemptKey = "scout",
                    Comment = "Spawn scout crate"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    StepId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ScheduleScenario,
                    Sequence = 3,
                    TargetScenarioId = 62,
                    DelayMilliseconds = 5000,
                    AttemptKey = "scout",
                    Comment = "Schedule scout follow-up"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 63,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnGroup,
                    Sequence = 1,
                    SpawnGroupId = 50,
                    AttemptKey = "medic",
                    Comment = "Spawn medic creatures"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 63,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnDynamicObject,
                    Sequence = 2,
                    DynamicObjectKey = "bootcamp-crate",
                    EntityClassId = 3147,
                    PosX = 12,
                    PosY = 0,
                    PosZ = 6,
                    Orientation = 0.5,
                    InitialInteractionEnabled = true,
                    AttemptKey = "medic",
                    Comment = "Spawn medic crate"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 62,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.EmitScenarioEvent,
                    Sequence = 1,
                    ScenarioEventId = 99,
                    Comment = "Scout follow-up placeholder"
                });
            return fixture;
        }

        private static MissionContentFixture CreateEscortRuntimeFixture()
        {
            var fixture = CreateRuntimeActorFixture();
            fixture.ScenarioSteps.Add(new MissionScenarioStepEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 60,
                StepId = 99,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionScenarioStepKind.EscortSpawnGroup,
                Sequence = 4,
                SpawnGroupId = 50,
                Comment = "Escort the spawned group"
            });
            return fixture;
        }

        private static MissionContentFixture CreateObjectiveTransferFixture(uint destinationMapContextId = 1221)
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Objectives.Clear();
            fixture.Actions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.MapContextIds.Add(destinationMapContextId);
            fixture.Objectives.AddRange(
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2101,
                    ClientBodyTextId = 2102,
                    Ordinal = 1,
                    InitialState = (byte)MissionObjectiveState.Incomplete,
                    IsRequired = true,
                    Comment = "Primary objective"
                },
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 11,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2201,
                    ClientBodyTextId = 2202,
                    Ordinal = 2,
                    InitialState = (byte)MissionObjectiveState.Inactive,
                    IsRequired = false,
                    Comment = "Follow-up objective"
                },
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 12,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2301,
                    ClientBodyTextId = 2302,
                    Ordinal = 3,
                    InitialState = (byte)MissionObjectiveState.Incomplete,
                    IsRequired = false,
                    Comment = "Deadline objective"
                });
            fixture.Transitions.Clear();
            fixture.Actions.Clear();
            fixture.Triggers.Clear();
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 12,
                TransitionId = 30,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = "Deadline completes objective"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 12,
                TransitionId = 30,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.TimerElapsed,
                Sequence = 1,
                DurationSeconds = 30,
                Comment = "Deadline"
            });
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.AddRange(
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.RevealObjective,
                    Sequence = 1,
                    TargetObjectiveId = 11,
                    Comment = "Reveal 11"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ActivateObjective,
                    Sequence = 2,
                    TargetObjectiveId = 11,
                    Comment = "Activate 11"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.StartDeadline,
                    Sequence = 3,
                    DelayMilliseconds = 30000,
                    Comment = "Start deadline"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 4,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.CompleteObjective,
                    Sequence = 4,
                    TargetObjectiveId = 10,
                    Comment = "Complete objective 10"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 5,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.FailObjective,
                    Sequence = 5,
                    TargetObjectiveId = 11,
                    Comment = "Fail objective 11"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 6,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SatisfyDeadline,
                    Sequence = 6,
                    Comment = "Satisfy deadline"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 7,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.EmitScenarioEvent,
                    Sequence = 7,
                    ScenarioEventId = 1,
                    Comment = "Emit scenario event"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 8,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.TransferPlayer,
                    Sequence = 8,
                    MapContextId = destinationMapContextId,
                    PosX = 4,
                    PosY = 0,
                    PosZ = 8,
                    Orientation = 1.5,
                    Comment = "Transfer player"
                });
            return fixture;
        }

        private static MissionContentFixture CreateResetFixture()
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Scenarios.Add(new MissionScenarioEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ScenarioId = 61,
                Requirement = MissionContentRequirement.Required,
                Name = "Repeatable",
                Comment = "Repeatable scenario"
            });
            fixture.ScenarioSteps.Clear();
            fixture.ScenarioSteps.AddRange(
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.ResetAttempt,
                    Sequence = 1,
                    TargetScenarioId = 61,
                    Comment = "Reset scenario 61"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.EmitScenarioEvent,
                    Sequence = 1,
                    AttemptKey = "scout",
                    ScenarioEventId = 1,
                    Comment = "Repeatable step"
                });
            return fixture;
        }

        private static MissionContentFixture CreateSharedRuntimeFixture(bool includeSecondMission)
        {
            var fixture = CreateRuntimeActorFixture();
            fixture.Indicators.Clear();
            fixture.Scenarios.Clear();
            fixture.ScenarioSteps.Clear();
            AddRuntimeScenarioAuthoring(fixture, 321);
            if (includeSecondMission)
            {
                AddValidMissionSkeleton(fixture, 322);
                AddRuntimeScenarioAuthoring(fixture, 322);
            }

            return fixture;
        }

        private static void AddRuntimeScenarioAuthoring(MissionContentFixture fixture, uint missionId)
        {
            fixture.SpawnGroups.RemoveAll(entry => entry.MissionId == missionId);
            fixture.Spawns.RemoveAll(entry => entry.MissionId == missionId);
            fixture.Scenarios.RemoveAll(entry => entry.MissionId == missionId);
            fixture.ScenarioSteps.RemoveAll(entry => entry.MissionId == missionId);
            fixture.SpawnGroups.Add(new MissionSpawnGroupEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                SpawnGroupId = 50,
                Requirement = MissionContentRequirement.Required,
                AreaId = null,
                MapContextId = 1220,
                Enabled = false,
                SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled,
                RespawnSeconds = null,
                Comment = $"Scenario spawn group for mission {missionId}"
            });
            fixture.Spawns.Add(new MissionSpawnEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                SpawnGroupId = 50,
                SpawnId = 1,
                CreatureId = 501,
                PosX = 8,
                PosY = 9,
                PosZ = 10,
                Rotation = 0.25,
                Quantity = 1
            });
            fixture.Scenarios.AddRange(
                new MissionScenarioEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    Requirement = MissionContentRequirement.Required,
                    Name = $"Mission{missionId}Spawn",
                    Comment = $"Mission {missionId} spawn scenario"
                },
                new MissionScenarioEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    Requirement = MissionContentRequirement.Required,
                    Name = $"Mission{missionId}Despawn",
                    Comment = $"Mission {missionId} despawn scenario"
                });
            fixture.ScenarioSteps.AddRange(
                new MissionScenarioStepEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnGroup,
                    Sequence = 1,
                    SpawnGroupId = 50,
                    Comment = $"Mission {missionId} spawn creatures"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ScenarioId = 60,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.SpawnDynamicObject,
                    Sequence = 2,
                    DynamicObjectKey = "shared-crate",
                    EntityClassId = 3147,
                    PosX = 12,
                    PosY = 0,
                    PosZ = 6,
                    Orientation = 0.5,
                    InitialInteractionEnabled = true,
                    Comment = $"Mission {missionId} spawn crate"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    StepId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.DespawnGroup,
                    Sequence = 1,
                    SpawnGroupId = 50,
                    Comment = $"Mission {missionId} despawn creatures"
                },
                new MissionScenarioStepEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ScenarioId = 61,
                    StepId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionScenarioStepKind.DespawnDynamicObject,
                    Sequence = 2,
                    DynamicObjectKey = "shared-crate",
                    Comment = $"Mission {missionId} despawn crate"
                });
        }

        private static void AddValidMissionSkeleton(MissionContentFixture fixture, uint missionId)
        {
            fixture.Definitions.Add(new MissionContentDefinitionEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                Requirement = MissionContentRequirement.Required,
                ClientNameTextId = 2000 + missionId,
                GiverId = 101,
                ReceiverId = 102,
                Level = 9,
                GroupType = 2,
                CategoryId = 3,
                Shareable = false,
                RadioCompleteable = false,
                Comment = $"Mission {missionId}"
            });
            fixture.Objectives.AddRange(
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2100 + missionId,
                    ClientBodyTextId = 2200 + missionId,
                    Ordinal = 1,
                    InitialState = (byte)MissionObjectiveState.Incomplete,
                    IsRequired = true,
                    Comment = $"Mission {missionId} objective 10"
                },
                new MissionObjectiveDefinitionEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 11,
                    Requirement = MissionContentRequirement.Required,
                    ClientNameTextId = 2300 + missionId,
                    ClientBodyTextId = 2400 + missionId,
                    Ordinal = 2,
                    InitialState = (byte)MissionObjectiveState.Inactive,
                    IsRequired = true,
                    Comment = $"Mission {missionId} objective 11"
                });
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = $"Mission {missionId} conversation completion"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.Conversation,
                Sequence = 1,
                NpcPackageId = 77,
                PlayerFlagId = 11,
                Comment = $"Mission {missionId} completion conversation"
            });
            fixture.Actions.AddRange(
                new MissionActionEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 1,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.CompleteObjective,
                    Sequence = 1,
                    TargetObjectiveId = 10,
                    ObjectiveState = (byte)MissionObjectiveState.Completed,
                    Comment = $"Mission {missionId} complete current objective"
                },
                new MissionActionEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 2,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.RevealObjective,
                    Sequence = 2,
                    TargetObjectiveId = 11,
                    Comment = $"Mission {missionId} reveal objective 11"
                },
                new MissionActionEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 3,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.ActivateObjective,
                    Sequence = 3,
                    TargetObjectiveId = 11,
                    ObjectiveState = (byte)MissionObjectiveState.Incomplete,
                    Comment = $"Mission {missionId} activate objective 11"
                },
                new MissionActionEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    ObjectiveId = 10,
                    TransitionId = 20,
                    ActionId = 4,
                    Requirement = MissionContentRequirement.Required,
                    Kind = MissionActionKind.GrantReward,
                    Sequence = 4,
                    RewardId = 40,
                    Comment = $"Mission {missionId} reward"
                });
            fixture.Rewards.Add(new MissionRewardDefinitionEntry
            {
                MissionId = missionId,
                ContentRevision = "deployment_11",
                RewardId = 40,
                Requirement = MissionContentRequirement.Required,
                Experience = 125,
                Credits = 75,
                Prestige = 10,
                SelectionCount = 1,
                Comment = $"Mission {missionId} reward"
            });
            fixture.RewardItems.AddRange(
                new MissionRewardItemEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    RewardId = 40,
                    ItemId = 1,
                    Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 28,
                    Quantity = 2
                },
                new MissionRewardItemEntry
                {
                    MissionId = missionId,
                    ContentRevision = "deployment_11",
                    RewardId = 40,
                    ItemId = 2,
                    Kind = MissionRewardItemKind.Selectable,
                    ItemTemplateId = 29,
                    Quantity = 1
                });
        }

        private static void ReplaceScenarioRewardWithNoSelectionPackage(
            MissionContentFixture fixture,
            uint rewardId)
        {
            fixture.Rewards.Add(new MissionRewardDefinitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                RewardId = rewardId,
                Requirement = MissionContentRequirement.Required,
                Experience = 125,
                Credits = 75,
                Prestige = 10,
                SelectionCount = 0,
                Comment = "Scenario-safe reward"
            });
            fixture.RewardItems.AddRange(
                new MissionRewardItemEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = rewardId,
                    ItemId = 41,
                    Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 28,
                    Quantity = 2
                },
                new MissionRewardItemEntry
                {
                    MissionId = 321,
                    ContentRevision = "deployment_11",
                    RewardId = rewardId,
                    ItemId = 42,
                    Kind = MissionRewardItemKind.Fixed,
                    ItemTemplateId = 29,
                    Quantity = 1
                });
        }

        private sealed class MissionContentLoadingFactory : IGameUnitOfWorkFactory
        {
            private readonly MissionTestContext _charFactory;
            private readonly IWorldUnitOfWork _worldUnit;

            internal MissionContentLoadingFactory(
                MissionTestContext charFactory,
                IWorldUnitOfWork worldUnit)
            {
                _charFactory = charFactory;
                _worldUnit = worldUnit;
            }

            public ICharUnitOfWork CreateChar() => _charFactory.CreateChar();
            public IWorldUnitOfWork CreateWorld() => _worldUnit;
        }

        private sealed class ManagerInstances : IDisposable
        {
            private readonly FieldInfo _mapsField = typeof(MapChannelManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _objectsField = typeof(DynamicObjectManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _creaturesField = typeof(CreatureManager)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly FieldInfo _missionsField = typeof(MissionApplication)
                .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
            private readonly object _previousMaps;
            private readonly object _previousObjects;
            private readonly object _previousCreatures;
            private readonly object _previousMissions;

            internal ManagerInstances(
                MapChannelManager maps,
                DynamicObjectManager objects,
                CreatureManager creatures,
                MissionApplication missions)
            {
                _previousMaps = _mapsField.GetValue(null);
                _previousObjects = _objectsField.GetValue(null);
                _previousCreatures = _creaturesField.GetValue(null);
                _previousMissions = _missionsField.GetValue(null);
                _mapsField.SetValue(null, maps);
                _objectsField.SetValue(null, objects);
                _creaturesField.SetValue(null, creatures);
                _missionsField.SetValue(null, missions);
            }

            public void Dispose()
            {
                _mapsField.SetValue(null, _previousMaps);
                _objectsField.SetValue(null, _previousObjects);
                _creaturesField.SetValue(null, _previousCreatures);
                _missionsField.SetValue(null, _previousMissions);
            }
        }
    }
}
