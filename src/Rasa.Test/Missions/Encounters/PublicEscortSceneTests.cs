using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Encounters
{
    using Rasa.Data;
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class PublicEscortSceneTests
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ProductionAcceptanceDrivesTheSharedActorRouteThenResetsForTheNextCharacter(bool ownerLost)
        {
            using var context = MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1));
            var second = context.CreateAdditionalClient(2);
            var start = SceneNavigationFixture.Attach(context.Map);
            var first = context.Map.NavMesh.Nearest(start + new System.Numerics.Vector3(2, 0, 0)).Value;
            var last = context.Map.NavMesh.Nearest(start + new System.Numerics.Vector3(4, 0, 0)).Value;
            var actor = context.AddNpc(77, position: start);
            actor.RunSpeed = 6.5f;
            actor.WalkSpeed = 3;
            actor.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            actor.SpawnPool = new SpawnPool
            {
                DbId = 77, MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map, Position = actor.Position
            };
            context.Map.SpawnPools.Add(actor.SpawnPool);
            var bindings = new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition> { ["guide"] = new("guide", SceneActorKind.PublicSpawn, 77) },
                new Dictionary<string, SceneRoute>
                {
                    ["outbound"] = new("outbound", new[]
                    {
                        new SceneWaypoint(new ScenePosition(first.X, first.Y, first.Z)),
                        new SceneWaypoint(new ScenePosition(last.X, last.Y, last.Z))
                    })
                }, new Dictionary<uint, SceneSequence>());
            context.Manager.Scenes.Bind(321, "example.escort", bindings);
            context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, 77, "guide", "example.escort"));

            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            Assert.IsNotNull(actor.Controller.ScriptedMove, "The real acceptance path must start movement, not just reserve a type.");
            Assert.IsFalse(context.Manager.AcceptOfferedMission(second, actor.EntityId, 321));
            Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
            if (ownerLost)
            {
                context.Map.ClientList.Remove(context.Client);
                for (var tick = 0; tick < 10; tick++)
                    context.Manager.Scenes.Tick(context.Map);
                Assert.IsTrue(actor.IsInteractable);
                Assert.AreEqual(MissionObjectiveState.Incomplete, context.Client.Player.Missions[321].Objectives[1].State);
                Assert.AreEqual(6.5f, actor.RunSpeed);
                Assert.IsTrue(context.Manager.AcceptOfferedMission(second, actor.EntityId, 321));
                return;
            }
            for (var tick = 0; tick < 60 && !actor.IsInteractable; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(context.Map, 250);
                context.Manager.Scenes.Tick(context.Map);
            }

            Assert.AreEqual(MissionObjectiveState.Completed, context.Client.Player.Missions[321].Objectives[1].State);
            Assert.IsTrue(actor.IsInteractable, "The NPC remains busy through its authored return.");
            Assert.AreEqual(actor.SpawnPool.Position, actor.Position);
            Assert.AreEqual(1, context.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Distinct().Count(creature => creature.DbId == 77));
            Assert.IsTrue(context.Manager.AcceptOfferedMission(second, actor.EntityId, 321));
            Assert.AreEqual(MissionObjectiveState.Incomplete, second.Player.Missions[321].Objectives[1].State,
                "Waiting for the previous run must not copy its progress.");
        }
    }
}
