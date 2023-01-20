using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    public class ExperienceForLevelPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ExperienceForLevelEntry.TableName, typeof(ExperienceForLevelEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, 0 };
            yield return new object[] { 2, 3000 };
            yield return new object[] { 3, 10500 };
            yield return new object[] { 4, 24000 };
            yield return new object[] { 5, 43000 };
            yield return new object[] { 6, 69500 };
            yield return new object[] { 7, 103500 };
            yield return new object[] { 8, 148500 };
            yield return new object[] { 9, 204000 };
            yield return new object[] { 10, 272500 };
            yield return new object[] { 11, 356000 };
            yield return new object[] { 12, 456500 };
            yield return new object[] { 13, 577000 };
            yield return new object[] { 14, 719500 };
            yield return new object[] { 15, 886500 };
            yield return new object[] { 16, 1082500 };
            yield return new object[] { 17, 1310500 };
            yield return new object[] { 18, 1575000 };
            yield return new object[] { 19, 1879500 };
            yield return new object[] { 20, 2230000 };
            yield return new object[] { 21, 2632500 };
            yield return new object[] { 22, 3091500 };
            yield return new object[] { 23, 3616500 };
            yield return new object[] { 24, 4212000 };
            yield return new object[] { 25, 4888500 };
            yield return new object[] { 26, 5654500 };
            yield return new object[] { 27, 6520500 };
            yield return new object[] { 28, 7497500 };
            yield return new object[] { 29, 8598500 };
            yield return new object[] { 30, 9837000 };
            yield return new object[] { 31, 11228000 };
            yield return new object[] { 32, 12788500 };
            yield return new object[] { 33, 14538500 };
            yield return new object[] { 34, 16496000 };
            yield return new object[] { 35, 18683000 };
            yield return new object[] { 36, 21125000 };
            yield return new object[] { 37, 23850000 };
            yield return new object[] { 38, 26887500 };
            yield return new object[] { 39, 30269000 };
            yield return new object[] { 40, 34031000 };
            yield return new object[] { 41, 38211000 };
            yield return new object[] { 42, 42853500 };
            yield return new object[] { 43, 48006500 };
            yield return new object[] { 44, 53721000 };
            yield return new object[] { 45, 60055000 };
            yield return new object[] { 46, 67069500 };
            yield return new object[] { 47, 74833500 };
            yield return new object[] { 48, 83421500 };
            yield return new object[] { 49, 92917500 };
            yield return new object[] { 50, 103410000 };
        }
    }
}
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    public class ExperienceForLevelPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ExperienceForLevelEntry.TableName, typeof(ExperienceForLevelEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, 0 };
            yield return new object[] { 2, 3000 };
            yield return new object[] { 3, 10500 };
            yield return new object[] { 4, 24000 };
            yield return new object[] { 5, 43000 };
            yield return new object[] { 6, 69500 };
            yield return new object[] { 7, 103500 };
            yield return new object[] { 8, 148500 };
            yield return new object[] { 9, 204000 };
            yield return new object[] { 10, 272500 };
            yield return new object[] { 11, 356000 };
            yield return new object[] { 12, 456500 };
            yield return new object[] { 13, 577000 };
            yield return new object[] { 14, 719500 };
            yield return new object[] { 15, 886500 };
            yield return new object[] { 16, 1082500 };
            yield return new object[] { 17, 1310500 };
            yield return new object[] { 18, 1575000 };
            yield return new object[] { 19, 1879500 };
            yield return new object[] { 20, 2230000 };
            yield return new object[] { 21, 2632500 };
            yield return new object[] { 22, 3091500 };
            yield return new object[] { 23, 3616500 };
            yield return new object[] { 24, 4212000 };
            yield return new object[] { 25, 4888500 };
            yield return new object[] { 26, 5654500 };
            yield return new object[] { 27, 6520500 };
            yield return new object[] { 28, 7497500 };
            yield return new object[] { 29, 8598500 };
            yield return new object[] { 30, 9837000 };
            yield return new object[] { 31, 11228000 };
            yield return new object[] { 32, 12788500 };
            yield return new object[] { 33, 14538500 };
            yield return new object[] { 34, 16496000 };
            yield return new object[] { 35, 18683000 };
            yield return new object[] { 36, 21125000 };
            yield return new object[] { 37, 23850000 };
            yield return new object[] { 38, 26887500 };
            yield return new object[] { 39, 30269000 };
            yield return new object[] { 40, 34031000 };
            yield return new object[] { 41, 38211000 };
            yield return new object[] { 42, 42853500 };
            yield return new object[] { 43, 48006500 };
            yield return new object[] { 44, 53721000 };
            yield return new object[] { 45, 60055000 };
            yield return new object[] { 46, 67069500 };
            yield return new object[] { 47, 74833500 };
            yield return new object[] { 48, 83421500 };
            yield return new object[] { 49, 92917500 };
            yield return new object[] { 50, 103410000 };
        }
    }
}