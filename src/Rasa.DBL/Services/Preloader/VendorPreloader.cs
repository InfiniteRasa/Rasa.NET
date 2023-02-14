using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class VendorPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, VendorEntry.TableName, typeof(VendorEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 21, 41 };
            yield return new object[] { 22, 15 };
            yield return new object[] { 23, 86 };
            yield return new object[] { 24, 128 };
            yield return new object[] { 25, 10 };
            yield return new object[] { 26, 141 };
            yield return new object[] { 27, 136 };
            yield return new object[] { 28, 137 };
            yield return new object[] { 29, 138 };
            yield return new object[] { 54, 80 };
            yield return new object[] { 61, 11 };
            yield return new object[] { 62, 12 };
            yield return new object[] { 63, 13 };
            yield return new object[] { 64, 81 };
            yield return new object[] { 67, 139 };
        }
    }
}
