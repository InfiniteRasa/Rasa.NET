using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Infected Foreans in the Ruins of Velon Das (map 1497) for Village Of The Damned (1981): the server
    /// had none.
    ///
    /// Sourced: "Kill 10 Infected Foreans among the Ruins of Velon Das." (client 20864); name "Infected
    /// Forean" 9859 (creaturenamelanguage) and class 25607 Redshirt_Forean_Gunner_Infected
    /// (physicalentityclassnamelanguage "Infected Forean"); Velon Das Ruins region marker 68 (-475, 393, 0)
    /// (uimapmarker). "They don't have a lot of health but hit fairly hard." (TaRapedia Village Of The
    /// Damned).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 30, hostile, 70% of a normal level 30 creature's hit points (2652: Thrax Grenadier -
    ///    Pools) and 130% of its weapon damage (113-170); the Forean gunner's goo gun (weapon 1/116,
    ///    appearance slot 13 class 6238, as NPC_Forean_Gunner 51).
    ///  - Four pools of 2-3 round the ruins' centre, on the navmesh's ground (flat within 3 m for 20 m,
    ///    reached from Trinity Bridge), none within 25 m of the Book of Velonar; respawn 300 s, radius 15.
    ///
    /// Ids: creature 750024, action 750026, pools 750030-750033, free before. Down takes out only these rows.
    /// </summary>
    public static class PlateauInfectedForeansV1
    {
        public const uint MapContextId = 1497;
        public const uint Infected = 750024, InfectedClass = 25607, InfectedName = 9859, Level = 30;
        public const uint HitPoints = 1856, Armor = 140;
        public const uint Weapon = 750026, GooGun = 6238, WeaponSlot = 13;
        public const uint FirstPool = 750030, LastPool = 750033;
        public const uint RespawnSeconds = 300, Radius = 15;

        /// <summary>(pool, x, y, z): round the Velon Das ruins.</summary>
        public static readonly (uint Id, double X, double Y, double Z)[] Pools =
        {
            (750030, -480, 393.59, 20),
            (750031, -500, 393.05, -10),
            (750032, -440, 392.68, 0),
            (750033, -480, 392.8, -40),
        };

        public static IEnumerable<string> Up()
        {
            yield return Action(Weapon, "Infected Forean - Plateau weapon 1/116", 1, 116, 1.0, 20.0, 2500, 0, 147, 221, 1);
            yield return Creature(Infected, "Infected Forean - Plateau", InfectedClass, Level, HitPoints, InfectedName, Weapon);
            yield return Stat(Infected, HitPoints, Armor);
            yield return $"INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES ({Infected}, {WeaponSlot}, {GooGun}, 1);";
            foreach (var pool in Pools)
                yield return PoolSeconds(pool.Id, pool.X, pool.Y, pool.Z, MapContextId, Infected, 2, 3, RespawnSeconds, Radius);
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {FirstPool} AND {LastPool};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Infected};";
            yield return $"DELETE FROM creature_stat WHERE id = {Infected};";
            yield return $"DELETE FROM creature WHERE id = {Infected};";
            yield return $"DELETE FROM creature_action WHERE id = {Weapon};";
        }
    }
}
