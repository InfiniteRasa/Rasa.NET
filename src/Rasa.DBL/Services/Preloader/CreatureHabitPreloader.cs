using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The habits Wire_creature_habits gives out, one row per (creature, habit): the client's CR_*
    /// action at the argument named for the creature's rank, its windup, a reuse, and no damage.
    /// </summary>
    public class CreatureHabitPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 63001, "Filcher 60 filcher_loot", 434, 1, 0.0, 0.0, 10000, 833, 0, 0, 1 };
            yield return new object[] { 63002, "Filcher 540017 filcher_loot", 434, 1, 0.0, 0.0, 10000, 833, 0, 0, 1 };
            yield return new object[] { 63003, "Filcher 540038 filcher_loot", 434, 1, 0.0, 0.0, 10000, 833, 0, 0, 1 };
            yield return new object[] { 63004, "Xanx 87 xanx_devour", 438, 1, 0.0, 3.0, 30000, 0, 0, 0, 1 };
            yield return new object[] { 63005, "Xanx 530008 xanx_devour", 438, 1, 0.0, 3.0, 30000, 0, 0, 0, 1 };
            yield return new object[] { 63006, "Xanx 540010 xanx_devour", 438, 1, 0.0, 3.0, 30000, 0, 0, 0, 1 };
            yield return new object[] { 63007, "Xanx 540020 xanx_devour", 438, 1, 0.0, 3.0, 30000, 0, 0, 0, 1 };
            yield return new object[] { 63008, "Xanx 540039 xanx_devour", 438, 1, 0.0, 3.0, 30000, 0, 0, 0, 1 };
            yield return new object[] { 63009, "Arioch (Xanx boss) 77 xanx_devour", 438, 2, 0.0, 3.0, 30000, 0, 0, 0, 1 };
            yield return new object[] { 63010, "Predator boss 520022 predator_scan", 425, 1, 0.0, 20.0, 8000, 0, 0, 0, 1 };
            yield return new object[] { 63011, "Predator boss 520057 predator_scan", 425, 1, 0.0, 20.0, 8000, 0, 0, 0, 1 };
        }
    }
}
