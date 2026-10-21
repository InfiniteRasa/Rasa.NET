extern alias RasaGame;

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Test.World;

    // A reload the player breaks off themselves - a move, a crouch - is told back to them as an
    // interrupt (ActionInterrupt), which is what floats "Interrupted" over them; one that ends
    // because they armed another weapon is not.
    [TestClass]
    [DoNotParallelize]
    public class ReloadInterruptTests
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void AnInterruptedReloadIsToldToThePlayerAndOneEndedByAnotherWeaponIsNot(bool interrupted)
        {
            using var context = new WeaponAmmoContext(clip: 3);
            var manager = new ManifestationManager(context);
            context.AddAmmo(30);
            CellManager.Instance.AddToWorld(context.Client);
            WorldTestContext.Drain(context.Client);

            manager.RequestWeaponReload(context.Client, false);
            var reload = context.World.Map.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponReload);
            context.World.Map.PerformRecovery.Remove(reload);

            if (interrupted)
                reload.IsInrerrupted = true;
            else
                reload.SourceId = context.Weapon.EntityId + 1000;

            WorldTestContext.Drain(context.Client);
            manager.WeaponReload(reload);

            var sent = WorldTestContext.Drain(context.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).ToList();
            var closed = sent.OfType<UserActionFailedPacket>().Single();
            Assert.IsNull(closed.MsgId);
            Assert.AreEqual(3u, context.Weapon.CurrentAmmo, "nothing loaded");

            if (interrupted)
            {
                var interrupt = sent.OfType<ActionInterruptPacket>().Single();
                Assert.AreEqual(ActionId.WeaponReload, interrupt.ActionId);
                Assert.IsTrue(sent.IndexOf(interrupt) < sent.IndexOf(closed));
            }
            else
                Assert.IsFalse(sent.OfType<ActionInterruptPacket>().Any());
        }
    }
}
