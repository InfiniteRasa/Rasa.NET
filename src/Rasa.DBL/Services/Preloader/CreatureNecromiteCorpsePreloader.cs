using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The summoned Necromite's corpse explosion, which Wire_necromite_corpse_explosion adds.
    /// </summary>
    public class CreatureNecromiteCorpsePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 69001, "Necromite (Thrax's) necromite_corpse_explosion", 490, 1, 0.0, 40.0, 10000, 800, 1963, 2399, 1 };
        }
    }
}
