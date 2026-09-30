using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Stats for the AFS turrets: body/mind/spirit 15, health as the creature row, armour 10 x level - 20.
    /// </summary>
    public class TurretCreatureStatsPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureStatEntry.TableName, typeof(CreatureStatEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 550001, 15, 15, 15, 35676, 420 };
            yield return new object[] { 550002, 15, 15, 15, 46266, 450 };
            yield return new object[] { 550003, 15, 15, 15, 30844, 450 };
            yield return new object[] { 550004, 15, 15, 15, 38905, 430 };
            yield return new object[] { 550005, 15, 15, 15, 25937, 430 };
            yield return new object[] { 550006, 15, 15, 15, 28284, 440 };
            yield return new object[] { 550007, 15, 15, 15, 42427, 440 };
            yield return new object[] { 550008, 15, 15, 15, 30000, 400 };
            yield return new object[] { 550009, 15, 15, 15, 27510, 390 };
            yield return new object[] { 550010, 15, 15, 15, 18340, 390 };
            yield return new object[] { 550011, 15, 15, 15, 25227, 380 };
            yield return new object[] { 550012, 15, 15, 15, 16818, 380 };
            yield return new object[] { 550013, 15, 15, 15, 811, 30 };
            yield return new object[] { 550014, 15, 15, 15, 40000, 480 };
            yield return new object[] { 550015, 15, 15, 15, 4460, 180 };
            yield return new object[] { 550016, 15, 15, 15, 8919, 260 };
            yield return new object[] { 550017, 15, 15, 15, 1928, 130 };
            yield return new object[] { 550018, 15, 15, 15, 2892, 130 };
            yield return new object[] { 550019, 15, 15, 15, 50454, 460 };
            yield return new object[] { 550020, 15, 15, 15, 33636, 460 };
            yield return new object[] { 550021, 15, 15, 15, 16818, 380 };
            yield return new object[] { 550022, 15, 15, 15, 14142, 360 };
            yield return new object[] { 550023, 15, 15, 15, 21213, 360 };
            yield return new object[] { 550024, 15, 15, 15, 12614, 300 };
            yield return new object[] { 550025, 15, 15, 15, 8409, 300 };
            yield return new object[] { 550026, 15, 15, 15, 10905, 330 };
            yield return new object[] { 550027, 15, 15, 15, 16358, 330 };
        }
    }
}
