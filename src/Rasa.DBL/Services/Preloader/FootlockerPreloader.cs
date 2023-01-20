using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class FootlockerPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, FootlockerEntry.TableName, typeof(FootlockerEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, 21030, 1220, 756.3, 294.2, 379.3, 0, "Alia Das Footlocker" };
            yield return new object[] { 2, 21030, 1220, -91.5, 221.3, -529.5, 0, "Twin Pillars Footlocker" };
        }
    }
}
