using System.Globalization;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Plain SQL rows for the Plateau's mission creatures, as PlateauBaneStalkersV1 writes them: valid on
    /// SQLite and MySQL. Fixed once a migration uses it.
    /// </summary>
    public static class PlateauCreatureSqlV1
    {
        public static string Action(uint id, string description, uint action, uint arg, double min, double max,
            uint cooldown, uint windup, uint damageMin, uint damageMax, uint type) =>
            "INSERT INTO creature_action (id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, " +
            $"min_damage, max_damage, damage_type) VALUES ({id}, '{description}', {action}, {arg}, {F(min)}, {F(max)}, " +
            $"{cooldown}, {windup}, {damageMin}, {damageMax}, {type});";

        public static string Creature(uint id, string comment, uint classId, uint level, uint hitPoints, uint nameId,
            uint action1, uint action2 = 0, uint action3 = 0) =>
            "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
            "action1, action2, action3, action4, action5, action6, action7, action8) " +
            $"VALUES ({id}, '{comment}', {classId}, 0, {level}, {hitPoints}, {nameId}, 9, 5, " +
            $"{action1}, {action2}, {action3}, 0, 0, 0, 0, 0);";

        public static string Stat(uint id, uint hitPoints, uint armor) =>
            $"INSERT INTO creature_stat (id, body, mind, spirit, health, armor) VALUES ({id}, 15, 15, 15, {hitPoints}, {armor});";

        public static string Pool(uint id, double x, double y, double z, uint mapContextId, uint creature,
            uint min, uint max, uint respawnSeconds, uint radius) =>
            "INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, " +
            "creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, " +
            "creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
            "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count, radius) " +
            $"VALUES ({id}, 0, 0, {respawnSeconds}, {F(x)}, {F(y)}, {F(z)}, 0, {mapContextId}, " +
            $"{creature}, {min}, {max}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, {radius});";

        public static string F(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
