using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Rasa.Services.Preloader
{
    /// <summary>Level-11 Bane occupation squads, separately replaced five minutes after a wipe.</summary>
    public static class LandingZoneOccupationContent
    {
        public const uint Left = 690101, Right = 690102, Ramparts = 690103, Outside = 690104;
        public static bool IsPool(uint id) => id >= Left && id <= Outside;
        // Courtyard posts flank the control point; the elevated post is the existing gunship pad.
        public static readonly Vector3[] Posts = {
            new(184,163.31784f,-60), new(211,163.31784f,-60),
            LandingZoneGarrisonContent.Gunship, new(198,165.43848f,-14)
        };
        public static readonly Vector3[][] Beats = {
            new[] { new Vector3(184,163.31784f,-65), new Vector3(184,163.31784f,-48) },
            new[] { new Vector3(211,163.31784f,-65), new Vector3(211,163.31784f,-48) },
            LandingZoneGarrisonContent.Catwalks[0],
            LandingZoneGarrisonContent.MechPatrol
        };
        public static IEnumerable<string> Up()
        {
            // These templates were previously only dynamic assault troops. Ordinary WORLD
            // garrisons require validated loot bindings; retain any existing custom binding.
            yield return "CREATE TABLE landing_zone_occupation_loot_repair (creature_id INTEGER NOT NULL PRIMARY KEY);";
            foreach (var creature in new[] { 620002u, 620003u, 620006u })
            {
                var profile = creature == 620006 ? "wilderness_v1_thrax_10_14_boss" : "wilderness_v1_thrax_10_14";
                yield return $"INSERT INTO landing_zone_occupation_loot_repair (creature_id) SELECT id FROM creature WHERE id={creature} " +
                    $"AND NOT EXISTS (SELECT 1 FROM creature_loot_binding WHERE map_context_id=1220 AND creature_id={creature});";
                yield return "INSERT INTO creature_loot_binding (map_context_id,creature_id,profile_key) " +
                    $"SELECT 1220,{creature},profile_key FROM loot_profile WHERE profile_key='{profile}' " +
                    $"AND EXISTS (SELECT 1 FROM landing_zone_occupation_loot_repair WHERE creature_id={creature});";
            }
            for (var i = 0; i < Posts.Length; i++)
            {
                var id = Left + (uint)i;
                var at = Posts[i];
                yield return "INSERT INTO spawnpool (id,mode,anim_type,respown_time,pos_x,pos_y,pos_z,rotation,map_context_id," +
                    "creature_1_Id,creature_1_min_count,creature_1_max_count,creature_2_Id,creature_2_min_count,creature_2_max_count," +
                    "creature_3_Id,creature_3_min_count,creature_3_max_count,creature_4_Id,creature_4_min_count,creature_4_max_count," +
                    "creature_5_Id,creature_5_min_count,creature_5_max_count,creature_6_Id,creature_6_min_count,creature_6_max_count,radius) VALUES " +
                    $"({id},1,1,3000,{N(at.X)},{N(at.Y)},{N(at.Z)},0,1220,620002,4,6,620003,1,1," +
                    (i == 0 ? "620006,1,1," : "0,0,0,") + "0,0,0,0,0,0,0,0,0,1.5);";
                yield return $"INSERT INTO control_point_link (control_point_id,kind,object_id) VALUES (4,1,{id});";
            }
        }
        public static IEnumerable<string> Down()
        {
            yield return "DELETE FROM control_point_link WHERE control_point_id=4 AND kind=1 AND object_id BETWEEN 690101 AND 690104;";
            yield return "DELETE FROM spawnpool WHERE id BETWEEN 690101 AND 690104;";
            foreach (var creature in new[] { 620002u, 620003u, 620006u })
            {
                var profile = creature == 620006 ? "wilderness_v1_thrax_10_14_boss" : "wilderness_v1_thrax_10_14";
                yield return $"DELETE FROM creature_loot_binding WHERE map_context_id=1220 AND creature_id={creature} AND profile_key='{profile}' " +
                    $"AND EXISTS (SELECT 1 FROM landing_zone_occupation_loot_repair WHERE creature_id={creature});";
            }
            yield return "DROP TABLE landing_zone_occupation_loot_repair;";
        }
        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
