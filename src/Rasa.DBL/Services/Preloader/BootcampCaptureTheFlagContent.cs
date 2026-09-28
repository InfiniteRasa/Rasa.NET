using System.Linq;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    internal static class BootcampCaptureTheFlagContent
    {
        internal static void Up(MigrationBuilder migration)
        {
            migration.Sql("update spawnpool set mode = 1 where map_context_id = 1985 and id in (520009, 520010);");
            migration.Sql(
                "insert into creature_action " +
                "(id, description, action_id, action_arg_id, range_min, range_max, cooldown, windup, min_damage, max_damage) values " +
                "(510214, 'Bootcamp Forean shaman staff', 1, 146, 1, 24, 1500, 0, 10, 15), " +
                "(510216, 'Bootcamp Thrax initiate pistol', 1, 1, 0.5, 24, 1300, 0, 8, 12);");
            migration.Sql(
                "insert into creature " +
                "(id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                "action1, action2, action3, action4, action5, action6, action7, action8) values " +
                "(510213, 'Bootcamp Forean Guardsman Initiate', 7034, 1, 5, 600, 7874, 7, 2, 5, 17, 0, 0, 0, 0, 0, 0), " +
                "(510214, 'Bootcamp Forean Shaman Initiate', 7035, 1, 5, 600, 7890, 7, 2, 510214, 0, 0, 0, 0, 0, 0, 0), " +
                "(510215, 'Bootcamp Forean Archer Initiate', 7036, 1, 5, 600, 7986, 7, 2, 28, 0, 0, 0, 0, 0, 0, 0), " +
                "(510216, 'Bootcamp Thrax Infantry Initiate', 29769, 0, 2, 180, 7674, 7, 0, 510216, 0, 0, 0, 0, 0, 0, 0), " +
                "(510217, 'Bootcamp AFS bridge soldier', 29423, 1, 4, 300, 0, 7, 0, 2, 0, 0, 0, 0, 0, 0, 0);");
            migration.Sql(
                "insert into creature_stat (id, body, mind, spirit, health, armor) values " +
                "(510216, 6, 6, 6, 180, 0), (510217, 12, 12, 12, 300, 50);");
            migration.Sql("update creature set run_speed = 7, action1 = 41 where id = 510210;");
            migration.Sql(
                "insert into creature_appearance (id, slot_id, class_id, color) values " +
                "(510213, 13, 6042, 1), (510214, 13, 6164, 1), (510215, 13, 6453, 1), " +
                "(510216, 13, 29884, 1), (510217, 13, 27220, 1), (510210, 13, 29885, 1);");

            BootcampWorldContentSeedData.Insert(migration, SpawnPoolEntry.TableName, typeof(SpawnPoolEntry), new[]
            {
                Pool(510216, 332, 121.90938, 64, 1.570796, 510217),
                Pool(510217, 332, 121.90938, 68, 1.570796, 510217),
                Pool(510218, 318, 120.80204, 64, -1.570796, 510216),
                Pool(510219, 318, 120.84154, 68, -1.570796, 510216),
                Pool(510220, 317.24707, 121.662315, 71.811775, -1.570796, 510216)
            });
            migration.Sql(
                "update mission_spawn_group set enabled = 1, comment = 'Forean companions' " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 1;");
            migration.Sql(
                "delete from mission_spawn where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 1;");
            migration.Sql(
                "insert into mission_spawn " +
                "(mission_id, content_revision, spawn_group_id, spawn_id, creature_id, pos_x, pos_y, pos_z, rotation, quantity) values " +
                "(1994, 'deployment_11', 1, 1, 510213, 368, 120.21479, 158, 0, 1), " +
                "(1994, 'deployment_11', 1, 2, 510214, 372, 119.956856, 158, 0, 1), " +
                "(1994, 'deployment_11', 1, 3, 510215, 374, 119.74777, 164, 0, 1);");
            migration.Sql(
                "update mission_spawn set pos_y = 109.577324 " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 2 and spawn_id = 1;");
            migration.Sql(
                "update mission_spawn set pos_y = 109.64925 " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 3 and spawn_id = 1;");
        }

        internal static void Down(MigrationBuilder migration)
        {
            migration.Sql(
                "update mission_spawn set pos_y = 109.25 " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 2 and spawn_id = 1;");
            migration.Sql(
                "update mission_spawn set pos_y = 109.0 " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 3 and spawn_id = 1;");
            migration.Sql(
                "delete from mission_spawn where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 1;");
            BootcampWorldContentSeedData.Insert(migration, MissionSpawnEntry.TableName, typeof(MissionSpawnEntry),
                BootcampWorldContentSeedData.MissionSpawns().Where(row => (uint)row[0] == 1994 && (uint)row[2] == 1));
            migration.Sql(
                "update mission_spawn_group set enabled = 0, comment = 'AFS escort' " +
                "where mission_id = 1994 and content_revision = 'deployment_11' and spawn_group_id = 1;");
            migration.Sql("delete from spawnpool where map_context_id = 1985 and id between 510216 and 510220;");
            migration.Sql("delete from creature_appearance where id between 510213 and 510217 or (id = 510210 and slot_id = 13 and class_id = 29885);");
            migration.Sql("delete from creature_stat where id in (510216, 510217);");
            migration.Sql("delete from creature where id between 510213 and 510217;");
            migration.Sql("delete from creature_action where id in (510214, 510216);");
            migration.Sql("update creature set run_speed = 0, action1 = 0 where id = 510210;");
            migration.Sql("update spawnpool set mode = 0 where map_context_id = 1985 and id in (520009, 520010);");
        }

        private static object[] Pool(
            uint id, double x, double y, double z, double rotation, uint creatureId) =>
            new object[]
            {
                id, 0U, 0U, 20U, x, y, z, rotation, 1985U, creatureId, 1U, 1U,
                0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U
            };
    }
}
