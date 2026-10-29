using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Velon Hollow's Bane outpost at the instance's level, its Bane officers and Mox Alpha Male, Receptive Officer
    /// Markin and Elder Hundra's package (PlateauVelonHollowBaneV1), and two Velon Hollow missions: Ancient
    /// Mysteries (1978, PlateauAncientMysteriesV1) and Mountain Spring Toxins (1979, PlateauMountainSpringToxinsV1).
    /// Data only.
    /// </summary>
    public partial class Plateau_velon_hollow_bane : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauVelonHollowBaneV1.Up())
                migrationBuilder.Sql(sql);
            PlateauAncientMysteriesV1.Up(migrationBuilder);
            PlateauMountainSpringToxinsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauMountainSpringToxinsV1.Down(migrationBuilder);
            PlateauAncientMysteriesV1.Down(migrationBuilder);
            foreach (var sql in PlateauVelonHollowBaneV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
