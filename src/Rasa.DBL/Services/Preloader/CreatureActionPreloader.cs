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
            yield return new object[] { 1, "melee attack thrax soldier", 1, 48, 0.5, 3.5, 1300, 0, 5, 12 };
            yield return new object[] { 2, "range attack afs light soldier", 1, 133, 1, 20, 800, 0, 10, 15 };
            yield return new object[] { 3, "thrax kick", 397, 1, 1, 4, 6000, 333, 20, 22 };
            yield return new object[] { 4, "young boargar melee", 1, 10, 1, 4, 2500, 0, 10, 20 };
            yield return new object[] { 5, "forean spearman melee", 174, 11, 1, 6, 3000, 0, 10, 45 };
            yield return new object[] { 6, "forean spearman lighting", 1, 194, 5, 20, 2800, 0, 5, 30 };
            yield return new object[] { 7, "Supply Sergeant Maddrey Weapon Range Attack", 1, 133, 1, 5, 2000, 0, 8, 35 };
            yield return new object[] { 8, "Weapon - Emplacement_AFS_Turret_Mini", 1, 242, 0, 50, 400, 400, 10, 20 };
            yield return new object[] { 9, "bane_hunter_invasion range", 1, 244, 5, 20, 1500, 0, 10, 35 };
            yield return new object[] { 10, "bane_hunter_invasion melee", 174, 33, 1, 5, 1400, 333, 20, 45 };
            yield return new object[] { 11, "bane_amoeboid_invasion melee", 174, 431, 1, 3, 2500, 0, 30, 60 };
            yield return new object[] { 12, "bane_amoeboid_invasion range", 1, 211, 3, 14, 2800, 1800, 20, 50 };
            yield return new object[] { 13, "NPC_Vehicle_AFS_Mech Minigun physical", 1, 224, 1.0, 20.5, 400, 0, 10, 15 };
            yield return new object[] { 14, "NPC_Vehicle_AFS_Mech Minigun laser", 149, 12, 1.0, 20.5, 1000, 0, 10, 15 };
            yield return new object[] { 15, "NPC_Vehicle_AFS_Mech Missiles Physical", 141, 17, 1.0, 40.0, 1000, 0, 20, 30 };
            yield return new object[] { 16, "Bane Caretaker", 1, 265, 0.5, 10.0, 800, 0, 10, 15 };
            yield return new object[] { 17, "WEAPON_ATTACK_FOREAN_SPEAR_RANGED", 1, 305, 1.0, 6.0, 3000, 0, 10, 45 };
            yield return new object[] { 18, "Hominis Machina", 1, 97, 0.5, 25.0, 500, 0, 10, 30 };
            yield return new object[] { 19, "Bane_Thrax_Grenadier", 1, 229, 5.0, 15.0, 500, 100, 15, 32 };
            yield return new object[] { 20, "Bane_Thrax_Grenadier", 1, 79, 1.0, 15.0, 800, 0, 10, 15 };
            yield return new object[] { 21, "Creature_Filcher", 1, 207, 1.0, 15.0, 800, 0, 16, 24 };
            yield return new object[] { 22, "Creature_Mox Melee Attack", 1, 13, 1.0, 15.0, 800, 0, 16, 24 };
            yield return new object[] { 23, "Creature_Mox Range Energy Attack", 1, 209, 1.0, 15.0, 800, 0, 16, 24 };
            yield return new object[] { 24, "bane_amoeboid_invasion range vomit", 1, 431, 3.0, 14.0, 2800, 1800, 15, 35  };
            yield return new object[] { 25, "young boargar melee stun", 1, 182, 1.0, 10.0, 2500, 1800, 10, 20 };
            yield return new object[] { 26, "young boargar melee knockback", 1, 235, 1.0, 5.0, 2500, 1800, 10, 20 };
            yield return new object[] { 27, "NPC_Forean_Gunner", 1, 116, 1.0, 20.0, 2500, 0, 15, 28 };
            yield return new object[] { 28, "NPC_Forean_Archer", 1, 145, 1.0, 30.0, 2500, 0, 15, 28 };
            yield return new object[] { 29, "Bane_Thrax_Technician_Boss", 1, 1, 0.5, 30.0, 800, 0, 10, 15 };
            yield return new object[] { 30, "Proctor Fulgor Lightbender Boss", 1, 149, 0.5, 30.0, 800, 0, 15, 25 };
            yield return new object[] { 31, "Arioch Xanx Boss", 174, 24, 0.5, 30.0, 800, 0, 15, 25 };
            yield return new object[] { 32, "Atropos Linker Boss", 1, 203, 0.5, 30.0, 800, 0, 15, 25 };
            yield return new object[] { 33, "Horntail Thrax Boss", 1, 1, 0.5, 30.0, 800, 0, 10, 15 };
            yield return new object[] { 34, "Old Scratch Thrax Boss", 1, 79, 0.5, 30.0, 800, 0, 15, 25 };
            yield return new object[] { 35, "Bane Hunter Boss", 1, 244, 0.5, 30.0, 800, 0, 15, 25 };
            yield return new object[] { 36, "Bane Linker Boss", 1, 203, 0.5, 30.0, 800, 0, 15, 25 };
            yield return new object[] { 37, "Linker Chest Blast", 263, 1, 0.5, 30.0, 2500, 1800, 15, 25 };
            yield return new object[] { 38, "Linker Ground Blast", 264, 2, 0.5, 30.0, 2500, 1800, 15, 25 };
            yield return new object[] { 39, "Linker Channel Blast", 410, 1, 0.5, 30.0, 2500, 1800, 15, 25 };
            yield return new object[] { 40, "Linker Hand Blast", 412, 2, 0.5, 30.0, 2500, 1800, 15, 25 };
            yield return new object[] { 41, "Bane Thrax Overseer Rifle", 1, 79, 0.5, 30.0, 800, 0, 15, 25 };
            yield return new object[] { 42, "Bane Shield Drones", 1, 45, 0.5, 30.0, 800, 0, 15, 20 };
            yield return new object[] { 43, "Bane Miasma regular", 174, 12, 0.5, 10.0, 800, 0, 15, 20 };
            yield return new object[] { 44, "Fithik Hive Monarch Boss", 174, 34, 0.5, 10.0, 800, 0, 15, 20 };
        }
    }
}
