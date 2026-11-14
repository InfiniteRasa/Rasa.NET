using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 3 creatures, Ligo Ashen Desert (map 1734), the Sheitan Highway north of Shadow's Edge Post, for
    /// Captain Sera Karter's Highway to Hell (1247), Lt. Colonel Trent Ivan's Air Superiority (1248) and Lt. Colonel Dania
    /// Caldwell's Artillery Annihilation (1249): the Thrax Ravager name on the World DB's unnamed Ashen Thrax Grenadier; Thrax
    /// Ravager and Thrax Soldier pools in and north of the Bane camp on the highway; the Desert Predator, new, on two beats along
    /// the road south of the camp; and the four Bane Mortars of the highway, each its own row so each kill is its own objective.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 3 790080-790119):
    ///  - 790080 the Desert Predator (creature, creature_stat); 790081-790084 Bane Heavy Mortar 1-4 (creature, creature_stat,
    ///    creature_appearance);
    ///  - 790085, 790086 the Thrax Ravager pools; 790087, 790088 the Thrax Soldier pools; 790089, 790090 the Desert Predator
    ///    pools, each with its beat (spawnpool_patrol).
    ///
    /// Sourced:
    ///  - Names (client creaturenamelanguage, 1.0): 9223 "Thrax Ravager", 9327 "Desert Predator", 355 "Thrax Soldier Mark VI"
    ///    (World DB 531008's). The Bane Mortar has no name id: the client names it from its class 7482, "Bane Mortar".
    ///  - Places: the Bane camp on the Sheitan Highway, north of Shadow's Edge Post (the client's 1247-1249 logs; the client map's
    ///    Bane floodlights, barricades and pillboxes at x -200..-120, z -347..-258; World DB region Sheitan Highway (-164, 301,
    ///    -363) r190). The mortars: World DB pools 592191-592194 on the client map's four mortar pads by the camp (BaneMortars),
    ///    "along the side of the road" (the client's 1249 offer). "The two Predators patrolling the Sheitan Highway" (the client's
    ///    1248 reminder); "head north along the road" (the 1248 offer).
    ///  - The World DB rows copied: Bane Mortar - Ashen Desert 592011 (class 7482, L42, 17500 hit points, armour 400, the launcher
    ///    in weapon slot 13); Alpha Class Recon Predator - Incline 520022 (class 3902 Creature_Bane_Predator, L42, 4700 hit
    ///    points, armour 410, its attacks), the World DB's Predator at the zone band's top.
    ///
    /// Upstream data changed here (NOTES-upstream-bugs: none, a mission need): 531009 "Thrax Grenadier - Ashen Desert" has no
    /// name id; it takes 9223 "Thrax Ravager" (triage D9: 1266 says the Ravagers carry grenade launchers). It is the grenadier of
    /// Ashen Desert, the Bane Conscription Facility and Indra Caverns, so all are Thrax Ravagers. The four highway mortar pools
    /// 592191-592194 take the copies 790081-790084 for 592011.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The Desert Predator: a copy of 520022 with the client name 9327; level 42 (520022's, the zone band's top).
    ///  - Two beats (Patrols: straight lines between floor heights), one Predator each, radius 0, 120 s respawn: the south one
    ///    on the road from (-170.6, -515.6) to (-160, -440), the north one from (-160, -425) to (-160, -365), south of the camp's
    ///    gate (z -343). Steps: the navmesh ground along the navmesh's path, 4-12 m apart (probe of 2026-11-09). The pool's
    ///    point, the Predator's home, is a step in the middle of its beat, and the beat is a loop from there (to the north end,
    ///    back past home to the south end, back home): a fight is left at BehaviorManager.MaxChaseDistance (60 m) from home
    ///    (ChasedTooFar), and every step is at most <see cref="MaxStepFromHome"/> from it, so a fight anywhere on the beat has
    ///    15 m and more before the leash.
    ///  - Pools of three, radius 0 (rings, as set 2's Atta), 120 s respawn: Ravagers inside the camp west (-180, -305) and east
    ///    (-140, -305) of the road, Soldiers at its north end (-160, -260) and north of it (-120, -225); each the navmesh ground at
    ///    its centre, 11 m or more from a wall, walked to from the Shadow's Edge Post waypoint (probe of 2026-11-09). The camp's
    ///    own pool 531034 (Soldiers, a Linker, a Technician, a Caretaker; radius 30) stays as it is.
    ///  - The mortar copies keep everything of 592011 but the id: Mortar n = the pool n of the client's "Destroy Mortar n", south
    ///    to north along the road (592191, 592192, 592193 west of it; 592194 east).
    ///
    /// Plain SQL, valid on SQLite and MySQL (the copies are INSERT ... SELECT); Down takes out only these rows and puts back the
    /// upstream values it changed, each only where this set's value still stands.
    /// </summary>
    public static class AshenSheitanHighwayNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;

        /// <summary>The Thrax of the highway (World DB): the Ravager (the grenadier, renamed) and the Soldier.</summary>
        public const uint Ravager = 531009, RavagerName = 9223, Soldier = 531008;

        /// <summary>The Desert Predator: id, name, the row it copies.</summary>
        public const uint Predator = 790080, PredatorName = 9327, PredatorCopy = 520022;

        /// <summary>The World DB's Ashen Desert Bane Mortar (BaneMortars) and its four highway pools' copies.</summary>
        public const uint Mortar = 592011;

        /// <summary>The highway mortars, Mortar 1-4: the copy, its pool (World DB, BaneMortars), the pool's place.</summary>
        public static readonly (uint Creature, uint Pool, (double X, double Y, double Z) At)[] Mortars =
        {
            (790081, 592191, (-210.888, 315.288, -409.662)),
            (790082, 592192, (-195.561, 317.335, -337.739)),
            (790083, 592193, (-180.580, 310.170, -282.694)),
            (790084, 592194, (-140.300, 310.429, -285.300))
        };

        /// <summary>Radius 0: rings round the pool's point (SpawnPoolManager.SpawnPoint), as set 2's Atta pools.</summary>
        public const uint PoolCreatures = 3, RespawnTenths = 1200, PoolRadius = 0;

        /// <summary>
        /// The Thrax pools: pool, creature, centre (as set 3 wrote them). Fix job E (AshenFixEV1.RavagerPools, migration
        /// 20271110001010) moves the two Ravager pools out of the camp pool 531034's reach and makes them pools of two.
        /// </summary>
        public static readonly (uint Pool, uint Creature, (double X, double Y, double Z) At)[] ThraxPools =
        {
            (790085, Ravager, (-180, 306.57, -305)),
            (790086, Ravager, (-140, 306.34, -305)),
            (790087, Soldier, (-160, 304.19, -260)),
            (790088, Soldier, (-120, 304.81, -225))
        };

        /// <summary>
        /// The Desert Predators' beats, south to north: pool, the step that is the pool's point (its home; <see cref="Home"/>),
        /// the steps one way. The beat is walked as a loop from home (<see cref="Beat"/>).
        /// </summary>
        public static readonly (uint Pool, int Home, (double X, double Y, double Z)[] Way)[] PredatorPools =
        {
            (790089, 5, new (double, double, double)[]
            {
                (-170.6, 295.8, -515.6), (-176.3, 295.07, -506.3), (-180, 294.47, -500), (-177.8, 294.51, -492.9),
                (-175.6, 295.51, -485.8), (-173.3, 297.11, -478.7), (-170, 298.56, -468), (-167.5, 300.07, -461), (-165, 301.25, -454),
                (-162.5, 300.39, -447), (-160, 299.52, -440)
            }),
            (790090, 2, new (double, double, double)[]
            {
                (-160, 297.59, -425), (-160.9, 298.68, -414.3), (-162, 299.39, -400), (-161.6, 299.17, -392.2), (-160.9, 300.06, -380.6),
                (-160, 301.35, -365)
            })
        };

        public const uint NextFreeId = 790091;

        /// <summary>The furthest a beat's step stands from its pool's point: the leash (BehaviorManager.MaxChaseDistance, 60 m) less 15 m.</summary>
        public const double MaxStepFromHome = 45;

        /// <summary>A Predator pool's point, its creature's home: the beat's middle step.</summary>
        public static (double X, double Y, double Z) Home(uint pool)
        {
            var (_, home, way) = PredatorPools.Single(entry => entry.Pool == pool);
            return way[home];
        }

        /// <summary>
        /// The steps of a beat as walked, from home (the first): on to the north end, back past home to the south end, and back
        /// towards home (Patrols: the last step leads to the first), every step of the way walked in both directions.
        /// </summary>
        public static (double X, double Y, double Z)[] Beat(uint pool)
        {
            var (_, home, way) = PredatorPools.Single(entry => entry.Pool == pool);
            return way.Skip(home).Concat(way.Take(way.Length - 1).Reverse()).Concat(way.Skip(1).Take(home - 1)).ToArray();
        }

        public static IEnumerable<string> Up()
        {
            yield return $"UPDATE creature SET name_id = {RavagerName} WHERE id = {Ravager} AND name_id = 0;";

            yield return Copy(Predator, "Desert Predator - Ashen Desert, Sheitan Highway (1248)", PredatorCopy, PredatorName);
            yield return CopyStat(Predator, PredatorCopy);
            for (var index = 0; index < Mortars.Length; index++)
            {
                var (creature, pool, _) = Mortars[index];
                yield return Copy(creature, $"Bane Heavy Mortar {index + 1} - Ashen Desert, Sheitan Highway (1249)", Mortar, null);
                yield return CopyStat(creature, Mortar);
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {creature}, slot_id, Class_id, color FROM creature_appearance WHERE id = {Mortar};";
                yield return $"UPDATE spawnpool SET creature_1_Id = {creature} WHERE id = {pool} AND creature_1_Id = {Mortar};";
            }

            foreach (var (pool, creature, at) in ThraxPools)
                yield return Pool(pool, at.X, at.Y, at.Z, MapContextId, creature, PoolCreatures, PoolCreatures, RespawnTenths, PoolRadius);
            foreach (var (pool, _, _) in PredatorPools)
            {
                var home = Home(pool);
                yield return Pool(pool, home.X, home.Y, home.Z, MapContextId, Predator, 1, 1, RespawnTenths, PoolRadius);
                var beat = Beat(pool);
                for (var step = 0; step < beat.Length; step++)
                    yield return "INSERT INTO spawnpool_patrol (pool_id, step, pos_x, pos_y, pos_z, facing, pause_ms) " +
                                 $"SELECT {pool}, {step}, {N(beat[step].X)}, {N(beat[step].Y)}, {N(beat[step].Z)}, NULL, 0 " +
                                 $"FROM spawnpool p WHERE p.id = {pool} AND NOT EXISTS (SELECT 1 FROM spawnpool_patrol x WHERE x.pool_id = {pool} AND x.step = {step});";
            }
        }

        public static IEnumerable<string> Down()
        {
            var predatorPools = string.Join(", ", PredatorPools.Select(pool => pool.Pool.ToString(CultureInfo.InvariantCulture)));
            var thraxPools = string.Join(", ", ThraxPools.Select(pool => pool.Pool.ToString(CultureInfo.InvariantCulture)));
            yield return $"DELETE FROM spawnpool_patrol WHERE pool_id IN ({predatorPools});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({predatorPools});";
            yield return $"DELETE FROM spawnpool WHERE id IN ({thraxPools});";
            foreach (var (creature, pool, _) in Mortars.Reverse())
            {
                // Only while the pool still holds this set's copy: a later change of the pool is not undone.
                yield return $"UPDATE spawnpool SET creature_1_Id = {Mortar} WHERE id = {pool} AND creature_1_Id = {creature};";
                yield return $"DELETE FROM creature_appearance WHERE id = {creature};";
                yield return $"DELETE FROM creature_stat WHERE id = {creature};";
                yield return $"DELETE FROM creature WHERE id = {creature};";
            }
            yield return $"DELETE FROM creature_stat WHERE id = {Predator};";
            yield return $"DELETE FROM creature WHERE id = {Predator};";
            yield return $"UPDATE creature SET name_id = 0 WHERE id = {Ravager} AND name_id = {RavagerName};";
        }

        /// <summary>
        /// <paramref name="sql"/> safe to run twice: <see cref="CrucibleSql.Guard"/>'s form, but for the beats' rows, which carry
        /// their own guard (spawnpool_patrol has no key in CrucibleSql.Keys; as AliaDasNpcs writes them).
        /// </summary>
        public static string Guarded(string sql) =>
            sql.StartsWith("INSERT INTO spawnpool_patrol ", System.StringComparison.Ordinal) ? sql : Crucible.CrucibleSql.Guard(sql);

        /// <summary>A copy of a World DB creature row with its own id, comment and (where given) name.</summary>
        private static string Copy(uint id, string comment, uint copy, uint? name) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"SELECT {id}, {S(comment)}, class_id, faction, level, max_hp, {(name.HasValue ? name.Value.ToString(CultureInfo.InvariantCulture) : "name_id")}, " +
            $"run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {copy};";

        private static string CopyStat(uint id, uint copy) =>
            "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
            $"SELECT {id}, body, mind, spirit, health, armor FROM creature_stat WHERE id = {copy};";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
