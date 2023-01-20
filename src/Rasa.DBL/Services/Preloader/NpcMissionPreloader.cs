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
            yield return new object[] { 429, 1, 100, 0, 0, "mission giver" };
            yield return new object[] { 429, 2, 101, 0, 0, "mission reciver" };
            yield return new object[] { 429, 3, 13096, 1, 0, "reward item" };
            yield return new object[] { 429, 0, 2, 430, 0, "test" };
            yield return new object[] { 429, 0, 3, 429, 0, "test" };
            yield return new object[] { 298, 1, 101, 0, 0, "mission reciver" };
            yield return new object[] { 298, 3, 28, 100, 0, "reward item" };
            yield return new object[] { 298, 3, 145, 1, 0, "reward item" };

        }
    }
}
