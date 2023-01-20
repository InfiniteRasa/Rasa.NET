using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class CreatureActionPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, "melee attack thrax soldier", 174, 46, 0.5, 3.5, 1300, 0, 5, 12 };
            yield return new object[] { 2, "range attack afs light soldier", 1, 133, 1, 20, 800, 0, 10, 15 };
            yield return new object[] { 3, "thrax kick", 397, 1, 1, 4, 6000, 333, 20, 22 };
            yield return new object[] { 4, "young boargar melee", 174, 10, 1, 4, 2500, 0, 10, 20 };
            yield return new object[] { 5, "forean spearman melee", 174, 11, 1, 6, 3000, 0, 10, 45 };
            yield return new object[] { 6, "forean spearman lighting", 203, 1, 5, 20, 2800, 0, 5, 30 };
            yield return new object[] { 7, "Supply Sergeant Maddrey Weapon Range Attack", 1, 133, 1, 5, 2000, 0, 8, 35 };
            yield return new object[] { 8, "Weapon - Emplacement_AFS_Turret_Mini", 1, 242, 0, 50, 400, 400, 10, 20 };
            yield return new object[] { 9, "bane_hunter_invasion range", 1, 244, 5, 20, 1500, 0, 10, 35 };
            yield return new object[] { 10, "bane_hunter_invasion melee", 174, 33, 1, 5, 1400, 333, 20, 45 };
            yield return new object[] { 11, "bane_amoeboid_invasion melee", 431, 1, 1, 3, 2500, 0, 30, 60 };
            yield return new object[] { 12, "bane_amoeboid_invasion range", 211, 1, 3, 14, 2800, 1800, 20, 50 };

        }
    }
}
