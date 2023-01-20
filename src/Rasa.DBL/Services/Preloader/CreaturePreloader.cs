using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    
    using Structures.World;

    public class CreaturePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, typeof(CreatureEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 1, "", 4313, 0, 5, 10000, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 2, "", 9244, 0, 20, 12000, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 3, "", 20757, 0, 11, 8000, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 4, "", 3846, 1, 4, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 5, "", 3915, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 6, "", 3848, 1, 6, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 7, "", 3848, 1, 7, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 8, "", 3868, 1, 8, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 9, "AFS_Turret_Mini", 11302, 1, 4, 1500, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 10, "Human Military Surplus", 26868, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 11, "Test Npc1", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 12, "Test Npc 2", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 13, "Test Npc 3", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 14, "Test Npc 4", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 15, "Test Npc 5", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 16, "Test Npc 6", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 17, "Test Npc 7", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 18, "Test Npc 8", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 19, "Test Npc 9", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 20, "Test Npc 10", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 21, "Test Vendor 1", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 22, "Test Vendor 2", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 23, "Test Vendor 3", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 24, "Test Vendor 4", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 25, "Test Vendor 5", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 26, "Test Vendor 6", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 27, "Test Vendor 7", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 28, "Test Vendor 8", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 29, "Test Vendor 9", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 30, "Clan Master", 3846, 1, 5, 120, 8838, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 100, "Outpost Commander Rogers", 3846, 1, 5, 555, 2973, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 101, "Field Sgt. Witherspoon", 3846, 1, 25, 555, 3072, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };

        }
    }
}
