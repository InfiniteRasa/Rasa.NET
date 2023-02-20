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
            yield return new object[] { 1, "Bane_Fithik_Winged", 4313, 0, 5, 1000, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 2, "Caretaker", 9244, 0, 20, 1000, 0, 9, 5, 16, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 3, "Thrax Soldier", 20757, 0, 11, 1000, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 4, "Human", 3846, 1, 4, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 5, "Human Red armor", 3915, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 6, "Human Female", 3848, 1, 6, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 7, "Human Female", 3848, 1, 7, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 8, "Hominis Machina", 3868, 1, 8, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
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
            yield return new object[] { 23, "Test Vendor 3", 3846, 1, 5, 555, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 24, "Test Vendor 4", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 25, "Test Vendor 5", 3846, 1, 5, 555, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 26, "Test Vendor 6", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 27, "Test Vendor 7", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 28, "Test Vendor 8", 3848, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 29, "Test Vendor 9", 3846, 1, 5, 555, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 30, "Clan Master", 3846, 1, 5, 120, 8838, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 31, "Medical Vendor Static", 20972, 1, 5, 555, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 32, "Weapons Vendor Static", 20972, 1, 5, 555, 10787, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 33, "Bane Amoeboid V1", 6032, 0, 6, 1000, 0, 9, 5, 12, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 34, "Bane Amoeboid V1 Boss", 10310, 0, 15, 1800, 0, 9, 5, 24, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 100, "Outpost Commander Rogers", 3846, 1, 5, 555, 2973, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 101, "Field Sgt. Witherspoon", 3846, 1, 25, 555, 3072, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 35, "Boargar General Spawn", 6031, 0, 2, 555, 0, 9, 5, 4, 25, 26, 0, 0, 0, 0, 0 };
            yield return new object[] { 36, "Bane Stalkerspawn General", 3781, 0, 8, 200, 0, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 37, "NPC_Forean_Spearman", 7034, 1, 7, 500, 0, 9, 5, 17, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 38, "Forean Elder Normal Static", 6163, 1, 9, 400, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 39, "Redshirt_Human_Soldier_Light_Male", 29423, 1, 8, 600, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 40, "NPC_Vehicle_AFS_Mech", 10235, 1, 9, 5000, 0, 9, 5, 13, 15, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 41, "Bane Hunter Invasion", 10166, 0, 8, 550, 0, 9, 5, 9, 10, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 42, "Council Elder Solis", 22636, 1, 10, 750, 3005, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 43, "Warrior Aprika", 22637, 1, 10, 900, 2969, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 44, "Young Forest Boargar", 6031, 0, 5, 400, 7803, 9, 5, 4, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 45, "Bane Hunter Invasion", 10166, 0, 6, 400, 0, 9, 5, 9, 10, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 46, "Redshirt_Human_Soldier_Heavy_Male", 29433, 1, 6, 700, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 47, "Bane Thrax Technician", 11320, 0, 5, 555, 0, 9, 5, 9, 10, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 48, "Bane Thrax Technician Boss", 10502, 0, 15, 1500, 8069, 0, 0, 29, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 49, "Bane Hunter Invasion Boss", 10327, 0, 15, 1800, 0, 0, 0, 9, 10, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 50, "NPC_Forean_Spearman", 7034, 1, 9, 500, 0, 9, 5, 17, 6, 5, 0, 0, 0, 0, 0 };
            yield return new object[] { 51, "NPC_Forean_Gunner", 6239, 1, 9, 500, 0, 9, 5, 27, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 52, "NPC_Forean_Shaman", 7035, 1, 9, 500, 3000, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 53, "NPC_Forean_Archer", 7036, 1, 9, 500, 0, 9, 5, 28, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 54, "NPC_Forean_Vendor_Shaman_Static", 22635, 1, 9, 500, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 55, "NPC_Forean_Vendor_Gunner_Static", 22636, 1, 9, 500, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 56, "NPC_Forean_Child_Static -No Mesh", 26705, 1, 9, 500, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 57, "Bane_Thrax_Grenadier", 9354, 0, 6, 555, 0, 9, 5, 19, 20, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 58, "NPC_Creature_Boargar", 10606, 0, 8, 555, 0, 9, 5, 4, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 59, "Creature_Mox", 6021, 0, 6, 555, 0, 9, 5, 22, 23, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 60, "Creature_Filcher", 6421, 0, 6, 555, 0, 9, 5, 21, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 61, "General Supply Vendor Lower Eloh Creek", 3848, 1, 6, 555, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 62, "General Supply Vendor Landing Zone", 3846, 1, 6, 555, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 63, "General Supply Vendor Twin Pillars", 3848, 1, 6, 555, 174, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 64, "General Supply Vendor Imperial Valley", 20972, 1, 6, 555, 175, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 65, "General Military Surplus Twin Pillars", 26868, 1, 6, 555, 10208, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 66, "General Military Surplus Twin Pillars", 26868, 1, 6, 555, 10207, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 67, "Prestige Supply Vendor Twin Pillars", 20972, 1, 6, 555, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 68, "Council Elder Solis", 22636, 1, 10, 750, 2970, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 69, "Council Elder Solis", 22636, 1, 10, 750, 2968, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 70, "Council Elder Solis", 22636, 1, 10, 750, 2981, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 71, "Armor Supply Vendor Twin Pillars", 20975, 1, 6, 555, 9869, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 72, "Armor Supply Vendor Alia Das", 20972, 1, 6, 555, 10788, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 73, "Ammo Supply Vendor Alia Das", 20972, 1, 6, 555, 10790, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 74, "Weapon Supply Twin Pillars", 20972, 1, 6, 555, 10789, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 75, "NPC_Bane_Thrax_Grenadier_Boss", 10612, 0, 15, 2000, 6716, 1, 1, 19, 20, 0, 0, 0, 0, 0, 0 };
        }
    }
}
