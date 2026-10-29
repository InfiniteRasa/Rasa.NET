using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Special Agent Perdu's missions' NPCs: Wainwright, Russ, the four implanted soldiers, Clean Slate's soldier; Perdu and Clark moved (PlateauPerduNpcsV1). Data only.
    /// </summary>
    public partial class Plateau_perdu_npcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauPerduNpcsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauPerduNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
