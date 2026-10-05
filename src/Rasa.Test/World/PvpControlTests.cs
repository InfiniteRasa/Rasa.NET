using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    // The control abilities on an enemy player (AbilityManager.PvpControl): Mind Control stuns
    // (P1) or stops them attacking (P2-P5) and helping (P4-P5), Traitor holds them back from the
    // caster's side until the caster hits them, Hack holds an enemy's machine until it or its
    // master is hurt or the master attacks, the mag flash blinds them, and a Spotter lengthens
    // the radar's reach for a stealthed enemy.
    [TestClass]
    [DoNotParallelize]
    public class PvpControlTests
    {
        private const uint RedClanId = 900021;
        private const uint BlueClanId = 900022;

        [TestMethod]
        public void MindControlFrightenStunsAnEnemyForHalfTheTimeAndThenTheyAreImmune()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, blue);

            WithFeud(world, () =>
            {
                Assert.IsTrue(MindControl(world, red, blue, 1, out var shown));
                Assert.AreEqual(Stuns.StunTypeId, shown);
                AssertLeft(7500, blue.Player.ActiveEffects.Values.Single(e => e.IsStun));

                Assert.IsFalse(MindControl(world, red, blue, 3, out _), "immune to reapplication");
            });
        }

        [TestMethod]
        public void MindControlStopsAnEnemyAttackingAndFromP4HelpingTheirSide()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            var blueMate = Fighter(world, BlueClanId, 12);
            var blueThird = Fighter(world, BlueClanId, 14);
            blue.Player.PartyId = blueMate.Player.PartyId = 77;
            PlaceAll(red, blue, blueMate, blueThird);

            WithFeud(world, () =>
            {
                Assert.IsTrue(MindControl(world, red, blue, 2, out var shown));
                Assert.AreEqual(AbilityManager.MindControlTypeId, shown);
                Assert.IsTrue(Pvp.MayNotAttack(blue.Player));
                Assert.IsFalse(Pvp.MayNotAssist(blue.Player), "P2-P3 may still help");
                AssertLeft(7500, blue.Player.ActiveEffects.Values.Single(e => e.TypeId == AbilityManager.MindControlTypeId));

                Assert.IsTrue(MindControl(world, red, blueThird, 4, out _));
                Assert.IsTrue(Pvp.MayNotAttack(blueThird.Player));
                Assert.IsTrue(Pvp.MayNotAssist(blueThird.Player));

                blueMate.Player.Attributes[Attributes.Health].Current = 500;
                Assert.AreEqual(0, ActorManager.Instance.Heal(blueMate.Player, 100, blueThird.Player.EntityId), "no healing anyone else");
                blueThird.Player.Attributes[Attributes.Health].Current = 500;
                Assert.AreEqual(100, ActorManager.Instance.Heal(blueThird.Player, 100, blueThird.Player.EntityId), "but themselves");

                var buff = new GameEffect
                {
                    TypeId = 285,
                    EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                    Source = blueThird.Player,
                    SourceId = blueThird.Player.EntityId,
                    IsBuff = true,
                    ExpiresTick = Environment.TickCount64 + 10000
                };
                GameEffectManager.Instance.Attach(world.Map, blueMate.Player, buff);
                Assert.IsFalse(blueMate.Player.ActiveEffects.ContainsKey(buff.EffectId), "no buffing anyone else");
            });

            Assert.IsTrue(AbilityManager.SquadWithin(world.Map, blue.Player, 50).Contains(blueMate.Player), "squad as before for one who may help");
        }

        [TestMethod]
        public void TraitorHoldsAnEnemyBackFromTheCastersSideUntilTheCasterHitsThem()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var redMate = Fighter(world, RedClanId, 4);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, redMate, blue);
            var redTurret = Summon(world, red, 2);

            try
            {
                WithFeud(world, () =>
                {
                    Assert.IsTrue((bool)Invoke("AttachPlayerTraitor", world.Map, red.Player, blue.Player, Level(ActionId.AaSpyTraitor, 1, 20)));
                    var traitor = blue.Player.ActiveEffects.Values.Single(e => e.Restrains);
                    AssertLeft(10000, traitor);

                    Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, red.Player, 100, blue.Player, out var onCaster));
                    Assert.IsTrue(onCaster.Immune);
                    Assert.AreEqual(0, ActorManager.Instance.Damage(world.Map, redMate.Player, 100, blue.Player));
                    Assert.IsTrue(Pvp.Restrained(blue.Player, redTurret), "nor the caster's side's creatures");

                    Assert.AreEqual(50, ActorManager.Instance.Damage(world.Map, blue.Player, 100, red.Player), "the caster attacks");
                    Assert.IsFalse(blue.Player.ActiveEffects.ContainsKey(traitor.EffectId), "and it is over");
                    Assert.AreEqual(50, ActorManager.Instance.Damage(world.Map, red.Player, 100, blue.Player));
                });
            }
            finally
            {
                Unsummon(world, redTurret);
            }
        }

        [TestMethod]
        public void HackHoldsAnEnemysMachineUntilItOrItsMasterIsHurtOrTheMasterAttacks()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);
            PlaceAll(red, blue);
            var bot = Summon(world, blue, 12);
            bot.ExtraFlags.Add(CreatureFlag.Mechanical);
            var info = Level(ActionId.AaSapperHack, 1, 20);

            try
            {
                WithFeud(world, () =>
                {
                    Assert.IsTrue(Hack(world, red, bot, info));
                    Assert.IsTrue(Stuns.IsStunned(bot));
                    AssertLeft(20000, bot.ActiveEffects.Values.Single(e => e.BreaksOnDamage));

                    AbilityManager.OnCreatureDamaged(world.Map, bot);
                    Assert.IsFalse(Stuns.IsStunned(bot), "hurt");

                    Assert.IsTrue(Hack(world, red, bot, info));
                    AbilityManager.OnPlayerDamaged(world.Map, blue.Player, 10);
                    Assert.IsFalse(Stuns.IsStunned(bot), "its master hurt");

                    Assert.IsTrue(Hack(world, red, bot, info));
                    AbilityManager.OnPlayerActed(world.Map, blue.Player, false);
                    Assert.IsTrue(Stuns.IsStunned(bot), "a heal or a buff is not an attack");
                    AbilityManager.OnPlayerActed(world.Map, blue.Player, true);
                    Assert.IsFalse(Stuns.IsStunned(bot), "its master attacks");
                });
            }
            finally
            {
                Unsummon(world, bot);
            }
        }

        [TestMethod]
        public void TheMagFlashBlindsEnemiesForHalfTheTimeAndDropsTheirTarget()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var redMate = Fighter(world, RedClanId, 3);
            var blue = Fighter(world, BlueClanId, 6);
            var far = Fighter(world, BlueClanId, 40);
            PlaceAll(red, redMate, blue, far);
            blue.Player.Target = red.Player.EntityId;

            WithFeud(world, () =>
            {
                var recovery = new AbilityRecoveryPacket(ActionId.AaRangerTacticalEvasion, 1, AbilityRecoveryPacket.HitDataKind.None);

                Invoke("FlashEnemies", world.Map, red.Player, Level(ActionId.AaRangerTacticalEvasion, 1, 5), 10f, 5000, recovery);

                AssertLeft(2500, blue.Player.ActiveEffects.Values.Single(e => e.Blinds));
                Assert.AreEqual(0UL, blue.Player.Target);
                Assert.IsFalse(redMate.Player.ActiveEffects.Values.Any(e => e.Blinds));
                Assert.IsFalse(far.Player.ActiveEffects.Values.Any(e => e.Blinds), "out of the radius");
                Assert.AreEqual(blue.Player.EntityId, recovery.Hits.Single().EntityId);
            });
        }

        [TestMethod]
        public void StealthArmorsRadarSignatureAndASpottersReachGoToTheClients()
        {
            using var world = new WorldTestContext();
            var red = Fighter(world, RedClanId);
            var blue = Fighter(world, BlueClanId, 10);

            Assert.AreEqual(1.0 + 75.0 / 72.0, AbilityManager.SpotterPerception(5), 1e-9);
            Assert.AreEqual(1.0, AbilityManager.SpotterPerception(0));

            Assert.IsFalse(ManifestationManager.Instance.CreatePlayerEntityData(blue, red).OfType<ToBePerceivedModifierPacket>().Any(), "1.0 is the client's own");

            blue.Player.DetectionRangePercent = 60;
            Assert.AreEqual(0.6, ManifestationManager.Instance.CreatePlayerEntityData(blue, red).OfType<ToBePerceivedModifierPacket>().Single().Mod, 1e-9);
            Assert.IsFalse(ManifestationManager.Instance.CreatePlayerEntityData(blue, blue).OfType<ToBePerceivedModifierPacket>().Any());

            red.Player.ToPerceiveModifier = AbilityManager.SpotterPerception(2);
            Assert.AreEqual(red.Player.ToPerceiveModifier, ManifestationManager.Instance.CreatePlayerEntityData(red, red).OfType<ToPerceiveModifierPacket>().Single().Mod);
            Assert.IsFalse(ManifestationManager.Instance.CreatePlayerEntityData(red, blue).OfType<ToPerceiveModifierPacket>().Any());
        }

        [TestInitialize]
        public void ForgetImmunities()
        {
            // Entity ids come round again from test to test; a caster's immunity list does not.
            var immune = (System.Collections.IDictionary)typeof(AbilityManager)
                .GetField("MindControlImmuneUntil", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

            lock (immune)
                immune.Clear();
        }

        private static bool MindControl(WorldTestContext world, Client caster, Client target, uint level, out int shown)
        {
            var args = new object[] { world.Map, caster.Player, target.Player, Level(ActionId.AaMedicMindControl, level, 15), 0 };
            var landed = (bool)typeof(AbilityManager).GetMethod("AttachPlayerMindControl", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Abilities(), args);
            shown = (int)args[4];
            return landed;
        }

        private static bool Hack(WorldTestContext world, Client caster, Creature pet, ActionLevelInfo info) =>
            (bool)Invoke("HackPet", world.Map, caster.Player, pet, info);

        private static object Invoke(string name, params object[] args)
        {
            var method = typeof(AbilityManager).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!;
            return method.Invoke(method.IsStatic ? null : Abilities(), args);
        }

        private static ActionLevelInfo Level(ActionId actionId, uint level, int durationSeconds)
        {
            var info = new ActionLevelInfo { ActionId = actionId, Level = level, MaxRange = 30 };
            info.Properties[AbilityProperty.Duration] = durationSeconds;
            return info;
        }

        private static void AssertLeft(int expectedMs, GameEffect effect)
        {
            var left = effect.ExpiresTick - Environment.TickCount64;
            Assert.IsTrue(Math.Abs(left - expectedMs) < 500, $"{left} ms left, expected about {expectedMs}");
        }

        private static Creature Summon(WorldTestContext world, Client master, float x)
        {
            var creature = new Creature
            {
                Name = "Bot",
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

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static void WithFeud(WorldTestContext world, Action body)
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

                body();
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
