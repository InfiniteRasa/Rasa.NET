using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Supply Chief Riggins and Dr. Hester at the Snake Pit, Valverde Pools (map 1304), for Building Materials (1314) and
    /// Medicinal Measures (1066). The server had the Snake Pit's vendors and Colonel "Snake" Washington, but not them.
    ///
    /// Sourced: client names 9385 "Supply Chief Riggins" and 8865 "Dr. Hester" (creaturenamelanguage); Hester's package
    /// 1129 (objectiveconversation 1328,2). Places: Riggins IGN TR Vault NPC 16 (-964.6, 915.7, 403.5), TaRapedia
    /// (-967, 915, 400); Hester TaRapedia (-961, 919, 469). Level 40, AFS, male: the TaRapedia NPC pages of both.
    /// Both points are on the navmesh's main region (0.2 m and 0.4 m above it).
    ///
    /// Reconstructed (ESTIMATE): the officer uniform that all of the Snake Pit's NPCs wear on the server (vendors Brooks,
    /// Maisha and Lane, and Snake); Riggins's package a server-only id (the client has none for him, as
    /// PlateauTheseOldBonesV1's Algona); the height of each place from the navmesh.
    ///
    /// Ids: creatures and pools 760200 and 760201, free before. Plain SQL, valid on SQLite and MySQL; down takes out
    /// only these rows.
    /// </summary>
    public static class PoolsSnakePitNpcsV1
    {
        public const uint MapContextId = PoolsMissionDataV1.MapContextId;
        public const uint MaleClass = Plateau.PlateauMaligoBaseV1.MaleClass;
        public const uint Level = 40;

        public const uint Riggins = 760200, RigginsName = 9385, RigginsPackage = 760200;
        public const uint Hester = 760201, HesterName = 8865, HesterPackage = 1129;

        public static readonly (double X, double Y, double Z) RigginsAt = (-964.6, 915.9, 403.5);
        public static readonly (double X, double Y, double Z) HesterAt = (-961, 919.4, 469);

        /// <summary>The Snake Pit's officer uniform (slot, class, color), as PlateauSnakePitV1's Snake wears.</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] Outfit =
        {
            (2, 4021, 4294934528),      // NPC_Clothing_Officer_1_Boots
            (13, 6271, 1),              // Weapon_Human_Redshirt_Pistol_Physical
            (14, 9781, 4278655809),     // AvatarSwap hair
            (15, 4023, 4294934528),     // NPC_Clothing_Officer_1_Torso
            (16, 4022, 4294934528),     // NPC_Clothing_Officer_1_Legs
            (17, 24008, 4286690539),    // AvatarSwap face, Caucasian
        };

        public static IEnumerable<string> Up()
        {
            foreach (var sql in Npc(Riggins, "Supply Chief Riggins - The Snake Pit", RigginsName, RigginsPackage, RigginsAt))
                yield return sql;
            foreach (var sql in Npc(Hester, "Dr. Hester - The Snake Pit", HesterName, HesterPackage, HesterAt))
                yield return sql;
        }

        public static IEnumerable<string> Down()
        {
            foreach (var id in new[] { Hester, Riggins })
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        /// <summary>One NPC of the Snake Pit in its uniform, as PoolsSnakePitContactsV1 adds more.</summary>
        internal static IEnumerable<string> Npc(uint id, string comment, uint name, uint package, (double X, double Y, double Z) at)
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({id}, '{comment}', {MaleClass}, 1, {Level}, 1000, {name}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            foreach (var (slot, cls, color) in Outfit)
                yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({id}, {slot}, {cls}, {color});";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment}');";
            yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
        }
    }
}
