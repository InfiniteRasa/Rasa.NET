using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// The pace a battlefield's spawn pools come back at (RespawnPace, appsettings.json's RespawnPace): by what they
    /// hold, never sooner than their row, only where it is asked for, and with the section off exactly as before.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class RespawnPaceTests
    {
        private const EntityClasses ThraxClass = (EntityClasses)9_960_001;
        private const EntityClasses BossClass = (EntityClasses)9_960_002;
        private const EntityClasses SoldierClass = (EntityClasses)9_960_003;
        private const EntityClasses VendorClass = (EntityClasses)9_960_004;
        private const EntityClasses SkeevClass = (EntityClasses)9_960_005;

        private const uint ThraxId = 4_196_000_001;
        private const uint BossId = 4_196_000_002;
        private const uint SoldierId = 4_196_000_003;
        private const uint VendorId = 4_196_000_004;
        private const uint FlaggedId = 4_196_000_005;
        private const uint LieutenantId = 4_196_000_006;
        private const uint SkeevId = 4_196_000_007;
        private const uint EggLayerId = 4_196_000_008;

        private readonly List<EntityClasses> _classes = new List<EntityClasses>();
        private readonly List<uint> _creatures = new List<uint>();
        private readonly List<uint> _loadedPools = new List<uint>();
        private RespawnPaceConfig _config;
        private System.Func<uint, bool> _staged;
        private System.Func<(HashSet<uint>, HashSet<uint>)> _targets;

        [TestInitialize]
        public void Start()
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());

            _config = RespawnPace.Config;
            _staged = SpawnPoolManager.Instance.IsStaged;
            _targets = RespawnPace.MissionTargets;

            RespawnPace.Config = new RespawnPaceConfig { Enabled = true };
            SpawnPoolManager.Instance.IsStaged = _ => false;
            RespawnPace.MissionTargets = () => (new HashSet<uint> { EggLayerId }, new HashSet<uint>());

            Class(ThraxClass, "Bane_Thrax_Soldier_Pistol");
            Class(BossClass, "Bane_Thrax_Soldier_Rifle_Boss");
            Class(SoldierClass, "Redshirt_Human_Soldier_Light_Male");
            Class(VendorClass, "Vendor_Human_Male");
            Class(SkeevClass, "NPC_Bane_Miniboss_Overseer_Skeev");

            Template(ThraxId, ThraxClass, TargetCategory.Hostile, armed: true);
            Template(BossId, BossClass, TargetCategory.Hostile, armed: true);
            Template(SoldierId, SoldierClass, TargetCategory.Friendly, armed: true);
            Template(VendorId, VendorClass, TargetCategory.Friendly, armed: false).Npc = new Npc();
            Template(FlaggedId, ThraxClass, TargetCategory.Hostile, armed: true).ExtraFlags.Add(CreatureFlag.UiIndicatorBoss);
            Template(LieutenantId, ThraxClass, TargetCategory.Hostile, armed: true).ExtraFlags.Add(CreatureFlag.Powerlevel4Lieutenant);
            Template(SkeevId, SkeevClass, TargetCategory.Hostile, armed: true);
            Template(EggLayerId, BossClass, TargetCategory.Hostile, armed: true).ExtraFlags.Add(CreatureFlag.UiIndicatorBoss);

            RespawnPace.Forget();
        }

        [TestCleanup]
        public void Restore()
        {
            RespawnPace.Config = _config;
            SpawnPoolManager.Instance.IsStaged = _staged;
            RespawnPace.MissionTargets = _targets;

            foreach (var id in _loadedPools)
                SpawnPoolManager.Instance.LoadedSpawnPools.Remove(id);

            foreach (var id in _classes)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(id);

            foreach (var id in _creatures)
                CreatureManager.Instance.LoadedCreatures.Remove(id);

            RespawnPace.Forget();
        }

        // ---- kinds

        [TestMethod]
        public void KindsComeFromTheFlagsTheClassAndHowManyPoolsShareABossRow()
        {
            using var world = new WorldTestContext();
            var creatures = CreatureManager.Instance.LoadedCreatures;

            Load(NewPool(world, 9_600_001, (BossId, 1, 1)));
            Load(NewPool(world, 9_600_002, (FlaggedId, 1, 1)));

            Assert.AreEqual(RespawnPace.Kind.Trash, RespawnPace.KindOf(creatures[ThraxId], 1220), "a Thrax trooper");
            Assert.AreEqual(RespawnPace.Kind.Trash, RespawnPace.KindOf(creatures[SoldierId], 1220), "an armed AFS soldier at a post");
            Assert.AreEqual(RespawnPace.Kind.Service, RespawnPace.KindOf(creatures[VendorId], 1220), "a vendor");
            Assert.AreEqual(RespawnPace.Kind.Boss, RespawnPace.KindOf(creatures[FlaggedId], 1220), "a boss by its flags, in one pool of its map");
            Assert.AreEqual(RespawnPace.Kind.Boss, RespawnPace.KindOf(creatures[BossId], 1220), "a boss by its class's name");
            Assert.AreEqual(RespawnPace.Kind.Boss, RespawnPace.KindOf(creatures[SkeevId], 1220), "a Miniboss, Overseer Skeev");
            Assert.AreEqual(RespawnPace.Kind.Elite, RespawnPace.KindOf(creatures[LieutenantId], 1220), "a lieutenant");
            Assert.AreEqual(RespawnPace.Kind.Mission, RespawnPace.KindOf(creatures[EggLayerId], 1220), "what a mission asks to be killed, boss or not");

            Load(NewPool(world, 9_600_003, (ThraxId, 3, 4), (BossId, 1, 1)));
            RespawnPace.Forget();

            Assert.AreEqual(RespawnPace.Kind.Elite, RespawnPace.KindOf(creatures[BossId], 1220), "the same row in two pools of its map: a camp's commander");
            Assert.AreEqual(RespawnPace.Kind.Boss, RespawnPace.KindOf(creatures[BossId], 1148), "on another map it stands in none");
            Assert.AreEqual(RespawnPace.Kind.Elite, RespawnPace.KindOf(NewPool(world, 9_600_004, (ThraxId, 3, 4), (BossId, 1, 1))), "a pool is of its strongest");
            Assert.AreEqual(RespawnPace.Kind.Mission, RespawnPace.KindOf(NewPool(world, 9_600_005, (EggLayerId, 1, 1), (FlaggedId, 1, 1))), "a mission's target before a boss");
            Assert.AreEqual(RespawnPace.Kind.Trash, RespawnPace.KindOf(NewPool(world, 9_600_006, (BossId, 0, 0))), "a slot of none holds nothing");
        }

        [TestMethod]
        public void TheMissionsTargetsAreTheCreaturesAndClassesTheirKillObjectivesName()
        {
            using (var context = MissionTestContext.WithProgressMission(
                       MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 630040)))
            {
                var (creatures, classes) = RespawnPace.TargetsOf(context.Manager.LoadedMissions.Values);

                CollectionAssert.AreEquivalent(new[] { 630040u }, creatures.ToArray());
                Assert.IsEmpty(classes);
            }

            using (var byClass = MissionTestContext.WithProgressMission(
                       MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureClassKilled, 10240)))
                Assert.IsTrue(RespawnPace.TargetsOf(byClass.Manager.LoadedMissions.Values).Classes.Contains(10240));
        }

        // ---- how long a cleared pool stays down

        [TestMethod]
        public void AClearedPoolStaysDownItsKindsTimeOrItsRowsWhenThatIsLonger()
        {
            using var world = new WorldTestContext();
            var pool = NewPool(world, 9_600_010, (ThraxId, 1, 1));
            pool.RespawnTime = 2000;

            Assert.AreEqual(73_000L, RespawnPace.Down(pool, 0.5), "the rank and file: 60-90 s, 75 s at the middle roll, less the row's 2 s");
            Assert.AreEqual(75_000L, pool.PaceDownMs);
            Assert.AreEqual(RespawnPace.Kind.Trash, pool.PaceKind);
            Assert.AreEqual(58_000L, RespawnPace.Down(pool, 0.0));
            Assert.AreEqual(88_000L, RespawnPace.Down(pool, 1.0));

            pool.RespawnTime = 120_000;
            Assert.AreEqual(0L, RespawnPace.Down(pool, 1.0), "a row of two minutes keeps its own time");
            Assert.AreEqual(120_000L, pool.PaceDownMs);

            pool.SpawnSlot[0] = new SpawnPoolSlot(BossId, 1, 1);
            pool.RespawnTime = 50_000;
            Load(pool);
            RespawnPace.Forget();
            Assert.AreEqual(300_000L - 50_000, RespawnPace.Down(pool, 0.0), "a named boss: 5-10 minutes, not its row's 50 s");
            Assert.AreEqual(600_000L - 50_000, RespawnPace.Down(pool, 1.0));

            pool.SpawnSlot[0] = new SpawnPoolSlot(EggLayerId, 1, 1);
            pool.RespawnTime = 2000;
            Assert.AreEqual(60_000L - 2000, RespawnPace.Down(pool, 0.0), "the egg-layers a mission asks for: 60-120 s");

            pool.SpawnSlot[0] = new SpawnPoolSlot(VendorId, 1, 1);
            Assert.AreEqual(28_000L, RespawnPace.Down(pool, 0.7), "a vendor: half a minute");
        }

        [TestMethod]
        public void TheSpawnPoolsTimerRunsTheWholePaceDown()
        {
            using var world = new WorldTestContext();
            var manager = new SpawnPoolManager(null);
            var pool = NewPool(world, 9_600_020, (ThraxId, 0, 0));   // a slot of none: the timer alone, nothing set down
            pool.RespawnTime = 2000;
            world.Map.SpawnPools.Add(pool);

            manager.IncreaseAliveCreatureCount(pool);
            manager.DecreaseAliveCreatureCount(world.Map, pool);

            Assert.IsTrue(pool.PaceDownMs >= 60_000 && pool.PaceDownMs <= 90_000, $"down {pool.PaceDownMs} ms");
            Assert.AreEqual(pool.RespawnTime - pool.PaceDownMs, pool.UpdateTimer, "the timer starts below zero by the difference");

            RunFor(manager, world.Map, pool.PaceDownMs - 1);
            Assert.AreEqual(pool.RespawnTime - 1, pool.UpdateTimer, "a millisecond short");
            manager.SpawnPoolWorker(world.Map, 1);
            Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer, "due");
        }

        [TestMethod]
        public void WhatPutsAPoolBackAtOnceStillDoes()
        {
            using var world = new WorldTestContext();
            var manager = new SpawnPoolManager(null);
            var pool = NewPool(world, 9_600_030, (ThraxId, 0, 0));
            pool.RespawnTime = 2000;
            world.Map.SpawnPools.Add(pool);

            manager.IncreaseAliveCreatureCount(pool);
            manager.DecreaseAliveCreatureCount(world.Map, pool);
            Assert.IsTrue(pool.UpdateTimer < 0, "down for the pace");

            // A GM's .respawn <pool> now, a control point's garrison let back, a public encounter's reset.
            pool.UpdateTimer = pool.RespawnTime;
            manager.SpawnPoolWorker(world.Map, 100);
            Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer, "due at once");
        }

        // ---- where it is kept

        [TestMethod]
        public void OnlyABattlefieldsOwnPoolsOnTheirOwnTimersKeepThePace()
        {
            using var world = new WorldTestContext();
            var pool = NewPool(world, 9_600_040, (ThraxId, 1, 1));
            pool.RespawnTime = 2000;

            Assert.IsTrue(RespawnPace.Applies(pool), "a camp of the Wilderness");

            pool.ScenarioKey = "scene";
            Assert.IsFalse(RespawnPace.Applies(pool), "a mission's");
            pool.ScenarioKey = null;

            pool.SceneRunId = "run";
            Assert.IsFalse(RespawnPace.Applies(pool), "a scene's");
            pool.SceneRunId = null;

            pool.SpawnPolicy = MissionSpawnGroupPolicy.ScenarioControlled;
            Assert.IsFalse(RespawnPace.Applies(pool), "a scenario's");
            pool.SpawnPolicy = default;

            pool.FollowOwnerCharacterId = 7;
            Assert.IsFalse(RespawnPace.Applies(pool), "an escort");
            pool.FollowOwnerCharacterId = 0;

            SpawnPoolManager.Instance.IsStaged = id => id == pool.DbId;
            Assert.IsFalse(RespawnPace.Applies(pool), "a public encounter's actor");
            SpawnPoolManager.Instance.IsStaged = _ => false;

            pool.Mode = SpawnPoolManager.ModeScripted;
            Assert.IsFalse(RespawnPace.Applies(pool), "scripted");
            pool.Mode = SpawnPoolManager.ModeControlPoint;
            pool.IsGarrison = true;
            Assert.IsFalse(RespawnPace.Applies(pool), "a control point's garrison comes back together");
            pool.Mode = SpawnPoolManager.ModeAutomatic;
            pool.IsGarrison = false;

            world.Map.IsPrivateInstance = true;
            Assert.IsFalse(RespawnPace.Applies(pool), "a player's private copy of the map");
            world.Map.IsPrivateInstance = false;

            world.Map.IsSquadInstance = true;
            Assert.IsFalse(RespawnPace.Applies(pool), "a squad's instance has its own clock");
            world.Map.IsSquadInstance = false;

            world.Map.IsSharedInstance = true;
            Assert.IsTrue(RespawnPace.Applies(pool), "a shared copy of a battlefield is the battlefield");
            world.Map.IsSharedInstance = false;

            var bootcamp = new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 0, 0), ClientList = new List<Client>() };
            pool.RuntimeMapChannel = bootcamp;
            Assert.IsFalse(RespawnPace.Applies(pool), "Bootcamp's battles come back at once");

            RespawnPace.Config.Maps = new List<uint> { 1985 };
            Assert.IsTrue(RespawnPace.Applies(pool), "the section names the maps");
            pool.RuntimeMapChannel = world.Map;
            Assert.IsFalse(RespawnPace.Applies(pool), "and then only those");
            RespawnPace.Config.Maps = new List<uint>();

            pool.RuntimeMapChannel = null;
            Assert.IsFalse(RespawnPace.Applies(pool), "a template on no map");
            Assert.AreEqual(0L, RespawnPace.Down(pool, 1.0));
            Assert.AreEqual(0L, pool.PaceDownMs);
            pool.RuntimeMapChannel = world.Map;

            RespawnPace.Config = new RespawnPaceConfig();
            Assert.IsFalse(RespawnPace.Applies(pool), "without the section");
            Assert.AreEqual(0L, RespawnPace.Down(pool, 1.0));
        }

        [TestMethod]
        public void WithTheSectionOffAPoolComesBackOnItsRowAsBefore()
        {
            using var world = new WorldTestContext();
            var manager = new SpawnPoolManager(null);
            var pool = NewPool(world, 9_600_050, (BossId, 0, 0));
            pool.RespawnTime = 2000;
            world.Map.SpawnPools.Add(pool);
            RespawnPace.Config = new RespawnPaceConfig();

            manager.IncreaseAliveCreatureCount(pool);
            manager.DecreaseAliveCreatureCount(world.Map, pool);
            Assert.AreEqual(0L, pool.UpdateTimer, "the timer starts at zero, as it always did");

            manager.SpawnPoolWorker(world.Map, 2000);
            Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer, "due after its row's two seconds");

            // A loss among survivors starts the top-up's time at zero, as before.
            manager.IncreaseAliveCreatureCount(pool);
            manager.IncreaseAliveCreatureCount(pool);
            manager.DecreaseAliveCreatureCount(world.Map, pool, CreatureManager.Instance.LoadedCreatures[BossId]);
            Assert.AreEqual(0L, pool.UpdateTimer);
        }

        // ---- a pool short of some

        [TestMethod]
        public void WhatIsKilledAmongSurvivorsComesBackNoSoonerThanItsPace()
        {
            using var world = new WorldTestContext();
            var manager = new SpawnPoolManager(null);
            var creatures = CreatureManager.Instance.LoadedCreatures;
            var pool = NewPool(world, 9_600_060, (ThraxId, 3, 3), (LieutenantId, 1, 1));
            pool.RespawnTime = 2000;
            Load(pool);

            for (var i = 0; i < 4; i++)
                manager.IncreaseAliveCreatureCount(pool);

            pool.UpdateTimer = pool.RespawnTime;

            RespawnPace.Lost(pool, null, 0.5);
            Assert.AreEqual(pool.RespawnTime, pool.UpdateTimer, "no creature named: nothing");

            // The first loss: the top-up's time starts (SpawnPoolManager), then waits the grunt's pace.
            manager.DecreaseAliveCreatureCount(world.Map, pool, creatures[ThraxId]);
            Assert.IsTrue(pool.UpdateTimer <= pool.RespawnTime - 60_000 && pool.UpdateTimer >= pool.RespawnTime - 90_000, $"timer {pool.UpdateTimer}");

            // Its lieutenant falls a little later: what was lost comes back no sooner than an elite's time from now.
            pool.UpdateTimer += 10_000;
            RespawnPace.Lost(pool, creatures[LieutenantId], 0.0);
            Assert.AreEqual(pool.RespawnTime - 180_000, pool.UpdateTimer);

            // A second grunt then: the elite's wait is longer and stands.
            RespawnPace.Lost(pool, creatures[ThraxId], 1.0);
            Assert.AreEqual(pool.RespawnTime - 180_000, pool.UpdateTimer);

            // A row longer than the pace: nothing changes.
            pool.RespawnTime = 900_000;
            pool.UpdateTimer = 100;
            RespawnPace.Lost(pool, creatures[LieutenantId], 1.0);
            Assert.AreEqual(100L, pool.UpdateTimer);
        }

        // ---- a game master

        [TestMethod]
        public void AGameMasterSeesWhatAPoolIsAndWhenItComesBack()
        {
            using var world = new WorldTestContext();
            var pool = NewPool(world, 9_600_070, (ThraxId, 2, 3));
            pool.RespawnTime = 2000;
            world.Map.SpawnPools.Add(pool);

            RespawnPace.Down(pool, 0.5);
            pool.UpdateTimer = pool.RespawnTime - pool.PaceDownMs + 15_000;

            var text = string.Join("\n", RespawnPace.Describe(pool));

            StringAssert.Contains(text, "pool 9600070 (Trash, 3 at most): its row 2 s, the pace 60-90 s");
            StringAssert.Contains(text, "down: back in 60 s (down 75 s this time, as Trash)");

            RespawnPace.Config = new RespawnPaceConfig();
            StringAssert.Contains(string.Join("\n", RespawnPace.Describe(pool)), "keeps no pace here");

            var player = world.CreateClient().Player;

            Assert.AreSame(pool, GmRespawnCommands.PoolFor(world.Map, player, new[] { ".respawn", "9600070" }, out _));
            Assert.IsNull(GmRespawnCommands.PoolFor(world.Map, player, new[] { ".respawn", "1" }, out var problem));
            Assert.AreEqual("No spawn pool 1 on this map.", problem);
            Assert.IsNull(GmRespawnCommands.PoolFor(world.Map, player, new[] { ".respawn" }, out problem), "nothing selected");
            StringAssert.Contains(problem, "Select a creature");
        }

        #region Fixtures

        /// <summary>Runs the pool worker for so long, a second a pass, as the server's CheckForCreatures does.</summary>
        private static void RunFor(SpawnPoolManager manager, MapChannel map, long ms)
        {
            for (; ms > 0; ms -= 1000)
                manager.SpawnPoolWorker(map, System.Math.Min(1000, ms));
        }

        private void Class(EntityClasses id, string name)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (classes.ContainsKey(id))
                return;

            classes.Add(id, new EntityClass((uint)id, name, 0, 0, new List<AugmentationType> { AugmentationType.Creature }, true));
            _classes.Add(id);
        }

        private Creature Template(uint id, EntityClasses classId, TargetCategory side, bool armed)
        {
            var template = new Creature { DbId = id, EntityClass = classId, TargetCategory = side };

            if (armed)
                template.Actions.Add(new CreatureAction { RangeMax = 20, MinDamage = 1, MaxDamage = 2, Cooldown = 1000 });

            CreatureManager.Instance.LoadedCreatures[id] = template;
            _creatures.Add(id);

            return template;
        }

        private void Load(SpawnPool pool)
        {
            SpawnPoolManager.Instance.LoadedSpawnPools[pool.DbId] = pool;
            _loadedPools.Add(pool.DbId);
        }

        private static SpawnPool NewPool(WorldTestContext world, uint id, params (uint Id, short Min, short Max)[] slots) => new SpawnPool
        {
            DbId = id,
            MapContextId = world.Map.MapInfo.MapContextId,
            RuntimeMapChannel = world.Map,
            Position = Vector3.Zero,
            RespawnTime = 90_000,
            SpawnSlot = slots.Select(s => new SpawnPoolSlot(s.Id, s.Min, s.Max)).ToList()
        };

        #endregion
    }
}
