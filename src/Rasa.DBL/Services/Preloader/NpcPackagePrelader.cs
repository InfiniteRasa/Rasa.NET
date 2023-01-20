using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class NpcPackagePrelader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, NpcPackageEntry.TableName, typeof(NpcPackageEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 100, 726, "test" };
            yield return new object[] { 101, 208, "test" };
        }
    }
}
