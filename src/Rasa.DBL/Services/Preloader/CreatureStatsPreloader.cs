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
            yield return new object[] { 31, 15, 15, 15, 120, 70 };
            yield return new object[] { 32, 15, 15, 15, 120, 70 };
            yield return new object[] { 33, 15, 15, 15, 120, 70 };
            yield return new object[] { 34, 15, 15, 15, 500, 120 };
            yield return new object[] { 35, 15, 15, 15, 120, 70 };
            yield return new object[] { 36, 15, 15, 15, 120, 70 };
            yield return new object[] { 37, 15, 15, 15, 120, 70 };
            yield return new object[] { 38, 15, 15, 15, 120, 70 };
            yield return new object[] { 39, 15, 15, 15, 120, 70 };
            yield return new object[] { 40, 15, 15, 15, 250, 120 };
            yield return new object[] { 41, 15, 15, 15, 120, 70 };
            yield return new object[] { 42, 15, 15, 15, 120, 70 };
            yield return new object[] { 43, 15, 15, 15, 120, 70 };
            yield return new object[] { 44, 15, 15, 15, 120, 70 };
            yield return new object[] { 45, 15, 15, 15, 120, 70 };
            yield return new object[] { 46, 15, 15, 15, 120, 70 };
            yield return new object[] { 47, 15, 15, 15, 120, 70 };
            yield return new object[] { 48, 15, 15, 15, 500, 120 };
            yield return new object[] { 49, 15, 15, 15, 500, 120 };
            yield return new object[] { 50, 15, 15, 15, 120, 70 };
            yield return new object[] { 51, 15, 15, 15, 120, 70 };
            yield return new object[] { 52, 15, 15, 15, 120, 70 };
            yield return new object[] { 53, 15, 15, 15, 120, 70 };
            yield return new object[] { 54, 15, 15, 15, 120, 70 };
            yield return new object[] { 55, 15, 15, 15, 120, 70 };
            yield return new object[] { 56, 15, 15, 15, 120, 70 };
            yield return new object[] { 57, 15, 15, 15, 120, 70 };
            yield return new object[] { 58, 15, 15, 15, 120, 70 };
            yield return new object[] { 59, 15, 15, 15, 120, 70 };
            yield return new object[] { 60, 15, 15, 15, 120, 70 };
            yield return new object[] { 61, 15, 15, 15, 120, 70 };
            yield return new object[] { 62, 15, 15, 15, 120, 70 };
            yield return new object[] { 63, 15, 15, 15, 120, 70 };
            yield return new object[] { 64, 15, 15, 15, 120, 70 };
            yield return new object[] { 65, 15, 15, 15, 120, 70 };
            yield return new object[] { 66, 15, 15, 15, 120, 70 };
            yield return new object[] { 67, 15, 15, 15, 120, 70 };
            yield return new object[] { 100, 15, 15, 15, 120, 70 };
            yield return new object[] { 101, 15, 15, 15, 120, 70 };
        }
    }
}