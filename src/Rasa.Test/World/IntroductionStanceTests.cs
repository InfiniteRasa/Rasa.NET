using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    // A player met after they drew their weapon, took up their combat stance or went AFK is
    // introduced so (ManifestationManager.CreatePlayerEntityData): every actor starts holstered,
    // at ease and not away on a client, and the change itself only reached those who could see
    // the player when it was made.
    [TestClass]
    [DoNotParallelize]
    public class IntroductionStanceTests
    {
        [TestMethod]
        public void SomeoneMetWithTheirWeaponOutInStanceAndAwayIsShownSo()
        {
            using var world = new WorldTestContext();
            var fighter = PlayerDeathTests.Player(world, 0, 0);
            var onlooker = PlayerDeathTests.Player(world, 5, 0);
            fighter.Player.WeaponReady = true;
            fighter.Player.InCombatMode = true;
            fighter.Player.IsAFK = true;

            var data = ManifestationManager.Instance.CreatePlayerEntityData(fighter, onlooker);

            Assert.IsTrue(data.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.IsTrue(data.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
            Assert.IsTrue(data.OfType<PlayerAfkPacket>().Single().IsAfk);

            // The weapon goes in the hand from the appearance and equipment, which come first.
            var ready = data.FindIndex(p => p is WeaponReadyPacket);
            Assert.IsTrue(data.FindIndex(p => p is AppearanceDataPacket) < ready);
            Assert.IsTrue(data.FindIndex(p => p is EquipmentInfoPacket) < ready);
        }

        [TestMethod]
        public void HolsteredAtEaseAndPresentIsWhatEveryActorStartsAsAndSendsNothing()
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 0, 0);
            var onlooker = PlayerDeathTests.Player(world, 5, 0);

            var data = ManifestationManager.Instance.CreatePlayerEntityData(player, onlooker);

            Assert.IsFalse(data.Any(p => p is WeaponReadyPacket || p is RequestVisualCombatModePacket || p is PlayerAfkPacket));
        }

        [TestMethod]
        public void TheirOwnClientIsNotToldAgain()
        {
            using var world = new WorldTestContext();
            var player = PlayerDeathTests.Player(world, 0, 0);
            player.Player.WeaponReady = true;
            player.Player.InCombatMode = true;
            player.Player.IsAFK = true;

            var data = ManifestationManager.Instance.CreatePlayerEntityData(player, player);

            // Its own stance is the client's, and PlayerAfk there says "You are AFK" again.
            Assert.IsFalse(data.Any(p => p is WeaponReadyPacket || p is RequestVisualCombatModePacket || p is PlayerAfkPacket));
        }

        [TestMethod]
        public void SomeoneWhoComesIntoViewLaterIsGivenThemInTheIntroduction()
        {
            using var world = new WorldTestContext();
            var fighter = PlayerDeathTests.Player(world, 0, 0);
            ManifestationManager.Instance.WeaponReady(fighter, true);
            ActorManager.Instance.RequestVisualCombatMode(fighter, true);
            fighter.Player.IsAFK = true;
            WorldTestContext.Drain(fighter);

            var comer = world.CreateClient(5, 0);
            CellManager.Instance.AddToWorld(comer);

            var made = WorldTestContext.Drain(comer)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .OfType<CreatePhysicalEntityPacket>()
                .Single(packet => packet.EntityId == fighter.Player.EntityId);

            Assert.IsTrue(made.EntityData.OfType<WeaponReadyPacket>().Single().WeaponReady);
            Assert.IsTrue(made.EntityData.OfType<RequestVisualCombatModePacket>().Single().CombatMode);
            Assert.IsTrue(made.EntityData.OfType<PlayerAfkPacket>().Single().IsAfk);
        }
    }
}
