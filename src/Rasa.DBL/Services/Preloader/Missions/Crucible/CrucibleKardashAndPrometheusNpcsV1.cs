using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Crucible set 8a NPCs, for Prometheus Rising (1210), Confirmed Kills (1212), Point Of Impact (1217) and It Came From
    /// Erdas (1218): two new rows, Professor Tiernan at the Eloh Crash Site on Ligo Crucible (1993) and Captain Amandla Botha at
    /// the Ashoka Settlement on Ashen Desert (1734), and two upstream rows moved, Major Keplinger and Larai Zupa Madias at
    /// Prometheus Outpost.
    ///
    /// Ids (the Crucible block 770000-770999; sets 1-7 used 770000-770038):
    ///  - 770039 Professor Tiernan (creature, creature_appearance, creature_stat, npc_package, spawn pool), on the Crucible;
    ///  - 770040 Captain Amandla Botha (creature, creature_appearance, creature_stat, npc_package, spawn pool), on Ashen Desert.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9365 "Professor Tiernan", 9278 "Captain Amandla Botha"; the World DB rows
    ///    510031 Major Keplinger (name 9208, package 1337) and 510029 Larai Zupa Madias (9205, no package). Packages (client
    ///    objectiveconversation, 1.0): (1210,1) 1337 Keplinger, (1217,1) and (1218,1) 1483 Tiernan, (1218,2) 1410 Botha.
    ///  - Places: Tiernan "the researcher in charge of the Eloh crash site near Devil's Pitchfork" (client 9585, 10541); the
    ///    crash site the client region "Eloh Crash Site" (580, 120, 232) and the escape pod static ArchElohReliquaryEscapePodv01
    ///    (574.9, 157.4, 231.9) (client map). Botha "Captain Botha at Ashaka Settlement in Ashen Desert" (client 10545, 14820;
    ///    TaRapedia "Ashoka Settlement"); World DB map_region 20062 "Ashoka Settlement" (418.3, 299.7, -472, r81.5). Keplinger
    ///    "Their CO at Prometheus is Major Keplinger" (client 9551). Madias "Near the CP gate", /loc 378.9, 168.0, 367.6
    ///    (TaRapedia Larai Zupa Madias, 0.5; its infobox zone "Mires" is a copy error).
    ///
    /// Upstream data fixed here (NOTES-upstream-bugs, D3): Keplinger (402.2, 169.77, 384.4) and Madias (402.2, 169.77, 391.4)
    /// stand in the placeholder cluster on the Prometheus Outpost control point (400.1, 170.4, 387.9), inside the control
    /// point's model (UsableControlPointElohV01, arch_eloh_controlpoint_v02_PVE: 12 x 17 m, 2.6 m high). Captain Tiersky
    /// (510021, set 9's 1224) stays there for now.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Tiernan a copy of Keplinger's row and look (human male, class 3846), level 40, 4500 hit points and armour 300 as the
    ///    other new NPCs, on the crater floor 23 m north-east of the escape pod; Botha a copy of Captain Flynn Harris's row and
    ///    look (Ashoka Settlement, class 3846; no source gives Botha's sex), level 40, 19 m west of the Ashoka Settlement waypoint.
    ///  - Madias at TaRapedia's x and z on the navmesh's y (168.3); Keplinger 16.4 m north of him by the CP gate, 23 m from the
    ///    control point's centre (no source gives his place).
    ///  - Every place on the server navmesh and a client floor, 3.7 m or more from a wall, nothing over the head, seen from 16
    ///    of 16 places 2 m round it, walked to from its area's waypoint with no lava on the walk (Tiernan from the Prometheus
    ///    Outpost waypoint; Botha from the Ashoka Settlement waypoint); 116 m or more from every hostile automatic pool's circle
    ///    and 450 m or more from every map link (probe 2026-11-07).
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out the new rows and puts the upstream ones back as they were.
    /// </summary>
    public static class CrucibleKardashAndPrometheusNpcsV1
    {
        public const uint Crucible = CrucibleMissionDataV1.MapContextId;
        public const uint AshenDesert = 1734;           // adv_arieki_ligo_ashendesert
        public const uint HitPoints = CrucibleOutpostIntrepidNpcsV1.HitPoints, Armor = CrucibleOutpostIntrepidNpcsV1.Armor;
        public const uint Friendly = 1;

        public const uint Keplinger = 510031, KeplingerPackage = 1337;
        public const uint Madias = 510029;
        public const uint Harris = 510008;              // Captain Flynn Harris - Ashoka Settlement (World DB)

        public const uint Tiernan = 770039, TiernanName = 9365, TiernanPackage = 1483, TiernanLevel = 40;
        public static readonly (double X, double Y, double Z) TiernanAt = (589.5, 154.37, 250);
        public const uint Botha = 770040, BothaName = 9278, BothaPackage = 1410, BothaLevel = 40;
        public static readonly (double X, double Y, double Z) BothaAt = (378, 280.69, -458.5);
        public const uint NextFreeId = 770041;

        /// <summary>The Prometheus Outpost waypoint (teleporter 315) and the Ashoka Settlement waypoint (teleporter 196), World DB.</summary>
        public static readonly (double X, double Y, double Z) PrometheusWaypoint = (435.0338, 168.04769, 406.9667);
        public static readonly (double X, double Y, double Z) AshokaWaypoint = (396.97266, 280.34375, -458.8086);

        /// <summary>The escape pod's deck at the Eloh Crash Site (the walkable top of ArchElohReliquaryEscapePodv01): the obelisk's place.</summary>
        public static readonly (double X, double Y, double Z) PodDeckAt = (575.5, 157.27, 232);

        /// <summary>The upstream pools moved: pool, the place before (World DB), the place now.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) Before, (double X, double Y, double Z) Now)[] Moves =
        {
            (Keplinger, (402.20001220703125, 169.77, 384.3999938964844), (379, 168.47, 384)),
            (Madias, (402.20001220703125, 169.77, 391.3999938964844), (378.9, 168.3, 367.6))
        };

        /// <summary>The new NPCs: id, comment, the row they copy, name, level, package, map, place.</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint Package, uint Map, (double X, double Y, double Z) At)[] New =
        {
            (Tiernan, "Professor Tiernan - Eloh Crash Site (1217, 1218)", Keplinger, TiernanName, TiernanLevel, TiernanPackage, Crucible, TiernanAt),
            (Botha, "Captain Amandla Botha - Ashoka Settlement (1218)", Harris, BothaName, BothaLevel, BothaPackage, AshenDesert, BothaAt)
        };

        /// <summary>Where an NPC of set 8a stands.</summary>
        public static (double X, double Y, double Z) At(uint pool) =>
            Moves.Any(move => move.Pool == pool) ? Moves.Single(move => move.Pool == pool).Now : New.Single(entry => entry.Id == pool).At;

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, name, level, package, map, at) in New)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, 0, 0, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return Stat(id, HitPoints, Armor);
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(comment)});";
                yield return Pool(id, at.X, at.Y, at.Z, map, id, 1, 1, 20, 0);
            }
            foreach (var move in Moves)
                yield return Place(move.Pool, move.Now);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var move in Moves.Reverse())
                yield return Place(move.Pool, move.Before);
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
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
