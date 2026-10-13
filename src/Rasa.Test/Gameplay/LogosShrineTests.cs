using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// A logos shrine used to the end teaches its logos and plays OBJ_RECEIVE_LOGOS (416, 1) from
    /// the shrine at the player, whose FX family is OBJECT_ABILITY_LOGOS_ELEMENT_ACQUIRE.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LogosShrineTests
    {
        [TestCleanup]
        public void ForgetTheShrines() => LogosTitles.Load(null);

        [TestMethod]
        public void ALogosLearnedAtItsShrinePlaysTheAcquireEffectOnceAndOnlyThen()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var map = player.MapChannel;
            var entry = harness.WorldContext.Set<LogosEntry>().AsNoTracking().ToList().First(logos => !player.Logos.Contains(logos.Id));
            var shrine = new Logos(entry) { Position = player.Position, MapContextId = player.MapContextId, RuntimeMapChannel = map };
            var manager = new DynamicObjectManager(harness.Context, missionManager: harness.Manager,
                characterManager: new CharacterManager(harness.Context, harness.Manager));

            map.DynamicObjects.Add(shrine);

            try
            {
                harness.Drain();

                Use(manager, map, shrine, harness);

                Assert.IsTrue(player.Logos.Contains(entry.Id), "learned");

                var perform = harness.Drain().OfType<PerformObjectAbilityPacket>().Single();
                Assert.AreEqual(ActionId.ObjReceiveLogos, perform.ActionId);
                Assert.AreEqual(1u, perform.ActionArgId);
                CollectionAssert.AreEqual(new[] { player.EntityId }, perform.Hits);
                Assert.IsEmpty(perform.Misses);
                Assert.IsEmpty(perform.Args);

                // Used again: nothing to learn, nothing acquired.
                Use(manager, map, shrine, harness);
                Assert.IsEmpty(harness.Drain().OfType<PerformObjectAbilityPacket>().ToList());
            }
            finally
            {
                map.DynamicObjects.Remove(shrine);
            }
        }

        [TestMethod]
        public void AUseWalkedAwayFromLearnsAndPlaysNothing()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var player = harness.Client.Player;
            var map = player.MapChannel;
            var entry = harness.WorldContext.Set<LogosEntry>().AsNoTracking().ToList().First(logos => !player.Logos.Contains(logos.Id));
            var shrine = new Logos(entry)
            {
                Position = player.Position + new System.Numerics.Vector3(DynamicObjectManager.MaxUseDistance + 5, 0, 0),
                MapContextId = player.MapContextId,
                RuntimeMapChannel = map
            };
            var manager = new DynamicObjectManager(harness.Context, missionManager: harness.Manager,
                characterManager: new CharacterManager(harness.Context, harness.Manager));

            map.DynamicObjects.Add(shrine);

            try
            {
                harness.Drain();

                Use(manager, map, shrine, harness);

                Assert.IsFalse(player.Logos.Contains(entry.Id));
                Assert.IsEmpty(harness.Drain().OfType<PerformObjectAbilityPacket>().ToList());
            }
            finally
            {
                map.DynamicObjects.Remove(shrine);
            }
        }

        /// <summary>The player's use of the shrine finishing, as ActorActionManager hands it on.</summary>
        private static void Use(DynamicObjectManager manager, MapChannel map, Logos shrine, BootcampRuntimeTestHarness.Harness harness)
        {
            shrine.TriggeredByPlayers.Add(harness.Client);
            manager.LogosRecovery(map, new ActionData(harness.Client.Player, ActionId.UseObject, DynamicObjectManager.LogosUseArgId, 0) { SourceId = shrine.EntityId });
        }
    }
}
