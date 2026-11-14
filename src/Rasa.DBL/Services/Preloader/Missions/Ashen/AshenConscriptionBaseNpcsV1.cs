using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 5 creatures, Ligo Ashen Desert (map 1734), the Bane Conscription Base north of the Boneyard, for
    /// Captain Akio Yamamoto's Death from Above (1352), Lieutenant Gabriel Infanger's Turning Out the Lights (1354) and
    /// Lieutenant Jan Blood's The Strongest Link (1355): the four trench mortars each their own row, Linker pools round the base,
    /// and Overseer Naxveik moved into the compound with the power generators.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 5 790160-790199):
    ///  - 790160-790163 Bane Heavy Mortar 1-4 of the trench (creature, creature_stat, creature_appearance);
    ///  - 790164-790168 the Linker pools.
    ///
    /// Sourced:
    ///  - The mortars: "Destroy the 4 Bane Mortars along the trench separating the Boneyard from the Bane Conscription Base"
    ///    (the client's 1352 log 11061); World DB pools 592199, 592202-592204 (BaneMortars, on the client map's mortar pads,
    ///    not linked to a control point) hold the Ashen Bane Mortar 592011.
    ///  - The Linkers: "There's a strong perimeter of Linkers protecting that base. Hundreds of them." (the client's 1355 offer
    ///    11146); "Linkers around the Bane Conscription Control Point now properly give player kill credit" (PlayTR EU patch
    ///    notes 2007-12-11, 0.5). The World DB's Linker - Ashen Desert 531014 (class 9751, L40, name 414 "Linker Master"),
    ///    the zone's only Linker row (one pool on the Sheitan Highway).
    ///  - Naxveik: World DB pool 520005 (Overseer Naxveik - Ashen Desert, name 9353, L44) at (305.4, 282.6, 84.4).
    ///    TaRapedia Overseer Naxveik: "Northwest of the Bane Conscription Garrison CP, at /loc 300, 20. Inside the compound
    ///    with three generators."; TaRapedia The Strongest Link: /loc 311, 26; Ellatha's 304, 83 is the map view (/loc z = map
    ///    y - 57: 26; triage X5). The client's 1355 log: "Overseer Naxveik, who is protecting the power generators". The World
    ///    DB row stands at Ellatha's map value, 60 m north of the compound and 14 m under it (upstream data, NOTES-upstream-bugs).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The mortar copies keep everything of 592011 but the id (as set 3's highway mortars): Mortar n = pool n of the four,
    ///    west to east along the trench.
    ///  - Five Linker pools of three (531014, radius 0, 120 s respawn) round the walled base north of the Garrison: at its
    ///    south gate, on the ramp up to it, and in its yard east and north of the missile launchers (1352); each on the navmesh
    ///    ground and a client floor, one floor in the column, walked to from the Ashoka Settlement waypoint, 130 m and more
    ///    from Control Point 16 and not linked to it (triage, "Control points") (probe of 2026-11-09).
    ///  - Naxveik moved into the compound: the plateau west of the Garrison (y 296), 50 m south of TaRapedia's /loc (300, 20),
    ///    where the compound has one floor (TaRapedia's spot has a second floor under the plateau's edge), 136 m from Control
    ///    Point 16; the generators of 1354 stand round him (AshenTurningOutTheLightsV1).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows, gives the mortar
    /// pools 592011 back and puts Naxveik back where upstream had him, each only while the row still holds this set's value.
    /// </summary>
    public static class AshenConscriptionBaseNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;

        /// <summary>The World DB's Ashen Desert Bane Mortar (BaneMortars), as AshenSheitanHighwayNpcsV1.Mortar.</summary>
        public const uint Mortar = AshenSheitanHighwayNpcsV1.Mortar;

        /// <summary>The trench mortars, Mortar 1-4 west to east: the copy, its pool (World DB, BaneMortars), the pool's place.</summary>
        public static readonly (uint Creature, uint Pool, (double X, double Y, double Z) At)[] Mortars =
        {
            (790160, 592199, (440.34, 288.134, -200.983)),
            (790161, 592202, (490.114, 281.215, -194.194)),
            (790162, 592203, (524.666, 283.705, -170.568)),
            (790163, 592204, (550.574, 287.546, -130.494))
        };

        /// <summary>The World DB's Linker - Ashen Desert, "Linker Master".</summary>
        public const uint Linker = 531014;

        /// <summary>Radius 0: rings round the pool's point (SpawnPoolManager.SpawnPoint), as set 2's Atta pools.</summary>
        public const uint LinkersPerPool = 3, RespawnTenths = 1200, PoolRadius = 0;

        /// <summary>The Linker pools: pool, centre (the navmesh ground).</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] LinkerPools =
        {
            (790164, (385, 304.31, 82)),
            (790165, (420, 299.37, 95)),
            (790166, (466, 304.06, 132)),
            (790167, (430, 304.13, 160)),
            (790168, (358, 304.16, 160))
        };

        /// <summary>Overseer Naxveik's pool (World DB): the place upstream gives it (its float values) and the place now.</summary>
        public const uint Naxveik = 520005;
        public static readonly (double X, double Y, double Z) NaxveikBefore = (305.4142150878906, 282.60791015625, 84.41419982910156);
        public static readonly (double X, double Y, double Z) NaxveikAt = (296, 296.49, -30);

        public const uint NextFreeId = 790169;

        public static IEnumerable<string> Up()
        {
            for (var index = 0; index < Mortars.Length; index++)
            {
                var (creature, pool, _) = Mortars[index];
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {creature}, {S($"Bane Heavy Mortar {index + 1} - Ashen Desert, the trench (1352)")}, class_id, faction, level, max_hp, name_id, " +
                             $"run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {Mortar};";
                yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                             $"SELECT {creature}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {Mortar};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {creature}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Mortar};";
                yield return $"UPDATE spawnpool SET creature_1_Id = {creature} WHERE id = {pool} AND creature_1_Id = {Mortar};";
            }

            foreach (var (pool, at) in LinkerPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, Linker, LinkersPerPool, LinkersPerPool, RespawnTenths, PoolRadius);

            yield return Place(Naxveik, NaxveikAt);
        }

        public static IEnumerable<string> Down()
        {
            // Only while Naxveik still stands where set 5 put him: a later move of his pool is not undone.
            yield return Place(Naxveik, NaxveikBefore).TrimEnd(';') +
                         $" AND ABS(pos_x - {N(NaxveikAt.X)}) < 0.01 AND ABS(pos_y - {N(NaxveikAt.Y)}) < 0.01 AND ABS(pos_z - {N(NaxveikAt.Z)}) < 0.01;";
            var pools = string.Join(", ", LinkerPools.Select(pool => pool.Pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool WHERE id IN ({pools});";
            foreach (var (creature, pool, _) in Mortars.Reverse())
            {
                // Only while the pool still holds this set's copy: a later change of the pool is not undone.
                yield return $"UPDATE spawnpool SET creature_1_Id = {Mortar} WHERE id = {pool} AND creature_1_Id = {creature};";
                yield return $"DELETE FROM creature_appearance WHERE id = {creature};";
                yield return $"DELETE FROM creature_stat WHERE id = {creature};";
                yield return $"DELETE FROM creature WHERE id = {creature};";
            }
        }

        private static string Place(uint pool, (double X, double Y, double Z) at) =>
            $"UPDATE spawnpool SET pos_x = {N(at.X)}, pos_y = {N(at.Y)}, pos_z = {N(at.Z)} WHERE id = {pool};";

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
