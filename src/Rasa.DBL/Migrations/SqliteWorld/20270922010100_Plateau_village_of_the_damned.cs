using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Village Of The Damned (mission 1981, Plateau) and the Infected Foreans in the Velon Das ruins it
    /// needs (PlateauInfectedForeansV1, PlateauVillageOfTheDamnedV1). Data only.
    /// </summary>
    public partial class Plateau_village_of_the_damned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauInfectedForeansV1.Up())
                migrationBuilder.Sql(sql);
            PlateauVillageOfTheDamnedV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauVillageOfTheDamnedV1.Down(migrationBuilder);
            foreach (var sql in PlateauInfectedForeansV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
