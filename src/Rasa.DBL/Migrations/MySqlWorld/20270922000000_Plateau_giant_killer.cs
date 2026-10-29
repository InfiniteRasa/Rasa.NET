using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Giant Killer (mission 939, Plateau) and two to four Kael guards in Goliath's pool
    /// (PlateauGiantKillerV1). Data only.
    /// </summary>
    public partial class Plateau_giant_killer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauGiantKillerV1.Up(migrationBuilder);
            PlateauGiantKillerScaleV1.Up(migrationBuilder);
            foreach (var sql in PlateauGiantKillerSquadV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauGiantKillerSquadV1.Down())
                migrationBuilder.Sql(sql);
            PlateauGiantKillerScaleV1.Down(migrationBuilder);
            PlateauGiantKillerV1.Down(migrationBuilder);
        }
    }
}
