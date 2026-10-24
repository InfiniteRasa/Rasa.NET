extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Gameplay;

    /// <summary>
    /// A player's killing blow may leave a creature staggering on one health instead of dead, its Critical Death window
    /// open at once with no stun (appsettings.json's CritDeath.StaggerChancePercent, CritDeathManager.StaggersOnKillingBlow):
    /// in both damage paths - a missile (weapon fire, a swing, an ability's missile) and ActorManager.Damage - with the
    /// armour taken as it would have been, never from a tick of damage over time, a creature that is not a player's, a
    /// creature its class says is never finished or one that is run by an ability; and with the chance at 0 nothing at all
    /// is decided. The window then ends as any other: left to run out it kills the creature to whoever opened it, finished
    /// it is the double kill (CritDeathCorpseTests holds the corpse side of both). A morphed player (Polymorph, the PAU
    /// suits) has no Critical Deaths unless CritDeath.FromMorphedPlayers says so: no stagger, no window, no finisher.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CritDeathStaggerTests
    {
        private CritDeathConfig _config;
        private Func<int, bool> _dice;

        /// <summary>The percentages the dice were thrown at since the last <see cref="Chance"/> (or the start of the test).</summary>
        private readonly List<int> _thrown = new List<int>();

        [TestInitialize]
        public void Setup()
        {
            _config = CritDeathManager.Config;
            _dice = CritDeathManager.Dice;
            _thrown.Clear();
        }

        [TestCleanup]
        public void TearDown()
        {
            CritDeathManager.Config = _config;
            CritDeathManager.Dice = _dice;
        }

        /// <summary>The chance in force, and dice that come up as told - and say what they were thrown for.</summary>
        private void Chance(int percent, bool comesUp = true, bool fromMorphed = false)
        {
            _thrown.Clear();
            CritDeathManager.Config = new CritDeathConfig { StaggerChancePercent = percent, FromMorphedPlayers = fromMorphed };
            CritDeathManager.Dice = chance =>
            {
                _thrown.Add(chance);

                return comesUp;
            };
        }

        // ---- the configuration

        [TestMethod]
        public void TheSectionsDefaultIsOneInFourAndSoIsTheShippedFilesAndAFileWithoutItsSection()
        {
            Assert.AreEqual(25, new CritDeathConfig().StaggerChancePercent);
            Assert.IsFalse(new CritDeathConfig().FromMorphedPlayers);
            Assert.AreEqual(25, new RasaGame::Rasa.Config.Config().CritDeath.StaggerChancePercent, "a file without the section");
            Assert.IsFalse(new RasaGame::Rasa.Config.Config().CritDeath.FromMorphedPlayers, "a file without the section");

            var shipped = new RasaGame::Rasa.Config.Config();

            new ConfigurationBuilder().AddJsonFile(Path.Combine(RepositoryRoot(), "src", "Rasa.Game", "appsettings.json"), optional: false)
                .Build().Bind(shipped);

            Assert.AreEqual(25, shipped.CritDeath.StaggerChancePercent, "appsettings.json says it");
            Assert.IsFalse(shipped.CritDeath.FromMorphedPlayers, "appsettings.json says it");

            var changed = new RasaGame::Rasa.Config.Config();

            using var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{ \"CritDeath\": { \"StaggerChancePercent\": 0, \"FromMorphedPlayers\": true } }"));
            new ConfigurationBuilder().AddJsonStream(json).Build().Bind(changed);

            Assert.AreEqual(0, changed.CritDeath.StaggerChancePercent, "and the file may turn it off");
            Assert.IsTrue(changed.CritDeath.FromMorphedPlayers, "and give morphed players theirs back");
        }

        [TestMethod]
        public void UntilAConfigurationIsLoadedNothingStaggersAndAMorphIsAsAnyPlayer()
        {
            // What the process holds before Server.ConfigLoaded has run - which no test, and no code that builds a world
            // without a server, ever does: the rule as it was.
            Assert.AreEqual(0, _config.StaggerChancePercent, "the static default is off");
            Assert.IsTrue(_config.FromMorphedPlayers, "and morphed players are as before");
        }

        [TestMethod]
        public void TheGmCommandShowsAndSetsTheRuleUntilTheNextReload()
        {
            using var world = new WorldTestContext();
            var gm = world.CreateClient();
            Chance(25);

            CritDeathManager.GmCommand(gm, new[] { ".critdeath" });
            Assert.AreEqual(25, CritDeathManager.Config.StaggerChancePercent, "shown, not changed");

            CritDeathManager.GmCommand(gm, new[] { ".critdeath", "100" });
            Assert.AreEqual(100, CritDeathManager.Config.StaggerChancePercent);
            Assert.IsFalse(CritDeathManager.Config.FromMorphedPlayers);

            CritDeathManager.GmCommand(gm, new[] { ".critdeath", "morph", "on" });
            Assert.AreEqual(100, CritDeathManager.Config.StaggerChancePercent);
            Assert.IsTrue(CritDeathManager.Config.FromMorphedPlayers);

            CritDeathManager.GmCommand(gm, new[] { ".critdeath", "101" });
            CritDeathManager.GmCommand(gm, new[] { ".critdeath", "morph", "maybe" });
            Assert.AreEqual(100, CritDeathManager.Config.StaggerChancePercent, "out of range changes nothing");
            Assert.IsTrue(CritDeathManager.Config.FromMorphedPlayers);
        }

        // ---- a morphed player: a creature's shape, a PAU suit

        [TestMethod]
        public void AMorphedPlayersKillingBlowKillsOutrightAndNothingIsThrown()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var shot = Spawn(world, "Shot", 0, health: 50, armor: 0);
            var hit = Spawn(world, "Hit", 3, health: 50, armor: 0);
            shooter.Player.MorphWeapon = new MorphWeaponItem();
            Chance(100);

            Shoot(world, shooter, shot, 200);
            ActorManager.Instance.Damage(world.Map, hit, 200, shooter.Player, out _, DamageType.Physical);

            Assert.AreEqual(CharacterState.Dead, shot.State);
            Assert.AreEqual(CharacterState.Dead, hit.State);
            Assert.IsEmpty(_thrown, "nothing was thrown for a morphed player's blows");

            // The same with the key on: as any player.
            var again = Spawn(world, "Again", 6, health: 50, armor: 0);
            Chance(100, fromMorphed: true);
            Shoot(world, shooter, again, 200);

            Assert.AreEqual(CharacterState.Dying, again.State);
        }

        [TestMethod]
        public void AMorphedPlayerOpensNoWindowByTheStunRuleEither()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "StunnedByAMorph", 0, health: 5, armor: 0);
            shooter.Player.MorphWeapon = new MorphWeaponItem();
            Chance(0);

            Stuns.Apply(world.Map, creature, shooter.Player, Stuns.StunTypeId, 3000, DamageType.Physical);

            Assert.AreNotEqual(CharacterState.Dying, creature.State);
            Assert.IsNull(CritDeathManager.PreDeathOf(creature));
        }

        [TestMethod]
        public void AMorphedSquadMateMayNotFinishAWindowAndItStaysForTheOthers()
        {
            using var world = new WorldTestContext();
            var opener = Shooter(world, x: 200);
            var morphed = Shooter(world, x: 12);
            var creature = Spawn(world, "Held", 10, health: 10, armor: 0);
            opener.Player.PartyId = 77;
            morphed.Player.PartyId = 77;
            Chance(100);

            Shoot(world, opener, creature, 100);
            Assert.AreEqual(CharacterState.Dying, creature.State);

            morphed.Player.MorphWeapon = new MorphWeaponItem();
            CritDeathManager.Instance.RequestCritDeathFinish(morphed, new RequestCritDeathFinishPacket
            {
                ActionId = ActionId.CriticalDeathFinisher,
                ActionArgId = 1,
                TargetId = creature.EntityId
            });

            Assert.IsFalse(world.Map.PerformRecovery.Any(action => action.ActionId == ActionId.CriticalDeathFinisher), "no finisher is wound up");
            Assert.AreEqual(0ul, CritDeathManager.PreDeathOf(creature).FinisherId, "and the window is nobody's to finish yet");
        }

        [TestMethod]
        public void TheChanceIsTheConfiguredPercentOfKillingBlowsAndAtTheExtremesNoneOrAll()
        {
            using var world = new WorldTestContext();
            var player = Shooter(world, x: 200).Player;
            var creature = Spawn(world, "Rolled", 0, 10, 0);

            // The real dice.
            CritDeathManager.Dice = _dice;

            int Staggers(int percent, int trials)
            {
                CritDeathManager.Config = new CritDeathConfig { StaggerChancePercent = percent };

                var count = 0;

                for (var i = 0; i < trials; i++)
                    if (CritDeathManager.Instance.StaggersOnKillingBlow(creature, player, periodic: false))
                        count++;

                return count;
            }

            Assert.AreEqual(0, Staggers(0, 300));
            Assert.AreEqual(300, Staggers(100, 300));

            var rolled = Staggers(25, 4000);

            // 25 percent of 4000 is 1000, with a standard deviation of 27: eight of them either way is not chance.
            Assert.IsTrue(rolled > 800 && rolled < 1200, $"{rolled} of 4000 killing blows staggered at 25 percent");
        }

        // ---- a killing blow of a weapon, a swing or an ability's missile

        [TestMethod]
        public void AtZeroAKillingMissileKillsAsItAlwaysDidAndNothingIsThrown()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "Plain", 0, health: 50, armor: 40);
            Chance(0);

            var missile = Shoot(world, shooter, creature, 200);

            Assert.AreEqual(CharacterState.Dead, creature.State);
            Assert.AreEqual(0, Health(creature));
            Assert.AreEqual(0, creature.Attributes[Attributes.Armor].Current);
            Assert.IsNull(CritDeathManager.PreDeathOf(creature), "no window");
            Assert.AreEqual(0, _thrown.Count, "nothing was decided");
            Assert.AreEqual(200, missile.DamageA, "the hit reports the whole blow, as before");
            Assert.AreEqual(1, missile.Args.HitData.Single().DeathBlow);
        }

        [TestMethod]
        public void AtOneHundredAKillingMissileLeavesTheCreatureOnOneHealthInItsWindowWithTheArmourGone()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var watcher = Watch(world, x: 60);
            var creature = Spawn(world, "Staggers", 0, health: 50, armor: 40);
            Chance(100);
            WorldTestContext.Drain(watcher);

            var missile = Shoot(world, shooter, creature, 200);

            Assert.AreEqual(CharacterState.Dying, creature.State, "held in its window");
            Assert.AreEqual(1, Health(creature), "one health is left");
            Assert.AreEqual(0, creature.Attributes[Attributes.Armor].Current, "the armour went as it would have: all of it");
            Assert.AreEqual(0, creature.Attributes[Attributes.Health].RefreshAmount, "nothing comes back while it is held");
            Assert.AreEqual(0, creature.Attributes[Attributes.Armor].RefreshAmount);
            Assert.IsFalse(creature.CritKilled);

            var window = CritDeathManager.PreDeathOf(creature);

            Assert.IsNotNull(window, "the window is on it");
            Assert.AreSame(shooter.Player, window.Source, "it is the shooter's");
            Assert.AreEqual(shooter.Player.EntityId, window.SourceId);
            Assert.IsTrue(window.ExpiresTick - Environment.TickCount64 > CritDeathManager.WindowMs - 1000, "for its whole time");
            Assert.AreEqual(1, _thrown.Count, "one throw, for the one killing blow");
            Assert.AreEqual(100, _thrown[0]);

            Assert.AreEqual(40 + 49, missile.DamageA, "the hit reports what it dealt: the armour's 40 and the 49 health the creature could lose");
            Assert.AreEqual(40 + 49, missile.Args.HitData.Single().FinalAmt);
            Assert.AreEqual(0, missile.Args.HitData.Single().DeathBlow, "it is not dead");

            var sent = Sent(watcher, creature);

            Assert.AreEqual(1, sent.OfType<UpdateHealthPacket>().Last().Health.Current, "the clients are told its one health");
            Assert.IsTrue(sent.OfType<GameEffectAttachedPacket>().Any(attached => attached.EffectTypeId == CritDeathManager.PreDeathTypeId),
                "and the window, which is what shows the skull");
            Assert.IsFalse(sent.OfType<StateChangePacket>().Any(change => change.StateIds.Contains(CharacterState.Dead)), "no death is announced");
        }

        [TestMethod]
        public void AMissileThatDoesNotKillNeverStaggersWhateverTheChance()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "Survives", 0, health: 500, armor: 0);
            Chance(100);

            Shoot(world, shooter, creature, 100);
            Shoot(world, shooter, creature, 399);

            Assert.AreEqual(1, Health(creature), "499 of its 500 is gone and it is on its feet: the second blow did not kill");
            Assert.AreEqual(CharacterState.Idle, creature.State);
            Assert.IsNull(CritDeathManager.PreDeathOf(creature));
            Assert.AreEqual(0, _thrown.Count, "nothing was thrown for either");
        }

        [TestMethod]
        public void AMissileThatTakesExactlyTheCreaturesHealthIsAKillingBlow()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "Exactly", 0, health: 100, armor: 0);
            Chance(100);

            Shoot(world, shooter, creature, 100);

            Assert.AreEqual(CharacterState.Dying, creature.State);
            Assert.AreEqual(1, Health(creature));
            Assert.IsNotNull(CritDeathManager.PreDeathOf(creature));
        }

        [TestMethod]
        public void AtTwentyFivePercentTheThrowIsOnePerKillingBlowAndAMissDiesAsItAlwaysDid()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var lucky = Spawn(world, "Lucky", 0, health: 10, armor: 0);
            var unlucky = Spawn(world, "Unlucky", 3, health: 10, armor: 0);
            var spared = Spawn(world, "Spared", 6, health: 1000, armor: 0);

            Chance(25, comesUp: false);
            Shoot(world, shooter, spared, 5);
            Assert.AreEqual(0, _thrown.Count, "a hit that does not kill is not thrown for");

            Shoot(world, shooter, unlucky, 50);
            CollectionAssert.AreEqual(new[] { 25 }, _thrown, "thrown at the configured percent");
            Assert.AreEqual(CharacterState.Dead, unlucky.State, "a miss is a death");
            Assert.AreEqual(0, Health(unlucky));

            Chance(25, comesUp: true);
            Shoot(world, shooter, lucky, 50);
            CollectionAssert.AreEqual(new[] { 25 }, _thrown);
            Assert.AreEqual(CharacterState.Dying, lucky.State);
        }

        [TestMethod]
        public void ASwingIsAMissileToo()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "Struck", 0, health: 30, armor: 0);
            Chance(100);

            var swing = new Missile
            {
                Source = shooter.Player,
                TargetActor = creature,
                TargetEntityId = creature.EntityId,
                DamageA = 90,
                DamageType = DamageType.Physical,
                ActionId = ActionId.WeaponMelee,
                ActionArgId = 1,
                IsMelee = true,
                CritChance = 0
            };

            MissileManager.Instance.MissileTrigger(world.Map, swing);

            Assert.AreEqual(CharacterState.Dying, creature.State);
            Assert.AreEqual(1, Health(creature));
        }

        // ---- ActorManager.Damage: abilities, constant fire, a bomb, a crit's arc

        [TestMethod]
        public void AtZeroAKillingHitOfDamageKillsAsItAlwaysDidAndAtOneHundredLeavesTheCreatureStaggering()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var killed = Spawn(world, "Killed", 0, health: 50, armor: 40);
            var staggered = Spawn(world, "Staggered", 3, health: 50, armor: 40);

            Chance(0);
            var taken = ActorManager.Instance.Damage(world.Map, killed, 200, shooter.Player, out var outcome, DamageType.Physical);

            Assert.AreEqual(CharacterState.Dead, killed.State);
            Assert.AreEqual(0, Health(killed));
            Assert.AreEqual(90, taken, "what came off the armour and the health");
            Assert.AreEqual(200, outcome.Delivered, "the hit reports the whole blow, as before");
            Assert.AreEqual(0, _thrown.Count);

            Chance(100);
            taken = ActorManager.Instance.Damage(world.Map, staggered, 200, shooter.Player, out outcome, DamageType.Physical);

            Assert.AreEqual(CharacterState.Dying, staggered.State);
            Assert.AreEqual(1, Health(staggered));
            Assert.AreEqual(0, staggered.Attributes[Attributes.Armor].Current, "the armour went as it would have");
            Assert.AreEqual(40 + 49, taken);
            Assert.AreEqual(40 + 49, outcome.Delivered, "the hit reports what it dealt");
            Assert.AreSame(shooter.Player, CritDeathManager.PreDeathOf(staggered).Source);
        }

        [TestMethod]
        public void AHitOfDamageThatDoesNotKillNeverStaggersWhateverTheChance()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "Scratched", 0, health: 500, armor: 0);
            Chance(100);

            var taken = ActorManager.Instance.Damage(world.Map, creature, 120, shooter.Player, out _, DamageType.Physical);

            Assert.AreEqual(120, taken);
            Assert.AreEqual(380, Health(creature));
            Assert.AreEqual(CharacterState.Idle, creature.State);
            Assert.AreEqual(0, _thrown.Count);
        }

        [TestMethod]
        public void ATickOfDamageOverTimeNeverStaggersAndNeitherDoesSomethingThatIsNotAPlayers()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var burning = Spawn(world, "Burning", 0, health: 10, armor: 0);
            var bitten = Spawn(world, "Bitten", 3, health: 10, armor: 0);
            var nobodys = Spawn(world, "Nobodys", 6, health: 10, armor: 0);
            var attacker = Spawn(world, "Attacker", 9, health: 1000, armor: 0);
            Chance(100);

            // The same blow, as an effect's tick: the clock of damage over time is GameEffectManager's, and it says so.
            ActorManager.Instance.Damage(world.Map, burning, 200, shooter.Player, out _, DamageType.Fire, isPeriodic: true);
            Assert.AreEqual(CharacterState.Dead, burning.State, "a tick kills");

            // A creature's blow on another, and one with no one behind it.
            ActorManager.Instance.Damage(world.Map, bitten, 200, attacker, out _, DamageType.Physical);
            Assert.AreEqual(CharacterState.Dead, bitten.State, "only a player's blow staggers");

            ActorManager.Instance.Damage(world.Map, nobodys, 200, null, out _, DamageType.Physical);
            Assert.AreEqual(0, Health(nobodys));

            Assert.AreEqual(0, _thrown.Count, "none of them was thrown for");
        }

        [TestMethod]
        public void ARealTickOfAnEffectOnACreatureNeverStaggersIt()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "Ruined", 0, health: 10, armor: 0);
            Chance(100);

            // A damage over time as GameEffectManager keeps it: a tick of 100000 on a creature with 10.
            var effect = new GameEffect
            {
                TypeId = 2,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                EffectLevel = 1,
                SourceId = shooter.Player.EntityId,
                Source = shooter.Player,
                SourceLevel = 1,
                IsBuff = false,
                TickIntervalMs = 1000,
                NextTickTick = Environment.TickCount64 - 1,
                ExpiresTick = Environment.TickCount64 + 60000,
                TickDamageMin = 100000,
                TickDamageMax = 100000,
                TickDamageType = DamageType.Fire
            };

            GameEffectManager.Instance.Attach(world.Map, creature, effect);
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(CharacterState.Dead, creature.State, "a tick of damage over time kills");
            Assert.AreEqual(0, _thrown.Count);
        }

        // ---- who may be left staggering

        [TestMethod]
        public void ACreatureItsClassSaysIsNeverFinishedJustDies()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var missileKilled = Spawn(world, "Boss", 0, health: 10, armor: 0);
            var damageKilled = Spawn(world, "Boss2", 3, health: 10, armor: 0);
            missileKilled.ExtraFlags.Add(CreatureFlag.NoCritKill);
            damageKilled.ExtraFlags.Add(CreatureFlag.NoCritKill);
            Chance(100);

            Shoot(world, shooter, missileKilled, 100);
            ActorManager.Instance.Damage(world.Map, damageKilled, 100, shooter.Player, out _, DamageType.Physical);

            Assert.AreEqual(CharacterState.Dead, missileKilled.State);
            Assert.AreEqual(CharacterState.Dead, damageKilled.State);
            Assert.AreEqual(0, _thrown.Count, "and it was not thrown for");
        }

        [TestMethod]
        public void APlayersPetAThingAnAbilityRunsAndAGunOnAMountJustDie()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var other = Shooter(world, x: 210);
            var pet = Spawn(world, "Pet", 0, health: 10, armor: 0);
            var mine = Spawn(world, "Mine", 3, health: 10, armor: 0);
            var turret = Spawn(world, "Turret", 6, health: 10, armor: 0);

            // An enemy player's summon: a creature with a master who is a player (a minion, a Traitor's creature).
            pet.MasterEntityId = other.Player.EntityId;
            mine.IsScripted = true;
            world.AddClass((EntityClasses)7482);
            turret.EntityClass = (EntityClasses)7482;   // Emplacement_Bane_Turret_Standard
            Chance(100);

            // Another player's pet is not this player's to hurt (Pvp), so it is asked directly.
            Assert.IsFalse(CritDeathManager.MayStagger(pet), "a player's pet is never left staggering");
            Assert.IsFalse(CritDeathManager.Instance.StaggersOnKillingBlow(pet, shooter.Player, periodic: false));

            ActorManager.Instance.Damage(world.Map, mine, 100, shooter.Player, out _, DamageType.Physical);
            Shoot(world, shooter, turret, 100);

            Assert.AreEqual(CharacterState.Dead, mine.State, "a creature an ability runs goes the way HandleCreatureKill ends it, not into a window");
            Assert.AreEqual(0, Health(mine));
            Assert.IsNull(CritDeathManager.PreDeathOf(mine));
            Assert.AreEqual(CharacterState.Dead, turret.State);
            Assert.AreEqual(0, _thrown.Count);
        }

        [TestMethod]
        public void ACreatureThatIsImmuneTakesNothingAndStaggersNot()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var creature = Spawn(world, "Immune", 0, health: 10, armor: 0);
            creature.ImmuneToAllDamage = true;
            Chance(100);

            var missile = Shoot(world, shooter, creature, 100);
            ActorManager.Instance.Damage(world.Map, creature, 100, shooter.Player, out var outcome, DamageType.Physical);

            Assert.AreEqual(10, Health(creature));
            Assert.AreEqual(CharacterState.Idle, creature.State);
            Assert.IsTrue(outcome.Immune);
            Assert.AreEqual(0, missile.DamageA);
            Assert.AreEqual(0, _thrown.Count);
        }

        // ---- the way in that was there before

        [TestMethod]
        public void AStunnedCreatureAtOrBelowEightPercentStillOpensItsWindowWithNoThrowAtAnyChance()
        {
            foreach (var percent in new[] { 0, 25, 100 })
            {
                using var world = new WorldTestContext();
                var shooter = Shooter(world, x: 200);
                var creature = Spawn(world, "Stunned" + percent, 0, health: 5, armor: 0);
                Chance(percent, comesUp: false);

                Stuns.Apply(world.Map, creature, shooter.Player, Stuns.StunTypeId, 3000, DamageType.Physical);

                Assert.AreEqual(CharacterState.Dying, creature.State, $"the stun rule at {percent} percent");
                Assert.AreEqual(5, Health(creature), "it keeps the health it had");
                Assert.IsNotNull(CritDeathManager.PreDeathOf(creature));
                Assert.AreEqual(0, _thrown.Count, "no dice: that way in never had any");
            }
        }

        [TestMethod]
        public void AStunnedCreatureAboveEightPercentAndAnUnstunnedOneNearDeathStayOutWhateverTheChance()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var healthy = Spawn(world, "Healthy", 0, health: 20, armor: 0);
            var near = Spawn(world, "NearDeath", 3, health: 5, armor: 0);
            Chance(100);

            Stuns.Apply(world.Map, healthy, shooter.Player, Stuns.StunTypeId, 3000, DamageType.Physical);
            Shoot(world, shooter, near, 0);

            Assert.AreEqual(CharacterState.Idle, healthy.State, "20 of 100 is not near death");
            Assert.AreEqual(CharacterState.Idle, near.State, "near death and not stunned, and no killing blow: nothing opens");
            Assert.IsNull(CritDeathManager.PreDeathOf(healthy));
            Assert.IsNull(CritDeathManager.PreDeathOf(near));
        }

        [TestMethod]
        public void ADyingCreatureTakesNothingMoreFromShotsFromAnyoneOrFromDamageOfAnyKind()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var squadMate = Shooter(world, x: 205);
            var creature = Spawn(world, "Held", 0, health: 50, armor: 40);
            Chance(100);

            Shoot(world, shooter, creature, 200);
            Assert.AreEqual(CharacterState.Dying, creature.State);

            // Another player's shot, its own area, an ability's hit and a tick: it is left alone, throw or no throw.
            var late = Shoot(world, squadMate, creature, 500);
            var taken = ActorManager.Instance.Damage(world.Map, creature, 500, squadMate.Player, out _, DamageType.Physical);
            var tick = ActorManager.Instance.Damage(world.Map, creature, 500, squadMate.Player, out _, DamageType.Fire, isPeriodic: true);

            Assert.AreEqual(1, Health(creature));
            Assert.AreEqual(CharacterState.Dying, creature.State);
            Assert.AreEqual(0, taken);
            Assert.AreEqual(0, tick);
            Assert.AreEqual(0, creature.Attributes[Attributes.Armor].Current);
            Assert.AreSame(shooter.Player, CritDeathManager.PreDeathOf(creature).Source, "the window is still the first shooter's");
            Assert.AreEqual(1, _thrown.Count, "and nothing was thrown for the later blows");
            Assert.IsFalse(late.Args.HitData.Any(hit => hit.DeathBlow == 1));
        }

        // ---- the window of a staggered creature runs as any other

        [TestMethod]
        public void AStaggeredCreatureLeftToDieIsACorpseAtZeroAndGoesLikeAnyOther()
        {
            using var world = new WorldTestContext();
            var shooter = Shooter(world, x: 200);
            var watcher = Watch(world, x: 60);
            var creature = Spawn(world, "RunsOut", 0, health: 50, armor: 40);
            Chance(100);

            Shoot(world, shooter, creature, 200);
            Assert.AreEqual(CharacterState.Dying, creature.State);
            WorldTestContext.Drain(watcher);

            // Nobody finishes it: the window runs out.
            CritDeathManager.PreDeathOf(creature).ExpiresTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(CharacterState.Dead, creature.State);
            Assert.AreEqual(0, Health(creature), "dead is at zero");
            Assert.AreEqual(0, creature.Attributes[Attributes.Health].RefreshAmount);
            Assert.AreEqual(0, creature.Attributes[Attributes.Armor].Current);
            Assert.IsFalse(creature.CritKilled, "it was not finished");

            var sent = Sent(watcher, creature);
            var health = sent.FindIndex(packet => packet is UpdateHealthPacket);
            var state = sent.FindIndex(packet => packet is StateChangePacket);

            Assert.IsTrue(health >= 0 && state > health, "the clients are told its health is gone and then that it is dead");
            Assert.AreEqual(0, ((UpdateHealthPacket)sent[health]).Health.Current);

            Think(world, LootDispenserManager.EmptyCorpseMs);

            Assert.IsFalse(InWorld(world, creature), "an empty corpse goes after its time");
        }

        [TestMethod]
        public void AStaggeredCreatureFinishedIsACritKilledCorpseAtZero()
        {
            using var world = new WorldTestContext();
            var finisher = Shooter(world, x: 12);
            var watcher = Watch(world, x: 60);
            var creature = Spawn(world, "Finished", 10, health: 50, armor: 40);
            Chance(100);

            Shoot(world, finisher, creature, 200);
            Assert.AreEqual(CharacterState.Dying, creature.State);
            Assert.AreEqual(1, Health(creature));

            // The melee press next to it, its windup, and the finishing animation played out.
            CritDeathManager.Instance.RequestCritDeathFinish(finisher, new RequestCritDeathFinishPacket
            {
                ActionId = ActionId.CriticalDeathFinisher,
                ActionArgId = 1,
                TargetId = creature.EntityId
            });
            var press = world.Map.PerformRecovery.Single(action => action.ActionId == ActionId.CriticalDeathFinisher);
            world.Map.PerformRecovery.Remove(press);
            CritDeathManager.Instance.PerformRecovery(world.Map, press);

            Assert.IsNull(CritDeathManager.PreDeathOf(creature), "the window is taken off for the finishing animation");
            Assert.AreEqual(CharacterState.Dying, creature.State);
            WorldTestContext.Drain(watcher);

            creature.ActiveEffects.Values.Single(effect => effect.TypeId == CritDeathManager.DeathTypeId).ExpiresTick = Environment.TickCount64 - 1;
            GameEffectManager.Instance.DoWork(world.Map, 500);

            Assert.AreEqual(CharacterState.Dead, creature.State);
            Assert.AreEqual(0, Health(creature));
            Assert.IsTrue(creature.CritKilled, "a finishing move destroys the body");

            Think(world, LootDispenserManager.EmptyCorpseMs);

            Assert.IsFalse(InWorld(world, creature));
        }

        [TestMethod]
        public void ASquadMateMayFinishAStaggeredCreatureAndStrangersMayNot()
        {
            using var world = new WorldTestContext();
            var opener = Shooter(world, x: 200);
            var mate = Shooter(world, x: 12);
            var stranger = Shooter(world, x: 11);
            var creature = Spawn(world, "Shared", 10, health: 10, armor: 0);
            opener.Player.PartyId = 77;
            mate.Player.PartyId = 77;
            Chance(100);

            Shoot(world, opener, creature, 100);

            Assert.IsTrue(CritDeathManager.MayFinish(mate.Player, CritDeathManager.PreDeathOf(creature).Source));
            Assert.IsFalse(CritDeathManager.MayFinish(stranger.Player, CritDeathManager.PreDeathOf(creature).Source));
            Assert.AreEqual(CritKill.Team, CritDeathManager.KindOf(mate.Player, opener.Player));
        }


        // ---- helpers

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Could not locate the repository root from the test output directory.");

            return directory.FullName;
        }

        private static int Health(Creature creature) => creature.Attributes[Attributes.Health].Current;

        /// <summary>A player standing at x, alive, with the cells around them but not in a cell's client list: what they kill pays them nothing.</summary>
        private static Client Shooter(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);

            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 100, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);

            return client;
        }

        /// <summary>A shot that lands now, with no crit and no cover.</summary>
        private static Missile Shoot(WorldTestContext world, Client shooter, Creature target, int damage)
        {
            var missile = new Missile
            {
                Source = shooter.Player,
                TargetActor = target,
                TargetEntityId = target.EntityId,
                DamageA = damage,
                DamageType = DamageType.Physical,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                CritChance = 0
            };

            MissileManager.Instance.MissileTrigger(world.Map, missile);

            return missile;
        }

        /// <summary>What the clients near the creature were told about it, in order.</summary>
        private static List<PythonPacket> Sent(Client watcher, Creature creature) =>
            WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == creature.EntityId).Select(message => message.Packet).ToList();

        private static void Think(WorldTestContext world, long milliseconds)
        {
            for (var elapsed = 0L; elapsed < milliseconds; elapsed += 250)
                BehaviorManager.Instance.MapChannelThink(world.Map, 250);
        }

        private static bool InWorld(WorldTestContext world, Creature creature) =>
            world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(creature));

        /// <summary>A hostile creature with the given health and armour, in the map's cells.</summary>
        private static Creature Spawn(WorldTestContext world, string name, float x, int health, int armor)
        {
            var max = Math.Max(100, health);

            var creature = new Creature
            {
                Name = name,
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, max, max, health, 2, 5);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, armor, 2, 5);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client standing in the map's cells, so what is sent about the creatures near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }
    }
}
