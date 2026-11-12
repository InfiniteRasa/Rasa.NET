using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 16 (the Bane Supply Depot) NPC, in the depot (squad instance 1451): C-6801 Copilot Ortega at the
    /// Survivor's Camp, Survivors' (664) receiver.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; MarshesPreoDasVillageNpcsV1 ends at 760381; checked 2026-11-03 against
    /// the World DB copy and the code of all worktrees: 760382-760399 free): 760382 Ortega (creature, creature_appearance,
    /// npc_package, pool). No teleporter.
    ///
    /// Sourced: client name 6483 "C-6801 Copilot Ortega" (creaturenamelanguage); package 542 (objectiveconversation 664,3 and
    /// 664,5). World DB: the depot's hospital 500260 "Survivor's Camp" (129.9, 125.6, 312.9), Lieutenant Morrison 510162 (the
    /// AFS officers' look). The client map: the dropship wreck ArchHumGenVehicleDropship (134.1, 133.3, 345.6).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3): Ortega a copy of Morrison's row and six slots at level 35, friendly, public,
    /// on the walked-to ground 7 m from the camp's hospital (navmesh probe of 2026-11-03). Resnik (6485) and Raven (no client
    /// name) are not added: no objective names them.
    /// </summary>
    public static class MarshesBaneSupplyDepotNpcsV1
    {
        public const uint MapContextId = 1451;          // adv_foreas_valverde_marshes_banesupplydepot, a squad instance
        public const uint Morrison = 510162;
        public const uint Ortega = 760382, OrtegaName = 6483, OrtegaPackage = 542, OrtegaLevel = 35;

        public static readonly (double X, double Y, double Z) OrtegaAt = (125, 127.01, 318);
        /// <summary>The depot's door on the Marshes (map_link 41) and the depot's arrival (map_link 39's place).</summary>
        public static readonly (double X, double Y, double Z) DoorAt = (451.56, 223.61, -581.94);

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Ortega}, 'C-6801 Copilot Ortega - Bane Supply Depot, the Survivor''s Camp (664)', class_id, 1, {OrtegaLevel}, " +
                         $"max_hp, {OrtegaName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {Morrison};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Ortega}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Morrison};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Ortega}, {OrtegaPackage}, 'C-6801 Copilot Ortega - Bane Supply Depot');";
            yield return Pool(Ortega, OrtegaAt.X, OrtegaAt.Y, OrtegaAt.Z, MapContextId, Ortega, 1, 1, 20, 0);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id = {Ortega};";
            yield return $"DELETE FROM npc_package WHERE id = {Ortega};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Ortega};";
            yield return $"DELETE FROM creature WHERE id = {Ortega};";
        }
    }
}
