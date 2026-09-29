using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// What a Thrax Grenadier boss summons (CreatureSummons): an Ability_Bane_Necromite at level 42,
    /// scaled to its summoner's when it comes. No spawn pool has it; no name id - the client
    /// names it from the class.
    /// </summary>
    public class CreatureNecromitePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, typeof(CreatureEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 570003, "Necromite (Thrax's)", 7528, 0, 42, 3750, 0, 9, 5, 68094, 68095, 0, 0, 0, 0, 0, 0 };
        }
    }
}
