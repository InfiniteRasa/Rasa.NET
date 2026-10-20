using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Navigation;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    [TestClass]
    [DoNotParallelize]
    public class WeaponChecksTests
    {
        private WeaponChecksConfig _previous;

        [TestInitialize]
        public void Initialize()
        {
            _previous = WeaponChecks.Config;
            WeaponChecks.Config = new WeaponChecksConfig();
        }

        [TestCleanup]
        public void Cleanup()
        {
            WeaponChecks.Config = _previous;
        }

        [TestMethod]
        public void FacingIsMeasuredOverTheGroundFromTheViewDirection()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            player.Rotation = 0;     // faces -Z

            Assert.AreEqual(0, WeaponChecks.AngleTo(player, new Vector3(0, 5, -10)), 0.01);
            Assert.AreEqual(90, WeaponChecks.AngleTo(player, new Vector3(10, 0, 0)), 0.01);
            Assert.AreEqual(180, WeaponChecks.AngleTo(player, new Vector3(0, 0, 10)), 0.01);
            Assert.AreEqual(0, WeaponChecks.AngleTo(player, player.Position), 0.01, "standing on it");

            Assert.IsTrue(WeaponChecks.IsFacing(player, new Vector3(10, 0, -1), WeaponChecks.FacingHalfAngle), "beside, a little ahead");
            Assert.IsTrue(WeaponChecks.IsFacing(player, new Vector3(10, 0, 5), WeaponChecks.FacingHalfAngle), "beside, a little behind: within the lock's leeway");
            Assert.IsFalse(WeaponChecks.IsFacing(player, new Vector3(1, 0, 10), WeaponChecks.FacingHalfAngle), "behind");
        }

        [TestMethod]
        public void ReachIsTwiceTheWeaponsRangeAndALittle()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            var target = world.CreateClient().Player;
            player.Rotation = 0;

            target.Position = new Vector3(0, 0, -160);
            Assert.IsNull(WeaponChecks.Judge(player, target, 80, null));

            target.Position = new Vector3(0, 0, -170);
            var finding = WeaponChecks.Judge(player, target, 80, null);
            Assert.IsNotNull(finding);
            Assert.AreEqual(WeaponChecks.Check.Range, finding.Value.Check);
            Assert.IsFalse(finding.Value.Refuse, "logged by default (BR-163)");

            WeaponChecks.Config = new WeaponChecksConfig { Range = "refuse" };
            Assert.IsTrue(WeaponChecks.Judge(player, target, 80, null).Value.Refuse);

            // A pistol's 20 m is a pistol's.
            target.Position = new Vector3(0, 0, -50);
            Assert.AreEqual(WeaponChecks.Check.Range, WeaponChecks.Judge(player, target, 20, null).Value.Check);
            Assert.IsNull(WeaponChecks.Judge(player, target, 0, null), "a weapon with no range is not held to one");
        }

        [TestMethod]
        public void BehindIsLoggedNotRefusedByDefault()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            var target = world.CreateClient().Player;
            player.Rotation = 0;
            target.Position = new Vector3(0, 0, 10);

            var finding = WeaponChecks.Judge(player, target, 80, null);
            Assert.AreEqual(WeaponChecks.Check.Facing, finding.Value.Check);
            Assert.IsFalse(finding.Value.Refuse);

            WeaponChecks.Config = new WeaponChecksConfig { Facing = "refuse" };
            Assert.IsTrue(WeaponChecks.Judge(player, target, 80, null).Value.Refuse);

            WeaponChecks.Config = new WeaponChecksConfig { Facing = "off" };
            Assert.IsNull(WeaponChecks.Judge(player, target, 80, null));
        }

        [TestMethod]
        public void ALoggedRangeFindingDoesNotHideARefusingOne()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            var target = world.CreateClient().Player;
            player.Rotation = 0;

            // 200 m behind: past an 80 m weapon's reach, and behind.
            target.Position = new Vector3(0, 0, 200);

            var logged = WeaponChecks.Judge(player, target, 80, null);
            Assert.AreEqual(WeaponChecks.Check.Range, logged.Value.Check, "both log: the first");
            Assert.IsFalse(logged.Value.Refuse);
            StringAssert.Contains(logged.Value.What, "degrees from where they face (log)", "the line names the other");

            WeaponChecks.Config = new WeaponChecksConfig { Facing = "refuse" };
            var refused = WeaponChecks.Judge(player, target, 80, null);
            Assert.AreEqual(WeaponChecks.Check.Facing, refused.Value.Check, "range on log no longer stands in for facing (BR-225)");
            Assert.IsTrue(refused.Value.Refuse);
            StringAssert.Contains(refused.Value.What, "with a weapon of range 80 (log)");

            // Both refuse: range, which has a message for the player.
            WeaponChecks.Config = new WeaponChecksConfig { Facing = "refuse", Range = "refuse" };
            Assert.AreEqual(WeaponChecks.Check.Range, WeaponChecks.Judge(player, target, 80, null).Value.Check);

            // Wholly behind a wall as well, with sight refusing alone.
            var wall = CoverMesh.Build(new float[] { -10, 0, 10, -10, 10, 10, 10, 10, 10, 10, 0, 10 }, new[] { 0, 1, 2, 0, 2, 3 });
            WeaponChecks.Config = new WeaponChecksConfig { Sight = "refuse" };
            var hidden = WeaponChecks.Judge(player, target, 80, wall);
            Assert.AreEqual(WeaponChecks.Check.Sight, hidden.Value.Check);
            Assert.IsTrue(hidden.Value.Refuse);
        }

        [TestMethod]
        public void WhollyBehindCoverIsLoggedAndPartlyIsNot()
        {
            using var world = new WorldTestContext();
            var player = world.CreateClient().Player;
            var target = world.CreateClient().Player;
            player.Rotation = 0;
            target.Position = new Vector3(0, 0, -20);

            // A wall across z = -10, 10 m high and 20 m wide: nothing of the target shows.
            var wall = CoverMesh.Build(new float[] { -10, 0, -10, -10, 10, -10, 10, 10, -10, 10, 0, -10 }, new[] { 0, 1, 2, 0, 2, 3 });
            var finding = WeaponChecks.Judge(player, target, 80, wall);
            Assert.AreEqual(WeaponChecks.Check.Sight, finding.Value.Check);
            Assert.IsFalse(finding.Value.Refuse);

            // A sandbag 1 m high: the head is in the clear.
            var sandbag = CoverMesh.Build(new float[] { -10, 0, -10, -10, 1, -10, 10, 1, -10, 10, 0, -10 }, new[] { 0, 1, 2, 0, 2, 3 });
            Assert.IsNull(WeaponChecks.Judge(player, target, 80, sandbag));

            Assert.IsNull(WeaponChecks.Judge(player, target, 80, null), "no cover mesh");

            // Point blank through the mesh's own thickness is not looked at.
            target.Position = new Vector3(0, 0, -2);
            Assert.IsNull(WeaponChecks.Judge(player, target, 80, wall));
        }

        [TestMethod]
        public void AShotOutOfReachIsFiredAndLoggedByDefault()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, -200).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;
            OutOfRangeMessages(context.Client);

            // The context's weapon has a range of 80: 165 m is its reach. Past it the shot goes,
            // for the floor of the range falloff, and a line is written.
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo);
            Assert.AreNotEqual(0, context.Client.Player.WeaponCheckLogTick, "logged");
            Assert.AreEqual(0, OutOfRangeMessages(context.Client), "nothing to tell: it went");
        }

        [TestMethod]
        public void AShotOutOfReachIsNotFiredAndCostsNothing()
        {
            WeaponChecks.Config = new WeaponChecksConfig { Range = "refuse" };

            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, -200).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;
            OutOfRangeMessages(context.Client);

            // The context's weapon has a range of 80: 165 m is its reach.
            Assert.IsFalse(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(0, context.World.Map.QueuedMissiles.Count);
            Assert.AreEqual(0, context.Client.Player.WeaponCheckHits, "one line written, nothing since");
            Assert.AreNotEqual(0, context.Client.Player.WeaponCheckLogTick);
            Assert.AreEqual(1, OutOfRangeMessages(context.Client), "the player is told why");

            target.Position = new Vector3(0, 0, -100);
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo);
        }

        [TestMethod]
        public void AShotBehindIsFiredInLogModeAndNotInRefuseMode()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, 30).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo);
            Assert.AreNotEqual(0, context.Client.Player.WeaponCheckLogTick, "logged");

            WeaponChecks.Config = new WeaponChecksConfig { Facing = "refuse" };
            context.Client.Player.NextShotAt = 0;
            Assert.IsFalse(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(6u, context.Weapon.CurrentAmmo);

            // Turned round: fired.
            context.Client.Player.Rotation = System.Math.PI;
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(5u, context.Weapon.CurrentAmmo);
        }

        [TestMethod]
        public void ALongShotBehindIsNotFiredWhenFacingRefuses()
        {
            WeaponChecks.Config = new WeaponChecksConfig { Facing = "refuse" };

            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, 200).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;
            OutOfRangeMessages(context.Client);

            Assert.IsFalse(manager.PlayerTryFireWeapon(context.Client), "past reach on log, and behind on refuse");
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
            var told = Messages(context.Client);
            CollectionAssert.Contains(told, PlayerMessage.PmFailedLineOfSight, "refused by the facing check");
            CollectionAssert.DoesNotContain(told, PlayerMessage.PmTargetOutOfRange, "the range finding was only logged");
        }

        [TestMethod]
        public void ABlindShotHasNothingToCheck()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            WeaponChecks.Config = new WeaponChecksConfig { Facing = "refuse", Range = "refuse", Sight = "refuse" };
            context.Client.Player.Target = 0;

            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(0, context.Client.Player.WeaponCheckLogTick);
        }

        [TestMethod]
        public void TheOutOfRangeMessageIsForARefusedRangeShotAndRateLimited()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            OutOfRangeMessages(client);

            var refused = new WeaponChecks.Finding(WeaponChecks.Check.Range, "far", true);

            WeaponChecks.Tell(client, refused, 1000);
            WeaponChecks.Tell(client, refused, 1000 + WeaponChecks.ToldQuietMs - 1);
            Assert.AreEqual(1, OutOfRangeMessages(client), "a held trigger is told once");

            WeaponChecks.Tell(client, refused, 1000 + WeaponChecks.ToldQuietMs);
            Assert.AreEqual(1, OutOfRangeMessages(client), "and again once the quiet time is up");

            WeaponChecks.Tell(client, new WeaponChecks.Finding(WeaponChecks.Check.Range, "far", false), 100_000);
            Assert.AreEqual(0, OutOfRangeMessages(client), "a shot that went says nothing");
        }

        [TestMethod]
        public void ARefusedFacingOrSightShotFailsItsLineOfSightCheck()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            MissionTestContext.Drain(client);

            // Behind: the client has no facing message, and shows this one itself for a target
            // not in front of the player.
            WeaponChecks.Tell(client, new WeaponChecks.Finding(WeaponChecks.Check.Facing, "behind", true), 1000);
            CollectionAssert.AreEqual(new[] { PlayerMessage.PmFailedLineOfSight }, Messages(client));

            // Held down, it is told once in the quiet time, whichever check refused it.
            WeaponChecks.Tell(client, new WeaponChecks.Finding(WeaponChecks.Check.Sight, "behind cover", true), 1000 + WeaponChecks.ToldQuietMs - 1);
            WeaponChecks.Tell(client, new WeaponChecks.Finding(WeaponChecks.Check.Range, "far", true), 1000 + WeaponChecks.ToldQuietMs - 1);
            Assert.IsEmpty(Messages(client));

            WeaponChecks.Tell(client, new WeaponChecks.Finding(WeaponChecks.Check.Sight, "behind cover", true), 1000 + WeaponChecks.ToldQuietMs);
            CollectionAssert.AreEqual(new[] { PlayerMessage.PmFailedLineOfSight }, Messages(client));

            // Logged only, the shot went: nothing to say.
            WeaponChecks.Tell(client, new WeaponChecks.Finding(WeaponChecks.Check.Facing, "behind", false), 100_000);
            WeaponChecks.Tell(client, new WeaponChecks.Finding(WeaponChecks.Check.Sight, "behind cover", false), 200_000);
            Assert.IsEmpty(Messages(client));
        }

        [TestMethod]
        public void LinesAreRateLimitedPerPlayer()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var finding = new WeaponChecks.Finding(WeaponChecks.Check.Facing, "behind", false);

            WeaponChecks.Report(client, finding, 1000);
            WeaponChecks.Report(client, finding, 2000);
            Assert.AreEqual(1000, client.Player.WeaponCheckLogTick);
            Assert.AreEqual(1, client.Player.WeaponCheckHits);

            WeaponChecks.Report(client, finding, 1000 + WeaponChecks.QuietMs);
            Assert.AreEqual(1000 + WeaponChecks.QuietMs, client.Player.WeaponCheckLogTick);
            Assert.AreEqual(0, client.Player.WeaponCheckHits);
        }

        /// <summary>How many "target out of range" messages went to the client since last asked.</summary>
        private static int OutOfRangeMessages(Client client) => MissionTestContext.Drain(client)
            .OfType<DisplayClientMessagePacket>().Count(message => message.MsgId == PlayerMessage.PmTargetOutOfRange);

        private static PlayerMessage[] Messages(Client client) => MissionTestContext.Drain(client)
            .OfType<DisplayClientMessagePacket>().Select(message => message.MsgId).ToArray();
    }
}
