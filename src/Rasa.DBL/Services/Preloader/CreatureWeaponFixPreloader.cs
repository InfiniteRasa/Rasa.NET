using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The weapon rows Fix_creature_weapons adds: each creature's own family weapon, with the
    /// damage, cooldown and near range of the row it replaces.
    /// </summary>
    public class CreatureWeaponFixPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, CreatureActionEntry.TableName, typeof(CreatureActionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            yield return new object[] { 70001, "Thrax Soldier 3 weapon 1/1", 1, 1, 1.0, 20.0, 800, 0, 10, 15, 6 };
            yield return new object[] { 70002, "Thrax Technician 47 weapon 1/296", 1, 296, 5.0, 20.0, 1500, 0, 10, 35, 5 };
            yield return new object[] { 70003, "Thrax Technician 47 weapon 174/46", 174, 46, 1.0, 5.0, 1400, 0, 20, 45, 1 };
            yield return new object[] { 70004, "Thraxus Machina 520001 weapon 1/250", 1, 250, 0.5, 40.0, 800, 0, 15, 25, 13 };
            yield return new object[] { 70005, "Thrax Grenadier boss 520008 weapon 1/229", 1, 229, 5.0, 15.0, 2500, 0, 15, 25, 2 };
            yield return new object[] { 70006, "Thrax Technician boss 520009 weapon 1/296", 1, 296, 0.5, 20.0, 800, 0, 15, 25, 5 };
            yield return new object[] { 70007, "Thrax Technician boss 520010 weapon 1/296", 1, 296, 0.5, 20.0, 800, 0, 15, 25, 5 };
            yield return new object[] { 70008, "Maw boss 520036 weapon 174/9", 174, 9, 0.5, 4.0, 1300, 0, 5, 12, 1 };
            yield return new object[] { 70009, "Thrax Technician boss 520037 weapon 1/296", 1, 296, 0.5, 20.0, 800, 0, 15, 25, 5 };
            yield return new object[] { 70010, "Thrax Technician boss 520041 weapon 1/296", 1, 296, 0.5, 20.0, 800, 0, 15, 25, 5 };
            yield return new object[] { 70011, "Thrax Technician boss 520042 weapon 1/296", 1, 296, 0.5, 20.0, 800, 0, 15, 25, 5 };
            yield return new object[] { 70012, "Thrax Technician boss 520045 weapon 1/296", 1, 296, 0.5, 20.0, 800, 0, 15, 25, 5 };
            yield return new object[] { 70013, "Thrax Technician boss 520052 weapon 1/296", 1, 296, 0.5, 20.0, 800, 0, 15, 25, 5 };
            yield return new object[] { 70014, "Thrax Grenadier boss 520061 weapon 1/229", 1, 229, 5.0, 15.0, 2500, 0, 15, 25, 2 };
            yield return new object[] { 70015, "Thrax Soldier 530001 weapon 1/1", 1, 1, 1.0, 20.0, 800, 0, 10, 15, 6 };
            yield return new object[] { 70016, "Thrax Technician 530003 weapon 1/296", 1, 296, 5.0, 20.0, 1500, 0, 10, 35, 5 };
            yield return new object[] { 70017, "Thrax Technician 530003 weapon 174/46", 174, 46, 1.0, 5.0, 1400, 0, 20, 45, 1 };
            yield return new object[] { 70018, "Thrax Technician 531003 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 480, 720, 5 };
            yield return new object[] { 70019, "Thrax Technician 531010 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 404, 605, 5 };
            yield return new object[] { 70020, "Thrax Technician 531020 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 523, 785, 5 };
            yield return new object[] { 70021, "Thrax Technician 531026 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 285, 428, 5 };
            yield return new object[] { 70022, "Thrax Technician 531031 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 571, 856, 5 };
            yield return new object[] { 70023, "Thrax Technician 531042 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 339, 509, 5 };
            yield return new object[] { 70024, "Thrax Technician 531047 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 240, 360, 5 };
            yield return new object[] { 70025, "Thrax Technician 531057 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 311, 467, 5 };
            yield return new object[] { 70026, "Thrax Technician 531065 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 101, 151, 5 };
            yield return new object[] { 70027, "Thrax Technician 531076 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 285, 428, 5 };
            yield return new object[] { 70028, "Thrax Technician 531084 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 143, 214, 5 };
            yield return new object[] { 70029, "Thrax Technician 531091 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 185, 278, 5 };
            yield return new object[] { 70030, "Thrax Technician 531103 weapon 1/296", 1, 296, 1.0, 20.0, 800, 0, 440, 660, 5 };
            yield return new object[] { 70031, "Thrax Soldier 560001 weapon 1/1", 1, 1, 1.0, 20.0, 800, 0, 10, 15, 6 };
        }
    }
}
