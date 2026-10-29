using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// A Bridge Too Far (mission 713, Plateau) and the Prototype Juggernaut north of Trinity Bridge it needs
    /// (PlateauPrototypeJuggernautV1, PlateauBridgeTooFarV1). Data only.
    /// </summary>
    public partial class Plateau_bridge_too_far : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauPrototypeJuggernautV1.Up())
                migrationBuilder.Sql(sql);
            PlateauBridgeTooFarV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBridgeTooFarV1.Down(migrationBuilder);
            foreach (var sql in PlateauPrototypeJuggernautV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
