using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Test.Gameplay;

    /// <summary>
    /// A level gained plays the level-up's burst of light (DING_EFFECT at level 1, level_up.pkg)
    /// round the player, for them and everyone round them: LevelUp only posts the status text.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LevelUpDingTests
    {
        [TestMethod]
        public void GainingLevelsDingsOnceAnnouncedAtLevelOne()
        {
            using var context = new WeaponAmmoContext();
            var manager = new ManifestationManager(context);
            var client = context.Client;
            var player = client.Player;
            player.Level = 1;
            player.Experience = 0;
            player.Attributes = Enum.GetValues<Attributes>().ToDictionary(
                attribute => attribute,
                attribute => new ActorAttributes(attribute, 100, 100, 100, 0, 0));
            WorldTestContext.Drain(client);

            // Several levels at once: one ding.
            manager.GainExperience(client, 900000);
            Assert.IsGreaterThan(2, (int)player.Level);

            var dings = player.ActiveEffects.Values.Where(effect => effect.TypeId == ManifestationManager.DingTypeId).ToList();
            Assert.HasCount(1, dings);
            Assert.AreEqual(1u, dings[0].EffectLevel, "level 1: level_up.pkg (level 3 is the Eloh control point takeover)");
            Assert.IsFalse(dings[0].AnnounceToNewcomers, "not for anyone who comes along afterwards");
            Assert.IsTrue(dings[0].ExpiresTick - Environment.TickCount64 is > 0 and <= ManifestationManager.DingMs);

            var attached = WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<GameEffectAttachedPacket>()
                .Single(packet => packet.EffectTypeId == ManifestationManager.DingTypeId);
            Assert.IsTrue(attached.Announced);
            Assert.AreEqual(1u, attached.EffectLevel);

            // No level, no ding.
            dings[0].ExpiresTick = 0;
            GameEffectManager.Instance.DoWork(context.World.Map, 1000);
            manager.GainExperience(client, 1);
            Assert.IsFalse(player.ActiveEffects.Values.Any(effect => effect.TypeId == ManifestationManager.DingTypeId));
        }
    }
}
