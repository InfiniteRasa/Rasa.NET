using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    public static class BootcampEvacuationReadyV4
    {
        public static void Up(MigrationBuilder migration)
        {
            var experience = BootcampFinaleDataV2.Experience();
            experience.Scene.Sequences[4].World.Insert(0,
                new RemoveActorIntent("clear-ready-evacuation-pad", "bootcamp-dropship-debris"));
            MissionDataMigration.UpdateExperience(migration, experience);
        }

        public static void Down(MigrationBuilder migration) =>
            MissionDataMigration.UpdateExperience(migration, BootcampFinaleDataV2.Experience());
    }
}
