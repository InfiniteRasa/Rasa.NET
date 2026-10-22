using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Temporal Chamber (map 2029): Veryin, Colonel Hudson and the teleporter to Alia Das
    /// (PlateauTemporalChamberNpcsV1); Array Of Hope (935), Future Sacrifice (1171) and The Final
    /// Assault (1167) (PlateauArrayOfHopeV1, PlateauFutureSacrificeV1, PlateauFinalAssaultV1). Data only.
    /// </summary>
    public partial class Plateau_temporal_chamber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauTemporalChamberNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PlateauArrayOfHopeV1.Up(migrationBuilder);
            PlateauFutureSacrificeV1.Up(migrationBuilder);
            PlateauFinalAssaultV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauFinalAssaultV1.Down(migrationBuilder);
            PlateauFutureSacrificeV1.Down(migrationBuilder);
            PlateauArrayOfHopeV1.Down(migrationBuilder);
            foreach (var sql in PlateauTemporalChamberNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
