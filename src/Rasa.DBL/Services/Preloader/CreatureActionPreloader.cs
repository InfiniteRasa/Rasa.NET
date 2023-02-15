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
            yield return new object[] { 3, "thrax kick", 1, 397, 1, 4, 6000, 333, 20, 22 };
            yield return new object[] { 4, "young boargar melee", 174, 10, 1, 4, 2500, 0, 10, 20 };
            yield return new object[] { 5, "forean spearman melee", 174, 11, 1, 6, 3000, 0, 10, 45 };
            yield return new object[] { 6, "forean spearman lighting", 1, 203, 5, 20, 2800, 0, 5, 30 };
            yield return new object[] { 7, "Supply Sergeant Maddrey Weapon Range Attack", 1, 133, 1, 5, 2000, 0, 8, 35 };
            yield return new object[] { 8, "Weapon - Emplacement_AFS_Turret_Mini", 1, 242, 0, 50, 400, 400, 10, 20 };
            yield return new object[] { 9, "bane_hunter_invasion range", 1, 244, 5, 20, 1500, 0, 10, 35 };
            yield return new object[] { 10, "bane_hunter_invasion melee", 174, 33, 1, 5, 1400, 333, 20, 45 };
            yield return new object[] { 11, "bane_amoeboid_invasion melee", 1, 1431, 1, 3, 2500, 0, 30, 60 };
            yield return new object[] { 12, "bane_amoeboid_invasion range", 1, 211, 3, 14, 2800, 1800, 20, 50 };
            yield return new object[] { 13, "NPC_Vehicle_AFS_Mech Minigun physical", 1, 224, 1.0, 20.5, 400, 0, 10, 15 };
            yield return new object[] { 14, "NPC_Vehicle_AFS_Mech Minigun laser", 149, 12, 1.0, 20.5, 1000, 0, 10, 15 };
            yield return new object[] { 15, "NPC_Vehicle_AFS_Mech Missiles Physical", 141, 17, 1.0, 40.0, 1000, 0, 20, 30 };
            yield return new object[] { 16, "Bane Caretaker", 1, 265, 0.5, 10.0, 800, 0, 10, 15 };
            yield return new object[] { 17, "WEAPON_ATTACK_FOREAN_SPEAR_RANGED", 1, 305, 1.0, 6.0, 3000, 0, 10, 45 };
            yield return new object[] { 18, "Hominis Machina", 1, 97, 0.5, 25.0, 500, 0, 10, 30 };
            yield return new object[] { 19, "THRAX_LIEUTENANT_V01", 1, 229, 5.0, 15.0, 500, 100, 15, 32 };
            yield return new object[] { 20, "THRAX_LIEUTENANT_V01", 1, 79, 1.0, 15.0, 800, 0, 10, 15 };
            yield return new object[] { 21, "Creature_Filcher", 174, 207, 1.0, 15.0, 800, 0, 16, 24 };
            yield return new object[] { 22, "Creature_Mox Melee Attack", 174, 13, 1.0, 15.0, 800, 0, 16, 24 };
            yield return new object[] { 23, "Creature_Mox Range Energy Attack", 1, 209, 1.0, 15.0, 800, 0, 16, 24 };
        }
    }
}
