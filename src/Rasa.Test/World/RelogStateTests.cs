extern alias RasaGame;

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
    using ClientState = RasaGame::Rasa.Data.ClientState;

    // What a character keeps across a logout and a map change (RelogVitals, EffectCarry), and a
    // connection that drops in a fight (CombatLogout).
    [TestClass]
    [DoNotParallelize]
    public class RelogStateTests
    {
        private const long Now = 1_800_000_000_000;   // a wall-clock time, Unix ms

        [TestMethod]
        public void LeavingTheWorldKeepsHealthArmourPowerAndThePenaltiesLeft()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var player = client.Player;

            PlayerDeath.RestorePenalties(world.Map, player, 2, 100_000, 20_000);
            player.Attributes[Attributes.Health].Current = 400;
            player.Attributes[Attributes.Armor].Current = 35;
            player.Attributes[Attributes.Power].Current = 12;

            var saved = RelogVitals.Capture(player, Now);

            Assert.AreEqual(400, saved.Health);
            Assert.AreEqual(35, saved.Armor);
            Assert.AreEqual(12, saved.Power);
            Assert.AreEqual(2, saved.RezTraumaStacks);
            Assert.AreEqual(100_000, saved.RezTraumaEndsAt - Now, 1000);
            Assert.AreEqual(20_000, saved.NoHealEndsAt - Now, 1000);
        }

        [TestMethod]
        public void NothingToKeepSavesNoPenaltiesAndADeadPlayerNoHealth()
        {
            using var world = new WorldTestContext();
            var player = Player(world).Player;

            var saved = RelogVitals.Capture(player, Now);
            Assert.AreEqual(0, saved.RezTraumaStacks);
            Assert.AreEqual(0L, saved.RezTraumaEndsAt);
            Assert.AreEqual(0L, saved.NoHealEndsAt);
            Assert.AreEqual(1000, saved.Health);

            player.State = CharacterState.Dead;
            player.Attributes[Attributes.Health].Current = 0;
            Assert.AreEqual(CharacterEntry.VitalNotSaved, RelogVitals.Capture(player, Now).Health, "comes back on full rather than at zero");
        }

        [TestMethod]
        public void PenaltiesSetAsideOnALoadingScreenAreKeptToo()
        {
            using var world = new WorldTestContext();
            var player = Player(world).Player;

            PlayerDeath.RestorePenalties(world.Map, player, 1, 90_000, 0);
            EffectCarry.Stash(player);
            GameEffectManager.Instance.ClearEffects(world.Map, player);

            var saved = RelogVitals.Capture(player, Now);

            Assert.AreEqual(1, saved.RezTraumaStacks);
            Assert.AreEqual(90_000, saved.RezTraumaEndsAt - Now, 1000);
        }

        [TestMethod]
        public void ArrivingPutsTheSavedValuesBackWithinTheirMaximums()
        {
            using var world = new WorldTestContext();
            var player = Player(world).Player;

            player.LeftWith = new SavedVitals { Health = 250, Armor = 9000, Power = CharacterEntry.VitalNotSaved };
            RelogVitals.ApplyVitals(player);

            Assert.AreEqual(250, player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(100, player.Attributes[Attributes.Armor].Current, "no more than the maximum now");
            Assert.AreEqual(50, player.Attributes[Attributes.Power].Current, "not saved: as worked out");

            player.LeftWith = new SavedVitals { Health = 0 };
            RelogVitals.ApplyVitals(player);
            Assert.AreEqual(1, player.Attributes[Attributes.Health].Current, "never arrives at zero");
        }

        [TestMethod]
        public void ArrivingPutsBackThePenaltiesForTheTimeTheyHaveLeft()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var player = client.Player;

            player.LeftWith = new SavedVitals { RezTraumaStacks = 2, RezTraumaEndsAt = Now + 100_000, NoHealEndsAt = Now + 10_000 };
            RelogVitals.RestorePenalties(client, Now);

            var trauma = player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessTypeId);
            Assert.AreEqual(2, trauma.Stacks);
            Assert.AreEqual(-40, trauma.PrimaryAttributesPercent);
            Assert.AreEqual(100_000, trauma.RemainingMs, 1000);
            Assert.IsTrue(GameEffectManager.HealingBlocked(player));
            Assert.IsNull(player.LeftWith, "once");
        }

        [TestMethod]
        public void PenaltiesThatRanOutWhileAwayOrAreOutOfBoundsAreNotGiven()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var player = client.Player;

            player.LeftWith = new SavedVitals { RezTraumaStacks = 3, RezTraumaEndsAt = Now - 1, NoHealEndsAt = Now - 1 };
            RelogVitals.RestorePenalties(client, Now);
            Assert.IsFalse(player.ActiveEffects.Values.Any(e => PlayerDeath.IsDeathPenalty(e.TypeId)));

            player.LeftWith = new SavedVitals { RezTraumaStacks = 9, RezTraumaEndsAt = Now + 36_000_000, NoHealEndsAt = Now + 36_000_000 };
            RelogVitals.RestorePenalties(client, Now);

            var trauma = player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessTypeId);
            Assert.AreEqual(3, trauma.Stacks, "REZ_SICKNESS_MAX_STACK");
            Assert.AreEqual(360_000, trauma.RemainingMs, 1000, "REZ_SICKNESS_MAX_DURATION");
            Assert.AreEqual(30_000, player.ActiveEffects.Values.Single(e => e.TypeId == PlayerDeath.RezSicknessNoHealTypeId).RemainingMs, 1000);
        }

        [TestMethod]
        public void DeathPenaltiesGoAcrossAMapChange()
        {
            using var world = new WorldTestContext();
            var player = Player(world).Player;

            PlayerDeath.RestorePenalties(world.Map, player, 1, 60_000, 15_000);
            EffectCarry.Stash(player);

            CollectionAssert.AreEquivalent(
                new[] { PlayerDeath.RezSicknessTypeId, PlayerDeath.RezSicknessNoHealTypeId },
                player.CarriedEffects.Select(c => c.Effect.TypeId).ToArray());
        }

        [TestMethod]
        public void ADroppedConnectionInAFightStaysUntilTheFightWouldHaveLetItGo()
        {
            using var world = new WorldTestContext();
            var player = Player(world).Player;
            const long tick = 1_000_000;

            player.InCombat = true;
            player.CombatExpiresAt = tick + 12_000;
            Assert.AreEqual(tick + 12_000, CombatLogout.LingerUntil(player, ClientState.Ingame, tick));

            player.CombatExpiresAt = tick + 1_000;
            Assert.AreEqual(tick + MapChannelManager.LogoutDelayMs, CombatLogout.LingerUntil(player, ClientState.Ingame, tick), "never less than the Logout button");

            player.CombatExpiresAt = tick + 600_000;
            Assert.AreEqual(tick + CombatRegen.CombatTimeoutMs, CombatLogout.LingerUntil(player, ClientState.Ingame, tick), "never more than one combat timeout");
        }

        [TestMethod]
        public void OutOfAFightLoggingOutDeadOrLoadingLeavesAtOnce()
        {
            using var world = new WorldTestContext();
            var player = Player(world).Player;
            const long tick = 1_000_000;

            player.CombatExpiresAt = tick + 12_000;
            Assert.AreEqual(0L, CombatLogout.LingerUntil(player, ClientState.Ingame, tick), "out of combat");

            player.InCombat = true;
            Assert.AreEqual(0L, CombatLogout.LingerUntil(player, ClientState.Loading, tick), "on a loading screen");

            player.RemoveFromMap = true;
            Assert.AreEqual(0L, CombatLogout.LingerUntil(player, ClientState.Ingame, tick), "already leaving by the Logout button");

            player.RemoveFromMap = false;
            player.State = CharacterState.Dead;
            Assert.AreEqual(0L, CombatLogout.LingerUntil(player, ClientState.Ingame, tick), "dead");
        }

        [TestMethod]
        public void ALingeringBodyCanBeFoughtAndAGoneOneCannot()
        {
            using var world = new WorldTestContext();
            var player = Player(world).Player;
            var creature = Monster(world, 3);

            try
            {
                player.Disconected = true;
                player.LingerUntil = Environment.TickCount64 + 10_000;
                Assert.IsTrue(player.IsLingering);
                Assert.IsTrue(Threat.CanFight(creature, player.EntityId), "dropped out of the fight: still in it");

                player.LingerUntil = Environment.TickCount64 - 1;
                Assert.IsTrue(player.IsGone);
                Assert.IsFalse(Threat.CanFight(creature, player.EntityId), "gone");
            }
            finally
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }
        }

        [TestMethod]
        public void TheMainLoopLeavesAFlaggedPlayerOnAMapToThatMapsWorker()
        {
            using var world = new WorldTestContext();
            var client = Player(world);
            var maps = new MapChannelManager(null);
            maps.MapChannelArray.Add(1220, world.Map);

            client.State = ClientState.Disconnected;
            client.Player.Disconected = true;
            client.Player.RemoveFromMap = true;
            maps.CleanupDisconnected(client);

            Assert.IsTrue(world.Map.ClientList.Contains(client), "the worker's, with its saves");
            Assert.IsTrue(EntityManager.Instance.Players.ContainsKey(client.Player.EntityId));

            client.Player.RemoveFromMap = false;
            maps.CleanupDisconnected(client);

            Assert.IsFalse(world.Map.ClientList.Contains(client));
            Assert.IsFalse(EntityManager.Instance.Players.ContainsKey(client.Player.EntityId));
        }

        private static Client Player(WorldTestContext world)
        {
            var client = world.CreateClient(0, 0);
            client.Player.Level = 10;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 50, 50, 50, 0, 0);
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static Creature Monster(WorldTestContext world, float x)
        {
            var creature = new Creature
            {
                Name = "Monster",
                TargetCategory = TargetCategory.Hostile,
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

            return creature;
        }
    }
}
