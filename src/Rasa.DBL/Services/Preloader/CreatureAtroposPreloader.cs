using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Atropos's chest blast and hand blast, which Wire_atropos_abilities adds.
    /// </summary>
    public class CreatureAtroposPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 67001, "Atropos (Linker boss) linker_chestblast", 263, 1, 1.0, 40.0, 20000, 7333, 135, 202, 1 };
            yield return new object[] { 67002, "Atropos (Linker boss) linker_hand_blast", 412, 2, 1.0, 40.0, 5000, 566, 50, 67, 1 };
        }
    }
}
