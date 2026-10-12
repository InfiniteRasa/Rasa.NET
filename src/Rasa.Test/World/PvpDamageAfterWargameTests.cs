using System;
using System.Collections;
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
    using Rasa.Structures.Char;

    // Players are enemies only across a wargame - a duel, a clan feud, a battleground - in which
    // they are on opposite sides (Pvp.AreEnemies). What one of them put on the other while it
    // lasted, or left shooting at them, used to go on landing after it ended: a damage-over-time
    // effect ticked on, and could kill, with Rez Trauma and equipment wear as any death; a
    // turret kept the aim it had. A clan feud is the wargame here; a duel and a battleground
    // make enemies the same way (Wargames).
    [TestClass]
    [DoNotParallelize]
    public class PvpDamageAfterWargameTests
    {
        private const uint Red = 900061, Blue = 900062;

        #region A damage-over-time effect

        [TestMethod]
        public void ADotPutOnInAFeudStopsWhenTheFeudEnds()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Fighters(world);

            AtFeud(world, red, blue, feud =>
            {
                var dot = Dot(world, red, blue);
                Tick(world, dot);
                Assert.IsTrue(Health(blue) < 1000, "it hurts while the feud lasts");

                End(feud);
                var before = Health(blue);
                Tick(world, dot);

                Assert.AreEqual(before, Health(blue), "nothing once it is over");
                Assert.IsFalse(blue.Player.ActiveEffects.ContainsKey(dot.EffectId), "and it comes off");
            });
        }

        [TestMethod]
        public void ADotPutOnInAFeudDoesNotKillAfterIt()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Fighters(world);

            AtFeud(world, red, blue, feud =>
            {
                var dot = Dot(world, red, blue);

                End(feud);
                blue.Player.Attributes[Attributes.Health].Current = 1;
                Tick(world, dot);

                Assert.AreNotEqual(CharacterState.Dead, blue.Player.State);
                Assert.AreEqual(1, Health(blue));
            });
        }

        #endregion

        #region Any damage

        [TestMethod]
        public void AHitOnAPlayerWhoIsNoEnemyTakesNothing()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Fighters(world);

            var taken = ActorManager.Instance.Damage(world.Map, blue.Player, 100, red.Player, out var outcome);

            Assert.AreEqual(0, taken);
            Assert.AreEqual(1000, Health(blue));
            Assert.IsTrue(outcome.Immune, "shown as Immune, as a hit PvP Safety stops");
        }

        [TestMethod]
        public void AHitByAPlayersCreatureOnAPlayerWhoIsNoEnemyTakesNothing()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Fighters(world);
            var pet = new Creature { MasterEntityId = red.Player.EntityId, MapContextId = world.Map.MapInfo.MapContextId, Level = 1 };

            Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, blue.Player, 100, pet, out _));
            Assert.AreEqual(1000, Health(blue));
        }

        [TestMethod]
        public void AHitOnAnEnemyStillLands()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Fighters(world);

            AtFeud(world, red, blue, feud =>
                Assert.IsTrue(ActorManager.Instance.Damage(world.Map, blue.Player, 100, red.Player, out _) > 0));
        }

        [TestMethod]
        public void AHitByACreatureNobodyOwnsStillLands()
        {
            using var world = new WorldTestContext();
            var (_, blue) = Fighters(world);
            var wild = new Creature { MapContextId = world.Map.MapInfo.MapContextId, Level = 1, TargetCategory = TargetCategory.Hostile };

            Assert.IsTrue(ActorManager.Instance.Damage(world.Map, blue.Player, 100, wild, out _) > 0);
        }

        #endregion

        #region A turret

        [TestMethod]
        public void ATurretStopsShootingAPlayerWhoIsNoLongerAnEnemy()
        {
            using var world = new WorldTestContext();
            world.AddClass(AbilityManager.TrapClass);
            var (red, blue) = Fighters(world);
            var abilities = Abilities();

            AtFeud(world, red, blue, feud =>
            {
                Turret(abilities, world, red);

                try
                {
                    Fire(abilities, world, red);
                    Assert.IsTrue(Health(blue) < 1000, "shot while the feud lasts");

                    Assert.AreSame(blue.Player, AimOf(red));

                    End(feud);
                    var before = Health(blue);
                    Fire(abilities, world, red);

                    Assert.AreEqual(before, Health(blue), "not once it is over");
                    Assert.IsNull(AimOf(red), "and it looks for someone else to shoot");
                }
                finally
                {
                    foreach (var trap in TrapsOf(red))
                        typeof(AbilityManager).GetMethod("RemoveTrap", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { trap });
                }
            });
        }

        [TestMethod]
        public void ATurretStopsShootingAnEnemyWhoTakesPvpSafety()
        {
            using var world = new WorldTestContext();
            world.AddClass(AbilityManager.TrapClass);
            var (red, blue) = Fighters(world);
            var abilities = Abilities();

            AtFeud(world, red, blue, feud =>
            {
                Turret(abilities, world, red);

                try
                {
                    Fire(abilities, world, red);
                    Assert.AreSame(blue.Player, AimOf(red));

                    // Back from a PvP death at a hospital (PlayerDeath): safe for a while.
                    Pvp.GiveSafety(world.Map, blue.Player);
                    Fire(abilities, world, red);

                    Assert.IsNull(AimOf(red), "one it would not pick now");
                }
                finally
                {
                    foreach (var trap in TrapsOf(red))
                        typeof(AbilityManager).GetMethod("RemoveTrap", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { trap });
                }
            });
        }

        #endregion

        #region Fixture

        private static void Turret(AbilityManager abilities, WorldTestContext world, Client owner)
        {
            var info = new ActionLevelInfo { ActionId = ActionId.AaEngineerTurret, Level = 1 };
            info.Properties[AbilityProperty.DamageAmountMin] = 100;
            info.Properties[AbilityProperty.DamageAmountMax] = 100;
            typeof(AbilityManager).GetMethod("PlaceTurret", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(abilities, new object[] { world.Map, owner.Player, info, new ActionData(owner.Player, info.ActionId, 1, 0, 0), false, 60 });
        }

        private static object AimOf(Client owner)
        {
            var trap = TrapsOf(owner).Single();

            return trap.GetType().GetField("Aim")!.GetValue(trap);
        }

        private static (Client Red, Client Blue) Fighters(WorldTestContext world)
        {
            var red = PlayerDeathTests.Player(world, 0, 0);
            var blue = PlayerDeathTests.Player(world, 5, 0);

            red.Player.ClanId = Red;
            blue.Player.ClanId = Blue;

            return (red, blue);
        }

        private static int Health(Client client) => client.Player.Attributes[Attributes.Health].Current;

        /// <summary>The Red clan and the Blue at feud for as long as the body runs, the two online.</summary>
        private static void AtFeud(WorldTestContext world, Client red, Client blue, Action<ClanFeuds.Feud> body)
        {
            ClanFeuds.Feud feud = null;

            lock (Server.Clients)
                Server.Clients.AddRange(new[] { red, blue });

            try
            {
                feud = ClanFeuds.Instance.Start(
                    new ClanEntry { Id = Red, Name = "Red", IsPvP = true },
                    new ClanEntry { Id = Blue, Name = "Blue", IsPvP = true });
                Assert.IsNotNull(feud);
                Assert.IsTrue(Pvp.AreEnemies(red.Player, blue.Player));

                body(feud);
            }
            finally
            {
                if (feud != null && ClanFeuds.Instance.FeudsOf(Red).Contains(feud))
                    ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                lock (Server.Clients)
                {
                    Server.Clients.Remove(red);
                    Server.Clients.Remove(blue);
                }
            }
        }

        private static void End(ClanFeuds.Feud feud)
        {
            ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);
            Assert.IsFalse(ClanFeuds.Instance.FeudsOf(Red).Contains(feud));
        }

        /// <summary>A damage-over-time effect of the attacker's on the victim: fifty a second for half a minute.</summary>
        private static GameEffect Dot(WorldTestContext world, Client attacker, Client victim)
        {
            var dot = new GameEffect
            {
                TypeId = 10000951,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                Source = attacker.Player,
                SourceId = attacker.Player.EntityId,
                SourceLevel = 50,
                TickIntervalMs = 1000,
                NextTickTick = Environment.TickCount64,
                TickDamageMin = 50,
                TickDamageMax = 50,
                TickDamageType = DamageType.Physical,
                ExpiresTick = Environment.TickCount64 + 30000
            };

            GameEffectManager.Instance.Attach(world.Map, victim.Player, dot);
            return dot;
        }

        /// <summary>The effect's next tick, now.</summary>
        private static void Tick(WorldTestContext world, GameEffect effect)
        {
            effect.NextTickTick = Environment.TickCount64;
            GameEffectManager.Instance.DoWork(world.Map, 100);
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static object[] TrapsOf(Client owner)
        {
            var traps = (IEnumerable)typeof(AbilityManager).GetField("Traps", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

            return traps.Cast<object>()
                .Where(trap => ReferenceEquals(trap.GetType().GetField("Owner")!.GetValue(trap), owner.Player))
                .ToArray();
        }

        /// <summary>The owner's turret's next shot, now.</summary>
        private static void Fire(AbilityManager abilities, WorldTestContext world, Client owner)
        {
            foreach (var trap in TrapsOf(owner))
                trap.GetType().GetField("NextShotAt")!.SetValue(trap, 0L);

            abilities.TrapWorker(world.Map);
        }

        #endregion
    }
}
