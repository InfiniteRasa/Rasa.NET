using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    /// <summary>
    /// Retains the Wilderness equipment migration boundary. PR105 already installs the
    /// authoritative itemclass.max_hp armor values; Wilderness does not own their rollback.
    /// </summary>
    public static class WildernessRewardEquipmentV1
    {
        public static void Up(MigrationBuilder migration)
        {
        }

        public static void Down(MigrationBuilder migration)
        {
        }
    }
}
