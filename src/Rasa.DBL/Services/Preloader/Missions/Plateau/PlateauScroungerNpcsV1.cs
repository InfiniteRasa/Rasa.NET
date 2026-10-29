using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The three NPCs of Comic Relief (1251), Scavenger Hunt (1015) and The Stalk Market (1017) at
    /// AFS Camp Resistance, Plateau (map 1497). None had a creature row; the client has their names
    /// and the NPC packages of their mission lines.
    ///
    ///  - Corporal "Relay" O'Brien (creaturenamelanguage 8615): male, level 31, /loc -621, 441, -234
    ///    (TaRapedia NPC page). Gives and takes back Comic Relief; no objective conversation of his.
    ///  - Major Lulu Lonigan (8616, package 999: objectiveconversation 1251,1 and 1251,7): female,
    ///    level 33 (TaRapedia NPC page). Her page gives O'Brien's /loc exactly, a copy (ESTIMATE): she
    ///    stands 4 m from him, -618, -237.
    ///  - Private "Scrounger" Scarpelo (8617, package 1000: objectiveconversation 1251,10): male,
    ///    level 31, /loc -622, 442, -227 (TaRapedia NPC page). Gives Scavenger Hunt and The Stalk Market.
    ///  - Each stands at the floor under that place measured on the client's terrain and entity
    ///    collision meshes (Rasa.ClientData): open ground, nothing over them. Bosley (510176) stands
    ///    on a platform 6 m higher, 11 m to the west.
    ///  - Looks (no source describes them, ESTIMATE): O'Brien and Scarpelo as the soldiers of the
    ///    recon squads' commanders' outfit without helmet (NPC_Clothing_Soldier_1 torso, legs, boots,
    ///    gloves; redshirt pistol); Lonigan in the officer's uniform of Major Nicholson (510069),
    ///    female swapset, no weapon. 1000 hit points, no walking or running speed, as the other
    ///    named NPCs.
    ///
    /// Ids 750018-750020 (creature, pool), free before. Plain SQL, valid on SQLite and MySQL.
    /// Down takes out only these rows.
    /// </summary>
    public static class PlateauScroungerNpcsV1
    {
        public const uint MapContextId = 1497;
        public const uint MaleClass = 3846;             // NPC_Human_Swapset_Male
        public const uint FemaleClass = 3848;           // NPC_Human_Swapset_Female
        public const uint OBrien = 750018, Lonigan = 750019, Scarpelo = 750020;
        public const uint First = 750018, Last = 750020;
        public const uint LoniganPackage = 999, ScarpeloPackage = 1000;

        private const uint Caucasian = 4286690539;

        private sealed record Npc(uint Id, string Comment, uint Class, uint NameId, uint Level, double X, double Y, double Z,
            (uint Slot, uint Class, uint Color)[] Outfit);

        /// <summary>Soldier: (slot, class, color), the commanders' soldier colors (PlateauReconSquadsV1).</summary>
        private static (uint, uint, uint)[] Soldier(uint hair, uint face) => new (uint, uint, uint)[]
        {
            (2, 3810, 4281223774),      // NPC_Clothing_Soldier_1_Boots
            (3, 3986, 4281625422),      // NPC_Clothing_Soldier_1_Gloves
            (13, 6271, 1),              // Weapon_Human_Redshirt_Pistol_Physical
            (14, hair, 4278655809),
            (15, 3809, 4281625422),     // NPC_Clothing_Soldier_1_Torso
            (16, 3811, 4281223774),     // NPC_Clothing_Soldier_1_Legs
            (17, face, Caucasian),
        };

        private static readonly Npc[] Npcs =
        {
            new(OBrien, "Corporal Relay OBrien - Plateau", MaleClass, 8615, 31, -621, 441.87, -234,
                Soldier(9355, 24011)),  // AvatarSwap_Hair_03, AvatarSwap_Face_Caucasian_Long_4
            new(Lonigan, "Major Lulu Lonigan - Plateau", FemaleClass, 8616, 33, -618, 441.87, -237, new (uint, uint, uint)[]
            {
                (2, 4021, 22120),       // NPC_Clothing_Officer_1_Boots (Major Nicholson's)
                (14, 9782, 4278655809), // AvatarSwap_Hair_06
                (15, 4023, 13933202),   // NPC_Clothing_Officer_1_Torso
                (16, 4022, 13933202),   // NPC_Clothing_Officer_1_Legs
                (17, 24009, Caucasian), // AvatarSwap_Face_Caucasian_Long_2
            }),
            new(Scarpelo, "Private Scrounger Scarpelo - Plateau", MaleClass, 8617, 31, -622, 442.37, -227,
                Soldier(3663, 7508)),   // hair as Field Op 750007, AvatarSwap_Face_Caucasian_Standard_2
        };

        private static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Lonigan, LoniganPackage, "Major Lulu Lonigan"),
            (Scarpelo, ScarpeloPackage, "Private Scrounger Scarpelo"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var npc in Npcs)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"VALUES ({npc.Id}, '{npc.Comment}', {npc.Class}, 1, {npc.Level}, 1000, {npc.NameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
                foreach (var (slot, cls, color) in npc.Outfit.OrderBy(slot => slot.Slot))
                    yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({npc.Id}, {slot}, {cls}, {color});";
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"VALUES ({npc.Id}, 0, 0, 20, {F(npc.X)}, {F(npc.Y)}, {F(npc.Z)}, 0, {MapContextId}, " +
                    $"{npc.Id}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            }

            foreach (var (creature, package, comment) in Packages)
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({creature}, {package}, '{comment}');";
        }

        private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM npc_package WHERE id BETWEEN {First} AND {Last};";
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {First} AND {Last};";
            yield return $"DELETE FROM creature_appearance WHERE id BETWEEN {First} AND {Last};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {First} AND {Last};";
        }
    }
}
