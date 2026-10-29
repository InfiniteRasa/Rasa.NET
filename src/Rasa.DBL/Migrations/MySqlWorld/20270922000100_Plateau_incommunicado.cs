using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Incommunicado (mission 1040, Plateau) and the three recon squads it sends the player to:
    /// commanders Carvelle, Petrie and Locke with their Field Ops (PlateauReconSquadsV1,
    /// PlateauIncommunicadoV1). Data only.
    /// </summary>
    public partial class Plateau_incommunicado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauReconSquadsV1.Up())
                migrationBuilder.Sql(sql);
            PlateauIncommunicadoV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauIncommunicadoV1.Down(migrationBuilder);
            foreach (var sql in PlateauReconSquadsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
