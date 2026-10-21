using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// General Bailey, Liaison Ridout and the Hermit; Retread McCormick moved to the Marshes (PlateauBaileyNpcsV1). Data only.
    /// </summary>
    public partial class Plateau_bailey_npcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauBaileyNpcsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauBaileyNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
