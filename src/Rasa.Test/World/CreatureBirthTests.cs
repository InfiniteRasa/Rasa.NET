using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// CREATURE_BIRTH is played as (actionId, argId) for its windup and its recovery
    /// (CreatureSummons.PlayBirth): the client's CreatureBirthAction.DoAction takes no hit lists.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureBirthTests
    {
        private Creature _born;

        [TestCleanup]
        public void Cleanup()
        {
            if (_born == null)
                return;

            EntityManager.Instance.UnregisterCreature(_born.EntityId);
            EntityManager.Instance.UnregisterActor(_born.EntityId);
            EntityManager.Instance.UnregisterEntity(_born.EntityId);
            _born = null;
        }

        [TestMethod]
        public void ABirthIsSentAsTheTwoArgumentWindupAndRecoveryTheClientsActionTakes()
        {
            using var world = new WorldTestContext();
            var watcher = world.CreateClient(x: 5);
            var seed = CellManager.Instance.GetCellSeed(watcher.Player.Position);
            watcher.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(watcher);

            _born = new Creature
            {
                Name = "Turret",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            _born.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            EntityManager.Instance.RegisterEntity(_born.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_born);
            EntityManager.Instance.RegisterActor(_born.EntityId, _born);
            var bornSeed = CellManager.Instance.GetCellSeed(_born.Position);
            _born.Cells = CellManager.Instance.CreateCellMatrix(world.Map, bornSeed & 0xFFFF, bornSeed >> 16);
            WorldTestContext.Drain(watcher);

            CreatureSummons.PlayBirth(world.Map, _born, 25540);

            var sent = WorldTestContext.Drain(watcher)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();

            var windup = sent.OfType<PerformWindupPacket>().Single();
            var recovery = sent.OfType<PerformRecoveryPacket>().Single();

            Assert.AreEqual(CreatureSummons.BirthAction, windup.ActionId);
            Assert.AreEqual(CreatureSummons.BirthAction, recovery.ActionId);
            Assert.AreEqual(25540u, recovery.ActionArgId, "keyed by the born creature's class");
            Assert.AreEqual(PerformType.TwoArgs, recovery.PerformType, "(actionId, argId): DoAction(actor) takes nothing more");
            Assert.IsEmpty(sent.OfType<AbilityRecoveryPacket>().ToList(), "not an ability's 6-tuple");
            Assert.IsLessThan(sent.IndexOf(recovery), sent.IndexOf(windup), "the windup first");
        }
    }
}
