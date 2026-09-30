using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The summoned Necromite's weapon in the weapon slot (Weapon_Creature_Necromite), so the
    /// client has the weapon its bite plays.
    /// </summary>
    public class CreatureNecromiteAppearancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureAppearanceEntry.TableName, typeof(CreatureAppearanceEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 570003, 13, 7077, 1 };
        }
    }
}
