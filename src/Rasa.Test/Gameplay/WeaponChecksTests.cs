using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Config;
    using Rasa.Managers;
    using Rasa.Navigation;
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
            Assert.IsTrue(finding.Value.Refuse, "refused by default");

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
        public void AShotOutOfReachIsNotFiredAndCostsNothing()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var target = context.World.CreateClient(0, -200).Player;
            context.Client.Player.Rotation = 0;
            context.Client.Player.Target = target.EntityId;

            // The context's weapon has a range of 80: 165 m is its reach.
            Assert.IsFalse(manager.PlayerTryFireWeapon(context.Client));
            Assert.AreEqual(7u, context.Weapon.CurrentAmmo);
            Assert.AreEqual(0, context.World.Map.QueuedMissiles.Count);
            Assert.AreEqual(0, context.Client.Player.WeaponCheckHits, "one line written, nothing since");
            Assert.AreNotEqual(0, context.Client.Player.WeaponCheckLogTick);

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
    }
}
