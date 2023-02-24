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
            yield return new object[] { 38, "Council Elder Moawi", 6163, 1, 9, 400, 2970, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 39, "Redshirt_Human_Soldier_Light_Male", 29423, 1, 8, 600, 0, 9, 5, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 40, "NPC_Vehicle_AFS_Mech", 10235, 1, 9, 5000, 0, 9, 5, 13, 15, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 41, "Bane Hunter Invasion", 10166, 0, 8, 550, 0, 9, 5, 9, 10, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 42, "Council Elder Solis", 22636, 1, 10, 750, 3005, 9, 5, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 43, "Warrior Aprika", 22637, 1, 10, 900, 2969, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
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
            yield return new object[] { 69, "Council Elder Solis", 7035, 1, 10, 750, 2968, 6707, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 70, "Council Elder Solis", 7035, 1, 10, 750, 2981, 6708, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 71, "Armor Supply Vendor Twin Pillars", 20975, 1, 6, 555, 182, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 72, "Armor Supply Vendor Alia Das", 20972, 1, 6, 555, 184, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 73, "Ammo Supply Vendor Alia Das", 20972, 1, 6, 555, 10790, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 74, "Weapon Supply Twin Pillars", 20972, 1, 6, 555, 10789, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 75, "Archfiend Grenadier Boss", 10612, 0, 15, 2000, 6716, 1, 1, 19, 20, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 76, "Proctor Fulgor Lightbender Boss", 10857, 0, 15, 2000, 510, 0, 0, 30, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 77, "Arioch Xanx Boss", 10523, 0, 15, 2000, 476, 0, 0, 31, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 78, "Atropos Linker Boss", 10335, 0, 15, 2000, 512, 0, 0, 32, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 79, "Horntail Thrax Boss", 10321, 0, 15, 2000, 6719, 9, 0, 33, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 80, "Old Scratch Thrax Boss", 10504, 0, 15, 2000, 6717, 9, 0, 34, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 81, "Bane Hunter Boss", 10327, 0, 15, 2000, 508, 9, 0, 35, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 82, "Bane Thrax Overseer Glognar", 10504, 0, 6, 1000, 6751, 9, 0, 41, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 83, "Bane Thrax Overseer Phlegg", 10504, 0, 6, 1000, 9312, 9, 0, 41, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 84, "Bane Thrax Overseer Rankash", 10504, 0, 6, 1000, 9313, 9, 0, 41, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 85, "Bane Shield Drone", 7233, 0, 6, 750, 8122, 9, 5, 42, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 86, "Bane Linker Regular", 9751, 0, 7, 600, 419, 9, 5, 36, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 87, "Bane Xanx", 7510, 0, 7, 600, 465, 9, 5, 31, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 88, "Bane Miasma", 6170, 0, 7, 600, 427, 9, 5, 43, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 89, "Fithik Hive Monarch Boss", 10323, 0, 10, 1000, 375, 0, 0, 44, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 90, "Bane Thrax Overseer Graal", 10504, 0, 10, 1000, 6698, 0, 0, 41, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 91, "Forean Council Advisor Todae", 7034, 1, 10, 1000, 6708, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 92, "Forean Council Luminary Doyan", 22636, 1, 10, 1000, 6707, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 93, "Forean Council Elder Nula", 7036, 1, 10, 1000, 2968, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 94, "Dr. Eleanan Corman Ranja Gorge", 6340, 1, 10, 1000, 2975, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 95, "Dr. Samual Corman", 6339, 1, 10, 1000, 2978, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 96, "AFS Officer Burke", 6339, 1, 10, 1000, 6735, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 97, "AFS Soldier Light", 29423, 1, 8, 600, 3080, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 98, "NPC_Forean_Spearman Ranger Anjuhi", 7034, 1, 9, 500, 201, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 99, "NPC_Forean_Spearman Ranger Tirna", 7034, 1, 9, 500, 135, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 102, "Forean Apprentice Juvak", 7036, 1, 10, 1000, 6728, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 103, "Arms Supplier Oliver", 29423, 1, 8, 600, 3096, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 104, "Aviation Coordinator Wagner", 29423, 1, 8, 600, 3079, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 105, "AFS Corperal Mendelson", 29423, 1, 8, 600, 5424, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 106, "Council Elder Barhui", 7035, 1, 10, 750, 3090, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 107, "Dr. Fanja Corman Ranja Gorge", 6340, 1, 10, 1000, 202, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 108, "Dr. Gwen Duvall", 6340, 1, 10, 1000, 6738, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 109, "Dr. Samual Corman", 6339, 1, 10, 1000, 6888, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 110, "Dr. Ojy", 6340, 1, 10, 1000, 6946, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 111, "Dr. Soji", 6339, 1, 10, 1000, 6948, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 112, "Dying Forean", 7035, 1, 10, 1000, 9729, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 113, "Forean Elder Gadfly", 22636, 1, 10, 1000, 6720, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 114, "Forean Elder Quillas", 22636, 1, 10, 1000, 9518, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 115, "AFS Engineer Salter", 29423, 1, 8, 600, 4525, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 116, "AFS Information Specialist Savious", 6339, 1, 10, 1000, 3074, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 117, "AFS Engineer Salter", 29423, 1, 8, 600, 4525, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 118, "AFS Officer Lt Col Cimoch", 6339, 1, 10, 1000, 9639, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 119, "AFS Officer Lt. Cmdr Parsons", 6339, 1, 10, 1000, 5269, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 120, "AFS Officer Lt. Saviours", 29423, 1, 10, 1000, 3073, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 121, "AFS Officer Lt Wood", 6339, 1, 10, 1000, 3095, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 122, "AFS Officer Maj. Bonham", 6340, 1, 10, 1000, 3059, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 123, "Matthew Corman", 29423, 1, 10, 1000, 3088, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 124, "Medic Quincy Corman", 29423, 1, 10, 1000, 2979, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 125, "Medical Assistant Duncan", 29423, 1, 10, 1000, 3082, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 126, "Mining Coord Richards", 29423, 1, 10, 1000, 3077, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 127, "Mining Tech Maxwell", 29423, 1, 10, 1000, 3078, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 128, "Operations Chief Sten Corman", 29423, 1, 10, 1000, 2980, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 129, "AFS Outpost Commnader Taylor", 29423, 1, 10, 1000, 4836, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 130, "AFS Outpost Commnader Randolph", 29423, 1, 10, 1000, 3076, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 131, "AFS Private Moore", 29423, 1, 10, 1000, 3083, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 132, "AFS Quartermaster Caufield", 29423, 1, 10, 1000, 2992, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 133, "Receptive Liason Langermon", 29423, 1, 10, 1000, 10010, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 134, "Receptive Liason Standley", 29423, 1, 10, 1000, 10011, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 135, "AFS Recon Spec Jennings", 29423, 1, 10, 1000, 3075, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 136, "Rewards Verification Officer", 29423, 1, 10, 1000, 9685, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 137, "Scott Corman", 29423, 1, 10, 1000, 7032, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 138, "Surveyor Hugh Corman", 29423, 1, 10, 1000, 3093, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            yield return new object[] { 139, "Forean Tribal Leader Oingin", 7034, 1, 10, 1000, 3094, 0, 0, 17, 6, 5, 0, 0, 0, 0, 0 };
            yield return new object[] { 140, "Victor Corman", 29423, 1, 10, 1000, 3084, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
    }
}
