extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Scenes
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Microsoft.EntityFrameworkCore;

    [TestClass]
    [DoNotParallelize]
    public class PublicSceneActorCleanupTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PublicResetOrFaultRemovesOnlyItsCreatedActorsBeforeTheNextRun(bool fault)
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var owned = fixture.Creature(fixture.RunId);
            var ownedObject = context.Map.DynamicObjects.Single(obj => obj.SceneRunId == fixture.RunId);
            var unrelated = fixture.Creature(fixture.UnrelatedRunId);
            var unrelatedObject = context.Map.DynamicObjects.Single(obj => obj.SceneRunId == fixture.UnrelatedRunId);
            if (fault)
            {
                fixture.UtcNow = DateTime.SpecifyKind(DateTime.MaxValue.AddMilliseconds(-1), DateTimeKind.Utc);
                Assert.IsTrue(context.Manager.Scenes.Submit(fixture.RunId,
                    new SceneObservation(SceneEventKind.Signal, 1, SequenceId: 1)));
                fixture.UtcNow = DateTime.UnixEpoch;
            }
            else
            {
                context.Client.State = ClientState.Disconnected;
                fixture.Maps.CleanupDisconnected(context.Client);
            }
            context.Manager.Scenes.Tick(context.Map);

            Assert.IsFalse(MapInstanceScope.Contains(context.Map, owned), "The ended run must not leave a live creature.");
            Assert.IsFalse(MapInstanceScope.Contains(context.Map, ownedObject), "The ended run must not leave a usable object.");
            Assert.IsFalse(context.Map.SpawnPools.Any(pool => pool.SceneRunId == fixture.RunId));
            Assert.IsFalse(context.Map.DynamicObjects.Any(obj => obj.SceneRunId == fixture.RunId));
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, unrelated));
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, unrelatedObject));
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, fixture.Giver));
            Assert.IsTrue(fixture.Giver.IsInteractable);

            Assert.IsTrue(context.Manager.AcceptOfferedMission(fixture.Other, fixture.Giver.EntityId, 321));
            Assert.AreEqual(2, context.Map.SpawnPools.Count(pool => pool.SceneRunId != null));
            Assert.AreEqual(2, context.Map.DynamicObjects.Count(obj => obj.SceneRunId != null));
            if (fault)
            {
                context.Client.State = ClientState.Disconnected;
                fixture.Maps.CleanupDisconnected(context.Client);
            }
            context.Manager.PublishInitialState(context.CreateCompetingClient());
            Assert.IsFalse(context.Map.SpawnPools.Any(pool => pool.SceneRunId == fixture.RunId));
        }

        [TestMethod]
        [DataRow("Wait")]
        [DataRow("Continue")]
        public void OwnerDetachPreservesSharedActorsAndTheirDefeatRouting(string policy)
        {
            using var fixture = new Fixture(policy);
            var context = fixture.Context;
            var owned = fixture.Creature(fixture.RunId);
            var ownedObject = context.Map.DynamicObjects.Single(obj => obj.SceneRunId == fixture.RunId);
            context.Client.State = ClientState.Disconnected;
            fixture.Maps.CleanupDisconnected(context.Client);
            context.Manager.Scenes.Tick(context.Map);

            Assert.IsTrue(MapInstanceScope.Contains(context.Map, owned));
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, ownedObject));
            Assert.IsFalse(fixture.Giver.IsInteractable);
            CreatureManager.Instance.HandleCreatureKill(context.Map, owned, null);
            using (var unit = context.CreateChar())
                Assert.IsNotNull(unit.CharacterMissions.Runtime.ActorState(fixture.RunId, "extra", 1));

            var reconnected = context.CreateCompetingClient();
            context.Manager.PublishInitialState(reconnected);
            fixture.Maps.CleanupDisconnected(context.Client);
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, ownedObject));
            Assert.AreEqual(1, context.Map.SpawnPools.Count(pool => pool.SceneRunId == fixture.RunId));
        }

        [TestMethod]
        public void AbandonmentAndLeaseResetCommitTogetherBeforeOwnedActorsAreRemoved()
        {
            using var fixture = new Fixture("Continue");
            var context = fixture.Context;
            var owned = fixture.Creature(fixture.RunId);
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            context.BeforeSave = database =>
            {
                if (database.ChangeTracker.Entries<MissionActorLeaseEntry>().Any(entry => entry.State == EntityState.Modified))
                    throw new DbUpdateException("Injected lease cancellation persistence failure.");
            };
            Assert.IsFalse(context.Manager.TryAbandon(context.Client, 321));
            Assert.IsTrue(MapInstanceScope.Contains(context.Map, owned));
            Assert.IsTrue(context.Manager.PublicActors.TryResolve(context.Map, handle, out _));
            Assert.IsTrue(context.Client.Player.Missions.ContainsKey(321));
            context.BeforeSave = null;

            Assert.IsTrue(context.Manager.TryAbandon(context.Client, 321));
            context.Manager.Scenes.Tick(context.Map);

            Assert.IsFalse(MapInstanceScope.Contains(context.Map, owned));
            Assert.IsFalse(context.Manager.PublicActors.TryResolve(context.Map, handle, out _));
            Assert.IsTrue(fixture.Giver.IsInteractable);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(fixture.Other, fixture.Giver.EntityId, 321));
        }

        [TestMethod]
        public void ReconciliationCannotRecreateActorsForAnInvalidatedAssignmentGeneration()
        {
            using var fixture = new Fixture();
            var context = fixture.Context;
            var owned = fixture.Creature(fixture.RunId);
            CellManager.Instance.RemoveCreatureFromWorld(context.Map, owned);
            using (var unit = context.CreateChar())
                unit.ExecuteTransaction(() => unit.CharacterMissions.GetByCharacterAndMission(1, 321).Generation++);

            context.Manager.Scenes.Reconcile(fixture.RunId, reconstruct: true);

            Assert.IsFalse(context.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Any(creature => creature.SpawnPool?.SceneRunId == fixture.RunId));
        }

        private sealed class Fixture : IDisposable
        {
            private readonly FieldInfo _missions = typeof(MissionApplication).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly FieldInfo _creatures = typeof(CreatureManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly object _previousMissions;
            private readonly object _previousCreatures;
            private readonly EntityClass _previousClass;
            internal MissionTestContext Context { get; }
            internal MapChannelManager Maps { get; }
            internal Client Other { get; }
            internal Creature Giver { get; }
            internal string RunId { get; }
            internal string UnrelatedRunId { get; }
            internal DateTime UtcNow { get; set; } = DateTime.UnixEpoch;

            internal Fixture(string ownerLossPolicy = "Reset")
            {
                Context = MissionTestContext.WithProgressMission(
                    MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1), utcNow: () => UtcNow);
                _previousMissions = _missions.GetValue(null);
                _previousCreatures = _creatures.GetValue(null);
                _missions.SetValue(null, Context.Manager);
                _previousClass = EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale];
                EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale] =
                    new EntityClass((uint)EntityClasses.HumanBaseMale, "fixture", 0, 0, new() { AugmentationType.Creature }, true);
                var creatures = new CreatureManager(Context, new ManifestationManager(Context), Context.Manager);
                creatures.LoadedCreatures[510210] = new Creature
                {
                    DbId = 510210, EntityClass = EntityClasses.HumanBaseMale,
                    AppearanceData = new(), State = CharacterState.Idle
                };
                _creatures.SetValue(null, creatures);
                Other = Context.CreateAdditionalClient(2);
                Giver = Context.AddNpc(77);
                Giver.State = CharacterState.Idle;
                Giver.AppearanceData = new();
                Giver.SpawnPool = new SpawnPool
                {
                    DbId = 77, RuntimeMapChannel = Context.Map,
                    MapContextId = Context.Map.MapInfo.MapContextId, Position = Giver.Position
                };
                Context.Map.SpawnPools.Add(Giver.SpawnPool);
                Maps = new MapChannelManager(Context, scenarioService: Context.Manager.ScenarioService);
                Maps.MapChannelArray.Add(Context.Map.MapInfo.MapContextId, Context.Map);
                UnrelatedRunId = Context.Manager.Scenes.Start(Other, "data.sequence", Bindings(false));
                Context.Manager.Scenes.Bind(321, "data.sequence", Bindings(true));
                Context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "guide", "data.sequence", ownerLossPolicy));
                Assert.IsTrue(Context.Manager.AcceptOfferedMission(Context.Client, Giver.EntityId, 321));
                Context.Manager.PublishInitialState(Context.Client);
                using var unit = Context.CreateChar();
                RunId = unit.CharacterMissions.Runtime.Scenes(1, 321).Single().RunId;
                Assert.IsNotNull(Creature(RunId));
            }

            internal Creature Creature(string runId) => Context.Map.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct().Single(creature => creature.SpawnPool?.SceneRunId == runId);

            private static SceneBindings Bindings(bool leased)
            {
                var actors = new Dictionary<string, SceneActorDefinition>
                {
                    ["extra"] = new("extra", SceneActorKind.Creature, 510210, new ScenePosition(0, 0, 0)),
                    ["cache"] = new("cache", SceneActorKind.Object, (uint)EntityClasses.HumanBaseMale, new ScenePosition(1, 0, 0))
                };
                if (leased)
                    actors["guide"] = new("guide", SceneActorKind.PublicSpawn, 77);
                return new SceneBindings("unversioned", actors, new Dictionary<string, SceneRoute>(),
                    new Dictionary<uint, SceneSequence>
                    {
                        [0] = new(worldIntents: new WorldIntent[]
                        {
                            new EnsureActorIntent("extra", "extra"), new EnsureActorIntent("cache", "cache")
                        }),
                        [1] = new(timers: new[] { new SequenceTimer("fault-clock", 1000, 0) })
                    });
            }

            public void Dispose()
            {
                foreach (var creature in Context.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Where(creature => creature.SpawnPool?.SceneRunId != null).Distinct().ToArray())
                    CellManager.Instance.RemoveCreatureFromWorld(Context.Map, creature);
                foreach (var obj in Context.Map.DynamicObjects.ToArray())
                    CellManager.Instance.RemoveFromWorld(Context.Map, obj);
                EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale] = _previousClass;
                _missions.SetValue(null, _previousMissions);
                _creatures.SetValue(null, _previousCreatures);
                Context.Dispose();
            }
        }
    }
}
