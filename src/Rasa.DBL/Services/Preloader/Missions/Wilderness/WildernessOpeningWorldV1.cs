using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessOpeningWorldV1
    {
        public const uint RangerCreatureId = 630001;
        public const uint RangerSpawnId = 630001;

        public static void Up(MigrationBuilder migration)
        {
            foreach (var spawnId in new uint[] { 64, 100 })
                migration.UpdateData("spawnpool", "id", spawnId,
                    new[] { "creature_1_min_count", "creature_1_max_count" },
                    new object[] { (byte)1, (byte)1 });
            migration.UpdateData("creature", "id", 38U, "class_id", 7035U);
            migration.InsertData("npc_package", new[] { "id", "package_id", "comment" },
                new object[,]
                {
                    { 38U, 113U, "Council Elder Moawi" },
                    { 42U, 168U, "Council Elder Solis" },
                    { 43U, 112U, "Warrior Apirka" }
                });
            migration.UpdateData("spawnpool", "id", 184U,
                new[] { "pos_x", "pos_y", "pos_z", "rotation" },
                new object[] { 784.7, 287.33997, 581.1, 3.0 });
            migration.UpdateData("creature", "id", 42U, "walk_speed", 0.0);

            foreach (var spawnId in new uint[] { 63, 72, 78 })
                migration.UpdateData("spawnpool", "id", spawnId, "creature_2_Id", 51U);

            migration.Sql(
                "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, " +
                "run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) " +
                "SELECT 630001, 'Alia escort ranger (reconstructed)', class_id, faction, 4, 600, 0, " +
                "6.5, 3, action1, action2, action3, action4, action5, action6, action7, action8 " +
                "FROM creature WHERE id = 37;");
            migration.InsertData("spawnpool",
                new[]
                {
                    "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation",
                    "map_context_id", "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
                    "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
                    "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
                    "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
                    "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
                    "creature_6_Id", "creature_6_min_count", "creature_6_max_count"
                },
                new object[]
                {
                    RangerSpawnId, (byte)0, (byte)0, 20U, 806.0, 301.80417, 499.0, 3.11, 1220U,
                    RangerCreatureId, (byte)1, (byte)1,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0
                });
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.DeleteData("spawnpool", "id", RangerSpawnId);
            migration.DeleteData("creature", "id", RangerCreatureId);
            foreach (var spawnId in new uint[] { 63, 72, 78 })
                migration.UpdateData("spawnpool", "id", spawnId, "creature_2_Id", 43U);
            migration.UpdateData("spawnpool", "id", 184U,
                new[] { "pos_x", "pos_y", "pos_z", "rotation" },
                new object[] { 809.3008, 302.09375, 503.76562, 5.54 });
            migration.UpdateData("creature", "id", 42U, "walk_speed", 5.0);
            foreach (var creatureId in new uint[] { 38, 42, 43 })
                migration.DeleteData("npc_package", "id", creatureId);
            migration.UpdateData("creature", "id", 38U, "class_id", 6163U);
            foreach (var spawnId in new uint[] { 64, 100 })
                migration.UpdateData("spawnpool", "id", spawnId,
                    new[] { "creature_1_min_count", "creature_1_max_count" },
                    new object[] { (byte)0, (byte)0 });
        }
    }
}
