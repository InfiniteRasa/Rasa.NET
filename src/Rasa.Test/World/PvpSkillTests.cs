using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    // The skills that reach an enemy player as they reach a creature (Pvp): areas and blasts catch
    // enemies (AbilityManager.VictimsWithin), a player's creatures fight their master's enemies and
    // are fought by them, what a creature of theirs does counts as theirs, and a debuff or a stun
    // from an enemy lasts half as long.
    [TestClass]
    [DoNotParallelize]
    public class PvpSkillTests
    {
        private const uint RedClanId = 900011;
        private const uint BlueClanId = 900012;

        [TestMethod]
        public void AnEnemysCreatureIsHostileAndFightsOnlyTheirEnemies()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            var loner = Fighter(world, 0, 15);
            PlaceAll(red, blue, loner);
            var turret = Summon(world, blue, 12);

            try
            {
                Assert.IsFalse(AbilityManager.IsHostile(red.Player, turret), "no feud yet");
                Assert.IsFalse(BehaviorManager.MayFight(turret, red.Player.EntityId));

                WithFeud(world, () =>
                {
                    Assert.AreEqual(TargetCategory.Hostile, Pvp.CategoryFor(turret, red.Player));
                    Assert.AreEqual(TargetCategory.Friendly, Pvp.CategoryFor(turret, blue.Player));
                    Assert.AreEqual(TargetCategory.Friendly, Pvp.CategoryFor(turret, loner.Player));

                    Assert.IsTrue(AbilityManager.IsHostile(red.Player, turret));
                    Assert.IsFalse(AbilityManager.IsHostile(loner.Player, turret));
                    Assert.IsFalse(AbilityManager.IsHostile(blue.Player, turret), "their own");

                    Assert.IsTrue(BehaviorManager.MayFight(turret, red.Player.EntityId));
                    Assert.IsFalse(BehaviorManager.MayFight(turret, loner.Player.EntityId));
                    Assert.IsFalse(BehaviorManager.MayFight(turret, blue.Player.EntityId));
                });

                Assert.IsFalse(BehaviorManager.MayFight(turret, red.Player.EntityId), "the feud is over");
            }
            finally
            {
                Unsummon(world, turret);
            }
        }

        [TestMethod]
        public void ACreaturesHitOnAnEnemyIsHalvedAndItsDefeatIsTheMasters()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, blue);
            var mine = Summon(world, blue, 2);

            try
            {
                WithFeud(world, feud =>
                {
                    Assert.AreEqual(50, ActorManager.Instance.Damage(world.Map, red.Player, 100, mine));
                    Assert.AreEqual(950, Health(red));

                    red.Player.Attributes[Attributes.Health].Current = 40;
                    ActorManager.Instance.Damage(world.Map, red.Player, 100, mine);

                    Assert.AreEqual(CharacterState.Dead, red.Player.State, "killed in the feud (PlayerDeath)");
                    Assert.AreEqual(1, feud.TargetKills, "blue's mine, blue's kill");
                    Assert.AreEqual(0, feud.ChallengerKills);

                    PlayerDeath.ReviveMe(red, null);
                    Assert.AreEqual(1000, Health(red), "back from the hospital on full");
                    Assert.IsTrue(Pvp.IsSafe(red.Player));

                    Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, red.Player, 100, mine, out var outcome), "PvP Safety");
                    Assert.IsTrue(outcome.Immune);
                });
            }
            finally
            {
                Unsummon(world, mine);
            }
        }

        [TestMethod]
        public void AnAreaCatchesEnemiesAndTheirCreaturesAndASummonLeavesTheSafeAlone()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var redMate = Fighter(world, RedClanId, 3);
            var blue = Fighter(world, BlueClanId, 6);
            var loner = Fighter(world, 0, 9);
            PlaceAll(red, redMate, blue, loner);
            var blueTurret = Summon(world, blue, 8);
            var redTurret = Summon(world, red, 4);

            try
            {
                WithFeud(world, () =>
                {
                    var caught = AbilityManager.VictimsWithin(world.Map, red.Player, red.Player.Position, 20);

                    CollectionAssert.Contains(caught, blue.Player);
                    CollectionAssert.Contains(caught, blueTurret);
                    CollectionAssert.DoesNotContain(caught, redMate.Player);
                    CollectionAssert.DoesNotContain(caught, loner.Player);
                    CollectionAssert.DoesNotContain(caught, redTurret);
                    CollectionAssert.DoesNotContain(caught, red.Player);

                    var sought = AbilityManager.FoesWithin(world.Map, red.Player, redTurret.Position, 20);
                    CollectionAssert.Contains(sought, blue.Player);
                    CollectionAssert.Contains(sought, blueTurret);

                    Pvp.GiveSafety(world.Map, blue.Player);

                    CollectionAssert.DoesNotContain(AbilityManager.FoesWithin(world.Map, red.Player, redTurret.Position, 20), blue.Player);
                });

                CollectionAssert.DoesNotContain(AbilityManager.VictimsWithin(world.Map, red.Player, red.Player.Position, 20), blueTurret);
            }
            finally
            {
                Unsummon(world, blueTurret);
                Unsummon(world, redTurret);
            }
        }

        [TestMethod]
        public void ADebuffOrAStunFromAnEnemyLastsHalfAsLong()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            var redMate = Fighter(world, RedClanId, 5);
            PlaceAll(red, blue, redMate);

            WithFeud(world, () =>
            {
                var debuff = Effect(world, red.Player, 285, false, 10000);
                GameEffectManager.Instance.Attach(world.Map, blue.Player, debuff);
                AssertLeft(5000, debuff);

                var buff = Effect(world, red.Player, 286, true, 10000);
                GameEffectManager.Instance.Attach(world.Map, redMate.Player, buff);
                AssertLeft(10000, buff);

                var friendly = Effect(world, red.Player, 287, false, 10000);
                GameEffectManager.Instance.Attach(world.Map, redMate.Player, friendly);
                AssertLeft(10000, friendly);

                Assert.IsTrue(AbilityManager.StunVictim(world.Map, blue.Player, red.Player, 4000, DamageType.Physical));
                AssertLeft(2000, blue.Player.ActiveEffects.Values.Single(e => e.IsStun));
            });
        }

        [TestMethod]
        public void AnAimArmedOnAPlayerLandsOnTheFirstHitThatHurts()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, blue);

            WithFeud(world, () =>
            {
                Actor landedOn = null;
                var aim = Effect(world, red.Player, 293, false, 20000);
                aim.OnDamaged = (m, holder, effect) => landedOn = holder;
                GameEffectManager.Instance.Attach(world.Map, blue.Player, aim);

                AbilityManager.OnPlayerDamaged(world.Map, blue.Player, 0);
                Assert.IsNull(landedOn, "nothing taken off");

                AbilityManager.OnPlayerDamaged(world.Map, blue.Player, 25);
                Assert.AreSame(blue.Player, landedOn);
                Assert.IsFalse(blue.Player.ActiveEffects.ContainsKey(aim.EffectId), "taken off as it lands");
            });
        }

        private static GameEffect Effect(WorldTestContext world, Actor source, int typeId, bool buff, int durationMs) => new GameEffect
        {
            TypeId = typeId,
            EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
            EffectLevel = 1,
            SourceId = source.EntityId,
            Source = source,
            SourceLevel = 1,
            IsBuff = buff,
            ExpiresTick = Environment.TickCount64 + durationMs
        };

        private static void AssertLeft(int expectedMs, GameEffect effect)
        {
            var left = effect.ExpiresTick - Environment.TickCount64;
            Assert.IsTrue(Math.Abs(left - expectedMs) < 500, $"{left} ms left, expected about {expectedMs}");
        }

        private static Creature Summon(WorldTestContext world, Client master, float x)
        {
            var creature = new Creature
            {
                Name = "Turret",
                MasterEntityId = master.Player.EntityId,
                TargetCategory = TargetCategory.Friendly,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);

            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);

            return creature;
        }

        private static void Unsummon(WorldTestContext world, Creature creature)
        {
            foreach (var cell in world.Map.MapCellInfo.Cells.Values)
                cell.CreatureList.Remove(creature);

            EntityManager.Instance.UnregisterEntity(creature.EntityId);
            EntityManager.Instance.UnregisterCreature(creature.EntityId);
            EntityManager.Instance.UnregisterActor(creature.EntityId);
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

        private static int Health(Client client) => (int)client.Player.Attributes[Attributes.Health].Current;

        private static void WithFeud(WorldTestContext world, Action body) => WithFeud(world, _ => body());

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
