using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class CreatureStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, typeof(CreatureStatEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, 5, 4, 3, 155, 53 };
            yield return new object[] { 2, 17, 4, 15, 255, 60 };
            yield return new object[] { 3, 15, 15, 15, 120, 70 };
            yield return new object[] { 4, 15, 15, 15, 120, 70 };
            yield return new object[] { 5, 15, 15, 15, 120, 70 };
            yield return new object[] { 6, 15, 15, 15, 120, 70 };
            yield return new object[] { 7, 15, 15, 15, 120, 70 };
            yield return new object[] { 8, 15, 15, 15, 120, 70 };
            yield return new object[] { 9, 15, 15, 15, 120, 70 };
            yield return new object[] { 10, 15, 15, 15, 120, 70 };
            yield return new object[] { 11, 15, 15, 15, 120, 70 };
            yield return new object[] { 12, 15, 15, 15, 120, 70 };
            yield return new object[] { 13, 15, 15, 15, 120, 70 };
            yield return new object[] { 14, 15, 15, 15, 120, 70 };
            yield return new object[] { 15, 15, 15, 15, 120, 70 };
            yield return new object[] { 16, 15, 15, 15, 120, 70 };
            yield return new object[] { 17, 15, 15, 15, 120, 70 };
            yield return new object[] { 18, 15, 15, 15, 120, 70 };
            yield return new object[] { 19, 15, 15, 15, 120, 70 };
            yield return new object[] { 20, 15, 15, 15, 120, 70 };
            yield return new object[] { 21, 15, 15, 15, 120, 70 };
            yield return new object[] { 22, 15, 15, 15, 120, 70 };
            yield return new object[] { 23, 15, 15, 15, 120, 70 };
            yield return new object[] { 24, 15, 15, 15, 120, 70 };
            yield return new object[] { 25, 15, 15, 15, 120, 70 };
            yield return new object[] { 26, 15, 15, 15, 120, 70 };
            yield return new object[] { 27, 15, 15, 15, 120, 70 };
            yield return new object[] { 28, 15, 15, 15, 120, 70 };
            yield return new object[] { 29, 15, 15, 15, 120, 70 };
            yield return new object[] { 30, 15, 15, 15, 120, 70 };
            yield return new object[] { 100, 15, 15, 15, 120, 70 };
            yield return new object[] { 101, 15, 15, 15, 120, 70 };

        }
    }
}