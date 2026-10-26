extern alias RasaGame;

using ClientState = RasaGame::Rasa.Data.ClientState;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.Communicator.Server;
using Rasa.Structures;

namespace Rasa.Test.World
{
    public partial class LandingZoneAssaultTests
    {
        private const string Warning = "The Bane are mustering for an attack on Landing Zone Control Point. Reinforcements requested!";
        private string[] Warnings() => _world.Drain().OfType<SystemMessagePacket>()
            .Select(p => p.TextMessage).Where(t => t.StartsWith("The Bane are mustering")).ToArray();

        [TestMethod]
        public void FiveMinuteWarningIsOncePerAssaultAndDoesNotStartItEarly()
        {
            _world.Drain();
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 3299999;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Warnings().Length);
            _now++;
            LandingZoneAssaults.Worker(_world.Map, Point);
            CollectionAssert.AreEqual(new[] { Warning }, Warnings());
            Assert.AreEqual(0, Pools.Length, "Warnings do not launch troops or relief squads.");
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 299999;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Warnings().Length);
            Assert.AreEqual(0, Pools.Length);
            _now++;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(6, Pools.Length);
            Assert.AreEqual(0, Warnings().Length);
            _now += 900000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 2400000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            CollectionAssert.AreEqual(new[] { Warning }, Warnings(), "The next hourly assault has its own warning.");
        }

        [TestMethod]
        public void ADelayedWorkerWarnsWithinTheWindowButNeverAfterTheAttackStarts()
        {
            _world.Drain();
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 3400000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            CollectionAssert.AreEqual(new[] { Warning }, Warnings());
            _now += 200000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(6, Pools.Length);
            Assert.AreEqual(0, Warnings().Length);
        }

        [TestMethod]
        public void JumpingPastTheDeadlineAndForcingImmediateAssaultsDoNotSendAdvanceWarnings()
        {
            _world.Drain();
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Assert.AreEqual(0, Warnings().Length);
            _now += 900000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 2700000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(6, Pools.Length);
            Assert.AreEqual(0, Warnings().Length, "A warning is not an after-the-fact attack notification.");
        }

        [TestMethod]
        public void AForcedLaunchAtTheWarningBoundaryDoesNotSendAnImmediateAdvanceWarning()
        {
            _world.Drain();
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 3300000;
            Assert.IsTrue(LandingZoneAssaults.TryStart(_world.Map, Point));
            Assert.AreEqual(6, Pools.Length);
            Assert.AreEqual(0, Warnings().Length);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void LosingAfsOwnershipOrDisablingAssaultsCancelsTheWarningAndRearmsOnRestart(bool disable)
        {
            _world.Drain();
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 3200000;
            if (disable) ControlPoints.Instance.Config.LandingZoneAssaultEnabled = false;
            else ControlPoints.Instance.SetOwner(Point, ControlPoints.Bane, null);
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 200000;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Warnings().Length);
            if (disable) ControlPoints.Instance.Config.LandingZoneAssaultEnabled = true;
            else ControlPoints.Instance.SetOwner(Point, ControlPoints.Afs, null);
            LandingZoneAssaults.Worker(_world.Map, Point);
            _now += 3299999;
            LandingZoneAssaults.Worker(_world.Map, Point);
            Assert.AreEqual(0, Warnings().Length);
            _now++;
            LandingZoneAssaults.Worker(_world.Map, Point);
            CollectionAssert.AreEqual(new[] { Warning }, Warnings());
        }

        [TestMethod]
        public void WarningReachesDistantZonePlayersButNotOtherZonesCopiesOrLoadingPlayers()
        {
            using var visitors = new WorldTestContext();
            var distant = visitors.CreateClient(-1000, -1000);
            var elsewhere = visitors.CreateClient();
            var copy = visitors.CreateClient();
            var loading = visitors.CreateClient();
            var added = new[] { distant, elsewhere, copy, loading };
            foreach (var client in added) _world.Map.ClientList.Add(client);
            distant.Player.MapChannel = _world.Map;
            elsewhere.Player.MapChannel = _world.Map;
            elsewhere.Player.MapContextId = 1148;
            loading.Player.MapChannel = _world.Map;
            try
            {
                // Loading clients have the same map but have not entered the world yet.
                loading.State = ClientState.Loading;
                _world.Drain();
                LandingZoneAssaults.Worker(_world.Map, Point);
                _now += 3300000;
                LandingZoneAssaults.Worker(_world.Map, Point);
                CollectionAssert.AreEqual(new[] { Warning }, Warnings());
                var messages = WorldTestContext.Drain(distant).Select(p => p.Message)
                    .OfType<Rasa.Packets.Protocol.CallMethodMessage>().Select(p => p.Packet).OfType<SystemMessagePacket>().ToArray();
                CollectionAssert.AreEqual(new[] { Warning }, messages.Select(p => p.TextMessage).ToArray());
                Assert.AreEqual(0, WorldTestContext.Drain(elsewhere).Count);
                Assert.AreEqual(0, WorldTestContext.Drain(copy).Count);
                Assert.AreEqual(0, WorldTestContext.Drain(loading).Count);
            }
            finally { foreach (var client in added) _world.Map.ClientList.Remove(client); }
        }
    }
}
