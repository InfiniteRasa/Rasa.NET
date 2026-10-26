using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;

    /// <summary>
    /// A player's summoned minion (a spotter, a bot, a clone) carries its MINION effect, whose
    /// OnAnnounceAttach is what makes the client count the minion's damage as its master's. A
    /// client that meets the minion after it was summoned is sent the effect announced too.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MinionNewcomerTests
    {
        [TestMethod]
        public void AClientThatMeetsAPlayersCloneLaterIsToldWhoseItIs()
        {
            using var world = new WorldTestContext();
            world.AddClass((EntityClasses)AbilityManager.CloneMaleClassId);
            world.AddClass((EntityClasses)AbilityManager.CloneFemaleClassId);

            var owner = world.CreateClient(x: 10);
            owner.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
            owner.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 100, 0, 0);
            CellManager.Instance.AddToWorld(owner);
            WorldTestContext.Drain(owner);

            var info = new ActionLevelInfo { ActionId = ActionId.AaExobiologistCreateClone, Level = 1 };
            Invoke("CreateClone", world.Map, owner, owner.Player, info, new ActionData(owner.Player, ActionId.AaExobiologistCreateClone, 1, 0, 0));

            var clone = world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct().Single(AbilityManager.IsSpotter);

            try
            {
                var minion = clone.ActiveEffects.Values.Single(effect => effect.TypeId == AbilityManager.CloneMinionTypeId);

                // Those there as it was summoned.
                var attached = WorldTestContext.Drain(owner).Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                    .Where(message => message.EntityId == clone.EntityId).Select(message => message.Packet).OfType<GameEffectAttachedPacket>()
                    .Single(packet => packet.EffectTypeId == AbilityManager.CloneMinionTypeId);
                Assert.IsTrue(attached.Announced);

                // Whoever turns up later: announced, from the master, so AddExtraDamageSource runs.
                var newcomer = world.CreateClient(x: 400);
                var shown = GameEffectManager.EffectsForNewcomer(clone, newcomer.Player).Single(packet => packet.EffectId == minion.EffectId);
                Assert.AreEqual((AbilityManager.CloneMinionTypeId, owner.Player.EntityId, true), (shown.EffectTypeId, shown.SourceId, shown.Announced));

                // And the master, coming back to it (a map change, out of range and in again).
                Assert.IsTrue(GameEffectManager.EffectsForNewcomer(clone, owner.Player).Single(packet => packet.EffectId == minion.EffectId).Announced);
            }
            finally
            {
                MinionManager.Instance.Dismiss(world.Map, clone);
                Invoke("SpotterWorker", world.Map);
            }
        }

        private static object Invoke(string name, params object[] args)
        {
            var method = typeof(AbilityManager).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!;
            return method.Invoke(method.IsStatic ? null : Abilities(), args);
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });
    }
}
