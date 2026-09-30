using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The summoned turret's gun in the weapon slot (Weapon_Creature_Ability_Bane_Turret), so the
    /// client has the weapon its attack fires.
    /// </summary>
    public class CreatureSummonAppearancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureAppearanceEntry.TableName, typeof(CreatureAppearanceEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 570001, 13, 20508, 1 };
        }
    }
}
