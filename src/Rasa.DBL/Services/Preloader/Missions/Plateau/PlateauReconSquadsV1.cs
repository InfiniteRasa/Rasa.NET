using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The three AFS recon squads of Incommunicado (mission 939's follow-up 1040) in Martyr's Canyon
    /// and Maligo Creek, Plateau (map 1497): a commander and his Field Ops each. None had a creature
    /// row; the client has their names, the commanders' NPC packages and mission lines.
    ///
    ///  - Alpha Squad Commander Carvelle (creaturenamelanguage 8847, package 1116), Beta Squad
    ///    Commander Petrie (8848, package 1118), Delta Squad Commander Locke (8849, package 1117):
    ///    level 35, human, male, AFS (TaRapedia NPC pages). The missions spell him "Carville"; the
    ///    NPC is "Carvelle" (client name; TaRapedia notes the mismatch).
    ///  - Each stands at his TaRapedia location, at the floor under it measured on the client's
    ///    terrain and entity collision meshes (Rasa.ClientData): Carvelle -432, 320.36, -797;
    ///    Petrie -147, 408.12, -882; Locke 164, 322.36, -771. Open ground, nothing over them.
    ///  - Recon Squad Alpha/Beta/Delta Field Op (8844, 8845, 8846): "The Commanders and their Recon
    ///    Squads are on patrol" (mission log 5824). Three, two and three of them, level 33, 3.5 m
    ///    round their commander, each at its measured floor. Count and level are reconstruction.
    ///  - Looks (no source describes them): the commanders as Captain Spaulding (640003) - soldier
    ///    torso, gloves and boots, helmet, officer legs, redshirt pistol; the Field Ops as AFS
    ///    Soldier Light (97) - heavy armor with helmet, redshirt pistol. Each has a face and hair of
    ///    his own. 1000 hit points, no walking or running speed, as the other named NPCs.
    ///
    /// Ids 750001-750011 (creature, pool), free before. Plain SQL, valid on SQLite and MySQL:
    /// no model maps these tables. Down takes out only these rows.
    /// </summary>
    public static class PlateauReconSquadsV1
    {
        public const uint MapContextId = 1497;
        public const uint NpcClass = 3846;              // NPC_Human_Swapset_Male
        public const uint Carvelle = 750001, Petrie = 750002, Locke = 750003;
        public const uint First = 750001, Last = 750011;
        public const uint CarvellePackage = 1116, PetriePackage = 1118, LockePackage = 1117;

        private const uint Caucasian = 4286690539, Asian = 4286886614;

        private sealed record Npc(uint Id, string Comment, uint NameId, uint Level, double X, double Y, double Z,
            bool Commander, uint Face, uint FaceColor, uint Hair);

        private static readonly Npc[] Npcs =
        {
            new(Carvelle, "Alpha Squad Commander Carvelle - Plateau", 8847, 35, -432, 320.36, -797, true, 24033, Caucasian, 25255),
            new(Petrie, "Beta Squad Commander Petrie - Plateau", 8848, 35, -147, 408.12, -882, true, 24009, Caucasian, 9355),
            new(Locke, "Delta Squad Commander Locke - Plateau", 8849, 35, 164, 322.36, -771, true, 24021, Caucasian, 3812),
            new(750004, "Recon Squad Alpha Field Op - Plateau", 8844, 33, -428.97, 320.36, -795.25, false, 24008, Caucasian, 9781),
            new(750005, "Recon Squad Alpha Field Op - Plateau", 8844, 33, -432.0, 320.36, -800.5, false, 24016, Asian, 9783),
            new(750006, "Recon Squad Alpha Field Op - Plateau", 8844, 33, -435.03, 320.61, -795.25, false, 24020, Caucasian, 9784),
            new(750007, "Recon Squad Beta Field Op - Plateau", 8845, 33, -143.97, 408.12, -883.75, false, 24010, Caucasian, 3663),
            new(750008, "Recon Squad Beta Field Op - Plateau", 8845, 33, -150.03, 408.11, -883.75, false, 20824, Asian, 3672),
            new(750009, "Recon Squad Delta Field Op - Plateau", 8846, 33, 167.03, 322.37, -769.25, false, 24011, Caucasian, 25256),
            new(750010, "Recon Squad Delta Field Op - Plateau", 8846, 33, 164.0, 322.36, -774.5, false, 3813, Caucasian, 25257),
            new(750011, "Recon Squad Delta Field Op - Plateau", 8846, 33, 160.97, 322.36, -769.25, false, 7508, Caucasian, 9356),
        };

        /// <summary>Captain Spaulding's (640003) outfit: (slot, class, color).</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] CommanderOutfit =
        {
            (1, 4008, 4281625422),      // Clothing_Helmet_3
            (2, 3810, 4281223774),      // NPC_Clothing_Soldier_1_Boots
            (3, 3986, 4281625422),      // NPC_Clothing_Soldier_1_Gloves
            (13, 6271, 1),              // Weapon_Human_Redshirt_Pistol_Physical
            (15, 3809, 4281625422),     // NPC_Clothing_Soldier_1_Torso
            (16, 4022, 4281223774),     // NPC_Clothing_Officer_1_Legs
        };

        /// <summary>AFS Soldier Light's (97) outfit: (slot, class, color).</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] FieldOpOutfit =
        {
            (1, 9808, 4280052432),      // Clothing_Armor_Heavy_1_Helm
            (2, 4299, 4280052432),      // Clothing_Armor_Heavy_1_Boots
            (3, 4303, 4280052432),      // Clothing_Armor_Heavy_1_Gloves
            (13, 6271, 1),              // Weapon_Human_Redshirt_Pistol_Physical
            (15, 4301, 4280052432),     // Clothing_Armor_Heavy_1_Torso
            (16, 4302, 4280052432),     // Clothing_Armor_Heavy_1_Legs
        };

        private static readonly (uint Creature, uint Package, string Comment)[] Packages =
        {
            (Carvelle, CarvellePackage, "Alpha Squad Commander Carvelle"),
            (Petrie, PetriePackage, "Beta Squad Commander Petrie"),
            (Locke, LockePackage, "Delta Squad Commander Locke"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var npc in Npcs)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) " +
                    $"VALUES ({npc.Id}, '{npc.Comment}', {NpcClass}, 1, {npc.Level}, 1000, {npc.NameId}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";

                var outfit = (npc.Commander ? CommanderOutfit : FieldOpOutfit)
                    .Concat(new[] { (14U, npc.Hair, 4278655809U), (17U, npc.Face, npc.FaceColor) })
                    .OrderBy(slot => slot.Item1);
                foreach (var (slot, cls, color) in outfit)
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
