using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Data;
    using Game.Missions.World;
    using Managers;
    using Rasa.Missions.Scenes;
    using Structures;

    /// <summary>
    /// SpawnPoolManager.PoolsTooClose, what ValidatePools reports at startup: hostile pools
    /// against safe ground and AFS turrets, with Bane emplacements and the pools a mission
    /// stages left where they belong.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SpawnPoolCheckTests
    {
        // A map and creature rows of this fixture's own, clear of the seeded ones.
        private const uint Map = 990777;
        private const uint OtherMap = 990778;

        private const uint Thrax = 9910001;
        private const uint BaneMortar = 9910002;
        private const uint AfsTurret = 9910003;
        private const uint Officer = 9910004;
        private const uint Overseer = 9910005;

        private static readonly (uint Map, Vector3 Position, string What)[] NoPlaces = Array.Empty<(uint, Vector3, string)>();

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
        }

        [TestInitialize]
        public void LoadCreatures()
        {
            var creatures = CreatureManager.Instance.LoadedCreatures;

            creatures[Thrax] = new Creature { DbId = Thrax, TargetCategory = TargetCategory.Hostile };
            // Emplacement_Bane_Turret_Standard and Emplacement_AFS_Turret_Standard.
            creatures[BaneMortar] = new Creature { DbId = BaneMortar, TargetCategory = TargetCategory.Hostile, EntityClass = (EntityClasses)7482 };
            creatures[AfsTurret] = new Creature { DbId = AfsTurret, TargetCategory = TargetCategory.Friendly, EntityClass = (EntityClasses)4064 };
            creatures[Officer] = new Creature { DbId = Officer, TargetCategory = TargetCategory.Friendly, Npc = new Npc() };
            // A Bane NPC: one that is talked to before it is fought.
            creatures[Overseer] = new Creature { DbId = Overseer, TargetCategory = TargetCategory.Hostile, Npc = new Npc() };
        }

        [TestCleanup]
        public void UnloadCreatures()
        {
            foreach (var id in new[] { Thrax, BaneMortar, AfsTurret, Officer, Overseer })
                CreatureManager.Instance.LoadedCreatures.Remove(id);
        }

        private static SpawnPoolManager Manager(params uint[] staged)
        {
            return new SpawnPoolManager(null) { IsStaged = id => staged.Contains(id) };
        }

        private static void Pool(SpawnPoolManager manager, uint id, uint creature, float x, float z, float radius = 0, uint map = Map, short mode = SpawnPoolManager.ModeAutomatic)
        {
            manager.LoadedSpawnPools.Add(id, new SpawnPool
            {
                DbId = id,
                MapContextId = map,
                Mode = mode,
                Position = new Vector3(x, 10, z),
                Radius = radius,
                SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(creature, 1, 2) }
            });
        }

        [TestMethod]
        public void ABaneEmplacementIsNoTurretToTheCampAroundItNorToItself()
        {
            var manager = Manager();

            // A Bane mortar on its mount and the squad that stands around it.
            Pool(manager, 1, BaneMortar, 100, 100);
            Pool(manager, 2, Thrax, 103, 100, radius: 20);

            var found = manager.PoolsTooClose(NoPlaces);

            Assert.AreEqual(0, found.Count, string.Join(" | ", found.Select(f => f.Message)));
        }

        [TestMethod]
        public void AnAfsTurretHasEveryHostilePoolInsideItsScanReported()
        {
            var manager = Manager();

            Pool(manager, 10, AfsTurret, 100, 100);
            Pool(manager, 11, Thrax, 110, 100);                     // 10 m off: inside the scan
            Pool(manager, 12, BaneMortar, 100, 112);                // a Bane gun is a hostile pool too
            Pool(manager, 13, Thrax, 140, 100, radius: 20);         // its edge 20 m off: outside
            Pool(manager, 14, Thrax, 100, 100, map: OtherMap);      // another map's ground
            Pool(manager, 15, Thrax, 100, 105, mode: SpawnPoolManager.ModeScripted);   // not on a timer

            var found = manager.PoolsTooClose(NoPlaces);

            Assert.AreEqual(2, found.Count, string.Join(" | ", found.Select(f => f.Message)));
            Assert.IsTrue(found.All(f => f.Map == Map));
            StringAssert.Contains(found.Single(f => f.Message.StartsWith("spawnpool 11:")).Message, "10 m from the turret of pool 10");
            StringAssert.Contains(found.Single(f => f.Message.StartsWith("spawnpool 12:")).Message, "12 m from the turret of pool 10");
        }

        [TestMethod]
        public void ACaptiveAMissionStagesInACampIsNotSafeGround()
        {
            // An officer held where the Bane camp is, and the camp.
            void Build(SpawnPoolManager manager)
            {
                Pool(manager, 20, Officer, 0, 0);
                Pool(manager, 21, Thrax, 5, 0, radius: 20);
            }

            var ambient = Manager();
            Build(ambient);
            var found = ambient.PoolsTooClose(NoPlaces);

            Assert.AreEqual(1, found.Count);
            StringAssert.Contains(found[0].Message, "spawnpool 21: its creatures can stand 0 m from the NPCs of pool 20, which should be safe ground.");

            var staged = Manager(20);
            Build(staged);
            found = staged.PoolsTooClose(NoPlaces);

            Assert.AreEqual(0, found.Count, string.Join(" | ", found.Select(f => f.Message)));
        }

        [TestMethod]
        public void AHostileActorAMissionStagesIsNotACamp()
        {
            // An overseer come to give an ultimatum, 4 m from the officer he gives it to; and
            // a camp that has no business there.
            void Build(SpawnPoolManager manager)
            {
                Pool(manager, 30, Officer, 200, 0);
                Pool(manager, 31, Overseer, 204, 0);
                Pool(manager, 32, Thrax, 200, 10);
            }

            var ambient = Manager();
            Build(ambient);
            var found = ambient.PoolsTooClose(NoPlaces);

            Assert.AreEqual(2, found.Count, string.Join(" | ", found.Select(f => f.Message)));

            var staged = Manager(31);
            Build(staged);
            found = staged.PoolsTooClose(NoPlaces);

            // The officer is safe ground still: the camp is reported, the overseer is not.
            Assert.AreEqual(1, found.Count, string.Join(" | ", found.Select(f => f.Message)));
            StringAssert.Contains(found[0].Message, "spawnpool 32: its creatures can stand 10 m from the NPCs of pool 30");
        }

        [TestMethod]
        public void AHospitalOrAWaypointPadIsSafeGroundOnItsOwnMap()
        {
            var manager = Manager();

            Pool(manager, 40, Thrax, 300, 300, radius: 10);
            Pool(manager, 41, Thrax, 300, 300, radius: 10, map: OtherMap);

            var found = manager.PoolsTooClose(new[] { (Map, new Vector3(312, 10, 300), "Hospital 7") });

            Assert.AreEqual(1, found.Count, string.Join(" | ", found.Select(f => f.Message)));
            Assert.AreEqual(Map, found[0].Map);
            StringAssert.Contains(found[0].Message, "spawnpool 40: its creatures can stand 2 m from Hospital 7, which should be safe ground.");
        }

        [TestMethod]
        public void APublicEncountersSpawnIsStaged()
        {
            var actors = new PublicActorLeaseService(null, () => null);

            Assert.IsFalse(actors.StagesSpawn(630070));

            actors.Bind(new PublicEncounterBinding(666, 630070, "pierre", "wilderness.escape-velocity"));

            Assert.IsTrue(actors.StagesSpawn(630070));
            Assert.IsFalse(actors.StagesSpawn(630071));
            Assert.IsFalse(actors.StagesSpawn(666));
        }
    }
}
