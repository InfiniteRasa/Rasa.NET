using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Database
{
    using Rasa.Repositories.World;

    /// <summary>
    /// .creatureappearance saves a slot of a creature's look in creature_appearance, a table with
    /// no key: Entity Framework could neither update nor add a row of it, and every save threw
    /// (CreatureRepository.CreateOrUpdateAppearance).
    /// </summary>
    [TestClass]
    public class CreatureAppearanceRepositoryTests
    {
        private const uint Creature = 999999;

        [TestMethod]
        public void ASlotIsAddedOnceAndThenChangedInItsRow()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.GetService<IMigrator>().Migrate();
            var creatures = new CreatureRepository(database.Context);

            creatures.CreateOrUpdateAppearance(Creature, 13, 3782, 7);
            var row = creatures.GetCreatureAppearances(Creature).Single();
            Assert.AreEqual((13u, 3782u, 7u), (row.SlotId, row.ClassId, row.Color));

            creatures.CreateOrUpdateAppearance(Creature, 13, 3790, 9);
            row = creatures.GetCreatureAppearances(Creature).Single();
            Assert.AreEqual((13u, 3790u, 9u), (row.SlotId, row.ClassId, row.Color), "the same row, changed");

            creatures.CreateOrUpdateAppearance(Creature, 1, 10, 0);
            CollectionAssert.AreEquivalent(new[] { 1u, 13u },
                creatures.GetCreatureAppearances(Creature).Select(entry => entry.SlotId).ToArray(), "another slot is a row of its own");
        }
    }
}
