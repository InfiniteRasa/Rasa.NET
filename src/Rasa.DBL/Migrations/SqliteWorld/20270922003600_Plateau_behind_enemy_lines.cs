using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Behind Enemy Lines (mission 555) (PlateauBehindEnemyLinesV1). Data only.
    /// </summary>
    public partial class Plateau_behind_enemy_lines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauBehindEnemyLinesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBehindEnemyLinesV1.Down(migrationBuilder);
        }
    }
}
