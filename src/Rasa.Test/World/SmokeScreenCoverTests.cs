using System.Collections;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;

    // Tactical Evasion P2 / P4, the smoke screen (AbilityManager.Stealth): the cover on whoever
    // stands in the smoke goes out at the pump's level, because the client has the cover's icon
    // only at levels 2 and 4 (gameeffectdata.iconOverride) and its buff bar leaves out an effect
    // it cannot find an icon for.
    [TestClass]
    [DoNotParallelize]
    public class SmokeScreenCoverTests
    {
        private const int SmokeScreenTypeId = 10000076;     // TACTICAL_EVASION_SMOKE_SCREEN_EFFECT
        private const int SmokeAuraTypeId = 10000077;       // TACTICAL_EVASION_SMOKE_SCREEN_AURA_EFFECT
        private const int TacticalRetreatTypeId = 10000078; // TACTICAL_EVASION_TELEPORT_EFFECT
        private const int RetreatMarkTypeId = 10000079;     // TACTICAL_EVASION_LOCATION_EFFECT

        [TestMethod]
        [DataRow(2u, 30)]
        [DataRow(4u, 60)]
        public void TheCoverIsSentAtThePumpsLevelSoItsIconShows(uint pump, int mitigation)
        {
            using var world = new WorldTestContext();
            var ranger = world.CreateClient(0, 0);
            ranger.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            CellManager.Instance.AddToWorld(ranger);
            WorldTestContext.Drain(ranger);

            var abilities = Abilities();
            var info = new ActionLevelInfo { ActionId = ActionId.AaRangerTacticalEvasion, Level = pump, MaxRange = 30 };
            info.Properties[AbilityProperty.EffectIntervalMs] = 1000;
            info.Properties[AbilityProperty.EffectRadius] = 10;
            info.Properties[AbilityProperty.EffectModifier] = mitigation;

            try
            {
                Invoke(abilities, "ThrowSmokeScreen", world.Map, ranger.Player, info, 10000);
                abilities.SmokeWorker(world.Map);

                var cover = ranger.Player.ActiveEffects.Values.Single(e => e.TypeId == SmokeScreenTypeId);
                Assert.AreEqual(pump, cover.EffectLevel);
                Assert.AreEqual(mitigation, cover.IncomingRangedPercent);

                var attached = Sent(ranger).OfType<GameEffectAttachedPacket>().ToList();
                Assert.AreEqual(pump, attached.Single(p => p.EffectTypeId == SmokeScreenTypeId).EffectLevel, "the cover, at the level its icon is at");
                Assert.AreEqual(pump, attached.Single(p => p.EffectTypeId == SmokeAuraTypeId).EffectLevel, "the smoke itself, at the level its FX is at");
            }
            finally
            {
                var screens = (IList)typeof(AbilityManager).GetField("SmokeScreens", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

                lock (screens)
                    screens.Clear();
            }
        }

        [TestMethod]
        public void TheRetreatAndItsFlagAreSentAtLevelOneAndTheRetreatIsAnnounced()
        {
            // P5: TACTICAL_EVASION_TELEPORT_EFFECT and _LOCATION_EFFECT have their FX at level 1
            // only, and TacticalEvasionAction announces nothing but the mag flash.
            using var world = new WorldTestContext();
            var ranger = world.CreateClient(0, 0);
            ranger.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            CellManager.Instance.AddToWorld(ranger);
            WorldTestContext.Drain(ranger);

            var abilities = Abilities();
            var info = new ActionLevelInfo { ActionId = ActionId.AaRangerTacticalEvasion, Level = 5, MaxRange = 30 };
            info.Properties[AbilityProperty.EffectDurationMs] = 60000;
            var recovery = new AbilityRecoveryPacket(ActionId.AaRangerTacticalEvasion, 5, AbilityRecoveryPacket.HitDataKind.None);

            try
            {
                Invoke(abilities, "TacticalEvasion", world.Map, ranger, ranger.Player, info, recovery);

                var retreat = ranger.Player.ActiveEffects.Values.Single(e => e.TypeId == TacticalRetreatTypeId);
                Assert.AreEqual(1u, retreat.EffectLevel);

                var attached = Sent(ranger).OfType<GameEffectAttachedPacket>().ToList();
                var sent = attached.Single(p => p.EffectTypeId == TacticalRetreatTypeId);
                Assert.AreEqual(1u, sent.EffectLevel, "the retreat, at the level its FX is at");
                Assert.IsTrue(sent.Announced, "announced: its icon and FX");
                Assert.AreEqual(1u, attached.Single(p => p.EffectTypeId == RetreatMarkTypeId).EffectLevel, "the flag on the ground, likewise");
            }
            finally
            {
                var screens = (IList)typeof(AbilityManager).GetField("SmokeScreens", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

                lock (screens)
                    screens.Clear();
            }
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static void Invoke(AbilityManager abilities, string name, params object[] args) =>
            typeof(AbilityManager).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(abilities, args);

        private static object[] Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => (object)message.Packet)
                .ToArray();
    }
}
