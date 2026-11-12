using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 14: the body tints of the Crucible creatures, 52 rows (CrucibleCreatureTintsV1, creature_tint).
    /// Data only, plain SQL; Down deletes only the rows that still have the values Up wrote.
    /// </summary>
    public partial class Crucible_creature_tints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleCreatureTintsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleCreatureTintsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
