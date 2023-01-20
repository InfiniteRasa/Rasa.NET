using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class ItemTemplateWeaponPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ItemTemplateWeaponEntry.TableName, typeof(ItemTemplateWeaponEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 145, 1, 1500, 1, 133, 0, 1, 1, 0, 1, 2, 15, 1, 800, 1, 800, 80, 25, 1, 80, 1, 1, 2 };
        }
    }
}
