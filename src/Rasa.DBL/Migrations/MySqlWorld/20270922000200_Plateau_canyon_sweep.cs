using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Canyon Sweep (mission 1045, Plateau), the Maligo Elite Guards and guardmasters it and the next
    /// two missions need (PlateauMaligoGuardsV1), and the flags that Incommunicado's deliveries now set
    /// (PlateauIncommunicadoFlagsV1, PlateauCanyonSweepV1). Data only.
    /// </summary>
    public partial class Plateau_canyon_sweep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauMaligoGuardsV1.Up())
                migrationBuilder.Sql(sql);
            PlateauIncommunicadoFlagsV1.Up(migrationBuilder);
            PlateauCanyonSweepV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauCanyonSweepV1.Down(migrationBuilder);
            PlateauIncommunicadoFlagsV1.Down(migrationBuilder);
            foreach (var sql in PlateauMaligoGuardsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
