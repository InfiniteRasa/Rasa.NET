using System.Collections.Generic;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Desolation Quarry's five Surveillance Sentinels (map 1497), Photo Ops' (929) targets: each drops its
    /// surveillance data disk. The server had no sentinel at all.
    ///
    /// Client: names "Desolation Quarry Surveillance Sentinel One" to "Five" (creaturenamelanguage
    /// 8909-8913); objectives 929,5-9 "Destroy the first [...fifth] Bane Surveillance Sentinel and obtain its
    /// surveillance data". Class 7344 Bane_Sentinel "Sentinel" (mesh 18148, skeleton 13923). That skeleton
    /// animates the client's bot weapon attack, WEAPON_ATTACK_BOTS 1/151 (action_level: recovery family 887,
    /// range 40), and no other weapon attack.
    ///
    /// Sourced: TaRapedia Photo Ops: "The surveillance bots are randomly patrolling different levels of
    /// Desolation Quarry [...] the small white dot indicating an elite mob. Each bot is numbered according
    /// to the Data Disk they drop, so you should work your way down the spiral"; "Once the bots see you,
    /// they usually call in a big group of Thrax".
    ///
    /// Reconstructed (ESTIMATE): level 31, elite: twice the Plateau Thrax Soldier's (531082, level 27) hit
    /// points, 4100, armor 200; 1/151 at 90-135 (the Thrax Soldier's weapon, 87-131), range 40, every 1.5 s.
    /// One sentinel a pool, numbered down the spiral: the rim (424), then 374, 340, 309 and the bottom by the
    /// landing pad (284), each on the navmesh's ground reached from Trinity Bridge; respawn 300 s, radius 8.
    /// The call for Thrax help is not built (the server has no such ability).
    ///
    /// Ids: creatures 650037-650041, action 650029, pools 650047-650051, free before. Plain SQL.
    /// </summary>
    public static class PlateauSurveillanceSentinelsV1
    {
        public const uint MapContextId = 1497;
        public const uint SentinelClass = 7344;         // Bane_Sentinel
        public const uint Weapon = 650029, Level = 31, HitPoints = 4100, Armor = 200;
        public const uint RespawnSeconds = 300, Radius = 8;

        /// <summary>(creature, pool, name id, place), sentinel One to Five.</summary>
        public static readonly (uint Id, uint Pool, uint Name, (double X, double Y, double Z) At)[] Sentinels =
        {
            (650037, 650047, 8909, (390, 424.08, -430)),
            (650038, 650048, 8910, (230, 374.02, -340)),
            (650039, 650049, 8911, (260, 339.68, -280)),
            (650040, 650050, 8912, (290, 309.1, -290)),
            (650041, 650051, 8913, (320, 283.78, -250)),
        };

        public static IEnumerable<string> Up()
        {
            yield return Action(Weapon, "Surveillance Sentinel - Plateau weapon 1/151", 1, 151, 0.5, 40, 1500, 0, 90, 135, 1);
            var number = 0;
            foreach (var sentinel in Sentinels)
            {
                number++;
                yield return Creature(sentinel.Id, $"Desolation Quarry Surveillance Sentinel {number} - Plateau", SentinelClass,
                    Level, HitPoints, sentinel.Name, Weapon);
                yield return Stat(sentinel.Id, HitPoints, Armor);
                yield return Pool(sentinel.Pool, sentinel.At.X, sentinel.At.Y, sentinel.At.Z, MapContextId, sentinel.Id, 1, 1,
                    RespawnSeconds, Radius);
            }
        }

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {Sentinels[0].Pool} AND {Sentinels[^1].Pool};";
            yield return $"DELETE FROM creature_stat WHERE id BETWEEN {Sentinels[0].Id} AND {Sentinels[^1].Id};";
            yield return $"DELETE FROM creature WHERE id BETWEEN {Sentinels[0].Id} AND {Sentinels[^1].Id};";
            yield return $"DELETE FROM creature_action WHERE id = {Weapon};";
        }
    }
}
