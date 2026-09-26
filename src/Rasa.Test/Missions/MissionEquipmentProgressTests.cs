using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Mission.Server;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.World;

    [TestClass]
    [DoNotParallelize]
    public class MissionEquipmentProgressTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CommittedEquipCompletesConfiguredEquipmentObjective(bool matchTemplateId)
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            const uint classId = 7000;
            const uint templateId = 2800;
            var fixture = CreateItemEquippedFixture(
                matchTemplateId
                    ? templateId
                    : classId,
                matchTemplateId);
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);
            var inventory = new InventoryManager(context, manager);
            var item = CreateEquippableArmorItem(
                context,
                inventory,
                templateId,
                classId);
            OverrideManifestationSingleton(context);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            context.Drain();

            inventory.RequestEquipArmor(
                context.Client,
                new RequestEquipArmorPacket
                {
                    SrcInventory = InventoryType.Personal,
                    SrcSlot = 0,
                    DestSlot = 0
                });

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());
            Assert.AreEqual(
                (uint)MissionObjectiveState.Completed,
                context.ReadProgress(321).Missions[321].Objectives[10].State);
            Assert.AreEqual(item.EntityId, context.Client.Player.Inventory.EquippedInventory[0]);
        }

        [TestMethod]
        public void DuplicateEquipPacketsDoNotPublishSecondMissionDelta()
        {
            using var context = MissionTestContext.WithCustomDefinitions(
                new Dictionary<uint, Mission>());
            const uint classId = 7000;
            const uint templateId = 2800;
            var fixture = CreateItemEquippedFixture(classId, matchTemplateId: false);
            var manager = LoadManager(context, fixture);
            var giver = context.AddNpc(101);
            var inventory = new InventoryManager(context, manager);
            OverrideManifestationSingleton(context);

            Assert.IsTrue(manager.AcceptOfferedMission(
                context.Client,
                giver.EntityId,
                321));
            var item = CreateEquippableArmorItem(
                context,
                inventory,
                templateId,
                classId);
            context.Drain();

            var request = new RequestEquipArmorPacket
            {
                SrcInventory = InventoryType.Personal,
                SrcSlot = 0,
                DestSlot = 0
            };
            inventory.RequestEquipArmor(context.Client, request);
            Assert.AreEqual(
                1,
                context.Drain().OfType<ObjectiveCompletedPacket>().Count());

            inventory.RequestEquipArmor(context.Client, request);

            Assert.AreEqual(
                MissionObjectiveState.Completed,
                context.Client.Player.Missions[321].Objectives[10].State);
            Assert.AreEqual(0, context.Drain().OfType<ObjectiveCompletedPacket>().Count());
        }

        private static Item CreateEquippableArmorItem(
            MissionTestContext context,
            InventoryManager inventory,
            uint templateId,
            uint classId)
        {
            context.AddRewardTemplate(templateId, classId);
            var classInfo = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)classId];
            classInfo.EquipableClassInfo = new EquipableClassInfo((EquipmentData)0);
            var item = context.CreateInventoryItem(templateId, classId, 1);
            inventory.AddItemBySlot(
                context.Client,
                InventoryType.Personal,
                item.EntityId,
                0,
                true,
                true);
            return item;
        }

        private static void OverrideManifestationSingleton(
            MissionTestContext context)
        {
            context.Client.Player.Attributes[Attributes.Body] =
                new ActorAttributes(Attributes.Body, 10, 10, 10, 0, 0);
            context.Client.Player.Attributes[Attributes.Mind] =
                new ActorAttributes(Attributes.Mind, 10, 10, 10, 0, 0);
            context.Client.Player.Attributes[Attributes.Spirit] =
                new ActorAttributes(Attributes.Spirit, 10, 10, 10, 0, 0);
            context.Client.Player.Attributes[Attributes.Health] =
                new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            context.Client.Player.Attributes[Attributes.Chi] =
                new ActorAttributes(Attributes.Chi, 100, 100, 100, 0, 0);
            context.Client.Player.Attributes[Attributes.Power] =
                new ActorAttributes(Attributes.Power, 100, 100, 100, 0, 0);
            context.Client.Player.Attributes[Attributes.Regen] =
                new ActorAttributes(Attributes.Regen, 0, 0, 0, 0, 0);
            context.Client.Player.Attributes[Attributes.Armor] =
                new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            typeof(ManifestationManager)
                .GetField("_instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(null, new ManifestationManager(context));
        }

        private static MissionContentFixture CreateItemEquippedFixture(
            uint subjectId,
            bool matchTemplateId)
        {
            var fixture = MissionContentFixture.CreateValid();
            fixture.Triggers.Clear();
            fixture.Actions.Clear();
            fixture.Transitions.Clear();
            fixture.Rewards.Clear();
            fixture.RewardItems.Clear();
            fixture.Indicators.Clear();
            fixture.Objectives.RemoveAll(objective => objective.ObjectiveId == 11);
            fixture.Transitions.Add(new MissionObjectiveTransitionEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                Requirement = MissionContentRequirement.Required,
                Sequence = 1,
                FromState = (byte)MissionObjectiveState.Incomplete,
                ToState = (byte)MissionObjectiveState.Completed,
                Comment = "Equip an item"
            });
            fixture.Triggers.Add(new MissionTriggerEntry
            {
                MissionId = 321,
                ContentRevision = "deployment_11",
                ObjectiveId = 10,
                TransitionId = 20,
                TriggerId = 1,
                Requirement = MissionContentRequirement.Required,
                Kind = MissionTriggerKind.ProgressEvent,
                Sequence = 1,
                EventKind = (byte)MissionProgressEventKind.ItemEquipped,
                SubjectId = subjectId,
                SourceSpawnResolved = matchTemplateId ? true : null,
                Comment = "Equip the configured item"
            });
            fixture.EntityClassIds.Add(7000);
            fixture.ItemTemplateClasses[2800] = 7000;
            return fixture;
        }

        private static MissionApplication LoadManager(
            MissionTestContext context,
            MissionContentFixture fixture)
        {
            var manager = new MissionApplication(
                new MissionContentLoadingFactory(context, fixture.CreateWorldUnitOfWork()),
                new Dictionary<uint, Mission>());
            var report = manager.LoadMissions();
            Assert.IsFalse(
                report.BlocksReadiness,
                string.Join(" | ", report.Diagnostics.Select(diagnostic => diagnostic.Code)));
            return manager;
        }

        private sealed class MissionContentLoadingFactory : IGameUnitOfWorkFactory
        {
            private readonly MissionTestContext _charFactory;
            private readonly IWorldUnitOfWork _worldUnit;

            internal MissionContentLoadingFactory(
                MissionTestContext charFactory,
                IWorldUnitOfWork worldUnit)
            {
                _charFactory = charFactory;
                _worldUnit = worldUnit;
            }

            public ICharUnitOfWork CreateChar() => _charFactory.CreateChar();
            public IWorldUnitOfWork CreateWorld() => _worldUnit;
        }
    }
}
