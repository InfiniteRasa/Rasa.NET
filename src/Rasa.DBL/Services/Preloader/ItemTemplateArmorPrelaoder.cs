using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class ItemTemplateArmorPrelaoder : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ItemTemplateArmorEntry.TableName, typeof(ItemTemplateArmorEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 13066, 35 };
            yield return new object[] { 13096, 23 };
            yield return new object[] { 13126, 59 };
            yield return new object[] { 13156, 70 };
            yield return new object[] { 13186, 0 };

        }
    }
}
