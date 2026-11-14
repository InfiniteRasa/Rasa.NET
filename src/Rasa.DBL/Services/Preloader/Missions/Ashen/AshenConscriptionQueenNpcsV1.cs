using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 20 creatures and pads, the Bane Conscription Facility (squad instance 1988), the Atta queen part:
    /// the Enraged Atta and the Atta Hive Queen of the east wing (What Makes Them Tick 1551), Enslaver Xahn in Information
    /// Extraction (Save the Queen 1501), Lt. Humphries, Lukaz and the Brann Prisoners of the Atta Observatory (Into the Lion's
    /// Den 1499: no pools, 1499's copies only), Commander Jawan on Thunderhead (1501's end; the zone is not built: his row and
    /// pool only), and two Bane teleporter pairs from the HQ's round hall, one to the east wing, one to the Enslaver Fortress.
    /// The World DB has none of them.
    ///
    /// Ids (the Ashen block 790000-790999; set 19 took 790659-790660, 790661 left free): 790662 the Enraged Atta (creature, stats) and 790663
    /// their pool, 790664 the Atta Hive Queen (creature, stats) and 790665 her pool, 790666 Enslaver Xahn (creature, stats) and
    /// 790667 his pool, 790668 Humphries, 790669 Lukaz, 790670 the Brann Prisoner (creature, look, stats; Humphries and Lukaz a
    /// package), 790671 Jawan (creature, look, stats, package, pool). The pads: local teleporters 925-928, free in the World DB
    /// and the code (set 19's 921/922; fix L takes 923/924).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 8094 "Atta Soldier", 8092 "Atta Hive Queen", 9708 "Enslaver Xahn", 9746
    ///    "Lt. Humphries", 9755 "Lukaz", 9483 "Brann Prisoner", 9525 "Commander Jawan". Packages (client objectiveconversation,
    ///    1.0): 1936 Humphries (1499,5), 1934 Lukaz (1499,6), 1648 Jawan (1501,7).
    ///  - Classes (client entityclass, 1.0): 24087 Creature_Atta_Queen (the plan's, G2), 11134 Boss_Enslaver. The queen level 49
    ///    (TaRapedia Atta Queen "a level 49 boss", met in the Bane Conscription Facility). Jawan level 45 at (672, 383, -527) on
    ///    Thunderhead Base (TaRapedia Commander Jawan).
    ///  - The pads' look: UsableTwoStateBaneTeleporterV01 (class 8044), as set 19's.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The Enraged Atta: the Ashen Atta Soldier row (531015: class 7081, actions, stats) named 8094 "Atta Soldier" (the client
    ///    has no "Enraged Atta" name; TaRapedia's "red Atta", the tint later, set 21), level 43; four in one pool (radius 5,
    ///    respawn 90 s as the facility's pools) in the arena ring's east hall (350, 97.61, -104).
    ///  - The queen: class 24087 with the Atta Imperial Guard's faction, actions and armour (520002), 9800 hit points (twice the
    ///    guard's), standing still (run and walk 0, as Ojasa's Atta Hive Queen 565016); alone in her pool at the arena ring's
    ///    south end (270, 97.61, -335), respawn 300 s (Kretas', fix F).
    ///  - Xahn: class 11134 with Overseer Mitecht's faction, actions, stats (520004, as Kretas), level 45; alone in his pool in
    ///    Information Extraction (6.5, 78.11, 166) among its four Enslaver cells (TaRapedia), respawn 900 s (ESTIMATE, longer than
    ///    the queen's: 1501's scene names the pool, so it returns on its own time in a squad's copy too). His 4900 hit points and
    ///    armour 420: ESTIMATE, copied from Kretas and Kuutk (Mitecht's row), no source.
    ///  - Humphries and Jawan copy Captain Flynn Harris (AFS, class 3846), Lukaz and the prisoners Mechanic Raji Lasat (a Brann,
    ///    class 7776, 510014); friendly, levels 43, 43, 40 and 45. Jawan on Thunderhead's base floor below TaRapedia's y (383 is
    ///    the roof there): (672, 339, -527), 8.7 m from the base's waypoint (teleporter 85 "Local: Thunderhead Base Services",
    ///    World DB), walked from it (the probe, 2026-11-12).
    ///  - The pads: the client floor has no walk from the HQ hall to the east wing (the arena) or to the Enslaver Fortress (the
    ///    probe's floods, 2026-11-12: the hall's component holds neither), so two public pairs, linked both ways, as set 19's: 925
    ///    on the hall's south-west (-2, 135.94, -379) to 926 in the arena ring's west (288, 96.02, -156); 927 on the hall's north
    ///    (16.5, 135.94, -359) to 928 on the deck before Information Extraction (6.5, 77.61, 110): flat 77.61 from z 102 to 116
    ///    (the probe's grid, 2026-11-12), north of the stair from Enslaver pool 531075's room (24 m outside its circle, its
    ///    spawn draws 20 m or more off) and 5 m outside 1501's area (r35), so the walk in passes no pool's middle (audit R1;
    ///    ESTIMATE of the fortress' way in).
    /// </summary>
    public static class AshenConscriptionQueenNpcsV1
    {
        public const uint Facility = AshenConscriptionEntryNpcsV1.Facility;
        public const uint Thunderhead = 1911;           // adv_arieki_ligo_thunderhead
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint EnragedAtta = 790662, EnragedAttaPool = 790663, EnragedAttaName = 8094, EnragedAttaLevel = 43;
        public const uint Queen = 790664, QueenPool = 790665, QueenName = 8092, QueenLevel = 49, QueenClass = 24087;
        public const uint QueenHitPoints = 9800, QueenArmor = 420;
        public const uint Xahn = 790666, XahnPool = 790667, XahnName = 9708, XahnLevel = 45, XahnClass = 11134;
        // ESTIMATE: 4900 hit points and armour 420 as Kretas and Kuutk (the Overseers' row, Mitecht), no source.
        public const uint XahnHitPoints = 4900, XahnArmor = 420;
        public const uint Humphries = 790668, HumphriesName = 9746, HumphriesPackage = 1936, HumphriesLevel = 43;
        public const uint Lukaz = 790669, LukazName = 9755, LukazPackage = 1934, LukazLevel = 43;
        public const uint Prisoner = 790670, PrisonerName = 9483, PrisonerLevel = 40;
        public const uint Jawan = 790671, JawanName = 9525, JawanPackage = 1648, JawanLevel = 45;
        public const uint NextFreeId = 790672;

        /// <summary>The World DB rows the new ones copy.</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris, Brann = 510014, AttaSoldier = 531015, ImperialGuard = 520002,
            Mitecht = AshenAshokaNpcsV1.Mitecht;

        public const uint AttaCount = 4, AttaRadius = 5, AttaRespawnTenths = 900, BossRespawnTenths = 3000, XahnRespawnTenths = 9000;

        public static readonly (double X, double Y, double Z) EnragedAttaAt = (350, 97.61, -104);
        public static readonly (double X, double Y, double Z) QueenAt = (270, 97.61, -335);
        public static readonly (double X, double Y, double Z) XahnAt = (6.5, 78.11, 166);
        public static readonly (double X, double Y, double Z) JawanAt = (672, 339.0, -527);

        /// <summary>The Bane teleporters: two local teleporter pairs (type 1), class 8044 UsableTwoStateBaneTeleporterV01.</summary>
        public const uint HallWingPad = 925, WingPad = 926, HallFortressPad = 927, FortressPad = 928;
        public const uint LocalTeleporter = AshenConscriptionEntryNpcsV1.LocalTeleporter, PadClass = AshenConscriptionEntryNpcsV1.PadClass;
        public static readonly (double X, double Y, double Z) HallWingPadAt = (-2, 135.94, -379), WingPadAt = (288, 96.02, -156),
            HallFortressPadAt = (16.5, 135.94, -359), FortressPadAt = (6.5, 77.61, 110);

        /// <summary>The pads: id, description, place; linked both ways.</summary>
        public static readonly (uint Id, string Description, (double X, double Y, double Z) At, uint To)[] Pads =
        {
            (HallWingPad, "Bane Teleporter: Conscription Facility, the HQ hall to the east wing (set 20)", HallWingPadAt, WingPad),
            (WingPad, "Bane Teleporter: Conscription Facility, the east wing's arena ring (set 20)", WingPadAt, HallWingPad),
            (HallFortressPad, "Bane Teleporter: Conscription Facility, the HQ hall to the Enslaver Fortress (set 20)", HallFortressPadAt, FortressPad),
            (FortressPad, "Bane Teleporter: Conscription Facility, the Enslaver Fortress (set 20)", FortressPadAt, HallFortressPad)
        };

        /// <summary>The new friendly creatures: id, comment, name, level, package (0: none), the row whose class and look they copy.</summary>
        public static readonly (uint Id, string Comment, uint Name, uint Level, uint Package, uint Copies)[] Friends =
        {
            (Humphries, "Lt. Humphries - Conscription Facility, the Atta pit (1499)", HumphriesName, HumphriesLevel, HumphriesPackage, Harris),
            (Lukaz, "Lukaz - Conscription Facility, the Brann cells (1499)", LukazName, LukazLevel, LukazPackage, Brann),
            (Prisoner, "Brann Prisoner - Conscription Facility, the Brann cells (1499)", PrisonerName, PrisonerLevel, 0, Brann),
            (Jawan, "Commander Jawan - Thunderhead Base (1501)", JawanName, JawanLevel, JawanPackage, Harris)
        };

        /// <summary>The new hostile creatures: id, comment, the row copied, class (0: the row's), name, level, hit points (0: the row's), armour, still.</summary>
        public static readonly (uint Id, string Comment, uint Copies, uint Class, uint Name, uint Level, uint HitPoints, uint Armor, bool Still)[] Foes =
        {
            (EnragedAtta, "Enraged Atta - Conscription Facility, the east wing (1551)", AttaSoldier, 0, EnragedAttaName, EnragedAttaLevel, 0, 0, false),
            (Queen, "Atta Hive Queen - Conscription Facility, the east wing (1551)", ImperialGuard, QueenClass, QueenName, QueenLevel, QueenHitPoints, QueenArmor, true),
            (Xahn, "Enslaver Xahn - Conscription Facility, Information Extraction (1501)", Mitecht, XahnClass, XahnName, XahnLevel, XahnHitPoints, XahnArmor, false)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, name, level, package, copies) in Friends)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, run_speed, walk_speed, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copies};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copies};";
                yield return Stat(id, HitPoints, Armor);
                if (package == 0)
                    continue;
                // npc_package.comment is varchar(50): the name and the mission only.
                var packageComment = comment[..comment.IndexOf(" - ", StringComparison.Ordinal)] + comment[comment.LastIndexOf(" (", StringComparison.Ordinal)..];
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
            }
            foreach (var (id, comment, copies, classId, name, level, hitPoints, armor, still) in Foes)
            {
                var kind = classId == 0 ? "class_id" : classId.ToString(CultureInfo.InvariantCulture);
                var health = hitPoints == 0 ? "max_hp" : hitPoints.ToString(CultureInfo.InvariantCulture);
                var speeds = still ? "0, 0" : "run_speed, walk_speed";
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, {kind}, faction, {level}, {health}, {name}, {speeds}, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copies};";
                yield return hitPoints == 0
                    ? $"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) SELECT {id}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {copies};"
                    : $"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) SELECT {id}, body, mind, spirit, {hitPoints}, {armor} FROM creature_stat WHERE id = {copies};";
            }
            yield return Pool(EnragedAttaPool, EnragedAttaAt.X, EnragedAttaAt.Y, EnragedAttaAt.Z, Facility, EnragedAtta, AttaCount, AttaCount, AttaRespawnTenths, AttaRadius);
            yield return Pool(QueenPool, QueenAt.X, QueenAt.Y, QueenAt.Z, Facility, Queen, 1, 1, BossRespawnTenths, 0);
            yield return Pool(XahnPool, XahnAt.X, XahnAt.Y, XahnAt.Z, Facility, Xahn, 1, 1, XahnRespawnTenths, 0);
            yield return Pool(Jawan, JawanAt.X, JawanAt.Y, JawanAt.Z, Thunderhead, Jawan, 1, 1, 20, 0);
        }

        /// <summary>The pads and links: each INSERT guarded already (only where the row is not there), as set 19's.</summary>
        public static IEnumerable<string> PadsUp()
        {
            foreach (var (id, description, at, _) in Pads)
                yield return Pad(id, description, at);
            foreach (var (id, _, _, to) in Pads)
                yield return Link(id, to);
        }

        public static IEnumerable<string> PadsDown()
        {
            foreach (var (id, _, _, to) in Pads.Reverse())
                yield return $"DELETE FROM local_teleporter_link WHERE from_teleporter_id = {id} AND to_teleporter_id = {to};";
            yield return $"DELETE FROM teleporter WHERE id IN ({string.Join(", ", Pads.Select(pad => pad.Id))}) AND map_context_id = {Facility} AND type = {LocalTeleporter};";
        }

        public static IEnumerable<string> Down()
        {
            var all = string.Join(", ", Creatures.Select(id => id.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({string.Join(", ", PoolIds)});";
            yield return $"DELETE FROM npc_package WHERE id IN ({all});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({all});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({all});";
            yield return $"DELETE FROM creature WHERE id IN ({all});";
        }

        /// <summary>The creature ids this set adds (for the tests).</summary>
        public static uint[] Creatures => Friends.Select(row => row.Id).Concat(Foes.Select(row => row.Id)).OrderBy(id => id).ToArray();

        /// <summary>The pool ids this set adds: the Enraged Atta's, the queen's, Xahn's, Jawan's.</summary>
        public static readonly uint[] PoolIds = { EnragedAttaPool, QueenPool, XahnPool, Jawan };

        private static string Pad(uint id, string description, (double X, double Y, double Z) at) =>
            "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
            $"SELECT {id}, {PadClass}, {LocalTeleporter}, {S(description)}, {R(at.X)}, {R(at.Y)}, {R(at.Z)}, 0, m.map_context_id " +
            $"FROM map_info m WHERE m.map_context_id = {Facility} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {id});";

        private static string Link(uint from, uint to) =>
            "INSERT INTO local_teleporter_link (from_teleporter_id, to_teleporter_id) " +
            $"SELECT t.id, {to} FROM teleporter t WHERE t.id = {from} AND EXISTS (SELECT 1 FROM teleporter u WHERE u.id = {to}) " +
            $"AND NOT EXISTS (SELECT 1 FROM local_teleporter_link x WHERE x.from_teleporter_id = {from});";

        private static string R(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
