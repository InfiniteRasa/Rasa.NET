using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Velon Hollow's Holy Vengeance (1977, PlateauHolyVengeanceV1): the Bane outpost cleared with Warrior Algona and
    /// two warriors, the Law of Asperion taken to Luminary Sampei. Data only.
    /// </summary>
    public partial class Plateau_holy_vengeance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauHolyVengeanceV1.Up(migrationBuilder);
            foreach (var sql in PlateauPoolMarkersV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauPoolMarkersV1.Down())
                migrationBuilder.Sql(sql);
            PlateauHolyVengeanceV1.Down(migrationBuilder);
        }
    }
}
