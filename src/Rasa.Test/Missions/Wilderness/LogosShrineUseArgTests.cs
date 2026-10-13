using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using static Rasa.Test.Missions.Wilderness.WildernessAliaBranchesTests;

namespace Rasa.Test.Missions.Wilderness
{
    // The client uses an object with the UseObject arg id its class carries in usabledata, and
    // files the action under it: the windup and the recovery must come back with that arg. Of
    // the 170 classes of the logos shrines placed in the world, 168 carry 6. Two carry 1, the arg
    // of a footlocker: 21214, the shrine of FEW in the Howling Maw (logos 167, map 2051), and
    // 21414, the shrine of VICTORY on Concordia Divide (logos 341, map 1148). The recovery was
    // picked by the arg, so theirs went to the footlocker's, which found no footlocker and
    // taught nothing: neither could be learned.
    //
    // A shrine of map 1220's stands in for them here - it is the arg the client sends that
    // matters, not where the shrine is.
    [TestClass]
    [DoNotParallelize]
    public class LogosShrineUseArgTests
    {
        private const uint LogosId = 10;

        [TestMethod]
        [DataRow(DynamicObjectManager.FootlockerUseArgId, DisplayName = "arg 1, as for FEW and VICTORY")]
        [DataRow(DynamicObjectManager.LogosUseArgId, DisplayName = "arg 6, as for the other 168")]
        public void AShrineTeachesItsLogosWhateverArgTheClientUsesItWith(uint argId)
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(211);

            Use(harness, argId);

            Assert.IsTrue(harness.Client.Player.Logos.Contains(LogosId), "learned");
            using var unit = harness.CreateChar();
            Assert.AreEqual(1, unit.CharacterLogoses.GetLogos(harness.Client.Player.Id).Count(id => id == LogosId), "and kept");
        }

        [TestMethod]
        public void TheRecoveryGoesBackUnderTheArgTheClientSent()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(211);

            var sent = Use(harness, DynamicObjectManager.FootlockerUseArgId);

            var recovery = sent.OfType<PerformRecoveryPacket>().Single(packet => packet.ActionId == ActionId.UseObject);
            Assert.AreEqual(DynamicObjectManager.FootlockerUseArgId, recovery.ActionArgId, "the client knows its action by (UseObject, 1)");
        }

        [TestMethod]
        public void AShrineUsedAgainTeachesNothingMore()
        {
            using var harness = CreateHarness();
            harness.SpawnWorld(211);

            Use(harness, DynamicObjectManager.FootlockerUseArgId);
            Use(harness, DynamicObjectManager.FootlockerUseArgId);

            Assert.AreEqual(1, harness.Client.Player.Logos.Count(id => id == LogosId));
        }

        /// <summary>The shrine used, from where it stands, and the use's ten seconds run out.</summary>
        private static System.Collections.Generic.IReadOnlyList<Rasa.Packets.PythonPacket> Use(WildernessRuntimeTestHarness harness, uint argId)
        {
            var shrine = harness.Map.DynamicObjects.OfType<Logos>().Single(logos => logos.Id == LogosId);
            harness.MoveTo(shrine.Position);
            harness.Drain();

            harness.Objects.RequestUseObjectPacket(harness.Client, new RequestUseObjectPacket
            {
                EntityId = shrine.EntityId, ActionId = ActionId.UseObject, ActionArgId = argId
            });
            ActorActionManager.Instance.DoWork(harness.Map, 10001);

            return harness.Drain();
        }
    }
}
