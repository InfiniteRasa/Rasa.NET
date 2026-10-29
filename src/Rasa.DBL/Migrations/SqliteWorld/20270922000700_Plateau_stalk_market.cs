using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Stalk Market (mission 1017, Plateau) and the Bane Stalkers south of Trinity Bridge it needs
    /// (PlateauBaneStalkersV1, PlateauStalkMarketV1). Data only.
    /// </summary>
    public partial class Plateau_stalk_market : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauBaneStalkersV1.Up())
                migrationBuilder.Sql(sql);
            PlateauStalkMarketV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauStalkMarketV1.Down(migrationBuilder);
            foreach (var sql in PlateauBaneStalkersV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
