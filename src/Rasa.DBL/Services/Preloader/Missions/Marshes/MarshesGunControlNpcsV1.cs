using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Marshes set 8 (Gun Control) NPCs and pool, Valverde Marshes (map 1454): Retread Lou at High Point Retreat (1865,
    /// 1866 and 1867 giver, 1868 receiver), Shaman Masai at Plateau Point (1867 receiver, 1868 giver), and a pool of three
    /// Lightbenders east of the camp for 1865's rifle power cores. The server had none of them. Retread Duvall (1867's
    /// choice, 1904's receiver) is set 7's (MarshesStalkerWoodsNpcsV1).
    ///
    /// Ids (block 760300-760399, MarshesPaludosNpcsV1; checked 2026-11-02 against the World DB and the code of all worktrees:
    /// 760329-760399 free): 760329 Lou (creature, creature_appearance, npc_package, spawn pool); 760330 Masai (the same);
    /// 760331 the Lightbender (creature, creature_stat, spawn pool).
    ///
    /// Sourced: client names 10323 "Retread Lou", 8596 "Shaman Masai" (creaturenamelanguage); Lou's package 2356
    /// (objectiveconversation 1868,1), Masai's 991 (objectiveconversation 1867,1 and 1867,3); both free in the World DB
    /// npc_package. TaRapedia NPC pages: Lou -776, 241, -542 (Stalker Woods), Masai -735, 217, 171 (Plateau Point). World DB:
    /// Retread Vincent 510168 (class 3846, six appearance slots); Council Elder Moawi 38 (class 7035, a Forean with a staff
    /// in slot 13, 10533); the Lightbender rows of class 7120 (566016 Caves of Donn: no name, the rifle 54012); the Marshes'
    /// level-36 Hunter Lieutenant 531054 (10406 hit points, armour 340).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3 or lower):
    ///  - Lou a copy of Retread Vincent's row and look with his name (set 7's way), Masai a copy of Moawi's (a Forean
    ///    shaman, his staff) with hers. At TaRapedia's /loc with the navmesh height (probe of 2026-11-02); Lou walked to from
    ///    the High Point Retreat waypoint, Masai from the Plateau Point waypoint and from the camp.
    ///  - The Lightbender: 566016's row (class 7120, its two actions, no name) at level 36 with 531054's hit points and
    ///    armour (the triage's Q16). No client name: creaturenamelanguage has no plain "Lightbender" (only kinds such as
    ///    8852 Comm Officer, 9073 Infiltrator, 10366 Sharpshooter, 10663 Raider), so name_id stays 566016's 0. Three, 120 s respawn (the Marshes rows' 1200 tenths), on the walked-to lowland 100 m east
    ///    of the camp (-680, 218.18, -520): the 1865 log's "to the east"; the triage's north-east place is a navmesh island.
    /// </summary>
    public static class MarshesGunControlNpcsV1
    {
        public const uint MapContextId = MarshesMissionDataV1.MapContextId;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent;
        public const uint Moawi = 38;                   // Council Elder Moawi: Masai's row and look

        public const uint Lou = 760329, LouName = 10323, LouPackage = 2356;
        public static readonly (double X, double Y, double Z) LouAt = (-776, 241.48, -542);

        public const uint Masai = 760330, MasaiName = 8596, MasaiPackage = 991;
        public static readonly (double X, double Y, double Z) MasaiAt = (-735, 217.95, 171);

        public const uint Lightbender = 760331, LightbenderPool = 760331;
        public const uint CavesLightbender = 566016;    // Lightbender - Caves of Donn (class 7120)
        public const uint LightbenderLevel = 36, LightbenderHitPoints = 10406, LightbenderArmor = 340;
        public const uint Lightbenders = 3, LightbenderRespawnTenths = 1200, LightbenderRadius = 8;
        public static readonly (double X, double Y, double Z) LightbenderAt = (-680, 218.18, -520);

        public static IEnumerable<string> Up()
        {
            foreach (var (id, comment, row, name, package, at) in new[]
            {
                (Lou, "Retread Lou - Marshes, High Point Retreat (1865-1868)", Vincent, LouName, LouPackage, LouAt),
                (Masai, "Shaman Masai - Marshes, Plateau Point (1867, 1868)", Moawi, MasaiName, MasaiPackage, MasaiAt)
            })
            {
                yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                             "action1, action2, action3, action4, action5, action6, action7, action8) " +
                             $"SELECT {id}, '{comment}', class_id, faction, level, max_hp, {name}, run_speed, walk_speed, action1, " +
                             $"action2, action3, action4, action5, action6, action7, action8 FROM creature WHERE id = {row};";
                yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                             $"SELECT {id}, slot_id, Class_id, color FROM creature_appearance WHERE id = {row};";
                yield return $"INSERT INTO npc_package (id, package_id, comment) VALUES ({id}, {package}, '{comment[..comment.IndexOf(" - ")]} - Marshes');";
                yield return Pool(id, at.X, at.Y, at.Z, MapContextId, id, 1, 1, 20, 0);
            }

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"SELECT {Lightbender}, 'Lightbender - Marshes, east of High Point Retreat (1865)', class_id, faction, " +
                         $"{LightbenderLevel}, {LightbenderHitPoints}, name_id, run_speed, walk_speed, action1, action2, action3, " +
                         $"action4, action5, action6, action7, action8 FROM creature WHERE id = {CavesLightbender};";
            yield return Stat(Lightbender, LightbenderHitPoints, LightbenderArmor);
            yield return Pool(LightbenderPool, LightbenderAt.X, LightbenderAt.Y, LightbenderAt.Z, MapContextId, Lightbender,
                Lightbenders, Lightbenders, LightbenderRespawnTenths, LightbenderRadius);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({Lou}, {Masai}, {LightbenderPool});";
            yield return $"DELETE FROM npc_package WHERE id IN ({Lou}, {Masai});";
            yield return $"DELETE FROM creature_appearance WHERE id IN ({Lou}, {Masai});";
            yield return $"DELETE FROM creature_stat WHERE id = {Lightbender};";
            yield return $"DELETE FROM creature WHERE id IN ({Lou}, {Masai}, {Lightbender});";
        }
    }
}
