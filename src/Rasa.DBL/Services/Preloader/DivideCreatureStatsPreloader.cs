using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Stats for the Divide garrison rows: body/mind/spirit 15 as the boss rows have them, health as the
    /// creature row, armour 10*level-20 times the tier's share (minion 0.5, thug 0.75, lieutenant 1.0).
    /// </summary>
    public class DivideCreatureStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, typeof(CreatureStatEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 530001, 15, 15, 15, 723, 65 };
            yield return new object[] { 530002, 15, 15, 15, 723, 65 };
            yield return new object[] { 530003, 15, 15, 15, 1051, 105 };
            yield return new object[] { 530004, 15, 15, 15, 1051, 105 };
            yield return new object[] { 530005, 15, 15, 15, 2188, 160 };
            yield return new object[] { 530006, 15, 15, 15, 1051, 105 };
            yield return new object[] { 530007, 15, 15, 15, 723, 65 };
            yield return new object[] { 530008, 15, 15, 15, 723, 65 };
            yield return new object[] { 530009, 15, 15, 15, 2188, 160 };
        }
    }
}
