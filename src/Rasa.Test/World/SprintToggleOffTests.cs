using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Test.Missions;
using Rasa.Test.Missions.Wilderness;

namespace Rasa.Test.World
{
    /// <summary>
    /// Sprint is "Open-ended (toggle to deactivate)", but SprintAction is no isToggle ability:
    /// the client judges the press that would end it as one that starts it, against the
    /// action_cost row (30 adrenaline at P1, BaseActorAbility.CheckConsumables). Once the drain
    /// had taken the bar below that, the client refused the press itself - "not enough
    /// adrenaline", no request - and sprint could not be turned off until the bar was empty.
    /// While sprint is on the owner's client now holds a MODULE_POWER_COST_MOD that takes the
    /// action's cost to nothing (GameEffectManager.WaiveCostWhileOn), so the press goes through
    /// and the server ends sprint with it.
    /// </summary>
    [TestClass, DoNotParallelize]
    public class SprintToggleOffTests
    {
        /// <summary>action_cost for AA_RECRUIT_SPRINT P1: 30 adrenaline.</summary>
        private const int CostAtP1 = 30;

        [TestMethod]
        public void TheOwnersClientIsToldThePressCostsNothingWhileSprintIsOn()
        {
            using var world = new WorldTestContext();
            var runner = Player(world, 0);
            var bystander = Player(world, 3);

            var sprint = GameEffectManager.Instance.AttachSprint(world.Map, runner.Player, Level(1));

            var waiver = Attached(runner).Single(packet => packet.EffectTypeId == GameEffectManager.PowerCostModTypeId);
            Assert.IsTrue(waiver.Announced, "ModulePowerCostMod reads its arguments in OnAnnounceAttach");
            Assert.AreEqual(runner.Player.EntityId, waiver.SourceId);
            Assert.AreEqual(3, waiver.Args.Count);
            Assert.AreEqual(-1.0, Convert.ToDouble(waiver.Args[0]), 0.0001, "amount");
            Assert.AreEqual((int)ActionId.AaRecruitSprint, Convert.ToInt32(waiver.Args[1]), "actionId");
            Assert.IsNull(waiver.Args[2], "actionArgId None: every pump");

            // BaseActorAbility.CheckConsumables: cost *= 1.0 + amount; refused when current < cost.
            var clientCost = (int)(CostAtP1 * (1.0 + Convert.ToDouble(waiver.Args[0])));
            Assert.AreEqual(0, clientCost, "no bar is short of it");

            Assert.IsFalse(Attached(bystander).Any(packet => packet.EffectTypeId == GameEffectManager.PowerCostModTypeId),
                "another client's own cost is not changed by it");
            Assert.IsTrue(runner.Player.ActiveEffects.ContainsKey(sprint.EffectId));
        }

        [TestMethod]
        public void OnlyTheOwnerMeetsItAgainAfterwards()
        {
            using var world = new WorldTestContext();
            var runner = Player(world, 0);
            var bystander = Player(world, 3);

            GameEffectManager.Instance.AttachSprint(world.Map, runner.Player, Level(1));

            var own = GameEffectManager.EffectsForNewcomer(runner.Player, runner.Player).Single(packet => packet.EffectTypeId == GameEffectManager.PowerCostModTypeId);
            Assert.AreEqual(3, own.Args.Count, "re-sent with its arguments, on a map change or a relog's view");
            Assert.IsFalse(GameEffectManager.EffectsForNewcomer(runner.Player, bystander.Player).Any(packet => packet.EffectTypeId == GameEffectManager.PowerCostModTypeId));
        }

        [TestMethod]
        public void ItComesOffWithSprintWhenTheBuffIsTakenOff()
        {
            using var world = new WorldTestContext();
            var runner = Player(world, 0);

            var sprint = GameEffectManager.Instance.AttachSprint(world.Map, runner.Player, Level(1));
            var waiverId = Attached(runner).Single(packet => packet.EffectTypeId == GameEffectManager.PowerCostModTypeId).EffectId;

            GameEffectManager.Instance.DettachEffect(world.Map, runner.Player, sprint);

            Assert.IsFalse(runner.Player.ActiveEffects.ContainsKey(waiverId));
            CollectionAssert.Contains(Detached(runner), waiverId, "the client's cost is back to its own");
        }

        [TestMethod]
        public void ItComesOffWithSprintWhenTheBarRunsDry()
        {
            using var world = new WorldTestContext();
            var runner = Player(world, 0);
            var sprint = GameEffectManager.Instance.AttachSprint(world.Map, runner.Player, Level(1));
            var waiverId = Attached(runner).Single(packet => packet.EffectTypeId == GameEffectManager.PowerCostModTypeId).EffectId;

            // The last point of the bar: the next tick takes it (1.5% of 100 a second).
            runner.Player.Attributes[Attributes.Chi].Current = 1;

            // Ticks of the drain until the last point is gone.
            var tickDrain = typeof(GameEffectManager).GetMethod("TickDrain", BindingFlags.Instance | BindingFlags.NonPublic)!;

            for (var tick = 0; tick < 10 && runner.Player.ActiveEffects.ContainsKey(sprint.EffectId); tick++)
                tickDrain.Invoke(GameEffectManager.Instance, new object[] { world.Map, runner.Player, sprint });

            Assert.IsFalse(runner.Player.ActiveEffects.ContainsKey(sprint.EffectId), "ended with the bar");
            Assert.IsFalse(runner.Player.ActiveEffects.ContainsKey(waiverId));
            CollectionAssert.Contains(Detached(runner), waiverId);
        }

        /// <summary>
        /// The whole round, as the client asks for it: sprint on with a full bar, the bar run
        /// down below the press's cost, and the press - which the client now sends - ends it.
        /// </summary>
        [TestMethod]
        public void ThePressBelowSprintsCostTurnsItOff()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var abilities = harness.LoadAbilities();
            var player = harness.Client.Player;
            player.Skills[SkillId.Sprint] = new SkillsData(SkillId.Sprint, (int)ActionId.AaRecruitSprint, 1);

            foreach (var logos in AbilityLogos.Of(ActionId.AaRecruitSprint))
                if (!player.Logos.Contains(logos))
                    player.Logos.Add(logos);

            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 100, 100, 100, 0, 0);

            lock (Server.Clients)
                Server.Clients.Add(harness.Client);

            try
            {
                abilities.RequestPerformAbility(harness.Client, ToyTests.Request((int)ActionId.AaRecruitSprint, 1, 0));
                var pending = harness.Map.PerformRecovery.Single(entry => entry.ActionId == ActionId.AaRecruitSprint);
                harness.Map.PerformRecovery.Remove(pending);
                abilities.PerformRecovery(harness.Map, pending);

                var sprint = player.ActiveEffects.Values.Single(effect => effect.TypeId == GameEffectManager.SprintTypeId);
                var waiver = player.ActiveEffects.Values.Single(effect => effect.TypeId == GameEffectManager.PowerCostModTypeId);

                // Run down below the 30 the client checks.
                player.Attributes[Attributes.Chi].Current = 12;
                WorldTestContext.Drain(harness.Client);

                abilities.RequestPerformAbility(harness.Client, ToyTests.Request((int)ActionId.AaRecruitSprint, 1, 0));

                Assert.IsFalse(player.ActiveEffects.ContainsKey(sprint.EffectId), "turned off");
                Assert.IsFalse(player.ActiveEffects.ContainsKey(waiver.EffectId));
                Assert.IsFalse(harness.Map.PerformRecovery.Any(entry => entry.ActionId == ActionId.AaRecruitSprint), "not started again");
                Assert.AreEqual(12, player.Attributes[Attributes.Chi].Current, "and nothing taken for it");

                var failed = Sent(harness.Client).OfType<UserActionFailedPacket>().Single();
                Assert.IsNull(failed.MsgId, "quietly: the client's windup is called off, no message");
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        #region Fixture

        private static ActionLevelInfo Level(uint pump)
        {
            var level = new ActionLevelInfo { ActionId = ActionId.AaRecruitSprint, Level = pump };

            foreach (var row in new ActionPropertyPreloader().Rows().Where(r => Convert.ToUInt32(r[1]) == 401 && Convert.ToUInt32(r[2]) == pump))
                level.Properties[(AbilityProperty)Convert.ToUInt32(row[3])] = Convert.ToInt32(row[4]);

            return level;
        }

        private static Client Player(WorldTestContext world, float x)
        {
            var client = PlayerDeathTests.Player(world, x, 0);
            client.Player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 100, 100, 100, 0, 0);
            return client;
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static List<GameEffectAttachedPacket> Attached(Client client) => Sent(client).OfType<GameEffectAttachedPacket>().ToList();

        private static List<int> Detached(Client client) => Sent(client).OfType<GameEffectDetachedPacket>().Select(packet => packet.EffectId).ToList();

        #endregion
    }
}
