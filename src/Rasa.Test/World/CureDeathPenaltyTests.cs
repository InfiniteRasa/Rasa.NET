using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;

    // A revive costs Rez Trauma (a fifth off the primary attributes for each death, to three) and
    // thirty seconds in which nothing heals (PlayerDeath). Neither is a buff, so Cure took both
    // off as debuffs: a Biotechnician's cleanse, or their own, ended the price of a death the
    // moment it was paid. A map link and a relog are no way out of it (EffectCarry, RelogVitals);
    // neither is a cleanse now, nor Protect's guard against debuffs.
    [TestClass]
    [DoNotParallelize]
    public class CureDeathPenaltyTests
    {
        private const int SlowTypeId = 12345;

        [TestMethod]
        public void ACleanseLeavesADeathsPenaltiesOn()
        {
            using var world = new WorldTestContext();
            var medic = PlayerDeathTests.Player(world, 0, 0);
            var fallen = BackFromADeath(world, 3);
            var slow = Slow(world, fallen);

            Cure(world, medic, 1, fallen.Player.EntityId);

            AssertPenaltiesOn(fallen);
            Assert.IsFalse(fallen.Player.ActiveEffects.ContainsKey(slow.EffectId), "an ordinary debuff still comes off");
        }

        [TestMethod]
        public void ACleanseOnOneselfLeavesThemOn()
        {
            using var world = new WorldTestContext();
            var fallen = BackFromADeath(world, 0);
            var slow = Slow(world, fallen);

            Cure(world, fallen, 1, 0);

            AssertPenaltiesOn(fallen);
            Assert.IsFalse(fallen.Player.ActiveEffects.ContainsKey(slow.EffectId));
        }

        [TestMethod]
        public void AGroupCleanseLeavesThemOnTheSquad()
        {
            using var world = new WorldTestContext();
            var medic = PlayerDeathTests.Player(world, 0, 0);
            var fallen = BackFromADeath(world, 5);
            medic.Player.PartyId = fallen.Player.PartyId = 41;
            var slow = Slow(world, fallen);

            Cure(world, medic, 2, 0, radius: 25);

            AssertPenaltiesOn(fallen);
            Assert.IsFalse(fallen.Player.ActiveEffects.ContainsKey(slow.EffectId), "the cleanse reached them");
        }

        [TestMethod]
        public void ProtectLeavesThemOn()
        {
            using var world = new WorldTestContext();
            var medic = PlayerDeathTests.Player(world, 0, 0);
            var fallen = BackFromADeath(world, 3);

            Cure(world, medic, 4, fallen.Player.EntityId, guardSeconds: 10);

            Assert.IsTrue(GameEffectManager.DebuffsBlocked(fallen.Player), "guarded");
            AssertPenaltiesOn(fallen);
        }

        [TestMethod]
        public void ProtectsGuardDoesNotKeepThemOff()
        {
            using var world = new WorldTestContext();
            var client = PlayerDeathTests.Player(world, 0, 0);
            var guard = new GameEffect { TypeId = 181, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), IsBuff = true, BlocksDebuffs = true };
            GameEffectManager.Instance.Attach(world.Map, client.Player, guard);

            // As they go back on for a character that left the world with them (RelogVitals).
            PlayerDeath.RestorePenalties(world.Map, client.Player, 2, 200000, 20000);

            AssertPenaltiesOn(client, stacks: 2);

            var slow = Slow(world, client);
            Assert.IsFalse(client.Player.ActiveEffects.ContainsKey(slow.EffectId), "an ordinary debuff is still kept off");
        }

        [TestMethod]
        public void TheyAreNotDebuffsCureTakes()
        {
            using var world = new WorldTestContext();
            var fallen = BackFromADeath(world, 0);
            var slow = Slow(world, fallen);

            CollectionAssert.AreEqual(new[] { slow }, AbilityManager.DebuffsOn(fallen.Player).ToArray());
        }

        #region Fixture

        /// <summary>A player who died and came back at a hospital: Rez Trauma and the no-healing on.</summary>
        private static Client BackFromADeath(WorldTestContext world, float x)
        {
            var client = PlayerDeathTests.Player(world, x, 0);
            client.Player.Level = 20;   // from DEATH_PENALTY_MIN_LEVEL 5 a death costs something

            client.Player.Attributes[Attributes.Health].Current = 0;
            PlayerDeath.AtZero(world.Map, client.Player, null);
            PlayerDeath.ReviveMe(client, null);

            AssertPenaltiesOn(client);
            WorldTestContext.Drain(client);

            return client;
        }

        private static void AssertPenaltiesOn(Client client, int stacks = 1)
        {
            var trauma = client.Player.ActiveEffects.Values.SingleOrDefault(e => e.TypeId == PlayerDeath.RezSicknessTypeId);

            Assert.IsNotNull(trauma, "Rez Trauma");
            Assert.AreEqual(stacks, trauma.Stacks);
            Assert.AreEqual(-20 * stacks, GameEffectManager.AttributePercentOf(client.Player, Attributes.Body));
            Assert.IsTrue(client.Player.ActiveEffects.Values.Any(e => e.TypeId == PlayerDeath.RezSicknessNoHealTypeId), "REZ_SICKNESS_NO_HEAL");
            Assert.IsTrue(GameEffectManager.HealingBlocked(client.Player), "nothing heals");
        }

        private static GameEffect Slow(WorldTestContext world, Client client)
        {
            var slow = new GameEffect { TypeId = SlowTypeId, EffectId = GameEffectManager.Instance.NextEffectId(world.Map), IsBuff = false };
            GameEffectManager.Instance.Attach(world.Map, client.Player, slow);

            return slow;
        }

        /// <summary>The performer's Cure at that pump, on the target (0: themselves, or the squad round them with a radius).</summary>
        private static void Cure(WorldTestContext world, Client performer, uint pump, ulong targetId, int radius = 0, int guardSeconds = 0)
        {
            var info = new ActionLevelInfo { ActionId = ActionId.AaBiotechnicianCure, Level = pump };

            if (radius > 0)
                info.Properties[AbilityProperty.RadiusAroundSource] = radius;

            if (guardSeconds > 0)
                info.Properties[AbilityProperty.Duration] = guardSeconds;

            var abilities = (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

            typeof(AbilityManager).GetMethod("Cure", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(abilities, new object[] { world.Map, performer.Player, new ActionData(performer.Player, ActionId.AaBiotechnicianCure, pump, targetId, 0), info });
        }

        #endregion
    }
}
