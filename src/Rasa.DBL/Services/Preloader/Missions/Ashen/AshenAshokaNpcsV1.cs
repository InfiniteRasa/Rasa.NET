using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 14 creatures, Ligo Ashen Desert (map 1734), round Ashoka Settlement: Lieutenant Daniel Chase (the
    /// giver and receiver of Supply Logistics 1358) at Shadow's Edge Post, Researchers Yorish and Jackson (the escorted of Echoes
    /// in the Sand 1736; scene actors, no pool) and Overseer Jirex (the dropper of Precious Tools 1286), all new (the World DB
    /// has none of them).
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 14 790520-790559): 790520 Chase (creature, look, stats,
    /// package, pool), 790521 Yorish and 790522 Jackson (creature, look, stats), 790523 Jirex (creature, stats) with his pool
    /// 790524.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9289 "Lieutenant Daniel Chase", 10060 "Researcher Yorish", 10061
    ///    "Researcher Jackson", 9793 "Overseer Jirex" (the client has his name: the triage's "no CN" was wrong; the server's boss
    ///    list BossTitles already counts name 9793 on Ashen Desert for 1580's "Jerix", so his kill counts there with nothing
    ///    else changed). None of them has an objective conversation in the client (Chase gives and takes 1358 by mission only).
    ///  - Places: TaRapedia Supply Logistics: Chase at Shadow's Edge Post (0.5); TaRapedia Echoes in the Sand: the researchers
    ///    set out from Captain Botha at Ashoka (0.5); TaRapedia Overseer Jirex: "Boss Thrax Soldier ... Near a pillbox, northeast
    ///    of Ashoka Settlement Waypoint" (0.5), Ellatha's boss map (474, -202), as a /loc (474, -259) (map Y - 57, 0.4); the client
    ///    map's destroyed Bane pillbox ArchBaneOutpostPillboxV01Destroyed at (454.3, 286.0, -272.9), 24 m from that /loc
    ///    (map statics).
    ///  - Jirex's kind: the Ashen Desert Overseers' own row (Overseer Mitecht 520004: class 10504 Bane_Thrax_Soldier_Rifle_Boss,
    ///    level 44, 4900 hit points, armour 420; TaRapedia's "Boss Thrax Soldier"). The class carries none of the client's
    ///    IMMUNE_* damage flags (creature_class_flag 125-135), as Overseer Ulovik's, whom a solo player beat in 1303.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Chase a copy of Captain Flynn Harris' row and look (AFS, class 3846), level 41, 4500 hit points and armour 300 (as set
    ///    1), his package a server-only one (his id, as set 1's Ivan); at (-120, -805) in Shadow's Edge Post, 15 m from the
    ///    waypoint, between it and the hospital.
    ///  - Yorish a copy of Mechanic Raji Lasat's row and look (Brann, class 7776: Kapa Grupa Lohen's representative), Jackson of
    ///    Professor Tiernan's (the Crucible's AFS researcher, class 3846); both friendly, level 42, the escorted's 4000 health and
    ///    armour 300 (EscortNpcStats) and the Marshes escorts' run 7 / walk 2 (MarshesEscortsNpcsV1), so they keep up with a
    ///    running player.
    ///  - Jirex at (426.8, -261): 30 m west of the destroyed pillbox, on the open floor by it (the pillbox's own ground is within
    ///    35 m of the Boneyard Thrax pool 531039 and the Hominis Machina pool 790245); 60 m or more from every other hostile
    ///    pool (the rule of 60 m between hostile pools: 531039 60.2, 790245 60.5, mortar 592199 61.5; probe of 2026-11-11: the
    ///    only floor near the pillbox that holds 60 m), 32 m from the road north out of Ashoka (Maiek's way, set 13).
    ///    One alone (fix F's boss rule), his respawn 3000 tenths = 300 s (fix F's boss respawn).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows.
    /// </summary>
    public static class AshenAshokaNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Chase = 790520, ChaseName = 9289, ChasePackage = 790520, ChaseLevel = 41;
        public const uint Yorish = 790521, YorishName = 10060;
        public const uint Jackson = 790522, JacksonName = 10061;
        public const uint ResearcherLevel = 42;
        public const uint ResearcherHealth = (uint)EscortNpcStats.Health, ResearcherArmor = (uint)EscortNpcStats.Armor;
        public const uint ResearcherRun = Marshes.MarshesEscortsNpcsV1.EscortRun, ResearcherWalk = Marshes.MarshesEscortsNpcsV1.EscortWalk;

        public const uint Jirex = 790523, JirexName = 9793, JirexPool = 790524;
        public const uint JirexClass = 10504;           // Bane_Thrax_Soldier_Rifle_Boss, the Ashen Overseers' class
        public const uint JirexLevel = 44, JirexHitPoints = 4900, JirexArmor = 420;
        public const uint JirexRespawnTenths = 3000;    // 300 s (fix F's boss respawn, ESTIMATE)
        public const uint NextFreeId = 790525;

        /// <summary>The World DB rows the new ones copy: Captain Flynn Harris (AFS), Mechanic Raji Lasat (Brann), Professor Tiernan (AFS researcher, PR #224), Overseer Mitecht (the Overseers' row).</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris, Raji = AshenIndraPassNpcsV1.Raji, Tiernan = 770039, Mitecht = 520004;

        public static readonly (double X, double Y, double Z) ChaseAt = (-120, 296.32, -805);
        public static readonly (double X, double Y, double Z) JirexAt = (426.8, 288.85, -261);

        /// <summary>The destroyed Bane pillbox northeast of the Ashoka waypoint (client map statics), TaRapedia's "near a pillbox".</summary>
        public static readonly (double X, double Y, double Z) PillboxAt = (454.3, 286.0, -272.9);

        /// <summary>The new creatures: id, comment, the row they copy, name, level, hit points (max_hp), run and walk speed (Chase's 0 and 0 are
        /// the copied row's own: Harris' row, as every Ashen NPC row, holds 0, the server's default speed).</summary>
        public static readonly (uint Id, string Comment, uint Copy, uint Name, uint Level, uint MaxHp, uint? Run, uint? Walk)[] Friends =
        {
            (Chase, "Lieutenant Daniel Chase - Ashen Desert, Shadow's Edge Post (1358)", Harris, ChaseName, ChaseLevel, HitPoints, 0, 0),
            (Yorish, "Researcher Yorish - Ashen Desert, Ashoka (1736 escort)", Raji, YorishName, ResearcherLevel, ResearcherHealth, ResearcherRun, ResearcherWalk),
            (Jackson, "Researcher Jackson - Ashen Desert, Ashoka (1736 escort)", Tiernan, JacksonName, ResearcherLevel, ResearcherHealth, ResearcherRun, ResearcherWalk)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, copy, name, level, maxHp, run, walk) in Friends)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {maxHp}, {name}, {run}, {walk}, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {copy};";
                yield return id == Chase ? Stat(id, HitPoints, Armor) : Stat(id, ResearcherHealth, ResearcherArmor);
            }
            yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({Chase}, {ChasePackage}, 'Lieutenant Daniel Chase (1358)');";
            yield return Pool(Chase, ChaseAt.X, ChaseAt.Y, ChaseAt.Z, MapContextId, Chase, 1, 1, 20, 0);

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Jirex}, 'Overseer Jirex - Ashen Desert, northeast of Ashoka (1286)', {JirexClass}, faction, {JirexLevel}, " +
                         $"{JirexHitPoints}, {JirexName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, " +
                         $"action7, action8 FROM creature WHERE id = {Mitecht};";
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                         $"SELECT {Jirex}, body, mind, spirit, {JirexHitPoints}, {JirexArmor} FROM creature_stat WHERE id = {Mitecht};";
            yield return Pool(JirexPool, JirexAt.X, JirexAt.Y, JirexAt.Z, MapContextId, Jirex, 1, 1, JirexRespawnTenths, 0);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Chase}, {JirexPool});";
            yield return $"DELETE FROM npc_package WHERE id = {Chase};";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Chase}, {Yorish}, {Jackson}, {Jirex});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Chase}, {Yorish}, {Jackson});";
            yield return $"DELETE FROM creature WHERE id IN ({Chase}, {Yorish}, {Jackson}, {Jirex});";
        }

        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Chase => ChaseAt,
            JirexPool => JirexAt,
            _ => throw new System.ArgumentOutOfRangeException(nameof(pool))
        };

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";

        /// <summary>The creature ids this set adds (for the tests).</summary>
        public static uint[] Creatures => Friends.Select(row => row.Id).Append(Jirex).ToArray();
    }
}
