using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class VendorItemPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, VendorItemEntry.TableName, typeof(VendorItemEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 23, 13066 };
            yield return new object[] { 24, 13096 };
            yield return new object[] { 25, 13126 };
            yield return new object[] { 26, 13156 };
            yield return new object[] { 27, 13186 };
            yield return new object[] { 29, 145 };
            yield return new object[] { 28, 97447 };
            yield return new object[] { 21, 13096 };
            yield return new object[] { 22, 28 };
            yield return new object[] { 22, 26 };
            yield return new object[] { 23, 28 };
            yield return new object[] { 25, 13096 };
            yield return new object[] { 54, 13066 };
            yield return new object[] { 54, 13096 };
            yield return new object[] { 54, 28 };
            yield return new object[] { 61, 28 };
            yield return new object[] { 62, 28 };
            yield return new object[] { 62, 145 };
            yield return new object[] { 63, 148 };
            yield return new object[] { 63, 28 };
            yield return new object[] { 63, 13066 };
            yield return new object[] { 63, 13096 };
            yield return new object[] { 64, 28 };
            yield return new object[] { 64, 13066 };
            yield return new object[] { 64, 13096 };
            yield return new object[] { 61, 145 };
            yield return new object[] { 61, 148 };
            yield return new object[] { 61, 3311 };
            yield return new object[] { 64, 3311 };
            yield return new object[] { 67, 3311 };
            yield return new object[] { 67, 148 };
            yield return new object[] { 67, 145 };
            yield return new object[] { 67, 28 };
        }
    }
}
