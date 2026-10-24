using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ustor Yard's Out of Thin Air (1190, PlateauOutOfThinAirV1): the Maligo Base Prisoner and the patrol's template
    /// (PlateauMaligoPrisonerV1). Data only.
    /// </summary>
    public partial class Plateau_out_of_thin_air : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauMaligoPrisonerV1.Up())
                migrationBuilder.Sql(sql);
            PlateauOutOfThinAirV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauOutOfThinAirV1.Down(migrationBuilder);
            foreach (var sql in PlateauMaligoPrisonerV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
