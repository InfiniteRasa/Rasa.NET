using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    // The field repair tool (TOOL_FIELD_REPAIR) and the dead. repairtool.py is a healing disc with
    // one check of its own: a dead player, and a dead creature that is BIOLOGICAL, are
    // TARGET_INVALID. The healing disc's own rule comes first - a dead target at all only at
    // Healing 3 - so what the tool may be aimed at dead is a machine.
    [TestClass]
    [DoNotParallelize]
    public class RepairToolTests
    {
        private const EntityClasses RepairToolClass = (EntityClasses)990781;
        private const EntityClasses HealingDiscClass = (EntityClasses)990782;
        internal const int Amount = 50;

        private readonly List<Creature> _creatures = new List<Creature>();

        [TestCleanup]
        public void ForgetTheCreatures()
        {
            foreach (var creature in _creatures)
                Forget(creature);

            _creatures.Clear();
        }

        [TestMethod]
        public void ADeadBiologicalCreatureIsNoTargetForTheRepairTool()
        {
            using var world = new WorldTestContext();
            var engineer = Arm(world, world.CreateClient(), ActionId.ToolFieldRepair, healing: 3);
            var soldier = Creature(world, 5, dead: true, CreatureFlag.Biological);
            // A hybrid of the two is biological still: the client asks for the one flag.
            var cyborg = Creature(world, 6, dead: true, CreatureFlag.Mechanical, CreatureFlag.Biological);

            foreach (var corpse in new[] { soldier, cyborg })
            {
                Assert.AreEqual(PlayerMessage.PmTargetInvalid, Refused(world, engineer, ActionId.ToolFieldRepair, corpse.EntityId));
                Assert.AreEqual(CharacterState.Dead, corpse.State);
                Assert.AreEqual(0, corpse.Attributes[Attributes.Health].Current);
            }
        }

        [TestMethod]
        public void ADeadMachineMayBeAimedAtAtHealingThreeAndNotBelow()
        {
            using var world = new WorldTestContext();
            var engineer = Arm(world, world.CreateClient(), ActionId.ToolFieldRepair, healing: 3);
            var turret = Creature(world, 5, dead: true, CreatureFlag.Mechanical);
            var machina = Creature(world, 6, dead: true, CreatureFlag.Machina);
            // No flag of either kind on its class: not BIOLOGICAL, so not refused for it.
            var unknown = Creature(world, 7, dead: true);

            foreach (var wreck in new[] { turret, machina, unknown })
            {
                // Taken, and nothing done to it: the tool does not bring a machine back.
                var landed = Use(world, engineer, ActionId.ToolFieldRepair, wreck.EntityId);

                Assert.IsEmpty(landed.Hits);
                Assert.AreEqual(CharacterState.Dead, wreck.State);
            }

            // Below Healing 3 neither tool is aimed at the dead at all (healdisc.py canTargetDead).
            engineer.Player.Skills[SkillId.SpecialistTools] = new SkillsData(SkillId.SpecialistTools, -1, 2);

            Assert.AreEqual(PlayerMessage.PmActionFailedTargetDead, Refused(world, engineer, ActionId.ToolFieldRepair, turret.EntityId));
        }

        [TestMethod]
        public void ADeadPlayerIsNoTargetForTheRepairTool()
        {
            using var world = new WorldTestContext();
            var engineer = Arm(world, world.CreateClient(), ActionId.ToolFieldRepair, healing: 3);
            var fallen = world.CreateClient(5, 0);
            fallen.Player.State = CharacterState.Dead;

            Assert.AreEqual(PlayerMessage.PmTargetInvalid, Refused(world, engineer, ActionId.ToolFieldRepair, fallen.Player.EntityId));

            // The disc's check is the first of the two, as in the client's CheckAction.
            engineer.Player.Skills.Remove(SkillId.SpecialistTools);

            Assert.AreEqual(PlayerMessage.PmActionFailedTargetDead, Refused(world, engineer, ActionId.ToolFieldRepair, fallen.Player.EntityId));
        }

        [TestMethod]
        public void TheLivingAreRepairedWhateverTheyAreMadeOf()
        {
            using var world = new WorldTestContext();
            var engineer = Arm(world, world.CreateClient(), ActionId.ToolFieldRepair, healing: 0);
            var soldier = Creature(world, 5, dead: false, CreatureFlag.Biological);
            var turret = Creature(world, 6, dead: false, CreatureFlag.Mechanical);

            foreach (var hurt in new[] { soldier, turret })
            {
                var landed = Use(world, engineer, ActionId.ToolFieldRepair, hurt.EntityId);

                // (armour, health): health on a creature.
                Assert.AreEqual(hurt.EntityId, landed.Hits.Single().EntityId);
                Assert.AreEqual(100 + Amount, hurt.Attributes[Attributes.Health].Current);
            }
        }

        [TestMethod]
        public void OneThatDiesDuringTheWindupTakesNothing()
        {
            using var world = new WorldTestContext();
            var engineer = Arm(world, world.CreateClient(), ActionId.ToolFieldRepair, healing: 3);
            var soldier = Creature(world, 5, dead: false, CreatureFlag.Biological);

            var landed = Use(world, engineer, ActionId.ToolFieldRepair, soldier.EntityId, duringWindup: () =>
            {
                soldier.State = CharacterState.Dead;
                soldier.Attributes[Attributes.Health].Current = 0;
            });

            Assert.IsEmpty(landed.Hits);
            Assert.AreEqual(0, soldier.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void TheHealingDiscsRuleForTheDeadIsItsOwn()
        {
            using var world = new WorldTestContext();
            var medic = Arm(world, world.CreateClient(), ActionId.ToolHealingDisc, healing: 3);
            var soldier = Creature(world, 5, dead: true, CreatureFlag.Biological);

            // A dead biological creature is the disc's to be aimed at, at Healing 3.
            Use(world, medic, ActionId.ToolHealingDisc, soldier.EntityId);

            medic.Player.Skills[SkillId.SpecialistTools] = new SkillsData(SkillId.SpecialistTools, -1, 2);

            Assert.AreEqual(PlayerMessage.PmActionFailedTargetDead, Refused(world, medic, ActionId.ToolHealingDisc, soldier.EntityId));
        }

        /// <summary>The request is refused with a message, and nothing is begun.</summary>
        internal static PlayerMessage? Refused(WorldTestContext world, Client client, ActionId actionId, ulong targetId)
        {
            MissionTestContext.Drain(client);

            ToolActionManager.Instance.RequestToolAction(client, Request(actionId, targetId));

            Assert.IsEmpty(world.Map.PerformRecovery, "nothing is begun");

            var failed = MissionTestContext.Drain(client).OfType<UserActionFailedPacket>().Single();
            Assert.AreEqual(actionId, failed.ActionId);
            return failed.MsgId;
        }

        /// <summary>The request is taken, the windup runs, and the tool lands.</summary>
        internal static ToolActionRecoveryPacket Use(WorldTestContext world, Client client, ActionId actionId, ulong targetId,
            System.Action duringWindup = null)
        {
            MissionTestContext.Drain(client);

            ToolActionManager.Instance.RequestToolAction(client, Request(actionId, targetId));

            Assert.IsFalse(MissionTestContext.Drain(client).OfType<UserActionFailedPacket>().Any(), "not refused");
            var action = world.Map.PerformRecovery.Single();
            Assert.AreEqual(actionId, action.ActionId);
            Assert.AreEqual(targetId, action.TargetId);
            world.Map.PerformRecovery.Clear();
            duringWindup?.Invoke();

            lock (Server.Clients)
                Server.Clients.Add(client);
            try
            {
                ToolActionManager.Instance.PerformRecovery(world.Map, action);
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(client);
            }

            return MissionTestContext.Drain(client).OfType<ToolActionRecoveryPacket>().Single();
        }

        /// <summary>The player with a direct tool of the action in hand, and the Tools skill that is the client's "Healing".</summary>
        internal static Client Arm(WorldTestContext world, Client client, ActionId actionId, int healing)
        {
            var classId = actionId == ActionId.ToolFieldRepair ? RepairToolClass : HealingDiscClass;

            world.AddClass(classId);
            EntityClassManager.Instance.LoadedEntityClasses[classId].WeaponClassInfo =
                new WeaponClassInfo(new WeaponClassEntry
                {
                    Id = (uint)classId,
                    AttackActionId = (uint)actionId,
                    AttackActionArgId = 1,
                    AmmoClassId = 0,
                    ClipSize = 8,
                    MinDamage = Amount,
                    MaxDamage = Amount
                });
            var tool = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                {
                    ItemTemplateId = (uint)classId,
                    ItemClass = (uint)classId
                })
            };
            EntityManager.Instance.RegisterEntity(tool.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(tool.EntityId, tool);
            client.Player.Inventory.EquippedInventory[13] = tool.EntityId;
            client.Player.WeaponReady = true;

            if (healing > 0)
                client.Player.Skills[SkillId.SpecialistTools] = new SkillsData(SkillId.SpecialistTools, -1, healing);

            CellManager.Instance.AddToWorld(client);
            return client;
        }

        /// <summary>A creature of the players' side, hurt or dead, of the substance its flags give it.</summary>
        private Creature Creature(WorldTestContext world, float x, bool dead, params CreatureFlag[] flags)
        {
            var creature = NewCreature(world, x, dead, flags);

            _creatures.Add(creature);
            return creature;
        }

        /// <summary>The same, for another class's tests to keep and forget (<see cref="Forget"/>).</summary>
        internal static Creature NewCreature(WorldTestContext world, float x, bool dead, params CreatureFlag[] flags)
        {
            var map = world.Map;
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Friendly,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Position = new Vector3(x, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = dead ? CharacterState.Dead : CharacterState.Standing,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                SpawnPool = new SpawnPool { SpawnSlot = new List<SpawnPoolSlot>() },
                ExtraFlags = flags.ToList()
            };

            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, dead ? 0 : 100, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);

            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);

            return creature;
        }

        internal static void Forget(Creature creature)
        {
            EntityManager.Instance.UnregisterEntity(creature.EntityId);
            EntityManager.Instance.UnregisterCreature(creature.EntityId);
        }

        private static RequestToolActionPacket Request(ActionId actionId, ulong targetId)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
            {
                writer.WriteTuple(3);
                writer.WriteInt((int)actionId);
                writer.WriteUInt(1);
                writer.WriteULong(targetId);
            }

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestToolActionPacket();
            packet.Read(reader);
            return packet;
        }
    }
}
