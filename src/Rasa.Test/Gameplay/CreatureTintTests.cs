using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    /// <summary>
    /// A creature's two body tints (BodyAttributes hue and hue2): chosen once when it spawns,
    /// and sent as they are to whoever is shown it, however often. They used to be rolled each
    /// time it was shown to a client, so two players saw two creatures, and one player a new
    /// one every time it came back into range. A FRIENDLY one - an AFS soldier, a vendor - has
    /// none, and keeps its model's stock colours.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureTintTests
    {
        private const uint ThraxSoldier = 3;            // Bane_Thrax_Soldier (20757)
        private const uint Infantryman = 400002;        // "Infantryman at a post, unarmed", Redshirt_Human_Soldier_Light_Male, FRIENDLY

        [TestMethod]
        public void ACreatureIsSentTheTintsItWasMadeWithEveryTimeItIsShown()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var soldier = Spawn(harness, ThraxSoldier);

            Assert.IsNotNull(soldier.Hue);
            Assert.IsNotNull(soldier.Hue2);

            harness.Drain();

            // Shown three times - to a client coming into range, going and coming back, or to
            // three clients: the same two colours each time.
            var shown = new List<BodyAttributesPacket>();

            for (var i = 0; i < 3; i++)
            {
                CreatureManager.Instance.CreateCreatureOnClient(harness.Client, soldier);
                shown.Add(harness.Drain().OfType<CreatePhysicalEntityPacket>()
                    .Single(created => created.EntityId == soldier.EntityId)
                    .EntityData.OfType<BodyAttributesPacket>().Single());
            }

            foreach (var body in shown)
            {
                Assert.AreSame(soldier.Hue, body.Hue);
                Assert.AreSame(soldier.Hue2, body.Hue2);
                Assert.AreEqual(BodyAttributesPacket.Ignore, body.IgnoreABVs);
                Assert.AreEqual(BodyAttributesPacket.Ignore, body.IgnoreWS);
            }

            Assert.AreEqual(shown[0].Hue.Hue, shown[2].Hue.Hue);
            Assert.AreEqual(shown[0].Hue2.Hue, shown[2].Hue2.Hue);
        }

        [TestMethod]
        public void EachCreatureRollsItsOwnAndARisenCorpseKeepsItsBodys()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var soldier = Spawn(harness, ThraxSoldier);
            var template = CreatureManager.Instance.LoadedCreatures[ThraxSoldier];

            // Made from the same row, and not given the row's colours or each other's.
            var another = CreatureManager.Instance.CreateCreature(ThraxSoldier, null);

            Assert.IsNotNull(another);
            Assert.AreNotSame(template.Hue, soldier.Hue);
            Assert.AreNotSame(template.Hue2, soldier.Hue2);
            Assert.AreNotSame(soldier.Hue, another.Hue);
            Assert.AreNotSame(soldier.Hue2, another.Hue2);
            Assert.IsNull(new Creature(soldier).Hue, "a copy is another creature, and has not spawned");
            Assert.IsNull(new Creature().Hue, "one made in code - a player's summon or pet - has none");

            // A corpse raised to fight for a player is the same body.
            soldier.Attributes[Attributes.Health].Current = 0;
            CreatureManager.Instance.HandleCreatureKill(harness.BootcampMap, soldier, null);

            var raised = (Creature)typeof(AbilityManager)
                .GetMethod("Raise", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Abilities(), new object[]
                {
                    harness.BootcampMap, harness.Client.Player, soldier,
                    new ActionLevelInfo { ActionId = ActionId.AaExobiologistReanimation, Level = 1, MaxRange = 40 }, false
                });

            Assert.IsNotNull(raised);
            Assert.AreNotSame(soldier, raised);
            Assert.AreSame(soldier.Hue, raised.Hue);
            Assert.AreSame(soldier.Hue2, raised.Hue2);
        }

        [TestMethod]
        public void AFriendlyCreatureHasNoTintAndKeepsItsStockColours()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var guard = Spawn(harness, Infantryman);

            Assert.AreEqual(TargetCategory.Friendly, guard.TargetCategory);
            Assert.IsNull(guard.Hue);
            Assert.IsNull(guard.Hue2);

            harness.Drain();
            CreatureManager.Instance.CreateCreatureOnClient(harness.Client, guard);

            var body = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                .Single(created => created.EntityId == guard.EntityId)
                .EntityData.OfType<BodyAttributesPacket>().Single();

            Assert.IsNull(body.Hue, "None, as a player's: the client paints no tint over the model");
            Assert.IsNull(body.Hue2);
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        /// <summary>One of this creature row, spawned by a pool beside the player.</summary>
        private static Creature Spawn(BootcampRuntimeTestHarness.Harness harness, uint creatureId)
        {
            var entry = harness.WorldContext.Set<CreatureEntry>().AsNoTracking().Single(row => row.Id == creatureId);
            var classEntry = harness.WorldContext.Set<EntityClassEntry>().AsNoTracking().Single(row => row.Id == entry.ClassId);
            var entityClass = new EntityClass(classEntry.Id, classEntry.ClassName, classEntry.MeshId, classEntry.ClassCollisionRole,
                classEntry.AugList.Split(',').Select(value => (AugmentationType)uint.Parse(value)).ToList(), classEntry.TargetFlag != 0);

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
                RespawnTime = 300_000,
                UpdateTimer = 300_000,
                SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(creatureId, 1, 1) }
            };

            harness.BootcampMap.SpawnPools.Clear();
            harness.BootcampMap.SpawnPools.Add(pool);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            return harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .Distinct()
                .Single(creature => creature.EntityClass == (EntityClasses)entry.ClassId);
        }
    }
}
