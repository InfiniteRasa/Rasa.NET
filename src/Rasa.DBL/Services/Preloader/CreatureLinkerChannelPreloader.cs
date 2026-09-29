using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The Linkers' channel rows Wire_linker_channel adds: CR_LINKER_CHANNEL 410/1, one per world
    /// Linker.
    /// </summary>
    public class CreatureLinkerChannelPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 66001, "Linker Abyss linker_channel", 410, 1, 0.0, 40.0, 20000, 5666, 0, 0, 1 };
            yield return new object[] { 66002, "Linker Ashen Desert linker_channel", 410, 1, 0.0, 40.0, 20000, 5666, 0, 0, 1 };
        }
    }
}
