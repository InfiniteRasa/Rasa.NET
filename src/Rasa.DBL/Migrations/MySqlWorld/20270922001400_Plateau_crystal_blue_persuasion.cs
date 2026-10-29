using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Crystal Blue Persuasion (mission 954, Plateau), the Mox Lair's Mox and its Alpha Male moved into it
    /// (PlateauMoxLairV1, PlateauCrystalBluePersuasionV1). Data only.
    /// </summary>
    public partial class Plateau_crystal_blue_persuasion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauMoxLairV1.Up())
                migrationBuilder.Sql(sql);
            PlateauCrystalBluePersuasionV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauCrystalBluePersuasionV1.Down(migrationBuilder);
            foreach (var sql in PlateauMoxLairV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
