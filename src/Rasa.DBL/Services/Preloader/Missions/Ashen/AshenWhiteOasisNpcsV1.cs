using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 11 NPCs, Ligo Ashen Desert (map 1734), White Oasis Post: Doctor Harold Meeks (the receiver of
    /// Analyzing the Harvesters 1295, the giver of Murky Waters 1298) and Agent Hale (1298's choice and its receiver), both new;
    /// Lieutenant Dania Fontecilla (upstream 510012, the giver and receiver of Gas Shortages 1356) moved off the control
    /// point's console. Captain Daris Linier (upstream 510007, package 1476, moved by set 1) gives 1295 and Prying Eyes (1303)
    /// where he stands.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 11 790400-790439): 790400 Meeks, 790401 Hale (creature,
    /// creature_appearance, creature_stat, npc_package, spawn pool each).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9356 "Doctor Harold Meeks", 9357 "Agent Hale". Hale's package 1478 (client
    ///    objectiveconversation 1298,1-3); Meeks has no objective conversation in the client (he gives 1298 and takes 1295 by
    ///    mission only): a server-only package (his id), as set 1's Ames.
    ///  - Places: both "White Oasis Post" / "White Oasis Outpost" (TaRapedia Analyzing the Harvesters, Murky Waters, 0.5; the
    ///    client's 1295 log "return to Dr. Harold Meeks at White Oasis Outpost", 1298 "He can be found at White Oasis Base").
    ///    Fontecilla "Lieutenant Dania Fontecilla at White Oasis Post" (the client's 1356 log).
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, triage U1, D7): Fontecilla's pool 510012 stood on the White Oasis control
    /// point's own spot (ControlPointSeed 17, class 3814, -511.08, 283.43, 164.35; its model reaches 6 m across x and 8.6 m
    /// across z), inside the console a player uses for the point. Moved 7.9 m west, off the console's model.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Meeks a copy of Captain Flynn Harris' row and look (AFS class 3846), Hale of Captain Marcus Emmons' (3846); level 40
    ///    (no source: the missions' band). 4500 hit points and armour 300, friendly, standing still, rotation 0 (as set 1).
    ///  - Each spot inside the post's fences (the client's fence statics x -533..-441, z 114..218): on the client's floor and the
    ///    server navmesh, 9 m or more from a wall, nothing over the head, seen from 16 of 16 places 2 m round it, 7 m or more from
    ///    the waypoint and the hospital, 170 m and more from every hostile pool, off the control point's model, walked to on the
    ///    client's floor from the White Oasis Post waypoint and back (probe of 2026-11-10, client files and
    ///    navmesh/adv_arieki_ligo_ashendesert). All three stand within 130 m of control point 17, so they serve only while the
    ///    AFS hold it (ControlPointServices, the existing rule; triage "Control points").
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows and puts
    /// Fontecilla back where upstream had her (only while she stands where this set put her).
    /// </summary>
    public static class AshenWhiteOasisNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Meeks = 790400, MeeksName = 9356, MeeksPackage = 790400, MeeksLevel = 40;
        public const uint Hale = 790401, HaleName = 9357, HalePackage = 1478, HaleLevel = 40;
        public const uint NextFreeId = 790402;

        /// <summary>The World DB rows the new NPCs copy: Captain Flynn Harris and Captain Marcus Emmons (AFS).</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris, Emmons = AshenLeadInNpcsV1.Emmons;

        /// <summary>The upstream NPCs of the set: Daris Linier (set 1's place) and Fontecilla (moved here, no package: she gives by id).</summary>
        public const uint DarisLinier = AshenLeadInNpcsV1.DarisLinier, DarisLinierPackage = AshenLeadInNpcsV1.DarisLinierPackage;
        public const uint Fontecilla = 510012;

        public static readonly (double X, double Y, double Z) MeeksAt = (-498, 282.39, 164);
        public static readonly (double X, double Y, double Z) HaleAt = (-516, 282.78, 176);

        /// <summary>Fontecilla's pool (World DB, upstream's spot as the StandingHeights correction stores it) and the place now.</summary>
        public static readonly (double X, double Y, double Z) FontecillaBefore = (-511.1000061035156, 281.76, 164.39999389648438);
        public static readonly (double X, double Y, double Z) FontecillaAt = (-519, 283.71, 164);

        /// <summary>The new NPCs: id, comment, npc_package comment (varchar(50)), the row they copy, name, level, package, place.</summary>
        public static readonly (uint Id, string Comment, string PackageComment, uint Copy, uint Name, uint Level, uint Package, (double X, double Y, double Z) At)[] New =
        {
            (Meeks, "Doctor Harold Meeks - Ashen Desert, White Oasis Post (1295)", "Doctor Harold Meeks (1295)", Harris, MeeksName, MeeksLevel, MeeksPackage, MeeksAt),
            (Hale, "Agent Hale - Ashen Desert, White Oasis Post (1298)", "Agent Hale (1298)", Emmons, HaleName, HaleLevel, HalePackage, HaleAt)
        };

        /// <summary>Where an NPC of set 11 stands: a new one's place, Fontecilla's new one, Daris Linier's (set 1).</summary>
        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Fontecilla => FontecillaAt,
            DarisLinier => AshenLeadInNpcsV1.At(DarisLinier),
            _ => New.Single(npc => npc.Id == pool).At
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, packageComment, copy, name, level, package, at) in New)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, 0, 0, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }
            yield return Place(Fontecilla, FontecillaAt) + Guard(FontecillaBefore);
        }

        public static IEnumerable<string> Down()
        {
            // Only while Fontecilla still stands where set 11 put her: a later move of her pool is not undone.
            yield return Place(Fontecilla, FontecillaBefore) + Guard(FontecillaAt);
            foreach (var (id, _, _, _, _, _, _, _) in New.Reverse())
            {
                yield return $"DELETE FROM spawnpool WHERE id = {id};";
                yield return $"DELETE FROM npc_package WHERE id = {id};";
                yield return $"DELETE FROM creature_stat WHERE id = {id};";
                yield return $"DELETE FROM creature_appearance WHERE id = {id};";
                yield return $"DELETE FROM creature WHERE id = {id};";
            }
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {D(at.X)}, pos_y = {D(at.Y)}, pos_z = {D(at.Z)} WHERE id = {pool}";

        /// <summary>The guard of an update: the pool still at <paramref name="at"/> (its stored values, to 0.01 m).</summary>
        private static string Guard((double X, double Y, double Z) at) =>
            $" AND ABS(pos_x - {D(at.X)}) < 0.01 AND ABS(pos_y - {D(at.Y)}) < 0.01 AND ABS(pos_z - {D(at.Z)}) < 0.01;";

        private static string D(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
