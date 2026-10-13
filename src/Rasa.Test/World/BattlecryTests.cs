using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// Battle cries (Battlecries): that a creature with a package is sent BattlecryNotification
    /// as its fight begins and ends, as it is hurt, struck critically and brought low, as it
    /// kills, and as it halts and walks on on its beat; that one with none is silent; and the
    /// packages seeded (creature_battlecry).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BattlecryTests
    {
        private const long Tick = 250;

        private const string Migration = "20261127000000_Add_creature_battlecries";
        private const string Before = "20261126000000_Add_ambient_npcs";

        private const int HumanMale = 8;
        private const int Treelurker = 3;
        private const int ThraxSoldier = 12;
        private const int DrillSergeant = 4;

        /// <summary>The entity class every fixture creature is of.</summary>
        private const uint FixtureClass = (uint)EntityClasses.HumanBaseMale;

        private Func<int, int> _roll;
        private Func<long> _now;
        private Action<uint, int> _keep;
        private readonly List<(uint CreatureId, int PackageId)> _kept = new List<(uint, int)>();
        private long _clock;
        private int _rolled;

        [TestInitialize]
        public void Initialize()
        {
            _roll = Battlecries.Roll;
            _now = Battlecries.Now;
            _keep = Battlecries.Keep;
            _kept.Clear();
            Battlecries.Keep = (creatureId, packageId) => _kept.Add((creatureId, packageId));
            _clock = 1_000_000;
            _rolled = 0;
            Battlecries.Roll = _ => _rolled;
            Battlecries.Now = () => _clock;
        }

        [TestCleanup]
        public void Cleanup()
        {
            Battlecries.Roll = _roll;
            Battlecries.Now = _now;
            Battlecries.Keep = _keep;
            Battlecries.Load(null);
        }

        #region What is sent, and to whom

        [TestMethod]
        public void TheNotificationIsThePackageTheCryAndASeed()
        {
            using var stream = new MemoryStream(MissionTestContext.Encode(new BattlecryNotificationPacket(12, 7, 4321)));
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(12, reader.ReadInt());
            Assert.AreEqual(7, reader.ReadInt());
            Assert.AreEqual(4321, reader.ReadInt());
        }

        [TestMethod]
        public void ThePackagesAreTheClientsTwentyOneAndTheirHundredAndThirtyCries()
        {
            Assert.HasCount(21, Battlecries.Packages);
            Assert.AreEqual(130, Battlecries.Packages.Values.Sum(types => types.Length));
            Assert.IsFalse(Battlecries.Packages.ContainsKey(1));

            // Eight voices with all eleven, three Thrax with the eight of a fight, and the odd ones.
            foreach (var voice in new[] { 8, 9, 16, 17, 18, 19, 21, 22 })
                CollectionAssert.AreEqual(Enumerable.Range(1, 11).ToArray(), Battlecries.Packages[voice]);

            foreach (var thrax in new[] { 11, 12, 13 })
                CollectionAssert.AreEqual(Enumerable.Range(1, 8).ToArray(), Battlecries.Packages[thrax]);

            CollectionAssert.AreEqual(new[] { 9, 10 }, Battlecries.Packages[DrillSergeant]);
            CollectionAssert.AreEqual(new[] { 1 }, Battlecries.Packages[Treelurker]);
            CollectionAssert.AreEqual(new[] { 1, 3, 4, 7 }, Battlecries.Packages[15]);

            Assert.IsTrue(Battlecries.Has(DrillSergeant, BattlecryType.StopPatrol));
            Assert.IsFalse(Battlecries.Has(DrillSergeant, BattlecryType.Aggro));
            Assert.IsFalse(Battlecries.Has(99, BattlecryType.Aggro));

            // Every cry is one of the eleven.
            Assert.IsTrue(Battlecries.Packages.Values.SelectMany(types => types).All(type => Enum.IsDefined((BattlecryType)type)));
        }

        [TestMethod]
        public void ACreaturesPackageIsItsOwnRowsAndElseItsClasssAndARowThatIsNoPackageIsPassedOver()
        {
            using var world = new WorldTestContext();
            var named = Spawn(world, x: 10);
            var other = Spawn(world, x: 12);

            named.DbId = 77;
            other.DbId = 78;

            Assert.AreEqual(2, Battlecries.Load(new[]
            {
                Row(CreatureBattlecryEntry.ScopeClass, FixtureClass, ThraxSoldier),
                Row(CreatureBattlecryEntry.ScopeCreature, 77, HumanMale),
                Row(CreatureBattlecryEntry.ScopeClass, 4242, 1),        // the client has no package 1
                Row(CreatureBattlecryEntry.ScopeCreature, 78, 999),
                Row(3, 78, HumanMale)                                   // no such scope
            }));

            Assert.AreEqual(HumanMale, Battlecries.PackageOf(named));
            Assert.AreEqual(ThraxSoldier, Battlecries.PackageOf(other));
            Assert.AreEqual(0, Battlecries.PackageOf(null));

            // Loaded again, a creature already thought about takes what it is given now.
            var watcher = Watch(world, x: 14);
            Think(world, 1);
            Battlecries.Load(new[] { Row(CreatureBattlecryEntry.ScopeCreature, 78, Treelurker) });

            Assert.AreEqual(0, Battlecries.PackageOf(named));
            Fight(named, watcher);
            Fight(other, watcher);
            Think(world, 2);

            var cries = Cries(watcher);
            Assert.HasCount(1, cries);
            Assert.AreEqual((other.EntityId, Treelurker, (int)BattlecryType.Aggro), (cries[0].EntityId, cries[0].Packet.PackageId, cries[0].Packet.TypeId));
        }

        [TestMethod]
        public void ACreatureWithNoPackageIsSilent()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var mute = Spawn(world, x: 10);

            Think(world, 1);
            Fight(mute, watcher);
            Think(world, 1);
            Hurt(mute, 90);
            Think(world, 1);
            Battlecries.Crit(world.Map, mute);
            Battlecries.KilledTarget(world.Map, mute);
            BehaviorManager.Instance.StopFighting(mute);
            Think(world, 1);

            Assert.IsEmpty(Cries(watcher));
        }

        #endregion

        #region Human voices

        [TestMethod]
        public void AnArmedHumanTakesOneOfItsSexsVoicesAtRandomAndKeepsItForItsLife()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var soldier = Armed(Spawn(world, x: 10));
            var other = Armed(Spawn(world, x: 11));
            var woman = Armed(Spawn(world, x: 12));
            woman.EntityClass = (EntityClasses)3981;       // Redshirt_Human_Swapset_Female

            _rolled = 0;
            Assert.AreEqual(Battlecries.MaleVoices[0], Battlecries.VoiceOf(soldier));
            _rolled = 2;
            Assert.AreEqual(Battlecries.MaleVoices[2], Battlecries.VoiceOf(other), "each spawn its own");
            _rolled = 1;
            Assert.AreEqual(Battlecries.FemaleVoices[1], Battlecries.VoiceOf(woman));

            // Rolled once: it stays the voice, a reload too, and nothing is kept for the row.
            _rolled = 2;
            Assert.AreEqual(Battlecries.MaleVoices[0], Battlecries.VoiceOf(soldier));
            Battlecries.Load(null);
            Assert.AreEqual(Battlecries.MaleVoices[0], Battlecries.VoiceOf(soldier));
            Assert.AreEqual(0, Battlecries.PackageOf(soldier), "not given in creature_battlecry");
            Assert.IsEmpty(_kept);

            // And it is the voice its fight is cried in.
            Think(world, 1);
            Fight(soldier, watcher);
            Think(world, 1);

            var aggro = Cries(watcher).Single(cry => cry.EntityId == soldier.EntityId).Packet;
            Assert.AreEqual((Battlecries.MaleVoices[0], (int)BattlecryType.Aggro), (aggro.PackageId, aggro.TypeId));
        }

        [TestMethod]
        public void AnUnarmedHumanEscortingNobodyAndAnyoneNotHumanAreSilent()
        {
            using var world = new WorldTestContext();
            var vendor = Spawn(world, x: 10);
            var corman = Armed(Spawn(world, x: 11));
            var child = Armed(Spawn(world, x: 12));
            var thrax = Armed(Spawn(world, x: 13));

            corman.EntityClass = (EntityClasses)4380;      // Redshirt_Corman_Male
            child.EntityClass = (EntityClasses)28698;      // Redshirt_Human_Male_Child
            thrax.EntityClass = (EntityClasses)3762;       // Bane_Thrax_Soldier_Pistol: no row loaded

            foreach (var silent in new[] { vendor, corman, child, thrax })
                Assert.AreEqual(0, Battlecries.VoiceOf(silent));

            // Armed later - a vendor turned soldier - it is looked at again and voiced.
            Armed(vendor);
            Assert.AreNotEqual(0, Battlecries.VoiceOf(vendor));
        }

        [TestMethod]
        public void AnEscortsVoiceIsKeptForItsRowAndEveryCopyOfItCriesInIt()
        {
            using var world = new WorldTestContext();
            var escort = Escorting(Spawn(world, x: 10), 501);
            var copy = Escorting(Spawn(world, x: 11), 501);
            var stranger = Escorting(Spawn(world, x: 12), 502);

            // Unarmed: an escort cries anyway.
            _rolled = 1;
            Assert.AreEqual(Battlecries.MaleVoices[1], Battlecries.VoiceOf(escort));
            CollectionAssert.AreEqual(new[] { (501u, Battlecries.MaleVoices[1]) }, _kept);
            Assert.AreEqual(Battlecries.MaleVoices[1], Battlecries.PackageOf(escort), "the row has it now");

            // Another copy of the row, for another player, rolls nothing: it has the row's.
            _rolled = 2;
            Assert.AreEqual(Battlecries.MaleVoices[1], Battlecries.VoiceOf(copy));
            Assert.AreEqual(Battlecries.MaleVoices[2], Battlecries.VoiceOf(stranger), "another row its own");
            Assert.HasCount(2, _kept);

            // Asked again, nothing more is kept; and a copy that is not escorting has the row's too.
            Battlecries.VoiceOf(escort);
            Assert.HasCount(2, _kept);
            var bystander = Spawn(world, x: 13);
            bystander.DbId = 501;
            Assert.AreEqual(Battlecries.MaleVoices[1], Battlecries.VoiceOf(bystander));

            // Once written it comes back with the rows (creature_battlecry, scope creature).
            Battlecries.Load(new[] { Row(CreatureBattlecryEntry.ScopeCreature, 501, Battlecries.MaleVoices[1]) });
            _rolled = 0;
            var respawned = Escorting(Spawn(world, x: 14), 501);
            Assert.AreEqual(Battlecries.MaleVoices[1], Battlecries.VoiceOf(respawned));
            Assert.HasCount(2, _kept);
        }

        [TestMethod]
        public void ASoldierMadeAnEscortKeepsTheVoiceItHadAndItIsKeptForItsRow()
        {
            using var world = new WorldTestContext();
            var soldier = Armed(Spawn(world, x: 10));
            soldier.DbId = 601;

            _rolled = 2;
            Assert.AreEqual(Battlecries.MaleVoices[2], Battlecries.VoiceOf(soldier));
            Assert.IsEmpty(_kept);

            _rolled = 0;
            Escorting(soldier, 601);
            Assert.AreEqual(Battlecries.MaleVoices[2], Battlecries.VoiceOf(soldier));
            CollectionAssert.AreEqual(new[] { (601u, Battlecries.MaleVoices[2]) }, _kept);
        }

        [TestMethod]
        public void AnEscortWithNoRowHasItsVoiceForItsLifeAndOneGivenInTheTableComesFirst()
        {
            using var world = new WorldTestContext();
            var unlisted = Escorting(Spawn(world, x: 10), 0);
            var given = Escorting(Armed(Spawn(world, x: 11)), 701);

            _rolled = 1;
            Assert.AreEqual(Battlecries.MaleVoices[1], Battlecries.VoiceOf(unlisted));
            Assert.IsEmpty(_kept, "no row to keep it for");

            Battlecries.Load(new[] { Row(CreatureBattlecryEntry.ScopeClass, FixtureClass, ThraxSoldier) });
            Assert.AreEqual(ThraxSoldier, Battlecries.VoiceOf(given), "its class's");
            Assert.AreEqual(ThraxSoldier, Battlecries.VoiceOf(unlisted), "its class's");
            Assert.IsEmpty(_kept);
        }

        [TestMethod]
        public void TheHumanClassesAreTheWorldsAdultHumanBodiesOfTheSexTheirVoicesAre()
        {
            CollectionAssert.AreEqual(new[] { 8, 16, 17 }, Battlecries.MaleVoices);
            CollectionAssert.AreEqual(new[] { 9, 18, 19 }, Battlecries.FemaleVoices);
            Assert.HasCount(53, Battlecries.HumanClasses);

            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using var world = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database);
                var names = world.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.ClassName);

                foreach (var (classId, voices) in Battlecries.HumanClasses)
                {
                    var name = names[classId];
                    var female = name.Contains("Female");

                    StringAssert.Contains(name, "Human");
                    Assert.IsTrue(female || name.Contains("Male"), name);
                    Assert.IsFalse(name.Contains("Child") || name.StartsWith("Z_"), name);
                    Assert.AreSame(female ? Battlecries.FemaleVoices : Battlecries.MaleVoices, voices, name);
                }

                // Every adult human body with a sex in its name is listed.
                var missing = names.Where(entry => entry.Value.Contains("Human") && System.Text.RegularExpressions.Regex.IsMatch(entry.Value, "(Male|Female)(_|$)")
                        && !entry.Value.Contains("Child") && !entry.Value.StartsWith("Z_")
                        && !Battlecries.HumanClasses.ContainsKey(entry.Key))
                    .Select(entry => entry.Value).ToList();
                Assert.IsEmpty(missing, string.Join(", ", missing));

                // A kept voice is a row of the creature's own, written over if it is kept again.
                var repository = new Rasa.Repositories.World.CreatureRepository(world);
                repository.SaveCreatureBattlecry(9001, 17);
                repository.SaveCreatureBattlecry(9001, 16);
                var kept = repository.GetBattlecries().Single(row => row.TargetId == 9001);
                Assert.AreEqual((CreatureBattlecryEntry.ScopeCreature, 16u), (kept.Scope, kept.PackageId));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        private static Creature Armed(Creature creature)
        {
            creature.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponAttack, ActionArgId = 1, RangeMin = 0, RangeMax = 30, MinDamage = 1, MaxDamage = 1, Cooldown = 1000 });
            return creature;
        }

        private static Creature Escorting(Creature creature, uint dbId)
        {
            creature.DbId = dbId;
            creature.SpawnPool = new SpawnPool { SpawnSlot = new List<SpawnPoolSlot>(), FollowOwnerCharacterId = 42 };
            return creature;
        }

        #endregion

        #region A fight

        [TestMethod]
        public void AFightItPicksIsAnAggroAndOneThatComesToItAnEnterCombat()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var picks = Spawn(world, x: 10);
            var struck = Spawn(world, x: 12);

            Give(FixtureClass, ThraxSoldier);
            _rolled = 17;
            Think(world, 1);

            Fight(picks, watcher);
            Hurt(struck, 5);
            Fight(struck, watcher);
            Think(world, 1);

            var cries = Cries(watcher);
            Assert.HasCount(2, cries);

            var aggro = cries.Single(cry => cry.EntityId == picks.EntityId).Packet;
            Assert.AreEqual((ThraxSoldier, (int)BattlecryType.Aggro, 17), (aggro.PackageId, aggro.TypeId, aggro.Seed));

            var entered = cries.Single(cry => cry.EntityId == struck.EntityId).Packet;
            Assert.AreEqual((ThraxSoldier, (int)BattlecryType.EnterCombat), (entered.PackageId, entered.TypeId));

            // Said once: the fight going on is not another.
            _clock += 60_000;
            _rolled = 99;
            Think(world, 3);
            Assert.IsEmpty(Cries(watcher));
        }

        [TestMethod]
        public void APackageWithOneOfTheTwoCriesItWhicheverWayTheFightBegan()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var lurker = Spawn(world, x: 10);

            Give(FixtureClass, Treelurker);
            Think(world, 1);

            Hurt(lurker, 5);
            Fight(lurker, watcher);
            Think(world, 1);

            Assert.AreEqual((int)BattlecryType.Aggro, Cries(watcher).Single().Packet.TypeId, "its alert is all it has");
        }

        [TestMethod]
        public void HurtInAFightItCriesNowAndThenAndOnceEachForHelpAndCloseToDeath()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var soldier = Spawn(world, x: 10);

            Give(FixtureClass, ThraxSoldier);
            Think(world, 1);
            Fight(soldier, watcher);
            Think(world, 1);
            Cries(watcher);

            // Hurt, the roll over the chance: nothing.
            Later();
            _rolled = Battlecries.HitChancePercent;
            Hurt(soldier, 10);
            Think(world, 1);
            Assert.IsEmpty(Cries(watcher));

            // Hurt, the roll under it.
            _rolled = Battlecries.HitChancePercent - 1;
            Hurt(soldier, 10);
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.ReceivedDamage, Cries(watcher).Single().Packet.TypeId);

            // Not hurt: nothing, whatever the roll.
            Later();
            Think(world, 2);
            Assert.IsEmpty(Cries(watcher));

            // At half: for help, once.
            Hurt(soldier, 30);
            Assert.AreEqual(50, soldier.Attributes[Attributes.Health].Current);
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.Help, Cries(watcher).Single().Packet.TypeId);

            Later();
            _rolled = 99;
            Hurt(soldier, 5);
            Think(world, 1);
            Assert.IsEmpty(Cries(watcher), "help is called once a fight");

            // At a quarter: close to death, once.
            Hurt(soldier, 20);
            Assert.AreEqual(25, soldier.Attributes[Attributes.Health].Current);
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.CloseToDeath, Cries(watcher).Single().Packet.TypeId);

            Later();
            Hurt(soldier, 5);
            Think(world, 1);
            Assert.IsEmpty(Cries(watcher));

            // Another fight, and it is all to say again.
            BehaviorManager.Instance.StopFighting(soldier);
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.ExitCombat, Cries(watcher).Single().Packet.TypeId);

            Later();
            Fight(soldier, watcher);
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.Aggro, Cries(watcher).Single().Packet.TypeId);

            Later();
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.CloseToDeath, Cries(watcher).Single().Packet.TypeId);
        }

        [TestMethod]
        public void BroughtLowWhileItMayNotCryItCriesWhenItMay()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var soldier = Spawn(world, x: 10);

            Give(FixtureClass, ThraxSoldier);
            Think(world, 1);
            Fight(soldier, watcher);
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.Aggro, Cries(watcher).Single().Packet.TypeId);

            // Inside the gap of its aggro: held.
            _clock += Battlecries.MinGapMs - 1;
            Hurt(soldier, 80);
            Think(world, 1);
            Assert.IsEmpty(Cries(watcher));

            _clock += 1;
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.CloseToDeath, Cries(watcher).Single().Packet.TypeId);

            // And help, which it passed on the way down, after that.
            Later();
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.Help, Cries(watcher).Single().Packet.TypeId);
        }

        [TestMethod]
        public void ACriticalHitAndAKillAreCried()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var soldier = Spawn(world, x: 10);

            Give(FixtureClass, ThraxSoldier);

            CritEffects.OnCritical(world.Map, soldier, watcher.Player, DamageType.Physical, 10);
            Assert.AreEqual((int)BattlecryType.ReceivedCriticalDamage, Cries(watcher).Single().Packet.TypeId);

            // Its next inside the gap is not heard; after it, it is.
            Battlecries.KilledTarget(world.Map, soldier);
            Assert.IsEmpty(Cries(watcher));

            Later();
            Battlecries.KilledTarget(world.Map, soldier);
            Assert.AreEqual((int)BattlecryType.KilledTarget, Cries(watcher).Single().Packet.TypeId);

            // The dead say nothing.
            Later();
            soldier.State = CharacterState.Dead;
            Battlecries.KilledTarget(world.Map, soldier);
            CritEffects.OnCritical(world.Map, soldier, watcher.Player, DamageType.Physical, 10);
            Assert.IsEmpty(Cries(watcher));
        }

        [TestMethod]
        public void APlayerItKillsIsAKilledTarget()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 14);
            var victim = Watch(world, x: 11);
            var soldier = Spawn(world, x: 10);

            Give(FixtureClass, ThraxSoldier);

            victim.Player.Attributes[Attributes.Health].Current = 0;
            Assert.IsTrue(PlayerDeath.AtZero(world.Map, victim.Player, soldier));

            Assert.AreEqual(CharacterState.Dead, victim.Player.State);
            Assert.AreEqual((int)BattlecryType.KilledTarget, Cries(watcher).Single(cry => cry.EntityId == soldier.EntityId).Packet.TypeId);
        }

        #endregion

        #region A beat

        [TestMethod]
        public void OnItsBeatItCriesAsItHaltsAndAsItWalksOnAndTheGapDoesNotHoldThoseBack()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2);

            Give(FixtureClass, HumanMale);
            sentry.Patrol = new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(12, 0, 0), pauseMs: 1000),
                new PatrolStep(new Vector3(14, 0, 0), pauseMs: 1000)
            };

            // The clock stands still: every cry here is inside the gap of the one before.
            var heard = new List<(int Tick, int Type)>();

            for (var tick = 0; tick < 40; tick++)
            {
                Think(world, 1);
                heard.AddRange(Cries(watcher).Select(cry => (tick, cry.Packet.TypeId)));
            }

            // Two halts and two settings off on the way out; the corner at the first step is neither.
            CollectionAssert.AreEqual(
                new[] { (int)BattlecryType.StopPatrol, (int)BattlecryType.StartPatrol, (int)BattlecryType.StopPatrol, (int)BattlecryType.StartPatrol },
                heard.Take(4).Select(cry => cry.Type).ToArray());

            // It stands its second of pause between a halt and the walking on.
            Assert.IsGreaterThanOrEqualTo(4, heard[1].Tick - heard[0].Tick);
        }

        [TestMethod]
        public void TakenOffItsBeatByAFightItCriesAsItTakesItUpAgainAndNotWhenItIsFirstPutOnIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2);

            Give(FixtureClass, HumanMale);
            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) };

            Think(world, 6);
            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, sentry.Controller.CurrentAction);
            Assert.IsEmpty(Cries(watcher), "put on its beat, it has nothing to resume");

            Fight(sentry, watcher);
            Think(world, 1);
            Assert.AreEqual((int)BattlecryType.Aggro, Cries(watcher).Single().Packet.TypeId);

            Later();
            BehaviorManager.Instance.StopFighting(sentry);
            sentry.TargetCategory = TargetCategory.Friendly;

            var heard = new List<int>();

            for (var tick = 0; tick < 6; tick++)
            {
                Think(world, 1);
                heard.AddRange(Cries(watcher).Select(cry => cry.Packet.TypeId));
            }

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, sentry.Controller.CurrentAction);
            CollectionAssert.AreEqual(new[] { (int)BattlecryType.ExitCombat, (int)BattlecryType.ResumePatrol }, heard.ToArray());
        }

        [TestMethod]
        public void TheTrainingOfficerCallsTheHaltAtHisTwoStopsAndTheOffAsHeLeavesThem()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true, worldMigration: BootcampAmbientGuardTests.Before);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            var officer = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.SpawnPool?.DbId == BootcampTrainingOfficer.PoolId);

            // The Bootcamp harness loads its own content: the rows as the seed has them.
            Battlecries.Load(CreatureBattlecryPreloader.Rows().Select(row => Row((uint)row[0], (uint)row[1], (int)(uint)row[2])));

            Assert.AreEqual(DrillSergeant, Battlecries.PackageOf(officer));

            harness.MovePlayerTo(officer);
            harness.Drain();
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();

            // One circuit: north with two stops of four seconds, and back south without one.
            var heard = new List<(int Tick, float X, int Type)>();

            for (var tick = 0; tick < 130; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, Tick);

                foreach (var cry in WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                             .Where(message => message.EntityId == officer.EntityId).Select(message => message.Packet).OfType<BattlecryNotificationPacket>())
                {
                    Assert.AreEqual(DrillSergeant, cry.PackageId);
                    heard.Add((tick, officer.Position.X, cry.TypeId));
                }
            }

            CollectionAssert.AreEqual(
                new[] { (int)BattlecryType.StopPatrol, (int)BattlecryType.StartPatrol, (int)BattlecryType.StopPatrol, (int)BattlecryType.StartPatrol },
                heard.Take(4).Select(cry => cry.Type).ToArray());

            // A cry is for what the think before left him doing, so the off finds him a think's
            // walk - a quarter of a metre - from the stop.
            Assert.AreEqual(380.4414f, heard[0].X, 0.001f, "the halt at the first stop");
            Assert.AreEqual(380.4414f - 0.25f, heard[1].X, 0.001f, "and the off from it");
            Assert.AreEqual(376.8125f, heard[2].X, 0.001f);
            Assert.AreEqual(376.8125f - 0.25f, heard[3].X, 0.001f);

            // Four seconds stood, at a quarter of a second a think.
            Assert.IsGreaterThanOrEqualTo(16, heard[1].Tick - heard[0].Tick);
        }

        #endregion

        #region The rows

        [TestMethod]
        public void TheSeedIsTheClassesNamedForTheirPackagesCreatureAndTheTrainingOfficer()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var rows = new Rasa.Repositories.World.CreatureRepository(context).GetBattlecries();
                    var classes = rows.Where(row => row.Scope == CreatureBattlecryEntry.ScopeClass).ToList();
                    var own = rows.Where(row => row.Scope == CreatureBattlecryEntry.ScopeCreature).ToList();

                    Assert.HasCount(59, rows);
                    Assert.HasCount(58, classes);

                    // Every package is one the client has, and every row is taken.
                    Assert.AreEqual(59, Battlecries.Load(rows));

                    // Each class is in the world's own table, and its name carries its package's creature.
                    var names = context.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.ClassName);
                    var creature = new Dictionary<uint, string[]>
                    {
                        [CreatureBattlecryPreloader.ThraxSoldier] = new[] { "Thrax_Soldier" },
                        [CreatureBattlecryPreloader.ThraxTechnician] = new[] { "Thrax_Technician" },
                        [CreatureBattlecryPreloader.Lightbender] = new[] { "Lightbender" },
                        [CreatureBattlecryPreloader.Linker] = new[] { "Linker" },
                        [CreatureBattlecryPreloader.Caretaker] = new[] { "Caretaker" },
                        [CreatureBattlecryPreloader.Treelurker] = new[] { "Treelurker" },
                        [CreatureBattlecryPreloader.Treemite] = new[] { "Treemite" },
                        [CreatureBattlecryPreloader.BrannFemale] = new[] { "Brann", "Female" },
                        [CreatureBattlecryPreloader.BrannMale] = new[] { "Brann", "_Male" }
                    };

                    foreach (var row in classes)
                    {
                        Assert.IsTrue(names.TryGetValue(row.TargetId, out var name), $"class {row.TargetId} is not an entity class");
                        Assert.IsTrue(creature[row.PackageId].All(part => name.Contains(part, StringComparison.Ordinal)), $"{name} has package {row.PackageId}");
                        Assert.IsFalse(name.Contains("Turret", StringComparison.Ordinal), name);
                    }

                    CollectionAssert.AreEquivalent(new uint[] { 3, 7, 10, 12, 13, 14, 15, 21, 22 }, classes.Select(row => row.PackageId).Distinct().ToArray());
                    Assert.AreEqual(17, classes.Count(row => row.PackageId == CreatureBattlecryPreloader.ThraxSoldier));

                    // The one creature row, and it is in the creature table.
                    Assert.AreEqual((BootcampTrainingOfficer.CreatureId, CreatureBattlecryPreloader.DrillSergeant), (own.Single().TargetId, own.Single().PackageId));
                    Assert.IsTrue(context.CreatureEntries.AsNoTracking().Any(entry => entry.Id == BootcampTrainingOfficer.CreatureId));
                }

                // Down takes the table away, and Up puts it back with its rows.
                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    context.GetService<IMigrator>().Migrate(Before);
                    Assert.AreEqual(0, Tables(context));

                    context.GetService<IMigrator>().Migrate(Migration);
                    Assert.AreEqual(1, Tables(context));
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    Assert.HasCount(59, new Rasa.Repositories.World.CreatureRepository(context).GetBattlecries());
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameTableAndRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE TABLE `creature_battlecry`");
            StringAssert.Contains(up, "`scope` int unsigned NOT NULL");
            StringAssert.Contains(up, "`target_id` int unsigned NOT NULL");
            StringAssert.Contains(up, "`package_id` int unsigned NOT NULL");
            StringAssert.Contains(up, "PRIMARY KEY (`scope`, `target_id`)");

            StringAssert.Contains(up, "(1, 3762, 12)");
            StringAssert.Contains(up, "(1, 7775, 21)");
            StringAssert.Contains(up, "(2, 400005, 4)");
            StringAssert.Contains(up, $"'{Migration}'");

            StringAssert.Contains(down, "DROP TABLE `creature_battlecry`");
        }

        #endregion

        #region Fixture

        private static int Tables(DbContext context) => context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = {0}", CreatureBattlecryEntry.TableName)
            .AsEnumerable().Single();

        private static CreatureBattlecryEntry Row(uint scope, uint target, int package) =>
            new CreatureBattlecryEntry { Scope = scope, TargetId = target, PackageId = (uint)package };

        private static void Give(uint classId, int package) =>
            Battlecries.Load(new[] { Row(CreatureBattlecryEntry.ScopeClass, classId, package) });

        /// <summary>Past the gap since its last cry.</summary>
        private void Later() => _clock += Battlecries.MinGapMs;

        private static void Think(WorldTestContext world, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);
        }

        private static void Fight(Creature creature, Client enemy) =>
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, enemy.Player.EntityId));

        private static void Hurt(Creature creature, int amount) =>
            creature.Attributes[Attributes.Health].Current -= amount;

        /// <summary>The battle cries the client has been sent since it was last asked, with whose they are.</summary>
        private static List<(ulong EntityId, BattlecryNotificationPacket Packet)> Cries(Client watcher) =>
            WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.Packet is BattlecryNotificationPacket)
                .Select(message => (message.EntityId, (BattlecryNotificationPacket)message.Packet)).ToList();

        /// <summary>A hostile creature of the fixture class with a hundred health, standing idle in the map's cells. It scans for nobody.</summary>
        private static Creature Spawn(WorldTestContext world, float x, float walkSpeed = 0)
        {
            var creature = new Creature
            {
                Name = "Crier",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                EntityClass = (EntityClasses)FixtureClass,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                WalkSpeed = walkSpeed,
                RunSpeed = walkSpeed * 2,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client with health, standing in the map's cells, so what is sent about what is near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }

        #endregion
    }
}
