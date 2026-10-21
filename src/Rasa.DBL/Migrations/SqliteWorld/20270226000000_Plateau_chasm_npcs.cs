using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Stainless Steel Bloodhound's Modified Bane Sentinel template (PlateauChasmNpcsV1). Data only.
    /// </summary>
    public partial class Plateau_chasm_npcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauChasmNpcsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauChasmNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
