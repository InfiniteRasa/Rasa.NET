using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Wedge Rock Outpost's Thibodeau, Devinchy, McKinley and Remick moved from the rock's top into the outpost, with Cryptic Messages' two markers on them (PlateauWedgeRockNpcsV1). Data only.
    /// </summary>
    public partial class Plateau_wedge_rock_npcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauWedgeRockNpcsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauWedgeRockNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
