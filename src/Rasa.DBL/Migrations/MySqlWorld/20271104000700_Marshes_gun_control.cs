using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes set 8, Gun Control (High Point Retreat): Retread Lou, Shaman Masai and a Lightbender pool
    /// (MarshesGunControlNpcsV1), with Lou's Gun Control (1865, MarshesGunControlV1), Gun Control - Part II (1866,
    /// MarshesGunControlPartTwoV1) and Gun Control - Part III (1867, MarshesGunControlPartThreeV1, Retread Duvall's choice),
    /// and its two ends Gun Control - Part IV for Lou (1868) and for Duvall (1904, MarshesGunControlPartFourV1). Data only.
    /// </summary>
    public partial class Marshes_gun_control : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesGunControlNpcsV1.Up())
                migrationBuilder.Sql(sql);
            MarshesGunControlV1.Up(migrationBuilder);
            MarshesGunControlPartTwoV1.Up(migrationBuilder);
            MarshesGunControlPartThreeV1.Up(migrationBuilder);
            MarshesGunControlPartFourV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesGunControlPartFourV1.Down(migrationBuilder);
            MarshesGunControlPartThreeV1.Down(migrationBuilder);
            MarshesGunControlPartTwoV1.Down(migrationBuilder);
            MarshesGunControlV1.Down(migrationBuilder);
            foreach (var sql in MarshesGunControlNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
