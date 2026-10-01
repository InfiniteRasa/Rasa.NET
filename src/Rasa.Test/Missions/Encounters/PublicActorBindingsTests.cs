using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Game.Missions.World;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.Missions.Encounters
{
    [TestClass]
    [DoNotParallelize]
    public class PublicActorBindingsTests
    {
        [TestMethod]
        public void RepeatedCatalogLoadsKeepTheCurrentPublicReservationAndPolicy()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            harness.SpawnWorld(100, WildernessOpeningWorldV1.RangerSpawnId);
            var ranger = harness.Npc(WildernessOpeningWorldV1.RangerSpawnId);
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, harness.Npc(100).EntityId, 1407));
            var handle = harness.Manager.PublicActors.Handle(harness.Map, WildernessOpeningWorldV1.RangerSpawnId);
            var policy = CreatureGameplayRules.Policy(ranger);
            Assert.IsNotNull(handle);

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);

            Assert.AreEqual(handle, harness.Manager.PublicActors.Handle(harness.Map, WildernessOpeningWorldV1.RangerSpawnId));
            Assert.IsTrue(harness.Manager.PublicActors.TryResolve(harness.Map, handle, out var current));
            Assert.AreSame(ranger, current);
            Assert.AreSame(policy, CreatureGameplayRules.Policy(ranger));
            using var unit = harness.CreateChar();
            var lease = unit.CharacterMissions.Runtime.Leases(handle.RunId).Single();
            Assert.AreEqual(handle.Generation, lease.Generation);
            Assert.AreEqual("Reserved", lease.State);
        }

        [TestMethod]
        public void EquivalentDeclarationsDoNotReplaceALivePolicy()
        {
            using var context = MissionTestContext.WithDefinitions(321);
            var actor = AddPublicActor(context);
            var binding = new PublicEncounterBinding(321, 77, "escort", "example.escort");
            context.Manager.PublicActors.Bind(binding, new ActorGameplayPolicy
            {
                Invulnerable = true, Tags = new[] { "guide", "protected" }
            });
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            var policy = CreatureGameplayRules.Policy(actor);

            context.Manager.PublicActors.Bind(binding with { }, new ActorGameplayPolicy
            {
                Invulnerable = true, Tags = new[] { "protected", "guide" }
            });

            Assert.AreEqual(handle, context.Manager.PublicActors.Handle(context.Map, 77));
            Assert.AreSame(policy, CreatureGameplayRules.Policy(actor));
            Assert.IsTrue(CreatureGameplayRules.IsInvulnerable(actor));
            Assert.IsFalse(actor.IsInteractable);
        }

        [TestMethod]
        public void ChangedOrRemovedDeclarationsFailWithoutPartiallyAddingBindings()
        {
            using var context = MissionTestContext.WithDefinitions(321, 322);
            var actor = AddPublicActor(context);
            var binding = new PublicEncounterBinding(321, 77, "escort", "example.escort");
            var policy = new ActorGameplayPolicy { Invulnerable = true };
            context.Manager.PublicActors.Bind(binding, policy);
            Assert.IsTrue(context.Manager.AcceptOfferedMission(context.Client, actor.EntityId, 321));
            var handle = context.Manager.PublicActors.Handle(context.Map, 77);
            var activePolicy = CreatureGameplayRules.Policy(actor);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                context.Manager.PublicActors.Bind(binding with { SpawnId = 78 }, policy));
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                context.Manager.PublicActors.Bind(binding, new ActorGameplayPolicy()));
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                context.Manager.PublicActors.ReloadBindings(Array.Empty<(PublicEncounterBinding, ActorGameplayPolicy)>()));
            Assert.ThrowsExactly<InvalidOperationException>(() => context.Manager.PublicActors.ReloadBindings(new[]
            {
                (new PublicEncounterBinding(322, 78, "guide", "example.escort"), policy),
                (binding with { OwnerLossPolicy = "Wait" }, policy)
            }));

            Assert.IsFalse(context.Manager.PublicActors.HasBinding(322));
            Assert.AreEqual(handle, context.Manager.PublicActors.Handle(context.Map, 77));
            Assert.AreSame(activePolicy, CreatureGameplayRules.Policy(actor));
            Assert.IsTrue(context.Manager.PublicActors.TryResolve(context.Map, handle, out var current));
            Assert.AreSame(actor, current);
            using var unit = context.CreateChar();
            Assert.AreEqual("Reserved", unit.CharacterMissions.Runtime.Leases(handle.RunId).Single().State);
        }

        private static Creature AddPublicActor(MissionTestContext context)
        {
            var actor = context.AddNpc(77);
            actor.IsInteractable = true;
            actor.SpawnPool = new SpawnPool
            {
                DbId = 77, MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map, Position = actor.Position
            };
            context.Map.SpawnPools.Add(actor.SpawnPool);
            context.Drain();
            return actor;
        }
    }
}
