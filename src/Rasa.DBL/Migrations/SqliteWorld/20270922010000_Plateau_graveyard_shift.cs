using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Graveyard Shift (mission 933, Plateau) and the Tomb Miasmas and Bulls at Vogren's Tomb it needs
    /// (PlateauTombMiasmasV1, PlateauGraveyardShiftV1). Data only.
    /// </summary>
    public partial class Plateau_graveyard_shift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauTombMiasmasV1.Up())
                migrationBuilder.Sql(sql);
            PlateauGraveyardShiftV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauGraveyardShiftV1.Down(migrationBuilder);
            foreach (var sql in PlateauTombMiasmasV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
