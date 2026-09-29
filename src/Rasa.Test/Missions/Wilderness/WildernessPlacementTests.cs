using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessPlacementTests
    {
        [TestMethod]
        public void LaterHubHumanContactsAreRealGroundedNpcEntities()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (creatureId, spawnId, packageId) in new[]
            {
                (132U, 210U, 133U), (103U, 180U, 254U), (115U, 193U, 382U),
                (133U, 211U, 0U), (134U, 212U, 2049U), (126U, 204U, 213U),
                (138U, 216U, 251U), (127U, 205U, 214U), (135U, 213U, 211U),
                (130U, 208U, 212U), (125U, 203U, 218U), (104U, 181U, 0U),
                (97U, 172U, 0U), (120U, 198U, 0U)
            })
            {
                var pool = harness.Map.SpawnPools.Single(candidate => candidate.DbId == spawnId);
                harness.MoveTo(pool.Position);
                harness.Drain();
                harness.SpawnWorld(spawnId);

                var npc = harness.Npc(spawnId);
                Assert.IsNotNull(npc, $"Creature {creatureId}, pool {spawnId} must introduce a real NPC.");
                Assert.AreEqual(creatureId, npc.DbId);
                Assert.AreEqual(packageId, npc.Npc.NpcPackageId);
                Assert.IsTrue(npc.AppearanceData.Count > 0,
                    $"Human contact {creatureId} requires the appearance of its NPC-capable class.");
                var height = harness.Map.NavMesh.GroundHeight(npc.Position);
                Assert.IsNotNull(height, $"Contact {creatureId} must have a walkable interaction surface.");
                Assert.IsTrue(Math.Abs(npc.Position.Y - height.Value) < 0.5f,
                    $"Contact {creatureId} is on the wrong vertical surface.");
                Assert.IsTrue(harness.Drain().OfType<CreatePhysicalEntityPacket>()
                    .Any(packet => packet.EntityId == npc.EntityId && packet.ClassId == npc.EntityClass),
                    $"The real contact {creatureId} must be introduced to the player.");
            }
        }

        [TestMethod]
        public void TrainingOfficerAndExistingClassTrainersKeepTheirRealServices()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            Assert.AreEqual(10604U, harness.World.Set<CreatureEntry>().Single(row => row.Id == 510006).NameId);
            harness.Client.Player.Level = 5;
            harness.Client.Player.Class = (uint)CharacterClass.Recruit;
            foreach (var (spawnId, dialogId) in new[] { (510006U, 337), (501001U, 5), (501002U, 13) })
            {
                harness.SpawnWorld(spawnId);
                var trainer = harness.Npc(spawnId);
                Assert.IsNotNull(trainer);
                Assert.IsTrue(trainer.Npc.NpcIsTrainer);
                if (spawnId == 510006)
                    Assert.AreEqual(2588U, trainer.Npc.NpcPackageId);
                harness.MoveTo(trainer.Position);
                harness.Drain();

                NpcManager.Instance.RequestNpcConverse(harness.Client,
                    new RequestNPCConversePacket { EntityId = trainer.EntityId });

                var packet = harness.Drain().OfType<ConversePacket>().Single();
                Assert.IsTrue(packet.ConvoDataDict.TryGetValue(ConversationType.Training, out var value));
                var training = (TrainingConverse)value;
                Assert.IsTrue(training.CanTrain);
                Assert.AreEqual(dialogId, training.DialogId);
            }
        }

        [TestMethod]
        public void AllocatedWorldPopulationUsesItsAuthoredHealthInsteadOfTheGenericFallback()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (spawn, health) in new[]
            {
                (530001U, 600), (530010U, 800), (530040U, 900), (530043U, 900),
                (530046U, 600), (530070U, 1000), (530071U, 600), (530076U, 750),
                (530100U, 900), (530120U, 1500)
            })
            {
                harness.SpawnWorld(spawn);
                var actor = harness.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .Single(creature => creature.SpawnPool?.DbId == spawn);
                Assert.AreEqual(health, actor.Attributes[Attributes.Health].CurrentMax);
                Assert.AreEqual(health, actor.Attributes[Attributes.Health].Current);
            }
        }
    }
}
