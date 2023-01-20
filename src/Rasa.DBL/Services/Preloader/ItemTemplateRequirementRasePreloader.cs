using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class ItemTemplateRequirementRasePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ItemTemplateRequirementRaceEntry.TableName, typeof(ItemTemplateRequirementRaceEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 36, 1 };
            yield return new object[] { 39, 1 };
            yield return new object[] { 42, 1 };
            yield return new object[] { 61, 1 };
            yield return new object[] { 99, 1 };
            yield return new object[] { 609, 1 };
            yield return new object[] { 610, 1 };
            yield return new object[] { 611, 1 };
            yield return new object[] { 682, 1 };
            yield return new object[] { 683, 1 };
            yield return new object[] { 684, 1 };
            yield return new object[] { 1775, 1 };
            yield return new object[] { 1776, 1 };
            yield return new object[] { 1941, 1 };
            yield return new object[] { 1942, 1 };
            yield return new object[] { 1943, 1 };
            yield return new object[] { 1944, 1 };
            yield return new object[] { 2245, 1 };
            yield return new object[] { 42276, 1 };
            yield return new object[] { 42277, 1 };
            yield return new object[] { 49668, 1 };
            yield return new object[] { 49669, 1 };
            yield return new object[] { 49670, 1 };
            yield return new object[] { 49671, 1 };
            yield return new object[] { 49672, 1 };
            yield return new object[] { 49673, 1 };
            yield return new object[] { 49674, 1 };
            yield return new object[] { 49675, 1 };
            yield return new object[] { 49676, 1 };
            yield return new object[] { 49677, 1 };
            yield return new object[] { 49678, 1 };
            yield return new object[] { 49679, 1 };
            yield return new object[] { 49680, 1 };
            yield return new object[] { 49681, 1 };
            yield return new object[] { 49682, 1 };
            yield return new object[] { 49683, 1 };
            yield return new object[] { 49684, 1 };
            yield return new object[] { 49685, 1 };
            yield return new object[] { 49686, 1 };
            yield return new object[] { 49687, 1 };
            yield return new object[] { 49688, 1 };
            yield return new object[] { 49689, 1 };
            yield return new object[] { 49690, 1 };
            yield return new object[] { 49691, 1 };
            yield return new object[] { 49692, 1 };
            yield return new object[] { 49693, 1 };
            yield return new object[] { 49694, 1 };
            yield return new object[] { 49695, 1 };
            yield return new object[] { 49696, 1 };
            yield return new object[] { 49697, 1 };
            yield return new object[] { 49698, 1 };
            yield return new object[] { 49699, 1 };
            yield return new object[] { 49700, 1 };
            yield return new object[] { 49701, 1 };
            yield return new object[] { 49702, 1 };
            yield return new object[] { 49703, 1 };
            yield return new object[] { 50311, 3 };
            yield return new object[] { 50312, 2 };
            yield return new object[] { 50313, 4 };
            yield return new object[] { 50711, 1 };
            yield return new object[] { 50712, 1 };
            yield return new object[] { 50713, 1 };
            yield return new object[] { 118927, 1 };
            yield return new object[] { 118928, 1 };
            yield return new object[] { 118966, 3 };
            yield return new object[] { 118968, 3 };
            yield return new object[] { 118969, 2 };
            yield return new object[] { 118970, 2 };
            yield return new object[] { 118971, 4 };
            yield return new object[] { 118972, 4 };

        }
    }
}
