using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// AFS turrets, one row per (turret, zone), 27 rows: Emplacement_AFS_Turret_Standard (4064) and
    /// Emplacement_AFS_Turret_Mini (11302), FRIENDLY, at the zone band top, health 3x / 2x a same-level
    /// player's base health, no speed - an emplacement never moves (Emplacements). No name id: the
    /// client names them from the class, "AFS Turret" and "AFS Light Turret".
    /// </summary>
    public class TurretCreaturePreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureEntry.TableName, typeof(CreatureEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 550001, "AFS Turret - Ashen Desert", 4064, 1, 44, 35676, 0, 0, 0, 59001, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550002, "AFS Turret - Crucible", 4064, 1, 47, 46266, 0, 0, 0, 59002, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550003, "AFS Light Turret - Crucible", 11302, 1, 47, 30844, 0, 0, 0, 59003, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550004, "AFS Turret - Thunderhead", 4064, 1, 45, 38905, 0, 0, 0, 59004, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550005, "AFS Light Turret - Thunderhead", 11302, 1, 45, 25937, 0, 0, 0, 59005, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550006, "AFS Light Turret - Abyss", 11302, 1, 46, 28284, 0, 0, 0, 59006, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550007, "AFS Turret - Abyss", 4064, 1, 46, 42427, 0, 0, 0, 59007, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550008, "AFS Turret - Incline", 4064, 1, 42, 30000, 0, 0, 0, 59008, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550009, "AFS Turret - Mires", 4064, 1, 41, 27510, 0, 0, 0, 59009, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550010, "AFS Light Turret - Mires", 11302, 1, 41, 18340, 0, 0, 0, 59010, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550011, "AFS Turret - Plains", 4064, 1, 40, 25227, 0, 0, 0, 59011, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550012, "AFS Light Turret - Plains", 11302, 1, 40, 16818, 0, 0, 0, 59012, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550013, "AFS Light Turret - Bootcamp", 11302, 1, 5, 811, 0, 0, 0, 59013, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550014, "AFS Light Turret - Earth", 11302, 1, 50, 40000, 0, 0, 0, 59014, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550015, "AFS Turret - Divide", 4064, 1, 20, 4460, 0, 0, 0, 59015, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550016, "AFS Turret - Palisades", 4064, 1, 28, 8919, 0, 0, 0, 59016, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550017, "AFS Light Turret - Wilderness", 11302, 1, 15, 1928, 0, 0, 0, 59017, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550018, "AFS Turret - Wilderness", 4064, 1, 15, 2892, 0, 0, 0, 59018, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550019, "AFS Turret - Howling Maw", 4064, 1, 48, 50454, 0, 0, 0, 59019, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550020, "AFS Light Turret - Howling Maw", 11302, 1, 48, 33636, 0, 0, 0, 59020, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550021, "AFS Light Turret - Descent", 11302, 1, 40, 16818, 0, 0, 0, 59021, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550022, "AFS Light Turret - Marshes", 11302, 1, 38, 14142, 0, 0, 0, 59022, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550023, "AFS Turret - Marshes", 4064, 1, 38, 21213, 0, 0, 0, 59023, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550024, "AFS Turret - Plateau", 4064, 1, 32, 12614, 0, 0, 0, 59024, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550025, "AFS Light Turret - Plateau", 11302, 1, 32, 8409, 0, 0, 0, 59025, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550026, "AFS Light Turret - Pools", 11302, 1, 35, 10905, 0, 0, 0, 59026, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 550027, "AFS Turret - Pools", 4064, 1, 35, 16358, 0, 0, 0, 59027, 0, 0, 0, 0, 0, 0, 0 };
        }
    }
}
