using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class ItemTemplatePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, ItemTemplateEntry.TableName, typeof(ItemTemplateEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 26, 2, 1, 0, 0, 0, 0, 0, 0, 2, 2, 1 };
            yield return new object[] { 28, 2, 1, 0, 0, 0, 0, 0, 0, 2, 2, 1 };
            yield return new object[] { 50, 2, 1, 0, 0, 0, 0, 0, 0, 2, 2, 1 };
            yield return new object[] { 145, 2, 1, 0, 0, 0, 0, 0, 0, 1, 100, 26 };
            yield return new object[] { 13066, 2, 1, 0, 0, 0, 0, 0, 0, 1, 30, 8 };
            yield return new object[] { 13096, 2, 1, 0, 0, 0, 0, 0, 0, 1, 20, 5 };
            yield return new object[] { 13126, 2, 1, 0, 0, 0, 0, 0, 0, 1, 40, 10 };
            yield return new object[] { 13156, 2, 1, 0, 0, 0, 0, 0, 0, 1, 50, 13 };
            yield return new object[] { 13186, 2, 1, 0, 0, 0, 0, 0, 0, 1, 60, 15 };
            yield return new object[] { 97447, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            

        }
    }
}
