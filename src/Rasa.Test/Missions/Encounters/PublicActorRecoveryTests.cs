using System;
using Rasa.Missions.Scenes;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Encounters
{
    using Rasa.Data;
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class PublicActorRecoveryTests
    {
        [TestMethod]
        [DataRow("Running")]
        [DataRow("Ended")]
        [DataRow("Faulted")]
        public void RecoveryWaitsForRealStaticSpawningAndReleasesEvenTerminalSceneLeases(string status)
        {
            using var assets = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            using var context = CreateContext();
            var second = context.CreateAdditionalClient(2);
            var singleton = typeof(MissionApplication).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)!;
            var previous = singleton.GetValue(null);
            try
            {
                singleton.SetValue(null, context.Manager);
                var pool = Pool(context);
                var spawns = new SpawnPoolManager(null);
                spawns.SpawnPoolWorker(context.Map, 0);
                var actor = Actor(context);
                context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, pool.DbId, "guide", "example.escort"));
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
                var old = context.Manager.PublicActors.Handle(context.Map, pool.DbId);
                using (var unit = context.CreateChar())
                    unit.ExecuteTransaction(() => unit.CharacterMissions.Runtime.Scene(old.RunId).Status = status);
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, actor);
                pool.AliveCreatures = 0;
                pool.UpdateTimer = pool.RespawnTime;

                var restarted = new MissionApplication(context, context.Manager.LoadedMissions);
                restarted.PublicActors.Bind(new PublicEncounterBinding(321, pool.DbId, "guide", "example.escort"));
                singleton.SetValue(null, restarted);
                restarted.PublicActors.Recover(context.Map);
                Assert.IsTrue(restarted.PublicActors.IsReserved(context.Map, pool.DbId));
                Assert.IsNull(Actor(context));
                spawns.SpawnPoolWorker(context.Map, 0);
                actor = Actor(context);
                Assert.IsNotNull(actor);
                Assert.IsFalse(actor.IsInteractable, "The real spawn path must not advertise a still-leased NPC as available.");
                restarted.PublicActors.Tick(context.Map);
                Assert.IsTrue(actor.IsInteractable);
                Assert.IsFalse(restarted.PublicActors.TryResolve(context.Map, old, out _));
                Assert.IsTrue(restarted.AcceptOfferedMission(second, actor.EntityId, 321));
            }
            finally { singleton.SetValue(null, previous); }
        }

        [TestMethod]
        public void UnreachableReturnUsesNormalRespawnWithoutKillRewardsAndKeepsTheLeaseUntilThen()
        {
            using var assets = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            using var context = CreateContext();
            var singleton = typeof(MissionApplication).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static)!;
            var previous = singleton.GetValue(null);
            try
            {
                singleton.SetValue(null, context.Manager);
                var pool = Pool(context);
                var spawns = new SpawnPoolManager(null);
                spawns.SpawnPoolWorker(context.Map, 0);
                var actor = Actor(context);
                context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, pool.DbId, "guide", "example.escort"));
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
                var handle = context.Manager.PublicActors.Handle(context.Map, pool.DbId);
                var before = context.ReadRewardTotals();
                actor.Position = new Vector3(8, 0, 0);
                actor.RunSpeed = 0;
                Assert.IsTrue(context.Manager.PublicActors.BeginReset(context.Map, handle.RunId, "OwnerLost"));
                context.Manager.PublicActors.Tick(context.Map);
                Assert.IsNull(Actor(context));
                Assert.IsTrue(context.Manager.PublicActors.IsReserved(context.Map, pool.DbId));
                Assert.AreEqual(before, context.ReadRewardTotals());
                Assert.AreEqual(0, context.Map.LootDispensers.Count);
                spawns.SpawnPoolWorker(context.Map, pool.RespawnTime - 1);
                Assert.IsNull(Actor(context));
                spawns.SpawnPoolWorker(context.Map, 1);
                Assert.IsNotNull(Actor(context));
                Assert.IsFalse(Actor(context).IsInteractable);
                context.Manager.PublicActors.Tick(context.Map);
                Assert.IsTrue(Actor(context).IsInteractable);
                Assert.IsFalse(context.Manager.PublicActors.IsReserved(context.Map, pool.DbId));
            }
            finally { singleton.SetValue(null, previous); }
        }

        private static MissionTestContext CreateContext()
        {
            var objective = new MissionObjectiveDefinition(1, 1, 1, new uint?[3], 0,
                MissionObjectiveState.Incomplete, true, new Dictionary<uint, MissionObjectiveCounterDefinition>(),
                new Dictionary<uint, MissionObjectiveItemCounterDefinition>(), Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>());
            return MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>
            {
                [321] = new(321, "Recovery fixture", 321, 510203, 510203, 1, 1, 1, false, false, new[] { objective }, true)
            });
        }

        private static SpawnPool Pool(MissionTestContext context)
        {
            var pool = new SpawnPool
            {
                DbId = 510203, RuntimeMapChannel = context.Map, MapContextId = context.Map.MapInfo.MapContextId,
                Position = Vector3.Zero, RespawnTime = 2000, UpdateTimer = 2000,
                SpawnSlot = new List<SpawnPoolSlot> { new(510203, 1, 1) }
            };
            context.Map.SpawnPools.Add(pool);
            return pool;
        }
        private static Creature Actor(MissionTestContext context) => context.Map.MapCellInfo.Cells.Values
            .SelectMany(cell => cell.CreatureList).Distinct().SingleOrDefault(actor => actor.SpawnPool?.DbId == 510203);
    }
}
