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
