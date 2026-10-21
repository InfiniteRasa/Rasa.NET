using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Definitions;
    using Rasa.Missions.Runtime;
    using Rasa.Structures;
    using Rasa.Structures.Missions;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// A mission counts what the player destroys (MissionProgressEventKind.ObjectDestroyed): a
    /// placed destructible (PlacedDestructibles) brought down by a player is recorded with their
    /// missions by its class, as a kill is - "Destroy 8 warnet nests".
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DestroyedObjectCreditTests
    {
        private const EntityClasses Nest = (EntityClasses)7099;      // UsableInertDestMinoswarnetnestDELETEV01
        private const EntityClasses Barrel = (EntityClasses)9260;    // UsableInertDestBaneBarrelV01

        private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _classes = new();

        [TestInitialize]
        public void Start()
        {
            PlacedDestructibles.Now = () => 7_000_000;
            PlacedDestructibles.Roll = (min, max) => 50_000;
            AddClass(Nest, "UsableInertDestMinoswarnetnestDELETEV01");
            AddClass(Barrel, "UsableInertDestBaneBarrelV01");
        }

        [TestCleanup]
        public void Restore()
        {
            PlacedDestructibles.MissionsOf = () => MissionApplication.Instance;
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();

            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            for (var i = _classes.Count - 1; i >= 0; i--)
            {
                if (_classes[i].Had)
                    classes[_classes[i].Id] = _classes[i].Was;
                else
                    classes.Remove(_classes[i].Id);
            }
        }

        [TestMethod]
        public void WhatThePlayerDestroysCountsByItsClassAndWhatFallsOnItsOwnDoesNot()
        {
            using var context = Counting(Nest, 2);
            var map = context.Map;
            PlacedDestructibles.MissionsOf = () => context.Manager;
            var here = map.MapInfo.MapContextId;
            PlacedDestructibles.Load(new[] { Row(1, Nest, here, 10, 10), Row(2, Nest, here, 14, 10), Row(3, Nest, here, 18, 10), Row(4, Barrel, here, 22, 10) });
            PlacedDestructibles.Place(map);

            try
            {
                var objective = context.Client.Player.Missions[321].Objectives[1];
                var player = context.Client.Player;

                // A barrel is not a nest; a nest that falls to nobody is nobody's.
                Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, 4), player));
                Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, 3), null));
                Assert.AreEqual(0U, objective.Counters[0]);

                Assert.IsTrue(PlacedDestructibles.Destroy(PlacedDestructibles.LiveOf(map, 1), player));
                Assert.AreEqual(1U, objective.Counters[0]);
                Assert.AreEqual(MissionObjectiveState.Incomplete, objective.State);

                // Shot down rather than taken down at once: the same.
                var second = PlacedDestructibles.LiveOf(map, 2);
                PlacedDestructibles.Hit(map, player, second.Object, (int)second.MaxHitPoints);
                Assert.IsTrue(second.IsDown);
                Assert.AreEqual(2U, objective.Counters[0]);
                Assert.AreEqual(MissionObjectiveState.Completed, objective.State);
            }
            finally
            {
                PlacedDestructibles.TakeAwayPutDown(map);
                PlacedDestructibles.Load(null);
            }
        }

        [TestMethod]
        public void ADestructionIsAKillForTheCountersThatTakeSeveralSubjects()
        {
            Assert.IsTrue(MissionProgressRule.IsKill(MissionProgressEventKind.ObjectDestroyed));
            var rule = MissionProgressRule.IncrementCounterOnAnySubject(MissionProgressEventKind.ObjectDestroyed, new uint[] { (uint)Nest, (uint)Barrel }, 0, 0, 8);
            Assert.AreEqual(MissionProgressEventKind.ObjectDestroyed, rule.Kind);
            Assert.AreEqual((uint)Nest, MissionProgressEvent.ObjectDestroyed((uint)Nest).SubjectId);
        }

        /// <summary>A mission of one objective: destroy <paramref name="count"/> of the class, counter 0.</summary>
        private static MissionTestContext Counting(EntityClasses classId, uint count)
        {
            var counters = new Dictionary<uint, MissionObjectiveCounterDefinition> { [0] = new(0, 0, count) };
            var objective = new MissionObjectiveDefinition(
                1, 1001, 1002, new uint?[] { 9000, null, null }, 0, MissionObjectiveState.Incomplete, true,
                counters, new Dictionary<uint, MissionObjectiveItemCounterDefinition>(), Array.Empty<MissionObjectiveConversation>(),
                Array.Empty<uint>(), Array.Empty<uint>(), Array.Empty<MissionIndicator>(),
                MissionProgressRule.IncrementCounterOnAnySubject(MissionProgressEventKind.ObjectDestroyed, new[] { (uint)classId }, 0, 0, count));
            var mission = new Mission(321, "Mission 321", 321, 77, 88, 5, 1, 2, true, false, new[] { objective }, true);
            var context = MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission> { [321] = mission });
            var giver = context.AddNpc(77);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
            context.Drain();
            return context;
        }

        private void AddClass(EntityClasses id, string name)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            _classes.Add((id, classes.TryGetValue(id, out var was), was));
            classes[id] = new EntityClass((uint)id, name, 0, 1, new List<AugmentationType> { AugmentationType.InertDestroyable }, true);
        }

        private static WorldDestructibleEntry Row(uint id, EntityClasses classId, uint map, float x, float z) => new()
        {
            Id = id, MapContextId = map, ClassId = (uint)classId, PosX = x, PosY = 0, PosZ = z, Rotation = 0, Comment = "fixture"
        };
    }
}
