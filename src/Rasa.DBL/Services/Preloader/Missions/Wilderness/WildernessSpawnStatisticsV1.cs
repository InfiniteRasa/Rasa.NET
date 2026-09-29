using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessSpawnStatisticsV1
    {
        private const string CreatureIds =
            "530001,530010,530040,530043,530046,530070,530071,530076,530077,530078,530079,530100,530120";

        public static void Up(MigrationBuilder migration) =>
            migration.Sql("INSERT INTO creature_stat (id, body, mind, spirit, health, armor) " +
                $"SELECT id, 15, 15, 15, max_hp, 100 FROM creature WHERE id IN ({CreatureIds});");

        public static void Down(MigrationBuilder migration) =>
            migration.Sql($"DELETE FROM creature_stat WHERE id IN ({CreatureIds});");
    }
}
