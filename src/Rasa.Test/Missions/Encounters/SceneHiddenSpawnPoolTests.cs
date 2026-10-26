using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Encounters
{
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;

    /// <summary>
    /// A scene hides a map's own spawn pool from its owner (HideSpawnPoolsIntent, HiddenCreatures): held by one of its
    /// actors, the pool's creatures are neither given to the owner's client nor heard on it, while everyone else sees
    /// them; shown again when that actor dies or is removed, or the run ends.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SceneHiddenSpawnPoolTests
    {
        private const uint Pool = 78;   // Fixture.StaticEnemy's

        [TestMethod]
        public void AHiddenPoolIsHiddenOnlyFromTheScenesOwner()
        {
            using var fixture = new PublicSceneActorPolicyTests.Fixture();
            var context = fixture.Context;
            var hidden = fixture.StaticEnemy();
            var other = context.CreateAdditionalClient(2);
            var run = Start(context);
            context.Drain();
            MissionTestContext.Drain(other);

            Assert.IsTrue(context.Manager.Scenes.Submit(run, Signal(1)));

            Assert.IsTrue(context.Drain().OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == hidden.EntityId),
                "taken off the owner's client");
            Assert.IsFalse(MissionTestContext.Drain(other).OfType<DestroyPhysicalEntityPacket>().Any(), "not off anyone else's");
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(hidden, context.Client));
            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(hidden, other));
            CreatureManager.Instance.CreateCreatureOnClient(context.Client, hidden);
            Assert.IsFalse(context.Drain().Any(), "nor given to the owner");
            NpcBarks.Bark(context.Map, hidden, 1);
            Assert.IsFalse(context.Drain().OfType<BarkPackage>().Any(), "nothing of it reaches the owner");
            Assert.IsTrue(MissionTestContext.Drain(other).OfType<BarkPackage>().Any(), "everyone else hears it");
        }

        [TestMethod]
        [DataRow("dies")]
        [DataRow("removed")]
        [DataRow("run ends")]
        public void AHiddenPoolIsShownAgainWhenItsHolderGoes(string end)
        {
            using var fixture = new PublicSceneActorPolicyTests.Fixture();
            var context = fixture.Context;
            var hidden = fixture.StaticEnemy();
            var run = Start(context);
            Assert.IsTrue(context.Manager.Scenes.Submit(run, Signal(1)));
            Assert.IsTrue(HiddenCreatures.IsHiddenFrom(hidden, context.Client));
            context.Drain();

            switch (end)
            {
                case "dies":
                    fixture.Kill(fixture.Actor(run, "holder"));
                    break;
                case "removed":
                    Assert.IsTrue(context.Manager.Scenes.Submit(run, Signal(2)));
                    break;
                default:
                    context.Manager.Scenes.LeaseReset(run);
                    break;
            }
            // Death is seen as the scene ticks; a removal or the run's end shows it at once.
            if (end == "dies")
                context.Manager.Scenes.Tick(context.Map);

            Assert.IsFalse(HiddenCreatures.IsHiddenFrom(hidden, context.Client), $"shown again ({end})");
            Assert.IsTrue(context.Drain().OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == hidden.EntityId),
                "and given back to the owner's client");
        }

        private static string Start(MissionTestContext context) =>
            context.Manager.Scenes.Start(context.Client, "data.sequence", new SceneBindings("unversioned",
                new Dictionary<string, SceneActorDefinition>
                {
                    ["holder"] = new("holder", SceneActorKind.Creature, 510210, new ScenePosition(0, 0, 0))
                },
                new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
                {
                    [0] = new(worldIntents: new[] { new EnsureActorIntent("ensure-holder", "holder") }),
                    [1] = new(worldIntents: new[] { new HideSpawnPoolsIntent("hide", "holder", new[] { Pool }) }),
                    [2] = new(worldIntents: new[] { new RemoveActorIntent("remove-holder", "holder") })
                }));

        private static SceneObservation Signal(uint sequence) => new(SceneEventKind.Signal, 1, SequenceId: sequence);
    }
}
