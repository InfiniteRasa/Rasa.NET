using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    [TestClass]
    [DoNotParallelize]
    public class AlternateMeshTests
    {
        private const uint AfsLightTurret = 550013;     // "AFS Light Turret - Bootcamp", Emplacement_AFS_Turret_Mini (11302)
        private const uint BaneMortar = 590001;         // Emplacement_Bane_Turret_Standard (7482)
        private const uint Ravager = 590004;            // Vehicle_Bane_Ravager (30080)
        private const uint ThraxSoldier = 3;            // Bane_Thrax_Soldier (20757): no wreck
        private const long RespawnMs = 300_000;

        [TestMethod]
        public void TheClassesWithAWreckCarryTheirFlag()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var flags = harness.WorldContext.Set<CreatureClassFlagEntry>().AsNoTracking()
                .Where(row => row.FlagId == (uint)CreatureFlag.AltMesh || row.FlagId == (uint)CreatureFlag.AltMeshDelayed3500)
                .ToDictionary(row => row.ClassId, row => (CreatureFlag)row.FlagId);

            Assert.HasCount(AlternateMesh.Wrecks.Count, flags, "one flag for each class with a wreck");

            foreach (var classId in new uint[] { 4064, 11302, 23902, 7482, 10509 })
                Assert.AreEqual(CreatureFlag.AltMesh, flags[classId], $"turret {classId}: its wreck at once");

            foreach (var classId in new uint[] { 3902, 30080 })
                Assert.AreEqual(CreatureFlag.AltMeshDelayed3500, flags[classId], $"walker {classId}: once it has fallen");

            foreach (var classId in AlternateMesh.Wrecks.Keys)
                Assert.IsTrue(flags.ContainsKey((uint)classId), $"{classId} has a wreck and no flag");
        }

        [TestMethod]
        [DataRow(4064u, 3912u)]     // AFS Turret: ArchHumBaseTurretDestroyed
        [DataRow(11302u, 21883u)]   // AFS Light Turret: ArchHumanBaseMiniTurretDestroyed
        [DataRow(23902u, 25407u)]   // Brann Turret: PropBrannTurretV01Destroyed
        [DataRow(7482u, 7479u)]     // Bane Mortar: ArchBaneGenObjMortarlauncherV01Destroyed
        [DataRow(10509u, 7479u)]    // Bane Light Mortar: the same
        [DataRow(3902u, 4097u)]     // Predator: CreatureThraxPredatorDestroyed
        public void AWreckIsTheModelOfTheDestroyedProp(uint classId, uint destroyedPropClassId)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var prop = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == destroyedPropClassId);

            Assert.AreEqual((int)prop.MeshId, AlternateMesh.Wrecks[(EntityClasses)classId]);
        }

        [TestMethod]
        public void TheFourNewCreaturesHaveTheirRows()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var rows = harness.WorldContext.Set<CreatureEntry>().AsNoTracking()
                .Where(row => row.Id >= 590001 && row.Id <= 590004).OrderBy(row => row.Id).ToList();

            CollectionAssert.AreEqual(new uint[] { 7482, 10509, 23902, 30080 }, rows.Select(row => row.ClassId).ToArray());

            foreach (var row in rows)
            {
                Assert.IsNotNull(harness.WorldContext.Set<CreatureStatEntry>().AsNoTracking().SingleOrDefault(stat => stat.Id == row.Id), $"{row.Id}: stats");
                Assert.IsNotNull(harness.WorldContext.Set<CreatureActionEntry>().AsNoTracking().SingleOrDefault(action => action.Id == row.Action1), $"{row.Id}: its gun");
                Assert.AreEqual(1, harness.WorldContext.Set<CreatureAppearanceEntry>().AsNoTracking().Count(look => look.Id == row.Id && look.SlotId == 13), $"{row.Id}: its weapon");
                Assert.IsFalse(harness.WorldContext.Set<SpawnPoolEntry>().AsNoTracking().Any(pool => pool.Creature1Id == row.Id), $"{row.Id}: not placed");
            }
        }

        [TestMethod]
        public void ATurretIsGivenTheEffectUnannouncedAndFromItself()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (turret, _) = Spawn(harness, AfsLightTurret);

            var attached = harness.Drain().OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == AlternateMesh.EffectTypeId);

            Assert.IsFalse(attached.Announced, "announced on attaching, SwapMeshDeath's own announce - the explosion - would return at once");
            Assert.AreEqual(turret.EntityId, attached.SourceId, "a source the client does not hold forces the announce");
            Assert.AreEqual(AlternateMesh.EffectLevel, attached.EffectLevel, "the one level with FX");
            Assert.AreEqual(turret.AlternateMeshEffectId, attached.EffectId);
            Assert.AreNotEqual(0, attached.EffectId);
            // (origMeshId, altMeshId, collisionRole, alreadySwapped): prop_hum_turret_auto_mini and its _destroyed.
            CollectionAssert.AreEqual(new object[] { 29308, 33452, 2, false }, attached.Args);
        }

        [TestMethod]
        public void DestroyedItBlowsUpIntoItsWreckAndStaysOnItsMount()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (turret, pool) = Spawn(harness, AfsLightTurret);
            harness.Drain();

            Kill(harness, turret);

            var packets = harness.Drain();
            var swap = packets.OfType<GameEffectCallPacket>().Single();
            Assert.AreEqual("SwapMeshDeath", swap.MethodName);
            Assert.AreEqual(turret.AlternateMeshEffectId, swap.EffectId);
            Assert.IsTrue(turret.AlternateMeshSwapped);
            Assert.IsTrue(IndexOf<StateChangePacket>(packets) < IndexOf<GameEffectCallPacket>(packets), "dead first, then its wreck");

            // Long past an empty corpse's time, and it is still there.
            BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, LootDispenserManager.EmptyCorpseMs + 5000);

            Assert.IsTrue(AlternateMesh.KeepsWreck(turret));
            Assert.AreSame(turret, EntityManager.Instance.GetCreature(turret.EntityId));
            Assert.IsEmpty(harness.Drain().OfType<DestroyPhysicalEntityPacket>().ToArray());
            Assert.AreEqual(0, pool.AliveCreatures);
        }

        [TestMethod]
        public void ItsPoolPutsTheWreckBackInServiceInPlace()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (turret, pool) = Spawn(harness, AfsLightTurret);
            turret.Attributes[Attributes.Armor].Current = 0;
            Kill(harness, turret);
            harness.Drain();

            // Not before its respawn comes round.
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, RespawnMs - 1000);
            Assert.AreEqual(CharacterState.Dead, turret.State);

            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 1000);

            Assert.AreNotEqual(CharacterState.Dead, turret.State);
            Assert.IsFalse(turret.AlternateMeshSwapped);
            Assert.AreEqual(turret.Attributes[Attributes.Health].CurrentMax, turret.Attributes[Attributes.Health].Current);
            Assert.AreEqual(turret.Attributes[Attributes.Armor].CurrentMax, turret.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(1, pool.AliveCreatures);
            Assert.AreEqual(0, pool.DeadCreatures);
            Assert.IsFalse(AlternateMesh.KeepsWreck(turret));
            Assert.AreSame(turret, Creatures(harness, 11302).Single(), "the same turret, not a second one in its wreckage");

            var packets = harness.Drain();
            var swap = packets.OfType<GameEffectCallPacket>().Single();
            Assert.AreEqual("SwapMeshRevive", swap.MethodName);
            Assert.AreEqual(turret.AlternateMeshEffectId, swap.EffectId);
            Assert.IsTrue(IndexOf<GameEffectCallPacket>(packets) < IndexOf<RevivedPacket>(packets), "its own model back before Revived shows its weapon on it");
            Assert.IsEmpty(packets.OfType<CreatePhysicalEntityPacket>().ToArray());

            // And it can be destroyed again.
            Kill(harness, turret);
            Assert.AreEqual("SwapMeshDeath", harness.Drain().OfType<GameEffectCallPacket>().Single().MethodName);
            Assert.IsTrue(AlternateMesh.KeepsWreck(turret));
        }

        [TestMethod]
        public void SomeoneArrivingAfterwardsIsGivenTheWreck()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (turret, _) = Spawn(harness, AfsLightTurret);
            Kill(harness, turret);
            harness.Drain();

            CreatureManager.Instance.CreateCreatureOnClient(harness.Client, turret);

            var attached = harness.Drain().OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == AlternateMesh.EffectTypeId);
            Assert.IsFalse(attached.Announced);
            CollectionAssert.AreEqual(new object[] { 29308, 33452, 2, true }, attached.Args, "alreadySwapped: the wreck from the start, no explosion");
        }

        [TestMethod]
        public void AWreckTakenAwayLetsItsPoolSpawnANewTurret()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (turret, pool) = Spawn(harness, AfsLightTurret);
            Kill(harness, turret);

            CellManager.Instance.RemoveCreatureFromWorld(harness.BootcampMap, turret);
            Assert.IsFalse(AlternateMesh.KeepsWreck(turret));

            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, RespawnMs);

            var fresh = Creatures(harness, 11302).Single();
            Assert.AreNotSame(turret, fresh);
            Assert.AreNotEqual(CharacterState.Dead, fresh.State);
            Assert.IsFalse(fresh.AlternateMeshSwapped);
            Assert.AreEqual(1, pool.AliveCreatures);
        }

        [TestMethod]
        public void AWalkerIsItsWreckOnceItHasFallen()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (ravager, _) = Spawn(harness, Ravager);
            harness.Drain();

            var died = Environment.TickCount64;
            Kill(harness, ravager);

            Assert.IsEmpty(harness.Drain().OfType<GameEffectCallPacket>().ToArray(), "it falls first");
            Assert.IsFalse(ravager.AlternateMeshSwapped);
            Assert.IsFalse(AlternateMesh.KeepsWreck(ravager), "a walker's wreck is a corpse like any other");

            AlternateMesh.DeadTick(harness.BootcampMap, ravager, died + AlternateMesh.DelayMs - 100);
            Assert.IsEmpty(harness.Drain().OfType<GameEffectCallPacket>().ToArray());

            AlternateMesh.DeadTick(harness.BootcampMap, ravager, ravager.AlternateMeshSwapAt);
            Assert.AreEqual("SwapMeshDeath", harness.Drain().OfType<GameEffectCallPacket>().Single().MethodName);
            Assert.IsTrue(ravager.AlternateMeshSwapped);

            // Once.
            AlternateMesh.DeadTick(harness.BootcampMap, ravager, died + 60_000);
            Assert.IsEmpty(harness.Drain().OfType<GameEffectCallPacket>().ToArray());

            // And its corpse goes when its time is up.
            BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, LootDispenserManager.EmptyCorpseMs + 5000);
            Assert.IsEmpty(Creatures(harness, 30080).ToArray());
        }

        [TestMethod]
        public void AWalkerRevivedBeforeItFellNeverBecomesItsWreck()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (ravager, _) = Spawn(harness, Ravager);
            Kill(harness, ravager);
            harness.Drain();

            CreatureSupport.Revive(harness.BootcampMap, ravager, 500, null);
            AlternateMesh.DeadTick(harness.BootcampMap, ravager, Environment.TickCount64 + 60_000);

            Assert.IsEmpty(harness.Drain().OfType<GameEffectCallPacket>().ToArray());
            Assert.IsFalse(ravager.AlternateMeshSwapped);
            Assert.AreEqual(0L, ravager.AlternateMeshSwapAt);
        }

        [TestMethod]
        public void ARevivedBaneMortarIsItsOwnModelAgain()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (mortar, pool) = Spawn(harness, BaneMortar);
            var attached = harness.Drain().OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == AlternateMesh.EffectTypeId);
            // prop_bane_mortarlauncher_v01 and its _destroyed.
            CollectionAssert.AreEqual(new object[] { 19366, 19367, 2, false }, attached.Args);

            Kill(harness, mortar);
            Assert.IsTrue(AlternateMesh.KeepsWreck(mortar));
            harness.Drain();

            // A Technician's Jumpstart (CreatureSupport.Jumpstart ends in Revive).
            CreatureSupport.Revive(harness.BootcampMap, mortar, 500, null);

            var packets = harness.Drain();
            Assert.AreEqual("SwapMeshRevive", packets.OfType<GameEffectCallPacket>().Single().MethodName);
            Assert.IsTrue(IndexOf<GameEffectCallPacket>(packets) < IndexOf<RevivedPacket>(packets));
            Assert.IsFalse(mortar.AlternateMeshSwapped);
            Assert.IsFalse(AlternateMesh.KeepsWreck(mortar), "its pool has nothing to put back");
            Assert.AreEqual(500, mortar.Attributes[Attributes.Health].Current);
            Assert.AreEqual(1, pool.AliveCreatures);

            // The respawn coming round finds it alive and leaves it be.
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, RespawnMs);
            Assert.AreSame(mortar, Creatures(harness, 7482).Single());
        }

        [TestMethod]
        public void ACreatureWithNoWreckIsGivenNothing()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var (soldier, _) = Spawn(harness, ThraxSoldier);

            Assert.IsEmpty(harness.Drain().OfType<GameEffectAttachedPacket>().Where(packet => packet.EffectTypeId == AlternateMesh.EffectTypeId).ToArray());
            Assert.IsNull(AlternateMesh.AttachedPacket(soldier));

            Kill(harness, soldier);

            Assert.IsEmpty(harness.Drain().OfType<GameEffectCallPacket>().ToArray());
            Assert.IsFalse(AlternateMesh.KeepsWreck(soldier));
            Assert.AreEqual(0, soldier.AlternateMeshEffectId);
        }

        /// <summary>
        /// The creature of this row, spawned by a pool of its own beside the player as the world's
        /// are: the row and its class (with its flags) read from the migrated database.
        /// </summary>
        private static (Creature Creature, SpawnPool Pool) Spawn(BootcampRuntimeTestHarness.Harness harness, uint creatureId)
        {
            var entry = harness.WorldContext.Set<CreatureEntry>().AsNoTracking().Single(row => row.Id == creatureId);
            var classEntry = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == entry.ClassId);
            var entityClass = new EntityClass(classEntry.Id, classEntry.ClassName, classEntry.MeshId, classEntry.ClassCollisionRole,
                classEntry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), classEntry.TargetFlag != 0);

            entityClass.CreatureFlags.AddRange(harness.WorldContext.Set<CreatureClassFlagEntry>().AsNoTracking()
                .Where(row => row.ClassId == entry.ClassId).Select(row => (CreatureFlag)row.FlagId));
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)entry.ClassId] = entityClass;
            CreatureManager.Instance.LoadedCreatures[entry.Id] = new Creature(entry) { AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };

            var pool = new SpawnPool
            {
                DbId = 990000 + creatureId % 1000,
                Position = harness.Client.Player.Position + new Vector3(3, 0, 0),
                MapContextId = harness.BootcampMap.MapInfo.MapContextId,
                RuntimeMapChannel = harness.BootcampMap,
                Mode = SpawnPoolManager.ModeAutomatic,
                AnimType = 0,
                RespawnTime = RespawnMs,
                UpdateTimer = RespawnMs,
                SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(creatureId, 1, 1) }
            };

            harness.BootcampMap.SpawnPools.Clear();
            harness.BootcampMap.SpawnPools.Add(pool);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            return (Creatures(harness, entry.ClassId).Single(), pool);
        }

        private static void Kill(BootcampRuntimeTestHarness.Harness harness, Creature creature)
        {
            creature.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, creature, null);
        }

        private static IEnumerable<Creature> Creatures(BootcampRuntimeTestHarness.Harness harness, uint classId) =>
            harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Where(creature => creature.EntityClass == (EntityClasses)classId)
                .Distinct();

        private static int IndexOf<T>(IReadOnlyList<PythonPacket> packets) where T : PythonPacket
        {
            for (var i = 0; i < packets.Count; i++)
                if (packets[i] is T)
                    return i;

            return -1;
        }
    }
}
