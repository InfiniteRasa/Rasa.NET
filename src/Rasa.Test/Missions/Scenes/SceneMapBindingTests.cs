using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Scenes
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Missions;
    using Rasa.Game.Missions.Content;
    using Rasa.Game.Missions.World;
    using Rasa.Managers;
    using Rasa.Missions.Content;
    using Rasa.Missions.Runtime;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;
    using Rasa.Test.Missions.Encounters;

    /// <summary>
    /// Where a mission's scene stands: on the map it is bound to (MissionSceneDefinition.BoundMapContextId), whatever map the
    /// mission was accepted on, and a transfer to the map its owner is already on is a move on that map (a passage).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SceneMapBindingTests
    {
        private const uint Bound = 9001;   // a map of this test's own, not the fixture's (1220)

        /// <summary>A scene map is a mission's own and names a map: zero, or one on an experience, is refused.</summary>
        [TestMethod]
        public void ASceneMapMustBeAMissionsAndNotZero()
        {
            static void Validate(uint mission, uint? map) => MissionSceneValidation.Validate(mission, "test",
                new MissionSceneDefinition { Script = "data.sequence", BoundMapContextId = map }, new[] { 1U });
            Validate(321, null);
            Validate(321, Bound);
            Assert.ThrowsExactly<MissionRuleException>(() => Validate(321, 0));
            Assert.ThrowsExactly<MissionRuleException>(() => Validate(0, Bound));
        }

        /// <summary>
        /// A scene bound to a map, its mission accepted on another: it waits there under a key of no map, starts when its
        /// owner comes onto a copy of its map, and goes with them to a later copy (a new squad's, one made again).
        /// </summary>
        [TestMethod]
        public void ASceneBoundToAMapWaitsForItsOwnerThereAndGoesToTheNextCopy()
        {
            using var fixture = new PublicSceneActorPolicyTests.Fixture();
            var context = fixture.Context;
            var accepted = context.Map;
            var giver = context.AddNpc(77);
            BindGuard(context);
            var copy = Copy(2);
            var next = Copy(3);
            try
            {
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
                Assert.IsTrue(context.Manager.Scenes.TryGetSceneMap(321, out var map));
                Assert.AreEqual(Bound, map);
                context.Manager.Scenes.Resume(context.Client);
                Assert.IsFalse(Guarded(accepted), "it waits off its map");
                Assert.AreEqual(SceneApplication.AwaitingMapKey(Bound), MapKey(context), "a key of no map");

                Enter(context, copy);
                Assert.IsTrue(Guarded(copy), "started on the copy of its map");
                Assert.AreEqual(PublicActorLeaseService.MapKey(copy), MapKey(context));

                Enter(context, next);
                Assert.IsTrue(Guarded(next), "and on the next copy");
                Assert.AreEqual(PublicActorLeaseService.MapKey(next), MapKey(context), "the run is in the new copy");
                Assert.IsFalse(Guarded(accepted));
            }
            finally
            {
                Enter(context, accepted, resume: false);
                foreach (var each in new[] { copy, next })
                    foreach (var creature in Creatures(each).ToArray())
                        CellManager.Instance.RemoveCreatureFromWorld(each, creature);
            }
        }

        /// <summary>
        /// A scene bound to a map, its mission accepted on a copy of that map (the giver stands inside, as Field Lieutenant
        /// Rapp in Ustor Yard): it starts there at once, and leaving and coming back does not start it a second time.
        /// </summary>
        [TestMethod]
        public void ASceneBoundToAMapStartsAtOnceWhenAcceptedOnACopyOfIt()
        {
            using var fixture = new PublicSceneActorPolicyTests.Fixture();
            var context = fixture.Context;
            var accepted = context.Map;
            BindGuard(context);
            var copy = Copy(2);
            var giver = context.AddNpc(77, copy);
            try
            {
                Enter(context, copy);
                Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, giver.EntityId, 321));
                context.Drain();
                Assert.IsTrue(Guarded(copy), "started on the copy it was accepted on");
                Assert.AreEqual(1, Guards(copy));
                Assert.AreEqual(PublicActorLeaseService.MapKey(copy), MapKey(context));

                Enter(context, accepted);
                Assert.IsFalse(Guarded(accepted), "not on another map");
                Enter(context, copy);
                Assert.IsTrue(Guarded(copy), "back on the copy");
                Assert.AreEqual(1, Guards(copy), "not started a second time");
                Assert.AreEqual(1, Runs(context), "one run");
            }
            finally
            {
                Enter(context, accepted, resume: false);
                foreach (var creature in Creatures(copy).ToArray())
                    CellManager.Instance.RemoveCreatureFromWorld(copy, creature);
            }
        }

        /// <summary>
        /// A transfer to the map the owner is on - their squad's copy too, which the map's own channel is not - takes them
        /// there as a passage on that map rather than a map change; refused when they cannot travel now.
        /// </summary>
        [TestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void ATransferToTheOwnersOwnMapIsAPassageOnIt(bool squadCopy, bool dead)
        {
            using var fixture = new PublicSceneActorPolicyTests.Fixture();
            var context = fixture.Context;
            context.Map.IsSquadInstance = squadCopy;
            var field = typeof(DynamicObjectManager).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var previous = field.GetValue(null);
            field.SetValue(null, new DynamicObjectManager(null, new MapChannelManager(null)));
            try
            {
                var world = new SceneWorldAdapter(context.Manager.PublicActors, (_, _) => { });
                var run = new SceneRun("passage", "unversioned", "data.sequence", 1, 1, 0, "{}", SceneStatus.Running, 1);
                world.Attach(run, new SceneBindings("unversioned", new Dictionary<string, SceneActorDefinition>(),
                    new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence> { [0] = new() }),
                    context.Client, context.Map);
                if (dead)
                    context.Client.Player.State = CharacterState.Dead;

                var result = world.Apply(run, new TransferIntent("go", context.Map.MapInfo.MapContextId, new ScenePosition(40, 0, 40), 1.5));

                if (dead)
                {
                    Assert.AreEqual(WorldEffectState.Failed, result.State);
                    Assert.AreEqual("The owner cannot travel now.", result.Failure);
                    Assert.IsNull(context.Client.PendingTransfer);
                }
                else
                {
                    Assert.AreEqual(WorldEffectState.Applied, result.State, result.Failure);
                    Assert.IsNotNull(context.Client.PendingTransfer, "on its way as a passage");
                }
                Assert.AreSame(context.Map, context.Client.Player.MapChannel, "no map change");
            }
            finally
            {
                context.Client.PendingTransfer = null;
                field.SetValue(null, previous);
            }
        }

        private static void BindGuard(MissionTestContext context)
        {
            context.Manager.Scenes.ClearBindings();
            context.Manager.Scenes.Bind(321, "data.sequence", new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>
                {
                    ["guard"] = new("guard", SceneActorKind.Creature, 510210, new ScenePosition(0, 0, 0))
                },
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = new(worldIntents: new[] { new EnsureActorIntent("ensure-guard", "guard") })
                }), Bound);
        }

        private static int Guards(MapChannel map) => Creatures(map).Count(creature =>
            creature.SpawnPool.SceneActorRole == "guard" && CreatureManager.IsLivingOnMap(map, creature));

        private static int Runs(MissionTestContext context)
        {
            using var unit = context.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(context.Client.Player.Id, 321).Count();
        }

        private static MapChannel Copy(uint instance) => new()
        {
            MapInfo = new MapInfo(Bound, "fixture_bound", 1556, 0),
            ClientList = new List<Client>(),
            PlayerLimit = 128,
            InstanceId = instance,
            IsSquadInstance = true
        };

        /// <summary>The owner leaves their map for <paramref name="to"/>, and their scenes are resumed there.</summary>
        private static void Enter(MissionTestContext context, MapChannel to, bool resume = true)
        {
            var client = context.Client;
            var from = client.Player.MapChannel;
            context.Manager.Scenes.Detach(client, from);
            CellManager.Instance.RemoveFromWorld(client);
            from.ClientList.Remove(client);
            client.Player.MapChannel = to;
            client.Player.MapContextId = to.MapInfo.MapContextId;
            to.ClientList.Add(client);
            CellManager.Instance.AddToWorld(client);
            if (resume)
                context.Manager.Scenes.Resume(client);
            context.Drain();
        }

        private static IEnumerable<Creature> Creatures(MapChannel map) => map.MapCellInfo.Cells.Values
            .SelectMany(cell => cell.CreatureList).Distinct().Where(creature => creature.SpawnPool?.SceneActorRole != null);

        private static bool Guarded(MapChannel map) => Creatures(map).Any(creature =>
            creature.SpawnPool.SceneActorRole == "guard" && CreatureManager.IsLivingOnMap(map, creature));

        private static string MapKey(MissionTestContext context)
        {
            using var unit = context.CreateChar();
            return unit.CharacterMissions.Runtime.Scenes(context.Client.Player.Id, 321).Single().MapKey;
        }
    }
}
