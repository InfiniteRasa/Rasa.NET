using System;
using System.Collections.Generic;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 16 creatures, Indra Caverns (squad instance 2055): the Gotha Processing Plant. Sergeant Jonas Drof and
    /// Scout Hansa Naimi at their second place, flown in by an AFS dropship (Meet Sergeant Drof 1532: "at the top of the ridge in
    /// the Bane mining area, near the entrance to Gotha Processing Plant"; the receiver of 1532 and the giver of Subversion 1451,
    /// the giver of Desistance 1452), and Overlord Kretas in Bane Central Administration (1452's boss, his hand opens his
    /// teleporter). The World DB has none of them; set 15's Drof and Naimi (AshenIndraFacilityNpcsV1) stay in the Brann facility.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 16 790600-790639): 790600 Drof's second row, 790601 Naimi's second
    /// row (creature, look, stats, package, pool each), 790602 Kretas (creature, stats) with his pool 790603.
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9427 "Sergeant Jonas Drof", 9530 "Scout Hansa Naimi", 9407 "Overlord Kretas".
    ///    Packages (client objectiveconversation, 1.0): 1544 Drof (1532,1), 1651 Naimi (set 15's).
    ///  - Places: client text 1532 (1.0) "the top of the ridge in the Bane mining area, near the entrance to Gotha Processing
    ///    Plant"; the plant's entrance static ArchBaneIndustrialEntranceV01 (-308, 128, -56) (map statics); Bane Central
    ///    Administration: the client map's ArchBaneIndustrialRoomControl24mV10 (-364, 168, -432), its north hall and the Command
    ///    Center monitor (-364, 176, -460) (map statics); the region name of the World DB (-364, 165, -432).
    ///  - Kretas' kind (ESTIMATE: no source gives his class, level, hit points or armour): the Ashen Overseers' row (Overseer Mitecht 520004: class 10504 Bane_Thrax_Soldier_Rifle_Boss, level 44,
    ///    4900 hit points, armour 420), as set 15's Kuutk; the class has none of the client's IMMUNE_* damage flags
    ///    (creature_class_flag 125-135).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Drof and Naimi copies of set 15's rows (Captain Flynn Harris' look, AFS), their levels; their second rows stand from the
    ///    start (an NPC that gives a mission is a pool's; the client's dropship arrival is not built), and set 15's stay in the
    ///    facility: both places show them (docs/ashen-playtest.md).
    ///  - Drof and Naimi just inside the plant's entrance hall, Drof at (-309, -53), Naimi at (-306, -57), 34 m and more outside
    ///    every hostile pool's circle (the ridge's Thrax pool 531096 west, 10 m below; 531089 35 m below in the mining cave: 3D).
    ///  - Kretas alone in Central Administration's north hall (-364, 168.71, -406), one, radius 0, respawn 3000 tenths (300 s, fix
    ///    F's boss rule; a public-spawn actor of 1452's scene, SpawnPoolManager.ReturnsOnOwnTime), 38 m and more outside every
    ///    hostile pool's circle (the Command Center's 531095 south, 531092 north at the tower): he is fought alone.
    /// </summary>
    public static class AshenGothaPlantNpcsV1
    {
        public const uint Caverns = AshenIndraFacilityNpcsV1.Caverns;
        public const uint HitPoints = AshenLeadInNpcsV1.HitPoints, Armor = AshenLeadInNpcsV1.Armor, Friendly = 1;

        public const uint Drof = 790600, DrofName = AshenIndraFacilityNpcsV1.DrofName, DrofPackage = AshenIndraFacilityNpcsV1.DrofPackage,
            DrofLevel = AshenIndraFacilityNpcsV1.DrofLevel;
        public const uint Naimi = 790601, NaimiName = AshenIndraFacilityNpcsV1.NaimiName, NaimiPackage = AshenIndraFacilityNpcsV1.NaimiPackage,
            NaimiLevel = AshenIndraFacilityNpcsV1.NaimiLevel;

        public const uint Kretas = 790602, KretasName = 9407, KretasPool = 790603;
        public const uint KretasClass = AshenIndraFacilityNpcsV1.KuutkClass;    // Bane_Thrax_Soldier_Rifle_Boss
        // ESTIMATE: class 10504, level 44, 4900 hit points and armour 420 copied from the Overseers' row (Mitecht), no source.
        public const uint KretasLevel = 44, KretasHitPoints = 4900, KretasArmor = 420;
        public const uint KretasRespawnTenths = 3000;   // 300 s (fix F's boss respawn, ESTIMATE)
        public const uint NextFreeId = 790604;

        /// <summary>The World DB rows the new ones copy: Captain Flynn Harris (AFS), Overseer Mitecht (the Overseers' row).</summary>
        public const uint Harris = AshenLeadInNpcsV1.Harris, Mitecht = AshenAshokaNpcsV1.Mitecht;

        public static readonly (double X, double Y, double Z) DrofAt = (-309, 129.55, -53);
        public static readonly (double X, double Y, double Z) NaimiAt = (-306, 129.55, -57);
        public static readonly (double X, double Y, double Z) KretasAt = (-364, 168.71, -406);

        /// <summary>The new friendly creatures: id, comment, name, level, package.</summary>
        public static readonly (uint Id, string Comment, uint Name, uint Level, uint Package)[] Friends =
        {
            (Drof, "Sergeant Jonas Drof - Indra Caverns, the Gotha plant's entrance (1532, 1451)", DrofName, DrofLevel, DrofPackage),
            (Naimi, "Scout Hansa Naimi - Indra Caverns, the Gotha plant's entrance (1452)", NaimiName, NaimiLevel, NaimiPackage)
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
                // npc_package.comment is varchar(50): the name and the missions only ("Sergeant Jonas Drof (1532, 1451)").
                var packageComment = comment[..comment.IndexOf(" - ", StringComparison.Ordinal)] + comment[comment.LastIndexOf(" (", StringComparison.Ordinal)..];
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, {S(packageComment)});";
                var at = At(id);
                yield return Pool(id, at.X, at.Y, at.Z, Caverns, id, 1, 1, 20, 0);
            }

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Kretas}, 'Overlord Kretas - Indra Caverns, Bane Central Administration (1452)', {KretasClass}, faction, {KretasLevel}, " +
                         $"{KretasHitPoints}, {KretasName}, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, " +
                         $"action7, action8 FROM creature WHERE id = {Mitecht};";
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                         $"SELECT {Kretas}, body, mind, spirit, {KretasHitPoints}, {KretasArmor} FROM creature_stat WHERE id = {Mitecht};";
            yield return Pool(KretasPool, KretasAt.X, KretasAt.Y, KretasAt.Z, Caverns, Kretas, 1, 1, KretasRespawnTenths, 0);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Drof}, {Naimi}, {KretasPool});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Drof}, {Naimi});";
            yield return $"DELETE FROM creature_stat WHERE id IN ({Drof}, {Naimi}, {Kretas});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Drof}, {Naimi});";
            yield return $"DELETE FROM creature WHERE id IN ({Drof}, {Naimi}, {Kretas});";
        }

        public static (double X, double Y, double Z) At(uint pool) => pool switch
        {
            Drof => DrofAt,
            Naimi => NaimiAt,
            KretasPool => KretasAt,
            _ => throw new ArgumentOutOfRangeException(nameof(pool))
        };

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";

        /// <summary>The creature ids this set adds (for the tests).</summary>
        public static uint[] Creatures => Friends.Select(row => row.Id).Append(Kretas).ToArray();
    }
}
