using System.Collections.Generic;
using Rasa.Missions.Scenes;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 3 (Harvesters) NPC and places, Valverde Marshes (map 1454): Lieutenant Brazos at Paludos, who gives
    /// Beware: Buried Gas Lines (913), and the seven gas harvester places in the swamp east of Falcon Hold that Reaping the
    /// Harvest (881) samples and Hassling the Harvesters (890) destroys. The server had no Brazos.
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760312-760399 free): 760312 Brazos (creature, creature_appearance, npc_package, spawn pool). No new creature pool: the
    /// set's other missions use upstream NPCs (Vetter 510165, Kline 510166, Taavik 510164) and the upstream bosses.
    ///
    /// Sourced: client name 8595 "Lieutenant Brazos" (creaturenamelanguage; the client has no objective conversation at him,
    /// 913 goes by giver and receiver). TaRapedia Beware: Buried Gas Lines: Brazos at Paludos, no /loc. Ten Ton Hammer Falcon
    /// Hold: the gas harvesters are "all throughout the watery swamp area to the east" of Falcon Hold, "pick any five".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Brazos is a copy of Lieutenant Morrison's row and look (510162, the server's other AFS lieutenant in the Marshes:
    ///    class 3846, level 10, 1000 hit points, his appearance slots), with Brazos's name; a server-only package (his id), as
    ///    the triage's rule for new NPCs (PoolsSpyNpcsV1's Sheen, MarshesFalconHoldNpcsV1's Horsh). He stands by the Paludos
    ///    med tent (-310, 125), the triage's place; height from the navmesh (probe of 2026-11-02).
    ///  - Seven harvester places 40-60 m apart in the swamp east of Falcon Hold (the client map has no harvester; Ten Ton
    ///    Hammer's "swamp to the east"), each on the navmesh and walked to from the Falcon Hold waypoint (probe of
    ///    2026-11-02). 890 uses the first five.
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); down takes out only these rows.
    /// </summary>
    public static class MarshesHarvestersNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;

        public const uint Brazos = 760312, BrazosName = 8595, BrazosPackage = 760312;
        public const uint Morrison = 510162;            // Lieutenant Morrison, Falcon Hold: Brazos's row and look
        public static readonly (double X, double Y, double Z) BrazosAt = (-310, 218.48, 125);

        /// <summary>The seven gas harvester places east of Falcon Hold (navmesh heights).</summary>
        public static readonly ScenePosition[] Harvesters =
        {
            new(-20f, 214.32f, 600f), new(40f, 214.08f, 580f), new(100f, 213.88f, 600f), new(0f, 214.08f, 640f),
            new(60f, 214.52f, 680f), new(-40f, 213.88f, 700f), new(40f, 213.88f, 720f)
        };

        /// <summary>The middle of the harvester field and a radius that holds all seven (the marker of 881 and 890).</summary>
        public static readonly (double X, double Y, double Z) FieldCentre = (26, 214, 646);
        public const double FieldRadius = 95;

        public static IEnumerable<string> Up()
        {
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Brazos}, 'Lieutenant Brazos - Paludos (913)', class_id, faction, level, max_hp, " +
                         $"{BrazosName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 " +
                         $"FROM creature WHERE id = {Morrison};";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Brazos}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Morrison};";
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Brazos}, {BrazosPackage}, 'Lieutenant Brazos - Paludos');";
            yield return Pool(Brazos, BrazosAt.X, BrazosAt.Y, BrazosAt.Z, MapContextId, Brazos, 1, 1, 20, 0);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id = {Brazos};";
            yield return $"DELETE FROM npc_package WHERE id = {Brazos};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Brazos};";
            yield return $"DELETE FROM creature WHERE id = {Brazos};";
        }
    }
}
