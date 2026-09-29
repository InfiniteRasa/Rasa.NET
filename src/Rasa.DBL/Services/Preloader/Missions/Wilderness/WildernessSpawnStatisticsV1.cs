using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessSpawnStatisticsV1
    {
        private const string CreatureIds =
            "630001,630010,630040,630043,630046,630070,630071,630076,630077,630078,630079,630100,630120";

        public static void Up(MigrationBuilder migration) =>
            migration.Sql("INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                $"SELECT id, 15, 15, 15, max_hp, 100 FROM creature WHERE id IN ({CreatureIds});");

        public static void Down(MigrationBuilder migration) =>
            migration.Sql($"DELETE FROM creature_stat WHERE id IN ({CreatureIds});");
    }
}
