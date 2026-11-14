using System;
using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 15 creatures, Indra Caverns (squad instance 2055, adv_arieki_ligo_ashendesert_indracaverns): the
    /// Brann storage facility's people and foes. Major Darkua Diren just inside the caverns (the receiver of Indra Caverns 1559,
    /// the giver of Threshold 1360), Sergeant Jonas Drof barricaded in the facility's Control Room (the receiver of 1360, the
    /// giver and receiver of Vindication 1395), Scout Hansa Naimi in an Enslaver cell (1395's talk), Thrax Overseer Kuutk (1395's
    /// boss, the Thrax Hand) and the two Bane pools of the Indra Watch Post (1360's "Take Back Indra Watch Post"). The World DB has
    /// none of them: every upstream Indra Caverns pool stands in the Gotha plant (x -86 and less) but a medic by the door.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 15 790560-790599): 790560 Diren, 790561 Drof, 790562 Naimi
    /// (creature, look, stats, package, pool each), 790563 Kuutk (creature, stats) with his pool 790564, the Watch Post pools
    /// 790565 (west) and 790566 (east).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9727 "Major Darkua Diren", 9427 "Sergeant Jonas Drof", 9530 "Scout Hansa
    ///    Naimi", 9408 "Thrax Overseer Kuutk". Packages (client objectiveconversation, 1.0): 1802 Diren (1452,8), 1544 Drof (1360,2
    ///    and 1532,1), 1651 Naimi (1395,3).
    ///  - Places: TaRapedia Indra Caverns (mission): Diren "right inside the instance" (0.5); client text 1360: Drof "barricaded in
    ///    this room", 1395: "the console in the Control Room" (1.0); the client map's Brann room ArchBrannCorridorRoom01ControlV01
    ///    at (-5, 0.8, -224) (map statics); the two Enslaver cells ArchBaneGenObjEnslaverCellV01 at (9.6, 24.5, -203.3) and (10.4,
    ///    24.5, -244.2) by the Indra Security Station (map statics); the Indra Watch Post region (-18, 9, -333) and its trenches and
    ///    bunkers ArchHumOutpostTrench*, ArchHumOutpostTrenchBunkerV01 / V03 (x -55 to 34, z -308 to -351, map statics).
    ///  - Kuutk's kind: the Ashen Overseers' own row (Overseer Mitecht 520004: class 10504 Bane_Thrax_Soldier_Rifle_Boss, level
    ///    44, 4900 hit points, armour 420), as set 14's Jirex; the class has none of the client's IMMUNE_* damage flags
    ///    (creature_class_flag 125-135). The Watch Post's Bane: the upstream Ashen Thrax rows the Gotha plant's pools use, Thrax
    ///    Soldier 531008 (class 20757) and the Ravager 531009 (class 9354; set 2 named it, D9), level 39, 5783 hit points; neither
    ///    class has an IMMUNE_* flag.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Diren, Drof and Naimi copies of Captain Flynn Harris' row and look (AFS, class 3846), as set 14's Chase; levels 44, 43,
    ///    42 (the instance's band 42-44, D5); 4500 hit points, armour 300 (AshenLeadInNpcsV1). Their packages the client's.
    ///  - Diren at (-53, -395), 29 m into the cave from the door's arrival (-40.7, -421.6), beside the medic 500021 (-60, -404.5),
    ///    43.5 m from the west Watch Post pool (radius 0; outside its creature's 18 m aggro range and the 34 m keep-out), 114 m
    ///    from the east one. Drof in the Control Room's middle (-5, -221). Naimi inside cell B (10.4, -244.2)
    ///    on its floor (the cell's local (0, 0, 0.8): its door side faces north-east, the walk's side); cell B, not A, as its floor is
    ///    joined to the Security Station's navmesh (cell A's is a 32 m2 island).
    ///  - Kuutk alone in the Science Lab (59, -403), south of its machine bank, its one door (the jamb at (36, -8, -364)) 47 m
    ///    off; one, radius 0, respawn 3000 tenths (300 s, fix F's boss rule; also in the squad's copy: a public-spawn actor of 1395's
    ///    scene, SpawnPoolManager.ReturnsOnOwnTime); 60 m and more outside every other hostile pool's
    ///    circle (the Watch Post's east pool 70 m off, 24 m above).
    ///  - The Watch Post: two pools of one each, radius 0 (to-fix row 26: a solo player must win; one at a time), respawn 1200
    ///    tenths (the upstream Indra Caverns pools'; never used: in the squad's copy a cleared pool comes back 45 minutes after
    ///    its last death, SquadInstanceConfig.RespawnMinutes, SquadInstanceState): a Thrax Soldier west in the low trench on the way in (-36, -355) and the Ravager east before the
    ///    facility's door (44, -335), 82 m apart (the rule of 60 m between hostile pools held). Both stand on the only way in:
    ///    every walk past the Watch Post meets them (1360's own fight; docs/ashen-playtest.md).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows.
    /// </summary>
    public static class AshenIndraFacilityNpcsV1
    {
        public const uint Caverns = 2055;               // adv_arieki_ligo_ashendesert_indracaverns, a squad instance
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Diren = 790560, DirenName = 9727, DirenPackage = 1802, DirenLevel = 44;
        public const uint Drof = 790561, DrofName = 9427, DrofPackage = 1544, DrofLevel = 43;
        public const uint Naimi = 790562, NaimiName = 9530, NaimiPackage = 1651, NaimiLevel = 42;

        public const uint Kuutk = 790563, KuutkName = 9408, KuutkPool = 790564;
        public const uint KuutkClass = 10504;           // Bane_Thrax_Soldier_Rifle_Boss, the Ashen Overseers' class
        public const uint KuutkLevel = 44, KuutkHitPoints = 4900, KuutkArmor = 420;
        public const uint KuutkRespawnTenths = 3000;    // 300 s (fix F's boss respawn, ESTIMATE)

        /// <summary>The Indra Watch Post's two pools: the upstream Thrax Soldier and Ravager rows (531008, 531009), one each.</summary>
        public const uint WatchPostWest = 790565, WatchPostEast = 790566;
        public const uint ThraxSoldier = 531008, ThraxRavager = 531009;
        public const uint WatchPostRespawnTenths = 1200;
        public const uint NextFreeId = 790567;

        /// <summary>The World DB rows the new ones copy: Captain Flynn Harris (AFS), Overseer Mitecht (the Overseers' row).</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris, Mitecht = AshenAshokaNpcsV1.Mitecht;

        /// <summary>Where the door from Ashen Desert puts a player (map_link 58's destination).</summary>
        public static readonly (double X, double Y, double Z) ArrivalAt = (-40.689, 8.9327, -421.6495);

        public static readonly (double X, double Y, double Z) DirenAt = (-53, 8.6, -395);
        public static readonly (double X, double Y, double Z) DrofAt = (-5, 0.58, -221);
        public static readonly (double X, double Y, double Z) NaimiAt = (9.83, 24.85, -244.76);
        public static readonly (double X, double Y, double Z) KuutkAt = (59, -7.42, -403);
        public static readonly (double X, double Y, double Z) WatchPostWestAt = (-36, 6.34, -355);
        public static readonly (double X, double Y, double Z) WatchPostEastAt = (44, 16.35, -335);

        /// <summary>The new friendly creatures: id, comment, name, level.</summary>
        public static readonly (uint Id, string Comment, uint Name, uint Level, uint Package)[] Friends =
        {
            (Diren, "Major Darkua Diren - Indra Caverns, inside the door (1559, 1360)", DirenName, DirenLevel, DirenPackage),
            (Drof, "Sergeant Jonas Drof - Indra Caverns, the Control Room (1360, 1395)", DrofName, DrofLevel, DrofPackage),
            (Naimi, "Scout Hansa Naimi - Indra Caverns, an Enslaver cell (1395)", NaimiName, NaimiLevel, NaimiPackage)
        };

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, name, level, package) in Friends)
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, {S(comment)}, class_id, {Friendly}, {level}, {HitPoints}, {name}, run_speed, walk_speed, " +
                             $"action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Harris};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Harris};";
                yield return Stat(id, HitPoints, Armor);
                // npc_package.comment is varchar(50): the name and the missions only ("Major Darkua Diren (1559, 1360)").
                var packageComment = comment[..comment.IndexOf(" - ", StringComparison.Ordinal)] + comment[comment.LastIndexOf(" (", StringComparison.Ordinal)..];
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
                var at = At(id);
                yield return Pool(id, at.X, at.Y, at.Z, Caverns, id, 1, 1, 20, 0);
            }

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Kuutk}, 'Thrax Overseer Kuutk - Indra Caverns, the Science Lab (1395)', {KuutkClass}, faction, {KuutkLevel}, " +
                         $"{KuutkHitPoints}, {KuutkName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, " +
                         $"action7, action8 FROM creature WHERE id = {Mitecht};";
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                         $"SELECT {Kuutk}, body, mind, spirit, {KuutkHitPoints}, {KuutkArmor} FROM creature_stat WHERE id = {Mitecht};";
            yield return Pool(KuutkPool, KuutkAt.X, KuutkAt.Y, KuutkAt.Z, Caverns, Kuutk, 1, 1, KuutkRespawnTenths, 0);
            yield return Pool(WatchPostWest, WatchPostWestAt.X, WatchPostWestAt.Y, WatchPostWestAt.Z, Caverns, ThraxSoldier, 1, 1, WatchPostRespawnTenths, 0);
            yield return Pool(WatchPostEast, WatchPostEastAt.X, WatchPostEastAt.Y, WatchPostEastAt.Z, Caverns, ThraxRavager, 1, 1, WatchPostRespawnTenths, 0);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Diren}, {Drof}, {Naimi}, {KuutkPool}, {WatchPostWest}, {WatchPostEast});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Diren}, {Drof}, {Naimi});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Diren}, {Drof}, {Naimi}, {Kuutk});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Diren}, {Drof}, {Naimi});";
            yield return $"DELETE FROM creature WHERE id IN ({Diren}, {Drof}, {Naimi}, {Kuutk});";
        }

        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Diren => DirenAt,
            Drof => DrofAt,
            Naimi => NaimiAt,
            KuutkPool => KuutkAt,
            WatchPostWest => WatchPostWestAt,
            WatchPostEast => WatchPostEastAt,
            _ => throw new System.ArgumentOutOfRangeException(nameof(pool))
        };

        /// <summary>The Watch Post's pools, west to east (1360's objective 3 waits for both dead at once).</summary>
        public static readonly uint[] WatchPostPools = { WatchPostWest, WatchPostEast };

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";

        /// <summary>The creature ids this set adds (for the tests).</summary>
        public static uint[] Creatures => Friends.Select(row => row.Id).Append(Kuutk).ToArray();
    }
}
