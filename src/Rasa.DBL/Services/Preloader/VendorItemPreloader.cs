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
            yield return new object[] { 26, 13156 };
            yield return new object[] { 27, 13186 };
            yield return new object[] { 29, 145 };
            yield return new object[] { 28, 28 };
            yield return new object[] { 21, 13096 };
            yield return new object[] { 22, 28 };
            yield return new object[] { 22, 26 };
            yield return new object[] { 23, 28 };
            yield return new object[] { 54, 13066 };
            yield return new object[] { 54, 13096 };
            yield return new object[] { 54, 28 };
            yield return new object[] { 62, 28 };
            yield return new object[] { 62, 145 };
            yield return new object[] { 64, 28 };
            yield return new object[] { 64, 13066 };
            yield return new object[] { 64, 13096 };
            yield return new object[] { 61, 145 };
            yield return new object[] { 61, 2229 };
            yield return new object[] { 61, 148 };
            yield return new object[] { 61, 28 };
            yield return new object[] { 61, 3738 };
            yield return new object[] { 61, 167 };
            yield return new object[] { 61, 11382 };
            yield return new object[] { 61, 4019 };
            yield return new object[] { 61, 3439 };
            yield return new object[] { 61, 3690 };
            yield return new object[] { 61, 3770 };
            yield return new object[] { 67, 145 };
            yield return new object[] { 25, 145 };
            yield return new object[] { 25, 148 };
            yield return new object[] { 25, 3057 };
            yield return new object[] { 25, 3061 };
            yield return new object[] { 25, 3256 };
            yield return new object[] { 25, 3315 };
            yield return new object[] { 25, 56 };
            yield return new object[] { 25, 28 };
            yield return new object[] { 25, 4017 };
            yield return new object[] { 25, 4019 };
            yield return new object[] { 31, 3869 };
            yield return new object[] { 31, 110841 };
            yield return new object[] { 31, 636 };
            yield return new object[] { 31, 110907 };
            yield return new object[] { 31, 45109 };
            yield return new object[] { 31, 56 };
            yield return new object[] { 71, 11567 };
            yield return new object[] { 71, 11568 };
            yield return new object[] { 71, 11569 };
            yield return new object[] { 71, 12303 };
            yield return new object[] { 71, 12486 };
            yield return new object[] { 71, 13077 };
            yield return new object[] { 71, 13107 };
            yield return new object[] { 71, 13137 };
            yield return new object[] { 71, 13167 };
            yield return new object[] { 71, 13197 };
            yield return new object[] { 72, 13066 };
            yield return new object[] { 72, 13096 };
            yield return new object[] { 72, 13126 };
            yield return new object[] { 72, 13186 };
            yield return new object[] { 72, 11561 };
            yield return new object[] { 72, 11564 };
            yield return new object[] { 72, 11565 };
            yield return new object[] { 72, 12446 };
            yield return new object[] { 72, 13156 };
            yield return new object[] { 72, 12267 };
            yield return new object[] { 25, 2229 };
            yield return new object[] { 25, 4018 };
            yield return new object[] { 25, 4019 };
            yield return new object[] { 73, 56 };
            yield return new object[] { 73, 110894 };
            yield return new object[] { 73, 32 };
            yield return new object[] { 73, 110898 };
            yield return new object[] { 73, 28 };
            yield return new object[] { 73, 110903 };
            yield return new object[] { 73, 638 };
            yield return new object[] { 73, 45106 };
            yield return new object[] { 73, 1766 };
            yield return new object[] { 73, 42272 };
            yield return new object[] { 73, 636 };
            yield return new object[] { 73, 110907 };
            yield return new object[] { 73, 30 };
            yield return new object[] { 73, 110911 };
            yield return new object[] { 63, 56 };
            yield return new object[] { 63, 110894 };
            yield return new object[] { 63, 32 };
            yield return new object[] { 63, 110898 };
            yield return new object[] { 63, 28 };
            yield return new object[] { 63, 110903 };
            yield return new object[] { 63, 638 };
            yield return new object[] { 63, 45106 };
            yield return new object[] { 63, 1766 };
            yield return new object[] { 63, 42272 };
            yield return new object[] { 63, 636 };
            yield return new object[] { 63, 110907 };
            yield return new object[] { 63, 30 };
            yield return new object[] { 63, 110911 };
            yield return new object[] { 74, 145 };
            yield return new object[] { 74, 148 };
            yield return new object[] { 74, 3057 };
            yield return new object[] { 74, 3256 };
            yield return new object[] { 74, 3061 };
            yield return new object[] { 74, 3315 };
            yield return new object[] { 74, 56 };
            yield return new object[] { 74, 28 };
            yield return new object[] { 74, 4017 };
            yield return new object[] { 74, 4019 };
            yield return new object[] { 74, 2229 };
            yield return new object[] { 31, 2229 };
        }
    }
}
