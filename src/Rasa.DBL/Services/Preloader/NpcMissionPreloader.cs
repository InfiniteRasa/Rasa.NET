using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class NpcMissionPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, NpcMissionEntry.TableName, typeof(NpcMissionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 321, 101, 100, 5, 1, 1, false, false, "Assemble With Lieutenant Perkins" };
            yield return new object[] { 429, 101, 100, 3, 2, 2, true, true, "River Recon" };
        }
    }
}
