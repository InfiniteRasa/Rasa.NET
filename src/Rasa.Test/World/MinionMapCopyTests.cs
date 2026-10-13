using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    // A private, shared or squad instance is another channel of the same map, with the same
    // context id. The minion worker runs once per channel and must only deal with the minions
    // standing on that channel (BR-199): otherwise a populated copy sees another copy's Spotter
    // bot as a minion whose master "is not here", dismisses it from a world it never stood in,
    // and leaves it standing orphaned on its own copy, answering no commands.
    [TestClass]
    [DoNotParallelize]
    public class MinionMapCopyTests
    {
        [TestMethod]
        public void AnotherCopyOfTheMapLeavesAMinionAlone()
        {
            using var world = new WorldTestContext();
            var copy = Copy(world.Map);
            var master = world.CreateClient();
            master.Player.MapChannel = copy;
            var bot = Summon(copy, master);
            try
            {
                // The map's own channel, and a third copy, each take their turn before the copy
                // the bot is on. Neither has any business with it.
                MinionManager.Instance.Worker(world.Map, 250);
                MinionManager.Instance.Worker(Copy(world.Map), 250);

                Assert.AreSame(bot, MinionManager.Instance.MinionsOf(master).Single(), "still the master's");
                Assert.AreEqual(master.Player.EntityId, bot.MasterEntityId);
                Assert.IsTrue(copy.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(bot)), "still standing on its copy");
                Assert.IsTrue(EntityManager.Instance.Creatures.ContainsKey(bot.EntityId));

                // Its own channel keeps it while the master is there ...
                MinionManager.Instance.Worker(copy, 250);
                Assert.AreSame(bot, MinionManager.Instance.MinionsOf(master).Single());

                // ... and is the one that takes it away once the master has gone.
                master.Player.MapChannel = world.Map;
                MinionManager.Instance.Worker(copy, 250);

                Assert.AreEqual(0, MinionManager.Instance.MinionsOf(master).Count);
                Assert.IsFalse(copy.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(bot)), "gone from its copy");
                Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(bot.EntityId));
            }
            finally
            {
                MinionManager.Instance.DismissAll(master);
                EntityManager.Instance.UnregisterEntity(bot.EntityId);
                EntityManager.Instance.UnregisterCreature(bot.EntityId);
                EntityManager.Instance.FreeEntity(bot.EntityId);
            }
        }

        [TestMethod]
        public void StandsOnGoesByTheChannelWhileInTheWorldAndByTheMapAfterwards()
        {
            using var world = new WorldTestContext();
            var copy = Copy(world.Map);
            var other = new MapChannel { MapInfo = new MapInfo(1148, "adv_foreas_concordia_divide", 1556, 0), ClientList = new List<Client>() };

            var standing = new Creature { MapContextId = world.Map.MapInfo.MapContextId, RuntimeMapChannel = copy };
            Assert.IsTrue(MinionManager.StandsOn(standing, copy));
            Assert.IsFalse(MinionManager.StandsOn(standing, world.Map), "same map, another channel");

            var removed = new Creature { MapContextId = world.Map.MapInfo.MapContextId, RuntimeMapChannel = null };
            Assert.IsTrue(MinionManager.StandsOn(removed, copy));
            Assert.IsTrue(MinionManager.StandsOn(removed, world.Map));
            Assert.IsFalse(MinionManager.StandsOn(removed, other));
        }

        private static MapChannel Copy(MapChannel map) => new()
        {
            MapInfo = map.MapInfo,
            InstanceId = 2,
            IsPrivateInstance = true,
            ClientList = new List<Client>(),
            PlayerLimit = map.PlayerLimit
        };

        private static Creature Summon(MapChannel mapChannel, Client master)
        {
            var bot = new Creature
            {
                Name = "Spotter",
                TargetCategory = TargetCategory.Friendly,
                MapContextId = mapChannel.MapInfo.MapContextId,
                Position = Vector3.Zero,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            bot.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);

            CellManager.Instance.AddToWorld(mapChannel, bot);
            MinionManager.Instance.Adopt(master, bot);
            WorldTestContext.Drain(master);
            return bot;
        }
    }
}
