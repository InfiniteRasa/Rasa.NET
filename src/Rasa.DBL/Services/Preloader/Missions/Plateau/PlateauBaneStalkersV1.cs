using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Bane Stalkers on the Plateau (map 1497) for The Stalk Market (1017): the server had none there.
    ///
    /// Sourced: "Hunt Bane Stalkers on the battlefield, south of Trinity Bridge" (client log 5588);
    /// "The Stalkers can be found south of Trinity Bridge, near the path to the mountains between
    /// the two bases. Frequently they will appear as a pair." (TaRapedia The Stalk Market). Trinity
    /// Bridge is map region 15 (-119.8, 386, 210.5).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - One creature, scaled to level 31 from the server's Marshes Stalker (540002, level 34): the
    ///    same client class 3781 "Stalker" and no name id (the client names it by its class), its
    ///    weapon 1/78, stalker_ovulate and stalker_egg_drop, hit points, armor and damage about 0.9
    ///    of it.
    ///  - Four pools of a pair, respawn 300 s, radius 20, south of the bridge on the navmesh's ground,
    ///    each in the walk area that the bridge's navmesh polygon reaches and flat within 3 m for
    ///    20 m round it.
    ///
    /// Ids: creature 750021, actions 750019-750021, pools 750021-750024, free before. Plain SQL,
    /// valid on SQLite and MySQL. Down takes out only these rows.
    /// </summary>
    public static class PlateauBaneStalkersV1
    {
        public const uint MapContextId = 1497;
        public const uint Stalker = 750021;
        public const uint StalkerClass = 3781;          // Stalker
        public const uint Level = 31, HitPoints = 4500, Armor = 220;
        public const uint FirstAction = 750019, LastAction = 750021;
        public const uint FirstPool = 750021, LastPool = 750024;
        public const uint PairSize = 2, RespawnSeconds = 300;

        /// <summary>(id, description, action, argument, range min, range max, reuse, windup, damage min, max, type)</summary>
        private static readonly (uint Id, string Description, uint Action, uint Arg, double Min, double Max,
            uint Cooldown, uint Windup, uint DamageMin, uint DamageMax, uint Type)[] Actions =
        {
            (750019, "Stalker - Plateau 1/78", 1, 78, 1.0, 60.0, 1500, 0, 216, 324, 6),
            (750020, "Stalker - Plateau stalker_ovulate", 441, 1, 0.0, 20.0, 60000, 0, 40, 78, 1),
            (750021, "Stalker - Plateau stalker_egg_drop", 442, 1, 0.0, 0.0, 0, 3666, 1472, 1767, 5),
        };

        /// <summary>(pool, x, y, z): south of Trinity Bridge, on the navmesh's ground.</summary>
        public static readonly (uint Id, double X, double Y, double Z)[] Pairs =
        {
            (750021, -120, 374.8, 40),
            (750022, -120, 374.98, -50),
            (750023, -210, 375.06, 60),
            (750024, -260, 375.92, -20),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var action in Actions)
                yield return "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, " +
                    $"min_damage, max_damage, damage_type) VALUES ({action.Id}, '{action.Description}', {action.Action}, {action.Arg}, " +
                    $"{F(action.Min)}, {F(action.Max)}, {action.Cooldown}, {action.Windup}, {action.DamageMin}, {action.DamageMax}, {action.Type});";

            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) " +
                $"VALUES ({Stalker}, 'Stalker - Plateau', {StalkerClass}, 0, {Level}, {HitPoints}, 0, 9, 5, " +
                $"{FirstAction}, {FirstAction + 1}, {FirstAction + 2}, 0, 0, 0, 0, 0);";
            yield return "INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                $"VALUES ({Stalker}, 15, 15, 15, {HitPoints}, {Armor});";

            foreach (var pair in Pairs)
                yield return "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
                    "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
                    "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
                    $"VALUES ({pair.Id}, 0, 0, {RespawnSeconds * 10}, {F(pair.X)}, {F(pair.Y)}, {F(pair.Z)}, 0, {MapContextId}, " +
                    $"{Stalker}, {PairSize}, {PairSize}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 20);";
        }

        private static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id BETWEEN {FirstPool} AND {LastPool};";
            yield return $"DELETE FROM creature_stat WHERE id = {Stalker};";
            yield return $"DELETE FROM creature WHERE id = {Stalker};";
            yield return $"DELETE FROM creature_action WHERE id BETWEEN {FirstAction} AND {LastAction};";
        }
    }
}
