using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The attacks Wire_boss_attacks gives the Strider, Predator and Howler bosses, one row per
    /// (creature, attack): the client's weapon pair or CR_* action, its windup, our reuse, and
    /// damage scaled to the boss's level.
    /// </summary>
    public class CreatureBossAttackPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 64001, "Daddy Long-Legs strider weapon 1/204", 1, 204, 1.0, 20.0, 1000, 0, 453, 679, 1 };
            yield return new object[] { 64002, "Daddy Long-Legs (Strider boss) strider_eye", 269, 1, 1.0, 40.0, 12000, 2766, 987, 1234, 4 };
            yield return new object[] { 64003, "Daddy Long-Legs (Strider boss) strider_laser_beam", 270, 1, 1.0, 40.0, 15000, 2733, 1110, 1357, 2 };
            yield return new object[] { 64004, "Daddy Long-Legs strider_ground_pulse", 473, 1, 0.0, 20.0, 20000, 2800, 1394, 1851, 7 };
            yield return new object[] { 64005, "Krammitron (Strider boss) strider weapon 1/204", 1, 204, 1.0, 20.0, 1000, 0, 349, 523, 1 };
            yield return new object[] { 64006, "Krammitron (Strider boss) strider_eye", 269, 1, 1.0, 40.0, 12000, 2766, 761, 951, 4 };
            yield return new object[] { 64007, "Krammitron (Strider boss) strider_laser_beam", 270, 1, 1.0, 40.0, 15000, 2733, 856, 1047, 2 };
            yield return new object[] { 64008, "Krammitron (Strider boss) strider_ground_pulse", 473, 1, 0.0, 20.0, 20000, 2800, 1075, 1427, 7 };
            yield return new object[] { 64009, "Alpha Class Recon Predator predator weapon 1/85", 1, 85, 1.0, 50.0, 3000, 0, 640, 960, 6 };
            yield return new object[] { 64010, "Alpha Class Recon Predator predator_missile", 426, 1, 1.0, 40.0, 8000, 0, 2181, 2844, 1 };
            yield return new object[] { 64011, "Iceram (Predator boss) predator weapon 1/85", 1, 85, 1.0, 50.0, 3000, 0, 349, 523, 6 };
            yield return new object[] { 64012, "Iceram (Predator boss) predator_missile", 426, 1, 1.0, 40.0, 8000, 0, 1189, 1551, 1 };
            yield return new object[] { 64013, "Kennilaxx (Howler boss) howler_shriek", 512, 1, 0.0, 15.0, 30000, 1000, 0, 0, 1 };
        }
    }
}
