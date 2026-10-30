using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Soyuz Salvage: Plateau (mission 2007): the debris pieces' use argument is 1, the one the client
    /// sends for classes 29783/29784 (generated/client/usabledata). The binding had 3, and the real
    /// client's use was refused. Data only.
    /// </summary>
    public partial class Plateau_soyuz_salvage_use_argument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PlateauSoyuzSalvageV1.UseArgumentSql(PlateauSoyuzSalvageV1.OldUseArgument, PlateauSoyuzSalvageV1.UseArgument));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(PlateauSoyuzSalvageV1.UseArgumentSql(PlateauSoyuzSalvageV1.UseArgument, PlateauSoyuzSalvageV1.OldUseArgument));
        }
    }
}
