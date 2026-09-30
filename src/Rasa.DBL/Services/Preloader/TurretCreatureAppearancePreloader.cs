using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The AFS turrets' guns in the weapon slot, so the client has the weapon its attack fires.
    /// </summary>
    public class TurretCreatureAppearancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureAppearanceEntry.TableName, typeof(CreatureAppearanceEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 550001, 13, 4082, 1 };
            yield return new object[] { 550002, 13, 4082, 1 };
            yield return new object[] { 550003, 13, 21843, 1 };
            yield return new object[] { 550004, 13, 4082, 1 };
            yield return new object[] { 550005, 13, 21843, 1 };
            yield return new object[] { 550006, 13, 21843, 1 };
            yield return new object[] { 550007, 13, 4082, 1 };
            yield return new object[] { 550008, 13, 4082, 1 };
            yield return new object[] { 550009, 13, 4082, 1 };
            yield return new object[] { 550010, 13, 21843, 1 };
            yield return new object[] { 550011, 13, 4082, 1 };
            yield return new object[] { 550012, 13, 21843, 1 };
            yield return new object[] { 550013, 13, 21843, 1 };
            yield return new object[] { 550014, 13, 21843, 1 };
            yield return new object[] { 550015, 13, 4082, 1 };
            yield return new object[] { 550016, 13, 4082, 1 };
            yield return new object[] { 550017, 13, 21843, 1 };
            yield return new object[] { 550018, 13, 4082, 1 };
            yield return new object[] { 550019, 13, 4082, 1 };
            yield return new object[] { 550020, 13, 21843, 1 };
            yield return new object[] { 550021, 13, 21843, 1 };
            yield return new object[] { 550022, 13, 21843, 1 };
            yield return new object[] { 550023, 13, 4082, 1 };
            yield return new object[] { 550024, 13, 4082, 1 };
            yield return new object[] { 550025, 13, 21843, 1 };
            yield return new object[] { 550026, 13, 21843, 1 };
            yield return new object[] { 550027, 13, 4082, 1 };
        }
    }
}
