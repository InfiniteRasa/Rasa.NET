using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 13 NPCs, Ligo Ashen Desert (map 1734), Indra Pass: Surveyor Malya Odesha (the giver and receiver of
    /// Missing Scientists 1280 and God of Storms 1281) and Researcher Endala Berish (the giver and receiver of The Bonds of Blood
    /// 1282), both new (the World DB has neither).
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 13 790480-790519): 790480 Odesha, 790481 Endala (creature,
    /// creature_appearance, creature_stat, npc_package, spawn pool each).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9355 "Surveyor Malya Odesha", 9354 "Researcher Endala Berish". Endala's
    ///    package 1475 (client objectiveconversation 1282,2); Odesha has no objective conversation in the client (she gives and
    ///    takes 1280 and 1281 by mission only).
    ///  - Places: TaRapedia Researcher Endala Berish: Brann, female, level 43, "in the cave directly outside of the Indra Caverns",
    ///    /loc 312, 324, 293 (0.5); Missing Scientists / God of Storms: Odesha at Indra Pass (0.5); the client's 1280 log "Return to
    ///    Surveyor Odesh in Indra Caves", 1282 "Return to Endala Berish in Indra Caves". World DB: the map link to Indra Caverns at
    ///    (321.87, 318.96, 319.60) r6 and the Indra Pass waypoint (262.19, 315.10, 344.38).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Odesha's package: a server-only one (her id, 790480), as set 11's Meeks (a design choice: the client has none for her).
    ///  - Both a copy of Mechanic Raji Lasat's row and look (Brann, class 7776; the World DB has no female Brann NPC look); level
    ///    43 (TaRapedia's Endala; Odesha the same). 4500 hit points and armour 300, friendly, standing still, rotation 0 (as set 1).
    ///  - Each spot outside the cave, at its mouth by the Indra Pass waypoint, not at TaRapedia's /loc in the cave: the client
    ///    collision files have no floor through the cave mouth (the terrain runs over the cave's floor from about x 277, z 337; the
    ///    client's terrain holes are not in them), so a walk from the waypoint to the cave cannot be checked (probe of 2026-11-10;
    ///    NOTES-upstream-bugs N26). Outside, each stands on the client's floor, one floor, under open sky, 7 m or more from the
    ///    waypoint, about 25 m from the cave mouth, walked to on the client's floor from the waypoint and back.
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows.
    /// </summary>
    public static class AshenIndraPassNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Odesha = 790480, OdeshaName = 9355, OdeshaPackage = 790480, OdeshaLevel = 43;
        public const uint Endala = 790481, EndalaName = 9354, EndalaPackage = 1475, EndalaLevel = 43;
        public const uint NextFreeId = 790482;

        /// <summary>The World DB row the new NPCs copy: Mechanic Raji Lasat (Brann, Ashoka Settlement).</summary>
        public const uint Raji = 510014;

        /// <summary>The Indra Pass waypoint (teleporter 198) and the map link to Indra Caverns (map_link 58), World DB.</summary>
        public static readonly (double X, double Y, double Z) Waypoint = (262.19140625, 315.09765625, 344.37890625);
        public static readonly (double X, double Y, double Z) CavernsLink = (321.8665, 318.9557, 319.6013);

        public static readonly (double X, double Y, double Z) OdeshaAt = (258, 315.49, 325);
        public static readonly (double X, double Y, double Z) EndalaAt = (266, 316.46, 337);

        /// <summary>The new NPCs: id, comment, npc_package comment (varchar(50)), name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, string PackageComment, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Odesha, "Surveyor Malya Odesha - Ashen Desert, Indra Pass (1280)", "Surveyor Malya Odesha (1280)", OdeshaName, OdeshaLevel, OdeshaPackage, OdeshaAt),
            (Endala, "Researcher Endala Berish - Ashen Desert, Indra Pass (1282)", "Researcher Endala Berish (1282)", EndalaName, EndalaLevel, EndalaPackage, EndalaAt)
        };

        public static (double X, double Y, double Z) At(uint pool) => New.Single(npc => npc.Id == pool).At;

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, packageComment, name, level, package, at) in New)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, 0, 0, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Raji};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Raji};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (id, _, _, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
