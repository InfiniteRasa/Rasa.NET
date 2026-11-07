using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 1 NPCs, Ligo Crucible (map 1993): the new Corporal Shandy (The Last Men Standing, 1142), Recon
    /// Sergeant Mullins (The Wire, 1145) and Top Khana Grupa Munit (1145), and the fixes to the upstream rows that set 1
    /// uses (Captain DeLisi, Private Drake, Larai Grupa Vitto, Viddia Grupa Donal, Labbna Grupa Rigs, Intelligence Officer
    /// D'Ambrose).
    ///
    /// Id block (checked 2026-11-07 against every table of the World DB and the code: no row and no constant uses
    /// 770000-770999; the Marshes block ends at 760399): the Crucible missions take 770000-770999. Set 1 uses 770000-770002:
    ///  - 770000 Corporal Shandy (creature, creature_appearance, creature_stat, npc_package, spawn pool);
    ///  - 770001 Recon Sergeant Mullins (the same rows);
    ///  - 770002 Top Khana Grupa Munit (the same rows).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9060 "Corporal Shandy", 9328 "Recon Sergeant Mullins", 9062 "Top Khana
    ///    Grupa Munit". Packages (client objectiveconversation, 1.0): (1145,6) 1454 Mullins, (1145,3) 1229 Munit, (1145,5)
    ///    1231 Rigs, (1142,1) 1219 DeLisi. The client gives Shandy no objective conversation (1142 goes by giver only).
    ///  - Places (TaRapedia NPC pages, 0.5): Shandy "in the wormhole chamber" (988, 140, 60), level 39; DeLisi (952, 144, 106);
    ///    Drake "opposite the hospital" (952, 144, 110); Rigs "just north of the Labbna waypoint" (-391, 249, 278).
    ///  - Quarters (client objectiveconversation texts, 1.0, and the World DB map_region rows): Vitto's Larai (8649; region
    ///    "Staal: Larai Quarter (room)" at -300, 180, 320), Donal's "viddinarina... the hospital... of Staal" (8659; region
    ///    "Viddia Quarter" at -216, 196, 472, by the Staal Infirmary waypoint), Munit's armory (8660; region "Top Khana
    ///    Quarter" at -215, 196, 205), Rigs' Labbna Quarter (8664). Mullins (TaRapedia The Wire, 0.5): "Convict Pass, due
    ///    west of the fort ... stationed near the entrance"; his own line (client 9999) "Head through the caverns behind me".
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, D3): DeLisi, Drake, Vitto, Donal and Rigs stood in two placeholder
    /// clusters at the client's map labels "Outpost Intrepid" (942-951, 152-171, 176-185) and "Staal" (-22 to -11, 188,
    /// 318-329); Rigs had no npc_package row (the client gives him 1231); D'Ambrose had 1229, which is Munit's (the client
    /// gives D'Ambrose 1301): a talk with him would complete Munit's objective.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Shandy a copy of DeLisi's row and look (AFS class 3846), Mullins of Lieutenant Sherer's (class 3848), Munit of
    ///    Vitto's (Brann class 7776); Shandy level 39 (TaRapedia), Mullins and Munit 40 (no source: the missions' level).
    ///    Shandy has a server-only package (his id), as Marshes' Horsh. Friendly, no run or walk speed (as upstream givers).
    ///  - Hit points: each new NPC a creature_stat row as the Marshes quest NPCs (MarshesQuestNpcsHoldV1: 4500 hit points,
    ///    armour 300); max_hp 4500 to match.
    ///  - Every place (new and moved) the nearest point to its source on the client's floor and the server navmesh, at
    ///    least 2 m from a wall, nothing over the head, seen from 12 or more of 16 places 2 m round it and walked to from
    ///    its area's waypoint (probe of 2026-11-07, client files and navmesh/adv_arieki_ligo_burningsteps). Mullins at the
    ///    east mouth of Convict Pass (the TaRapedia "entrance", the end nearest the fort: 553 m walk from the Fort
    ///    Intrepid waypoint). Vitto, Donal and Munit at their quarter's region point. Every one stands 260 m or more from
    ///    the nearest hostile pool's circle (World DB).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows and puts the
    /// upstream rows back as they were.
    /// </summary>
    public static class CrucibleOutpostIntrepidNpcsV1
    {
        public const uint MapContextId = CrucibleMissionDataV1.MapContextId;
        public const uint HitPoints = 4500, Armor = 300, Friendly = 1;

        public const uint Shandy = 770000, ShandyName = 9060, ShandyPackage = 770000, ShandyLevel = 39;
        public const uint Mullins = 770001, MullinsName = 9328, MullinsPackage = 1454, MullinsLevel = 40;
        public const uint Munit = 770002, MunitName = 9062, MunitPackage = 1229, MunitLevel = 40;
        public const uint NextFreeId = 770003;

        /// <summary>The upstream rows the new NPCs copy: DeLisi (Shandy), Lieutenant Sherer (Mullins), Vitto (Munit).</summary>
        public const uint DeLisi = 510019, Drake = 510034, Sherer = 510030;
        public const uint Vitto = 510027, Donal = 510037, Rigs = 510025, DAmbrose = 510023;
        public const uint DeLisiPackage = 1219, VittoPackage = 1225, DonalPackage = 1227, RigsPackage = 1231;
        public const uint DAmbrosePackage = 1301, DAmbroseOldPackage = 1229;

        public static readonly (double X, double Y, double Z) ShandyAt = (987.07, 140, 59.65);
        public static readonly (double X, double Y, double Z) MullinsAt = (292.49, 167.43, 736.36);
        public static readonly (double X, double Y, double Z) MunitAt = (-215, 196.5, 205);

        /// <summary>The upstream pools moved: pool, the place before (World DB), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (DeLisi, (946.9000244140625, 152, 180.6999969482422), (952, 144.5, 106)),
            (Drake, (949.4000244140625, 167.32, 176.39999389648438), (952.46, 144.5, 110.89)),
            (Vitto, (-21.5, 187.77, 323.1000061035156), (-300, 180.5, 320)),
            (Donal, (-11, 187.77, 319.70001220703125), (-216, 195.69, 472)),
            (Rigs, (-17.5, 187.77, 328.6000061035156), (-392.77, 249.46, 276.23))
        };

        /// <summary>Where an NPC of set 1 stands: a new one's place, or a moved upstream pool's new one.</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Shandy => ShandyAt,
            Mullins => MullinsAt,
            Munit => MunitAt,
            _ => Moves.Single(move => move.Pool == pool).Now
        };

        /// <summary>The new NPCs: id, comment, the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Shandy, "Corporal Shandy - Outpost Intrepid (1142)", DeLisi, ShandyName, ShandyLevel, ShandyPackage, ShandyAt),
            (Mullins, "Recon Sergeant Mullins - Convict Pass (1145)", Sherer, MullinsName, MullinsLevel, MullinsPackage, MullinsAt),
            (Munit, "Top Khana Grupa Munit - Staal, Top Khana Quarter (1145)", Vitto, MunitName, MunitLevel, MunitPackage, MunitAt)
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
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(comment)});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
            foreach (var (pool, _, now) in Moves)
                yield return Place(pool, now);
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Rigs}, {RigsPackage}, 'Labbna Grupa Rigs - Staal');";
            yield return $"UPDATE npc_package SET package_id = {DAmbrosePackage} WHERE id = {DAmbrose} AND package_id = {DAmbroseOldPackage};";
        }

        public static IEnumerable<string> Down()
        {
            yield return $"UPDATE npc_package SET package_id = {DAmbroseOldPackage} WHERE id = {DAmbrose} AND package_id = {DAmbrosePackage};";
            yield return $"DELETE FROM npc_package WHERE id = {Rigs} AND package_id = {RigsPackage};";
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

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
