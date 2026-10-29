using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Cryptic Messages (mission 557, Plateau), turned in by radio (PlateauCrypticMessagesV1). Data only.
    /// </summary>
    public partial class Plateau_cryptic_messages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauCrypticMessagesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauCrypticMessagesV1.Down(migrationBuilder);
        }
    }
}
