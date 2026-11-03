using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Mind Bender (mission 905, Plateau): each extraction completes with its find, so one talk gives the
    /// implant; the second talk had no client line (to-fix #49) (PlateauMindBenderV1.ExtractOnFind). Data only.
    /// </summary>
    public partial class Plateau_mind_bender_extract_on_find : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauMindBenderV1.ExtractOnFind(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauMindBenderV1.ExtractByTalk(migrationBuilder);
        }
    }
}
