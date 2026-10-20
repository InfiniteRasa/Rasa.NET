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

        // A Hunter's Howler attacked 100 ms after it was born. The client plays the birth as
        // the Howler's current action for its recovery and holds every other action sent for it
        // behind that one: the bites landed on the server while the client still showed the
        // Howler growing.
        [TestMethod]
        public void ASummonDoesNothingUntilItsBirthIsOver()
        {
            using var harness = Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness.Create();
            harness.LoadAbilities();
            var player = harness.Client.Player;
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            CellManager.Instance.AddToWorld(harness.Client);

            // A Hunter of the Divide, with nothing but its pet.
            var hunter = harness.Creatures.CreateCreature(530007, null);
            hunter.Actions.RemoveAll(action => action.ActionId != CreatureSummons.HunterPet);
            harness.Creatures.SetLocation(hunter, player.Position + new Vector3(8, 0, 0), 0, player.MapContextId);
            CellManager.Instance.AddToWorld(harness.Map, hunter);
            Assert.IsTrue(AbilityManager.Instance.TryGetLevel(CreatureSummons.BirthAction, 7336, out var birth));

            Assert.IsTrue(CreatureSummons.Perform(harness.Map, hunter, hunter.Actions[0], player));
            System.Threading.Thread.Sleep(CreatureWindups.WindupMsOf(hunter.Actions[0], LevelOf(hunter.Actions[0])) + 100);
            CreatureWindups.Worker(harness.Map);

            var howler = EntityManager.Instance.Creatures.Values.Single(creature => creature.MasterEntityId == hunter.EntityId);
            var born = System.Environment.TickCount64;
            Assert.IsTrue(CreatureSummons.IsBusy(howler), "being born");
            WorldTestContext.Drain(harness.Client);

            var first = Act(harness, howler, born);

            Assert.IsGreaterThanOrEqualTo(birth.RecoveryMs, first, "nothing performed before its birth is over");
            Assert.IsFalse(CreatureSummons.IsBusy(howler));
        }

        /// <summary>The map run in real time until the creature performs something; how long after it was born that was.</summary>
        private static long Act(Rasa.Test.Missions.Wilderness.WildernessRuntimeTestHarness harness, Creature creature, long born)
        {
            for (var tick = 0; tick < 100; tick++)
            {
                harness.Tick(100);
                CreatureWindups.Worker(harness.Map);
                MissileManager.Instance.DoWork(harness.Map, 100);

                if (WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                        .Any(call => call.EntityId == creature.EntityId && call.Packet is PerformWindupPacket or Rasa.Packets.MapChannel.Server.PerformRecovery.CreatureAbilityRecovery))
                    return System.Environment.TickCount64 - born;

                System.Threading.Thread.Sleep(100);
            }

            Assert.Fail("it never acted");
            return 0;
        }

        private static ActionLevelInfo LevelOf(CreatureAction action) =>
            AbilityManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var info) ? info : null;
    }
}
