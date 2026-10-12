using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;

    /// <summary>
    /// A Sapper's Crab Mines were cleared only by the worker of the map they were put down on,
    /// which runs while that map has players, and the three-mine limit counted the Sapper's mines
    /// on every map. Three mines left on a map that then emptied held the ability back everywhere
    /// until a relog or somebody walking onto that map (BR-174). Now the limit counts the mines on
    /// the Sapper's own map, and leaving a map takes theirs away.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrabMineLeaveTests
    {
        [TestMethod]
        public void LeavingTheMapTakesTheMinesAwayAndFreesTheLimit()
        {
            using var world = new WorldTestContext();
            world.AddClass(AbilityManager.CrabMineClass);
            var sapper = world.CreateClient();

            try
            {
                for (var i = 0; i < AbilityManager.MaxCrabMines; i++)
                    Deploy(world, sapper.Player);

                var mines = MinesOn(world.Map);

                Assert.AreEqual(AbilityManager.MaxCrabMines, mines.Count);
                Assert.AreEqual(AbilityManager.MaxCrabMines, AbilityManager.CrabMinesOf(sapper.Player));

                // On another copy of the map (or any other map) the ones left behind do not count.
                var copy = new MapChannel { MapInfo = world.Map.MapInfo, InstanceId = 2, IsPrivateInstance = true, ClientList = new List<Client>() };
                sapper.Player.MapChannel = copy;
                Assert.AreEqual(0, AbilityManager.CrabMinesOf(sapper.Player));
                sapper.Player.MapChannel = world.Map;

                // Leaving the map, by whatever route: the mines go with nothing to tick them.
                AbilityManager.PlayerLeaving(sapper.Player);

                Assert.AreEqual(0, AbilityManager.CrabMinesOf(sapper.Player));
                Assert.AreEqual(0, MinesOn(world.Map).Count, "out of the world");

                foreach (var mine in mines)
                {
                    Assert.IsFalse(AbilityManager.IsCrabMine(mine));
                    Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(mine.EntityId));
                }
            }
            finally
            {
                AbilityManager.PlayerLeaving(sapper.Player);
            }
        }

        [TestMethod]
        public void SomeoneElseLeavingLeavesTheMinesAlone()
        {
            using var world = new WorldTestContext();
            world.AddClass(AbilityManager.CrabMineClass);
            var sapper = world.CreateClient();
            var other = world.CreateClient(x: 5);

            try
            {
                Deploy(world, sapper.Player);

                AbilityManager.PlayerLeaving(other.Player);

                Assert.AreEqual(1, AbilityManager.CrabMinesOf(sapper.Player));
                Assert.AreEqual(1, MinesOn(world.Map).Count);
            }
            finally
            {
                AbilityManager.PlayerLeaving(sapper.Player);
            }
        }

        private static void Deploy(WorldTestContext world, Manifestation player)
        {
            var info = new ActionLevelInfo { ActionId = ActionId.AaSapperCrabMines, Level = 1 };

            typeof(AbilityManager).GetMethod("DeployCrabMine", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Abilities(), new object[] { world.Map, player, info, null });
        }

        private static List<Creature> MinesOn(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(creature => creature.EntityClass == AbilityManager.CrabMineClass)
                .Distinct()
                .ToList();

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });
    }
}
