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
            yield return new object[] { 28, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 50, 2, 1, 0, 0, 0, 0, 0, 0, 2, 2, 1 };
            yield return new object[] { 145, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 148, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 3311, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 13066, 2, 1, 0, 0, 0, 0, 0, 0, 1, 30, 8 };
            yield return new object[] { 13096, 2, 1, 0, 0, 0, 0, 0, 0, 1, 20, 5 };
            yield return new object[] { 13126, 2, 1, 0, 0, 0, 0, 0, 0, 1, 40, 10 };
            yield return new object[] { 13156, 2, 1, 0, 0, 0, 0, 0, 0, 1, 50, 13 };
            yield return new object[] { 13186, 2, 1, 0, 0, 0, 0, 0, 0, 1, 60, 15 };
            yield return new object[] { 97447, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 56, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110894, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110896, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110897, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110902, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 32, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110898, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110899, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110901, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110900, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45031, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110903, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110904, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110905, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110906, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 638, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45106, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45107, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45108, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45109, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 1766, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 42272, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 42273, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 42274, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 42275, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 636, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110907, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110908, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110909, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110910, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 30, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110911, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110912, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110913, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 110914, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 1765, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45102, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45103, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45104, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 45105, 2, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 42410, 1, 1, 0, 0, 0, 0, 0, 0, 2, 1, 1 };
            yield return new object[] { 3738, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 167, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 11382, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 4019, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 3439, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 3690, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
            yield return new object[] { 3770, 2, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1 };
        }
    }
}
