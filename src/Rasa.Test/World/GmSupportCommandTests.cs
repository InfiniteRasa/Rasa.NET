using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// The GM support commands (<see cref="GmSupportCommands"/>): finding a player, reading their
    /// details and bags, and standing them back up - each answered in the GM's chat.
    /// </summary>
    [TestClass]
    public class GmSupportCommandTests
    {
        [TestMethod]
        public void WhoListsEveryoneInTheWorldAndNarrowsByWord()
        {
            using var context = NewContext();
            var (gm, player) = TwoPlayers(context);

            Run(gm, player, () => GmSupportCommands.Who(gm, new[] { ".who" }));
            var all = Said(gm);
            StringAssert.Contains(all, "2 in the world");
            StringAssert.Contains(all, "Keeper");
            StringAssert.Contains(all, "Wanderer");

            Run(gm, player, () => GmSupportCommands.Who(gm, new[] { ".who", "wander" }));
            var narrowed = Said(gm);
            StringAssert.Contains(narrowed, "Wanderer");
            Assert.IsFalse(narrowed.Contains("Keeper"), narrowed);
        }

        [TestMethod]
        public void PlayerInfoFindsByFamilyNameOrCharacterId()
        {
            using var context = NewContext();
            var (gm, player) = TwoPlayers(context);

            Run(gm, player, () => GmSupportCommands.PlayerInfo(gm, new[] { ".playerinfo", "wanderer" }));
            StringAssert.Contains(Said(gm), $"character {player.Player.Id}");

            Run(gm, player, () => GmSupportCommands.PlayerInfo(gm, new[] { ".playerinfo", $"#{player.Player.Id}" }));
            StringAssert.Contains(Said(gm), "Wanderer");
        }

        [TestMethod]
        public void SomeoneNotInTheWorldIsSaidToBeSo()
        {
            using var context = NewContext();
            var (gm, player) = TwoPlayers(context);

            Run(gm, player, () => GmSupportCommands.PlayerInfo(gm, new[] { ".playerinfo", "Nobody" }));
            StringAssert.Contains(Said(gm), "Nobody is not in the world.");

            Run(gm, player, () => GmSupportCommands.Revive(gm, new[] { ".revive" }));
            StringAssert.Contains(Said(gm), "Usage: .revive");
        }

        [TestMethod]
        public void InventoryListsWhatIsInTheBackpack()
        {
            using var context = NewContext();
            var (gm, player) = TwoPlayers(context);
            var item = context.CreateInventoryItem(900001, 17280, 3);
            gm.Player.Inventory.PersonalInventory[0] = item.EntityId;

            Run(gm, player, () => GmSupportCommands.ShowInventory(gm, new[] { ".inventory", "Keeper", "backpack" }));
            var said = Said(gm);
            StringAssert.Contains(said, "backpack (1):");
            StringAssert.Contains(said, "x3 (template 900001");
            Assert.IsFalse(said.Contains("equipped"), said);
        }

        [TestMethod]
        public void GmCannotGoToOrSummonThemselves()
        {
            using var context = NewContext();
            var (gm, player) = TwoPlayers(context);

            Run(gm, player, () => GmSupportCommands.Goto(gm, new[] { ".goto", "Keeper" }));
            StringAssert.Contains(Said(gm), "You can't go to yourself.");

            Run(gm, player, () => GmSupportCommands.Summon(gm, new[] { ".summon", "Keeper" }));
            StringAssert.Contains(Said(gm), "You can't summon yourself.");
        }

        [TestMethod]
        public void ReviveRefusesTheLivingAndStandsUpTheDeadWithoutPenalties()
        {
            using var context = NewContext();
            var (gm, player) = TwoPlayers(context);

            Run(gm, player, () => GmSupportCommands.Revive(gm, new[] { ".revive", "Wanderer" }));
            StringAssert.Contains(Said(gm), "is not dead");

            player.Player.State = CharacterState.Dead;
            player.Player.StateBeforeDeath = CharacterState.Idle;
            var effects = player.Player.ActiveEffects.Count;

            Run(gm, player, () => GmSupportCommands.Revive(gm, new[] { ".revive", "Wanderer" }));

            Assert.AreNotEqual(CharacterState.Dead, player.Player.State);
            Assert.AreEqual(effects, player.Player.ActiveEffects.Count, "no rez sickness or other penalty effect");
            StringAssert.Contains(Said(player), "A game master has revived you.");
        }

        private static MissionTestContext NewContext() =>
            MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>());

        private static (Client Gm, Client Player) TwoPlayers(MissionTestContext context)
        {
            var gm = context.Client;
            gm.Player.FamilyName = "Keeper";
            gm.Player.Level = 20;
            var player = context.CreateAdditionalClient(4242);
            player.Player.FamilyName = "Wanderer";
            player.Player.Level = 20;
            MissionTestContext.Drain(gm);
            return (gm, player);
        }

        private static void Run(Client gm, Client player, System.Action command)
        {
            GmSupportCommands.OnlineClients = () => new[] { gm, player };
            try
            {
                command();
            }
            finally
            {
                GmSupportCommands.OnlineClients = null;
            }
        }

        private static string Said(Client client) =>
            string.Join("\n", MissionTestContext.Drain(client).OfType<SystemMessagePacket>().Select(p => p.TextMessage));
    }
}
