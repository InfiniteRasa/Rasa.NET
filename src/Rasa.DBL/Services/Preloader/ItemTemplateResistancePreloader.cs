using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class ItemTemplateResistancePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ItemTemplateResistanceEntry.TableName, typeof(ItemTemplateResistanceEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 13066, 2, 2 };
            yield return new object[] { 13096, 3, 3 };
            yield return new object[] { 13126, 4, 4 };
            yield return new object[] { 13156, 5, 5 };
            yield return new object[] { 13156, 6, 6 };
            yield return new object[] { 13186, 7, 7 };
            yield return new object[] { 13186, 13, 13 };

        }
    }
}
