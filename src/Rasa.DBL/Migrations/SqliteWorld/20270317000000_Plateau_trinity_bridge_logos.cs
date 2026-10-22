using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Trinity Bridge Logos: Future, Disperse, Logos, Far and Distance (missions 1047, 1048, 1049, 1051, 1057, Plateau) (PlateauTrinityBridgeLogosV1). Data only.
    /// </summary>
    public partial class Plateau_trinity_bridge_logos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauTrinityBridgeLogosV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauTrinityBridgeLogosV1.Down(migrationBuilder);
        }
    }
}
