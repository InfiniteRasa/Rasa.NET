using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes: A Taste of Their Own Medicine's (546) hack waves attack the player, not Hax and the medic, and are
    /// three Thrax each, so one player can hold the hack (MarshesHackWavesOnPlayerV1). Down writes the binding back as
    /// MarshesTasteOfTheirOwnMedicineV1 wrote it. Data only.
    /// </summary>
    public partial class Marshes_hack_waves_on_player : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => MarshesHackWavesOnPlayerV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => MarshesHackWavesOnPlayerV1.Down(migrationBuilder);
    }
}
