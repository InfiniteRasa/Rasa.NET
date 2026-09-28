extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Scenes
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Game.Missions.World;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions.Encounters;

    [TestClass]
    [DoNotParallelize]
    public class PublicSceneLifecycleTests
    {
        [TestMethod]
        public void PublicDisconnectAndFreshClientEntryRecoverTheSameSceneWithTheNewClient()
        {
            var now = DateTime.UnixEpoch;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => now);
            context.Manager.Scenes.Bind(321, "data.sequence", TimedSignal());
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Manager.PublishInitialState(context.Client);
            var maps = Maps(context);
            context.Client.State = ClientState.Disconnected;

            maps.CleanupDisconnected(context.Client);
            var reconnected = context.CreateCompetingClient();
            context.Manager.PublishInitialState(reconnected);
            now = now.AddSeconds(3);
            context.Manager.Scenes.Tick(context.Map);

            Assert.AreEqual(MissionObjectiveState.Completed, reconnected.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
        }

        [TestMethod]
        public void PublicMapTransferAndReturnPauseAnActiveSceneWaitAndResumeOnce()
        {
            var now = DateTime.UnixEpoch;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => now);
            context.Manager.Scenes.Bind(321, "data.sequence", TimedSignal(SceneClockPolicy.ActiveScene));
            var giver = context.AddNpc(77);
            giver.State = CharacterState.Idle;
            giver.AppearanceData = new Dictionary<EquipmentData, AppearanceData>();
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Manager.PublishInitialState(context.Client);
            var maps = Maps(context);
            var destination = new MapChannel
            {
                MapInfo = new MapInfo(1300, "destination", 1556, 0),
                ClientList = new()
            };
            maps.MapChannelArray.Add(1300, destination);
            Assert.IsTrue(maps.ChangeMap(context.Client, destination, Vector3.Zero, 0));
            maps.MapLoaded(context.Client);
            now = now.AddSeconds(10);
            context.Manager.Scenes.Tick(context.Map);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);

            Assert.IsTrue(maps.ChangeMap(context.Client, context.Map, Vector3.Zero, 0));
            maps.MapLoaded(context.Client);
            context.Manager.Scenes.Tick(context.Map);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
            now = now.AddSeconds(2);
            context.Manager.Scenes.Tick(context.Map);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[1].State);
        }

        [TestMethod]
        public void ContinuePolicyKeepsTheSharedSceneRunningForAnEligibleParticipantAfterOwnerDisconnect()
        {
            var now = DateTime.UnixEpoch;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1),
                creditPolicy: new MissionCreditPolicy(MissionCreditMode.EncounterParticipants, 20), utcNow: () => now);
            var member = context.CreateAdditionalClient(2);
            var giver = context.AddNpc(77);
            giver.SpawnPool = new SpawnPool
            {
                DbId = 77, RuntimeMapChannel = context.Map, MapContextId = context.Map.MapInfo.MapContextId,
                Position = giver.Position
            };
            context.Map.SpawnPools.Add(giver.SpawnPool);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(member, giver.EntityId, 321));
            using var party = new GroupMissionCreditTests.PartyScope(context.Client, member);
            var sequence = TimedSignal();
            var bindings = new SceneBindings(sequence.Release,
                new Dictionary<string, SceneActorDefinition> { ["guide"] = new("guide", SceneActorKind.PublicSpawn, 77) },
                new Dictionary<string, SceneRoute>(), sequence.Sequences.ToDictionary(entry => entry.Key, entry => entry.Value));
            context.Manager.Scenes.Bind(321, "data.sequence", bindings);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "guide", "data.sequence", "Continue", true));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Manager.PublishInitialState(context.Client);
            context.Client.State = ClientState.Disconnected;
            Maps(context).CleanupDisconnected(context.Client);

            now = now.AddSeconds(3);
            context.Manager.Scenes.Tick(context.Map);

            Assert.AreEqual(MissionObjectiveState.Completed, member.Player.Missions[321].Objectives[1].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, giver));
            Assert.IsFalse(giver.IsInteractable);
        }

        [TestMethod]
        public void DisconnectPausePersistenceFailureStopsWorkAndRecoversTheRemainingActiveWait()
        {
            var now = DateTime.UnixEpoch;
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => now);
            context.Manager.Scenes.Bind(321, "data.sequence", TimedSignal(SceneClockPolicy.ActiveScene));
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Manager.PublishInitialState(context.Client);
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionTimerEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected detach timer persistence failure.");
            };
            context.Client.State = ClientState.Disconnected;

            Maps(context).CleanupDisconnected(context.Client);
            Assert.IsNull(context.Client.Player.MapChannel);
            now = now.AddSeconds(10);
            context.Manager.Scenes.Tick(context.Map);
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
            context.BeforeSave = null;
            var reconnected = context.CreateCompetingClient();
            context.Manager.PublishInitialState(reconnected);
            context.Manager.Scenes.Tick(context.Map);
            Assert.AreEqual(MissionObjectiveState.Incomplete, reconnected.Player.Missions[321].Objectives[1].State);
            now = now.AddSeconds(2);
            context.Manager.Scenes.Tick(context.Map);
            Assert.AreEqual(MissionObjectiveState.Completed, reconnected.Player.Missions[321].Objectives[1].State);
        }

        [TestMethod]
        public void ReturnMapLoadedDeliversPendingSceneInputAfterTheCharacterBecomesIngame()
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => DateTime.UnixEpoch);
            context.Manager.Scenes.Bind(321, "data.sequence", TimedSignal());
            var giver = context.AddNpc(77);
            giver.State = CharacterState.Idle;
            giver.AppearanceData = new();
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Manager.PublishInitialState(context.Client);
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionSceneEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected scene dispatch failure before travel.");
            };
            context.Manager.TryExecuteScenario(context.Client, 321, 1);
            context.BeforeSave = null;
            var maps = Maps(context);
            var destination = new MapChannel { MapInfo = new MapInfo(1300, "destination", 1556, 0), ClientList = new() };
            maps.MapChannelArray.Add(1300, destination);
            Assert.IsTrue(maps.ChangeMap(context.Client, destination, Vector3.Zero, 0));
            maps.MapLoaded(context.Client);
            Assert.IsTrue(maps.ChangeMap(context.Client, context.Map, Vector3.Zero, 0));

            maps.MapLoaded(context.Client);

            Assert.AreEqual(ClientState.Ingame, context.Client.State);
            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[1].State);
        }

        private static MapChannelManager Maps(MissionTestContext context)
        {
            var maps = new MapChannelManager(context, scenarioService: context.Manager.ScenarioService,
                updateCharacter: (_, _, _) => { }, refreshStats: (_, _) => { },
                assignPlayer: client => context.Manager.PublishInitialState(client), enterMapChannels: _ => { });
            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            return maps;
        }

        private static SceneBindings TimedSignal(SceneClockPolicy clock = SceneClockPolicy.WallClock) =>
            new("unversioned", new Dictionary<string, SceneActorDefinition>(), new Dictionary<string, SceneRoute>(),
                new Dictionary<uint, SceneSequence>
                {
                    [0] = new(timers: new[] { new SequenceTimer("credit", 2000, 1, clock) }),
                    [1] = new(signals: new[] { new SceneMissionSignal(321, 1, 1) })
                });
    }
}
