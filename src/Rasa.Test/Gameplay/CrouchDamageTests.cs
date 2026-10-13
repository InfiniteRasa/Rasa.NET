using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Structures;

    [TestClass]
    public class CrouchDamageTests
    {
        private const double AimRate = 0.01;    // a bead of 10 a second: a standing bead is full in 8 s, a crouched one in 5 s

        [TestMethod]
        public void AFullyBeadedCrouchedShotOutdamagesAFullyBeadedStandingOne()
        {
            var standing = Beaded(crouched: false);
            var crouched = Beaded(crouched: true);

            Assert.AreEqual(0.82, Accuracy.DamageFactor(standing, 60000), 1e-9);
            Assert.AreEqual(1.0, Accuracy.DamageFactor(crouched, 60000), 1e-9);
            Assert.IsFalse(Accuracy.IsFullBead(standing, 60000));
            Assert.IsTrue(Accuracy.IsFullBead(crouched, 60000));
        }

        [TestMethod]
        public void TheSameBeadDoesTheSameDamageWhicheverTheStance()
        {
            // 4 s of beading: 40 standing, 80 crouched (twice the rate) - and 80 is 80 either way.
            var standing = Beaded(crouched: false, ms: 8000);
            var crouched = Beaded(crouched: true, ms: 4000);

            Assert.AreEqual(Accuracy.DamageFactor(standing, 8000), Accuracy.DamageFactor(crouched, 4000), 1e-9);
            Assert.AreEqual(0.82, Accuracy.DamageFactor(crouched, 4000), 1e-9);
        }

        [TestMethod]
        public void NoBeadIsATenth()
        {
            var player = Beaded(crouched: true, ms: 0);

            Assert.AreEqual(Accuracy.NoBeadDamage, Accuracy.DamageFactor(player, 0), 1e-9);
        }

        [TestMethod]
        [DataRow(ActionId.WeaponMelee)]
        [DataRow(ActionId.CrMiasmaMelee)]
        [DataRow(ActionId.CrFilcherMelee)]
        [DataRow(ActionId.CrLoperMelee)]
        [DataRow(ActionId.CrHowlerMeleeAttack)]
        [DataRow(ActionId.CrMawMelee)]
        [DataRow(ActionId.CrAmoeboidMelee)]
        [DataRow(ActionId.CrXanxMelee)]
        [DataRow(ActionId.CrFlaregasherMelee)]
        [DataRow(ActionId.CrGranitourMelee)]
        [DataRow(ActionId.CrAttaSoldierMelee)]
        [DataRow(ActionId.CrAttaGrubMelee)]
        [DataRow(ActionId.CrShieldDroneAttack)]
        [DataRow(ActionId.CrWardenBotShock)]
        public void CreatureMeleeAbilitiesAreBlows(ActionId actionId)
        {
            Assert.IsTrue(CreatureAttacks.IsMelee(new CreatureAction { ActionId = actionId }));
        }

        [TestMethod]
        public void AWeaponShotIsNotABlow()
        {
            Assert.IsFalse(CreatureAttacks.IsMelee(new CreatureAction { ActionId = ActionId.WeaponAttack }));
            Assert.IsFalse(CreatureAttacks.IsMelee(null));
        }

        private static Manifestation Beaded(bool crouched, long ms = 60000)
        {
            var player = new Manifestation { Target = 1, IsCrouching = crouched };

            Accuracy.Reset(player, AimRate, 0);
            Accuracy.Current(player, ms);
            return player;
        }
    }
}
