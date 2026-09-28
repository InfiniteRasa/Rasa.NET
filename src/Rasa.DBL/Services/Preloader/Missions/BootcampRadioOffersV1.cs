using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;

namespace Rasa.Services.Preloader.Missions
{
    public static class BootcampRadioOffersV1
    {
        public static void Up(MigrationBuilder migration)
        {
            var sources = JsonSerializer.Serialize(new[]
            {
                new MissionOfferSourceDefinition(MissionOfferSourceKind.ServerEvent, "bootcamp.arrival", 1985, true,
                    new CustomRequirement("character.starting-experience-active"))
            });
            migration.Sql("INSERT INTO mission_channel_policy " +
                "(mission_id, content_revision, acceptance_channel, completion_channel, radio_sources) " +
                "SELECT mission_id, content_revision, 3, 1, '" + sources.Replace("'", "''") + "' " +
                "FROM mission_content_definition WHERE mission_id = 1990 AND content_revision = 'deployment_11';");
        }
    }
}
