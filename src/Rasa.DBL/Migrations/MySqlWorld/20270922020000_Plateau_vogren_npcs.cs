using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Caretaker Tendahi and Vogren's hologram on Mount Reverence (PlateauVogrenNpcsV1). Data only.
    /// </summary>
    public partial class Plateau_vogren_npcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauVogrenNpcsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauVogrenNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
