using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Defiant Ones (mission 887, Plateau), Transportation Officer Mamede, and Peterson, Mauer and McLaughlin
    /// moved to their Fort Defiance places (PlateauFortDefianceNpcsV1, PlateauDefiantOnesV1). Data only.
    /// </summary>
    public partial class Plateau_defiant_ones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauFortDefianceNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PlateauDefiantOnesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauDefiantOnesV1.Down(migrationBuilder);
            foreach (var sql in PlateauFortDefianceNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
