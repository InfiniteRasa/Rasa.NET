using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// Tactical Evasion P1 / P3, the mag flash (AbilityManager.Stealth): the flash is attached
    /// quietly and the client is told who it blinded by the recovery's hitdata, a list of entity
    /// ids - TacticalEvasionAction.DoAbility announces TACTICAL_EVASION_MAG_FLASH_EFFECT on
    /// GetEntities(hitdata), which plays its FX, shows its icon and, on a flashed player's own
    /// client, lets go of their target (BlindEffect). With hitdata left empty nobody saw it.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TacticalEvasionFlashTests
    {
        private const uint TacticalEvasion = 10000005;
        private const int MagFlashTypeId = 10000075;    // TACTICAL_EVASION_MAG_FLASH_EFFECT

        private readonly List<Creature> _creatures = new List<Creature>();

        [TestMethod]
        [DataRow(1u)]
        [DataRow(3u)]
        public void TheFlashNamesWhoItBlindedInItsHitdata(uint pump)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var manager = ToyTests.CreateManager(harness, TacticalEvasion, pump, 0);

            try
            {
                var near = Spawn(map, player.Position + new Vector3(4, 0, 0));
                var farther = Spawn(map, player.Position + new Vector3(0, 0, -6));
                harness.Drain();

                Resolve(manager, harness.Client, pump);

                var packets = harness.Drain();
                var recovery = packets.OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == ActionId.AaRangerTacticalEvasion);
                Assert.AreEqual(AbilityRecoveryPacket.HitDataKind.EntityIds, recovery.Kind);
                CollectionAssert.AreEquivalent(new[] { near.EntityId, farther.EntityId }, recovery.Hits.Select(h => h.EntityId).ToArray());

                // The flash itself went out quietly; the client announces it from the hitdata.
                var flashes = packets.OfType<GameEffectAttachedPacket>().Where(p => p.EffectTypeId == MagFlashTypeId).ToList();
                Assert.HasCount(2, flashes);
                Assert.IsTrue(flashes.All(p => !p.Announced));

                var (hits, hitdata) = Decode(recovery);
                CollectionAssert.AreEqual(hits, hitdata, "hitdata is the hits' own ids, one for one");
            }
            finally
            {
                Remove(map);
            }
        }

        [TestMethod]
        [DataRow(2u)]
        [DataRow(4u)]
        [DataRow(5u)]
        public void TheSmokeAndTheRetreatKeepTheirHitdataEmpty(uint pump)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var manager = ToyTests.CreateManager(harness, TacticalEvasion, pump, 0);

            try
            {
                harness.Drain();
                Resolve(manager, harness.Client, pump);

                var recovery = harness.Drain().OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == ActionId.AaRangerTacticalEvasion);
                Assert.AreEqual(AbilityRecoveryPacket.HitDataKind.None, recovery.Kind, "nothing for DoAbility to announce a flash on");
                Assert.IsEmpty(Decode(recovery).HitData);
            }
            finally
            {
                var screens = (System.Collections.IList)typeof(AbilityManager).GetField("SmokeScreens", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

                lock (screens)
                    screens.Clear();
            }
        }

        private static void Resolve(AbilityManager manager, Client client, uint pump)
        {
            var actions = (Dictionary<ActionId, ActionInfo>)typeof(AbilityManager).GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
            var action = actions[ActionId.AaRangerTacticalEvasion];

            typeof(AbilityManager).GetMethod("ResolveTimedEffect", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(manager, new object[]
            {
                client.Player.MapChannel, client, client.Player, action, action.Levels[pump],
                new ActionData(client.Player, ActionId.AaRangerTacticalEvasion, pump, 0)
            });
        }

        /// <summary>PerformRecovery(actionId, actionArgId, hits, misses, missdata, hitdata), as the client reads it.</summary>
        private static (List<ulong> Hits, List<ulong> HitData) Decode(AbilityRecoveryPacket recovery)
        {
            using var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(recovery))));

            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)ActionId.AaRangerTacticalEvasion, reader.ReadUInt());
            reader.ReadUInt();

            var hits = Enumerable.Range(0, reader.ReadList()).Select(_ => reader.ReadULong()).ToList();
            Assert.AreEqual(0, reader.ReadList(), "misses");
            Assert.AreEqual(0, reader.ReadList(), "missdata");
            var hitdata = Enumerable.Range(0, reader.ReadList()).Select(_ => reader.ReadULong()).ToList();

            return (hits, hitdata);
        }

        private Creature Spawn(MapChannel map, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);

            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);

            _creatures.Add(creature);
            return creature;
        }

        private void Remove(MapChannel map)
        {
            foreach (var cell in map.MapCellInfo.Cells.Values)
                cell.CreatureList.RemoveAll(_creatures.Contains);

            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }

            _creatures.Clear();
        }
    }
}
