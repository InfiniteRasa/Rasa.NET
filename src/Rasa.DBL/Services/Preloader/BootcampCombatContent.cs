using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    internal static class BootcampCombatContent
    {
        internal static void Up(MigrationBuilder migration)
        {
            for (var level = 8; level <= 13; level++)
            {
                var id = 510221 + level - 8;
                migration.Sql(
                    "insert into creature_action " +
                    "(id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage) values " +
                    $"({id}, 'Bootcamp route Thrax rifle level {level}', 1, 1, 0.5, 24, 1600, 0, {level + 4}, {level + 9});");
                migration.Sql(
                    "insert into creature " +
                    "(id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                    "action1, action2, action3, action4, action5, action6, action7, action8) values " +
                    $"({id}, 'Bootcamp route Thrax level {level}', 29769, 0, {level}, {level * 60}, 7674, 7, 2, " +
                    $"{id}, 0, 0, 0, 0, 0, 0, 0);");
                migration.Sql(
                    "insert into creature_stat (id, body, mind, spirit, health, armor) values " +
                    $"({id}, {level * 3}, {level * 3}, {level * 3}, {level * 60}, {level * 12});");
                migration.Sql(
                    "insert into creature_appearance (id, slot_id, class_id, color) values " +
                    $"({id}, 13, 29884, 1);");
            }
            migration.Sql("update creature set run_speed = 7, walk_speed = 2, action1 = 2 where id = 510207;");
            migration.Sql("update creature set level = 13 where id = 510210;");
            migration.Sql(
                "update mission_spawn set pos_y = 109.2201 " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 3 and spawn_id = 1;");
            BootcampWorldContentSeedData.Insert(migration, SpawnPoolEntry.TableName, typeof(SpawnPoolEntry), Pools());
        }

        internal static void Down(MigrationBuilder migration)
        {
            migration.Sql("delete from spawnpool where map_context_id = 1985 and id between 510230 and 510271;");
            migration.Sql("delete from creature_appearance where id between 510221 and 510226;");
            migration.Sql("delete from creature_stat where id between 510221 and 510226;");
            migration.Sql("delete from creature where id between 510221 and 510226;");
            migration.Sql("delete from creature_action where id between 510221 and 510226;");
            migration.Sql("update creature set run_speed = 0, walk_speed = 0, action1 = 0 where id = 510207;");
            migration.Sql("update creature set level = 6 where id = 510210;");
            migration.Sql(
                "update mission_spawn set pos_y = 109.64925 " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 3 and spawn_id = 1;");
        }

        // Measured on the checked-in mesh: cave, bridge approach, base, missing team, crash-site approach.
        private static IEnumerable<object[]> Pools() => new[]
        {
            Pool(510230, 257.5, 124.997215, 68, 510221),
            Pool(510231, 260.61, 125.05937, 67.67001, 510221),
            Pool(510232, 258.88647, 125.93595, 69.31893, 510221),
            Pool(510233, 239.59999, 112.30938, 90.000015, 510222),
            Pool(510234, 242.5, 112.35953, 90, 510222),
            Pool(510235, 241, 111.09601, 92, 510222),
            Pool(510236, 241.51999, 112.84938, 89.04001, 510222),
            Pool(510237, 212.5, 105.16959, 96, 510222),
            Pool(510238, 215.5, 105.232285, 96, 510222),
            Pool(510239, 214, 105.18951, 98, 510222),
            Pool(510240, 156.5, 105.109375, 100, 510223),
            Pool(510241, 159.5, 105.109375, 100, 510223),
            Pool(510242, 158, 105.109375, 100.000015, 510223),
            Pool(510243, 158, 105.109375, 98, 510223),
            Pool(510244, 122.5, 109.16167, 143, 510224),
            Pool(510245, 125.5, 109.188034, 143, 510224),
            Pool(510246, 124, 109.17083, 145, 510224),
            Pool(510247, 89.5, 109.320274, 157, 510225),
            Pool(510248, 92.5, 109.45938, 157, 510225),
            Pool(510249, 91, 109.45938, 159, 510225),
            Pool(510250, 91, 109.289246, 155, 510225),
            Pool(510251, 56.400013, 109.568634, 123, 510223),
            Pool(510252, 59.5, 109.71702, 122.00002, 510223),
            Pool(510253, 56.400013, 109.357994, 125, 510223),
            Pool(510254, 8.5, 102.01406, 130.80002, 510224),
            Pool(510255, 11.2, 103.65938, 130.80002, 510224),
            Pool(510256, 10, 102.46201, 132, 510224),
            Pool(510257, 10, 102.928116, 130.80002, 510224),
            Pool(510258, -58.5, 87.70938, 82, 510225),
            Pool(510259, -55.5, 87.70938, 82, 510225),
            Pool(510260, -57, 87.70938, 84, 510225),
            Pool(510261, -112.5, 83.82739, 43, 510224),
            Pool(510262, -109.5, 83.95938, 43, 510224),
            Pool(510263, -111, 84.308945, 45, 510224),
            Pool(510264, -111, 83.92533, 41, 510224),
            Pool(510265, -156.5, 84.88703, 8, 510225),
            Pool(510266, -153.5, 84.86038, 8, 510225),
            Pool(510267, -155, 84.88563, 10, 510225),
            Pool(510268, -192.5, 94.4254, -30, 510226),
            Pool(510269, -189.5, 95.01596, -30, 510226),
            Pool(510270, -191, 93.97192, -28, 510226),
            Pool(510271, -191.81537, 95.10938, -30.83075, 510226)
        };

        private static object[] Pool(uint id, double x, double y, double z, uint creatureId) =>
            new object[]
            {
                id, 0U, 0U, 120U, x, y, z, 0.0, 1985U, creatureId, 1U, 1U,
                0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U
            };
    }
}
