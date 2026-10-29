using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Desolation Quarry's five Surveillance Sentinels (PlateauSurveillanceSentinelsV1). Data only.
    /// </summary>
    public partial class Plateau_surveillance_sentinels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSurveillanceSentinelsV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSurveillanceSentinelsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
