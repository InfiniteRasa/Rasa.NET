using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 1 NPCs, Ligo Ashen Desert (map 1734): the new Sergeant Bernard Ames (The Dustiest Place on Earth,
    /// 1250), Lt. Colonel Trent Ivan (Good Day to Die, 1283), Lt. Colonel Dania Caldwell (You Can't Drink Ash, 1284) and
    /// Captain Akio Yamamoto (1283), and the upstream rows set 1 uses moved out of their placeholder spots (Colonel Mason
    /// Linier, Captain Flynn Harris, Captain Marcus Emmons, Captain Daris Linier).
    ///
    /// Id block (triage 2026-11-09: no row of the World DB and no constant of the code uses 783000-799999): the Ashen Desert
    /// missions take 790000-790999, set 1 790000-790039. Set 1 uses 790000-790003:
    ///  - 790000 Sergeant Bernard Ames (creature, creature_appearance, creature_stat, npc_package, spawn pool);
    ///  - 790001 Lt. Colonel Trent Ivan (the same rows);
    ///  - 790002 Lt. Colonel Dania Caldwell (the same rows);
    ///  - 790003 Captain Akio Yamamoto (the same rows).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9279 "Sergeant Bernard Ames", 9261 "Lt. Colonel Trent Ivan", 9262 "Lt.
    ///    Colonel Dania Caldwell", 9303 "Captain Akio Yamamoto". Packages (client objectiveconversation, 1.0): (1283,1) 1414
    ///    Yamamoto, (1250,1) 1389 Mason Linier, (1273,1) 1411 Emmons, (1284,1) 1476 Daris Linier (the upstream rows have them).
    ///    The client gives Ames, Ivan and Caldwell no objective conversation (their missions go by giver only).
    ///  - Places: Ames "Ashen Lookout" (TaRapedia and TR MMO DB, 0.5; the World DB waypoint Ashen Desert Lookout at 351.5, 311,
    ///    -822.1 and the client's camp statics round the communication tent at 338.5, 310.9, -822.9); Yamamoto "Boneyard
    ///    Outpost, in eastern tent, 686, 290, -495", level 49 (TaRapedia Captain Aiko Yamamoto, 0.5); Ivan, Caldwell and Mason
    ///    Linier "Shadow's Edge Post" (TaRapedia mission lists, 0.5; no coordinates); Emmons "Stonewall Ridge", Harris
    ///    "Ashoka Settlement", Daris Linier "White Oasis Post" (TaRapedia, 0.5).
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, triage U1, D7): Mason Linier stood on the Shadow's Edge weapon vendor
    /// with Ida Hayner (-163.4, 296.39, -709), Harris 4 m from the Ashoka waypoint, Emmons at the Stonewall Ridge region's
    /// centre (179.3, 316.37, -557.2) away from every tower, Daris Linier 3.68 m from the White Oasis control point's point.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Ames a copy of Harris' row and look (AFS class 3846), Ivan of Mason Linier's (3846), Caldwell of Ida Hayner's (the
    ///    female AFS class 3848), Yamamoto of Emmons' (3846); Ames, Ivan and Caldwell level 40 (no source: the missions'
    ///    level), Yamamoto 49 (TaRapedia). Ames, Ivan and Caldwell have server-only packages (their ids), as the Crucible's
    ///    Shandy. Friendly, no run or walk speed (as upstream givers).
    ///  - Facing: every new pool and every moved one has rotation 0 (no source, as the upstream rows).
    ///  - Hit points: each new NPC a creature_stat row as the Crucible's quest NPCs (4500 hit points, armour 300); max_hp 4500.
    ///  - Places, by the statics of the client map: Ames in the Lookout camp by the communication tent; Mason Linier at the
    ///    entrance of the Shadow's Edge HQ (the client's HQ holosign at -127.4, 278.5, -934 over the corridors under the post;
    ///    the entrance with its barricades at -132, 284, -875), Ivan in the east barracks (region 20614), Caldwell in the west
    ///    barracks (region 20615); Emmons by the Stonewall Command Tower (the Stonewall Ridge waypoint's tower, -61, 353, -401;
    ///    TaRapedia 1277 "Start with this tower here"); Harris 8 m from the Ashoka waypoint; Daris Linier 2.0 m from his upstream
    ///    spot (4.17 m from the control point's point). Each the nearest point to its source on the client's floor and the server navmesh, at least 2 m from a
    ///    wall, nothing over the head, seen from 16 of 16 places 2 m round it and from the end of the walk to it from its
    ///    area's waypoint, 7 m or more from every waypoint and local teleporter, 22 m or more outside every hostile pool's
    ///    circle (probe of 2026-11-09, client files and navmesh/adv_arieki_ligo_ashendesert). Yamamoto stands at TaRapedia's
    ///    loc as it is.
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows and puts the
    /// upstream rows back as they were.
    /// </summary>
    public static class AshenLeadInNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint HitPoints = 4500, Armor = 300, Friendly = 1;

        public const uint Ames = 790000, AmesName = 9279, AmesPackage = 790000, AmesLevel = 40;
        public const uint Ivan = 790001, IvanName = 9261, IvanPackage = 790001, IvanLevel = 40;
        public const uint Caldwell = 790002, CaldwellName = 9262, CaldwellPackage = 790002, CaldwellLevel = 40;
        public const uint Yamamoto = 790003, YamamotoName = 9303, YamamotoPackage = 1414, YamamotoLevel = 49;
        public const uint NextFreeId = 790004;

        /// <summary>The upstream rows set 1 uses (World DB): their packages, and the rows the new NPCs copy.</summary>
        public const uint MasonLinier = 510010, Harris = 510008, Emmons = 510009, DarisLinier = 510007, IdaHayner = 510011;
        public const uint MasonLinierPackage = 1389, HarrisPackage = 1412, EmmonsPackage = 1411, DarisLinierPackage = 1476;

        public static readonly (double X, double Y, double Z) AmesAt = (345, 311.14, -818);
        public static readonly (double X, double Y, double Z) IvanAt = (-97, 297.05, -792);
        public static readonly (double X, double Y, double Z) CaldwellAt = (-158, 297.05, -792);
        public static readonly (double X, double Y, double Z) YamamotoAt = (686, 289.93, -495);

        /// <summary>The upstream pools moved: pool, the place before (World DB), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (MasonLinier, (-163.39999389648438, 296.39, -709.0), (-132, 284.05, -875)),
            (Harris, (401.0, 280.45, -458.5), (405, 281.41, -459.5)),
            (Emmons, (179.3000030517578, 316.37, -557.2000122070312), (-62, 350.48, -414)),
            (DarisLinier, (-507.3999938964844, 282.29, 164.39999389648438), (-507.4, 282.73, 162.4))
        };

        /// <summary>Where an NPC of set 1 stands: a new one's place, or a moved upstream pool's new one.</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Ames => AmesAt,
            Ivan => IvanAt,
            Caldwell => CaldwellAt,
            Yamamoto => YamamotoAt,
            _ => Moves.Single(move => move.Pool == pool).Now
        };

        /// <summary>The new NPCs: id, comment, the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Ames, "Sergeant Bernard Ames - Ashen Desert, Ashen Lookout (1250)", Harris, AmesName, AmesLevel, AmesPackage, AmesAt),
            (Ivan, "Lt. Colonel Trent Ivan - Ashen Desert, Shadow's Edge Post (1283)", MasonLinier, IvanName, IvanLevel, IvanPackage, IvanAt),
            (Caldwell, "Lt. Colonel Dania Caldwell - Ashen Desert, Shadow's Edge Post (1284)", IdaHayner, CaldwellName, CaldwellLevel, CaldwellPackage, CaldwellAt),
            (Yamamoto, "Captain Akio Yamamoto - Ashen Desert, Boneyard Outpost (1283)", Emmons, YamamotoName, YamamotoLevel, YamamotoPackage, YamamotoAt)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, name, level, package, at) in New)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, 0, 0, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(PackageComment(comment))});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
            foreach (var (pool, _, now) in Moves)
                yield return Place(pool, now);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (pool, before, _) in Moves)
                yield return Place(pool, before);
            foreach (var (id, _, _, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        /// <summary>npc_package.comment is varchar(50) on MySQL: the name and the mission ("Sergeant Bernard Ames (1250)").</summary>
        private static string PackageComment(string comment) => comment.Substring(0, comment.IndexOf(" - ")) +
                                                                 comment.Substring(comment.LastIndexOf(" ("));

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
