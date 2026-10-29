using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Comic Relief (mission 1251, Plateau) and its three NPCs at Camp Resistance - Corporal "Relay" O'Brien,
    /// Major Lulu Lonigan, Private "Scrounger" Scarpelo (PlateauScroungerNpcsV1, PlateauComicReliefV1). Data only.
    /// </summary>
    public partial class Plateau_comic_relief : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauScroungerNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PlateauComicReliefV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauComicReliefV1.Down(migrationBuilder);
            foreach (var sql in PlateauScroungerNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
