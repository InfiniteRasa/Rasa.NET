using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessSkeevWorldV1
    {
        public static void Up(MigrationBuilder migration)
        {
            WildernessWaveAPopulationV1.Creature(migration, 630130, 82, 28589, 0, 15, 1500, 6734, 0, 0,
                "Thrax Overseer Skeev (reconstructed)");
            migration.Sql("INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                "SELECT 630130, slot_id, Class_id, color FROM creature_appearance WHERE id = 82;");
            migration.InsertData("npc_package", new[] { "id", "package_id", "comment" },
                new object[] { 630130U, 595U, "Thrax Overseer Skeev" });
            migration.InsertData("creature_stat", new[] { "id", "body", "mind", "spirit", "health", "armor" },
                new object[] { 630130U, 15, 15, 15, 1500, 100 });
            WildernessWaveAPopulationV1.Spawn(migration, 630130, 630130, -400, 173.679004, 178, -1.19028995);
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.DeleteData("spawnpool", "id", 630130U);
            migration.DeleteData("npc_package", "id", 630130U);
            migration.Sql("DELETE FROM creature_appearance WHERE id = 630130;");
            migration.DeleteData("creature_stat", "id", 630130U);
            migration.DeleteData("creature", "id", 630130U);
        }
    }
}
