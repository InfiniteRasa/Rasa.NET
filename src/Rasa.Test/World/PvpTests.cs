using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Wargame.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Player against player (Pvp): two clans at feud are enemies - HOSTILE to each other, their
    // weapons and damage abilities hurt each other at PVP_DAMAGE_MODIFIER, a player an enemy brings
    // to zero is defeated (the kill counted, back on full, PvP Safety for 60 s), and PvP Safety
    // turns an enemy's hit into Immune.
    [TestClass]
    [DoNotParallelize]
    public class PvpTests
    {
        private const uint RedClanId = 900001;
        private const uint BlueClanId = 900002;

        [TestMethod]
        public void PlayersOnOppositeSidesOfAFeudAreEnemiesAndNobodyElseIs()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var redMate = Fighter(world, RedClanId, 5);
            var blue = Fighter(world, BlueClanId, 10);
            var loner = Fighter(world, 0, 15);

            Assert.IsFalse(Pvp.AreEnemies(red.Player, blue.Player), "no feud yet");

            WithFeud(world, () =>
            {
                Assert.IsTrue(Pvp.AreEnemies(red.Player, blue.Player));
                Assert.IsTrue(Pvp.AreEnemies(blue.Player, red.Player));
                Assert.IsFalse(Pvp.AreEnemies(red.Player, redMate.Player), "same clan, same side");
                Assert.IsFalse(Pvp.AreEnemies(red.Player, loner.Player), "not in the feud");
                Assert.IsFalse(Pvp.AreEnemies(red.Player, red.Player));
            });

            Assert.IsFalse(Pvp.AreEnemies(red.Player, blue.Player), "the feud is over");
        }

        [TestMethod]
        public void AnEnemyIsIntroducedHostileAndEveryoneElseFriendly()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            var loner = Fighter(world, 0, 15);

            WithFeud(world, () =>
            {
                Assert.AreEqual(TargetCategory.Hostile, CategoryIn(red, blue));
                Assert.AreEqual(TargetCategory.Hostile, CategoryIn(blue, red));
                Assert.AreEqual(TargetCategory.Friendly, CategoryIn(red, loner));
                Assert.AreEqual(TargetCategory.Friendly, CategoryIn(red, red));
            });

            Assert.AreEqual(TargetCategory.Friendly, CategoryIn(red, blue));
        }

        [TestMethod]
        public void AFeudStartingAndEndingRetargetsTheTwoSides()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            CellManager.Instance.AddToWorld(red);
            CellManager.Instance.AddToWorld(blue);
            WorldTestContext.Drain(red);
            WorldTestContext.Drain(blue);

            WithFeud(world, () =>
            {
                Assert.IsTrue(MissionTestContext.Drain(red).OfType<TargetCategoryPacket>().Any(p => p.TargetCategory == TargetCategory.Hostile));
                Assert.IsTrue(MissionTestContext.Drain(blue).OfType<TargetCategoryPacket>().Any(p => p.TargetCategory == TargetCategory.Hostile));
            });

            var after = MissionTestContext.Drain(red).OfType<TargetCategoryPacket>().ToList();
            Assert.IsTrue(after.Count > 0);
            Assert.IsTrue(after.All(p => p.TargetCategory == TargetCategory.Friendly));
        }

        [TestMethod]
        public void AWeaponHitOnAnEnemyDoesHalfAndOnAnyoneElseNothing()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            var loner = Fighter(world, 0, 15);
            PlaceAll(red, blue, loner);

            WithFeud(world, () =>
            {
                Shoot(world, red, blue, 100);
                Assert.AreEqual(950, Health(blue), "PVP_DAMAGE_MODIFIER 0.5");

                Shoot(world, red, loner, 100);
                Assert.AreEqual(1000, Health(loner));
            });
        }

        [TestMethod]
        public void PvpSafetyTurnsAnEnemyHitImmuneBothWays()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, blue);

            WithFeud(world, () =>
            {
                Pvp.GiveSafety(world.Map, blue.Player);

                var onSafe = Shoot(world, red, blue, 100);
                Assert.AreEqual(1000, Health(blue));
                Assert.AreEqual(1, onSafe.Args.HitData.Single().WasImune);

                Shoot(world, blue, red, 100);
                Assert.AreEqual(1000, Health(red), "a safe player deals no PvP damage either");

                Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, blue.Player, 100, red.Player, out var outcome));
                Assert.IsTrue(outcome.Immune);
            });
        }

        [TestMethod]
        public void AnEnemyBroughtToZeroIsDefeatedCountedRestoredAndMadeSafe()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, blue);
            blue.Player.Attributes[Attributes.Health].Current = 40;
            blue.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 200, 200, 0, 0, 0);

            WithFeud(world, feud =>
            {
                MissionTestContext.Drain(blue);

                Shoot(world, red, blue, 100);

                Assert.AreEqual(1000, Health(blue), "back on full");
                Assert.AreEqual(200, blue.Player.Attributes[Attributes.Armor].Current, "armour too");
                Assert.IsTrue(Pvp.IsSafe(blue.Player));
                Assert.AreEqual(1, feud.ChallengerKills, "red challenged, red scored");
                Assert.AreEqual(0, feud.TargetKills);

                var packets = MissionTestContext.Drain(blue);
                Assert.IsTrue(packets.OfType<WargameScoreboardPacket>().Any());
                Assert.IsTrue(packets.OfType<GameEffectAttachedPacket>().Any(p => p.EffectTypeId == Pvp.SafetyTypeId));
            });
        }

        [TestMethod]
        public void AbilityDamageIsHalvedAndHealingInAFightIsHalved()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var redMate = Fighter(world, RedClanId, 5);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, redMate, blue);

            WithFeud(world, () =>
            {
                var healed = ActorManager.Instance.Heal(Wounded(redMate), 100, red.Player.EntityId);
                Assert.AreEqual(100, healed, "not in a PvP fight: the whole heal");

                Assert.AreEqual(50, ActorManager.Instance.Damage(world.Map, blue.Player, 100, red.Player));
                Assert.AreEqual(950, Health(blue));

                healed = ActorManager.Instance.Heal(Wounded(blue), 100, blue.Player.EntityId);
                Assert.AreEqual(50, healed, "in a PvP fight: PVP_HEALING_MODIFIER");

                Assert.AreEqual(100, ActorManager.Instance.Heal(Wounded(redMate), 100, 0), "regeneration is left alone");
            });
        }

        [TestMethod]
        public void AnAreaDamageAbilityCatchesEnemiesAndSparesFriends()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var redMate = Fighter(world, RedClanId, 4);
            var blue = Fighter(world, BlueClanId, 8);
            var loner = Fighter(world, 0, 6);
            PlaceAll(red, redMate, blue, loner);

            var info = new ActionLevelInfo { ActionId = ActionId.AaRecruitLightning, Level = 1, MaxRange = 20 };
            info.Properties[AbilityProperty.DamageAmountMin] = 100;
            info.Properties[AbilityProperty.DamageAmountMax] = 100;
            info.Properties[AbilityProperty.RadiusAroundSource] = 15;
            var action = new ActionInfo { ActionId = ActionId.AaRecruitLightning, Module = "abilities.energywave" };
            action.Levels[1] = info;

            WithFeud(world, () =>
            {
                BootcampRuntimeTestHarness.InvokeResolveDirectDamage(Abilities(), world.Map, red, action, info,
                    new ActionData(red.Player, ActionId.AaRecruitLightning, 1, 0, 0));

                Assert.IsTrue(Health(blue) == 950 || Health(blue) == 937, $"half, or half of a PvP crit: {Health(blue)}");
                Assert.AreEqual(1000, Health(redMate));
                Assert.AreEqual(1000, Health(loner));
                Assert.AreEqual(1000, Health(red));
            });
        }

        [TestMethod]
        public void APlayersStunOnAnEnemyLastsHalfAsLong()
        {
            var red = new Manifestation();
            var blue = new Manifestation();
            var creature = new Creature();

            Assert.AreEqual(1500, Pvp.ScaleDuration(red, blue, 3000));
            Assert.AreEqual(3000, Pvp.ScaleDuration(creature, blue, 3000));
            Assert.AreEqual(50, Pvp.ScaleDamage(red, blue, 100));
            Assert.AreEqual(1, Pvp.ScaleDamage(red, blue, 1));
            Assert.AreEqual(100, Pvp.ScaleDamage(creature, blue, 100));
        }

        private static Client Fighter(WorldTestContext world, uint clanId, float x = 0)
        {
            var client = world.CreateClient(x, 0);
            client.Player.ClanId = clanId;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            return client;
        }

        private static void PlaceAll(params Client[] clients)
        {
            foreach (var client in clients)
                CellManager.Instance.AddToWorld(client);

            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        private static Manifestation Wounded(Client client)
        {
            client.Player.Attributes[Attributes.Health].Current = 500;
            return client.Player;
        }

        private static int Health(Client client) => (int)client.Player.Attributes[Attributes.Health].Current;

        private static TargetCategory CategoryIn(Client shown, Client viewer)
        {
            return ManifestationManager.Instance.CreatePlayerEntityData(shown, viewer)
                .OfType<TargetCategoryPacket>().Single().TargetCategory;
        }

        /// <summary>A shot that lands now, with no crit.</summary>
        private static Missile Shoot(WorldTestContext world, Client shooter, Client target, int damage)
        {
            var missile = new Missile
            {
                Source = shooter.Player,
                TargetActor = target.Player,
                TargetEntityId = target.Player.EntityId,
                DamageA = damage,
                DamageType = DamageType.Physical,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                CritChance = 0
            };

            MissileManager.Instance.MissileTrigger(world.Map, missile);

            return missile;
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static void WithFeud(WorldTestContext world, Action body) => WithFeud(world, _ => body());

        /// <summary>Red challenges Blue and Blue accepts; the fighters are online for the feud's messages.</summary>
        private static void WithFeud(WorldTestContext world, Action<ClanFeuds.Feud> body)
        {
            var online = world.Map.ClientList.ToList();

            lock (Server.Clients)
                Server.Clients.AddRange(online);

            ClanFeuds.Feud feud = null;

            try
            {
                feud = ClanFeuds.Instance.Start(
                    new ClanEntry { Id = RedClanId, Name = "Red", IsPvP = true },
                    new ClanEntry { Id = BlueClanId, Name = "Blue", IsPvP = true });
                Assert.IsNotNull(feud);

                body(feud);
            }
            finally
            {
                if (feud != null)
                    ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                lock (Server.Clients)
                    foreach (var client in online)
                        Server.Clients.Remove(client);
            }
        }
    }
}
