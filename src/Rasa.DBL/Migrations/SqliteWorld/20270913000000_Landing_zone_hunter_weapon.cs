using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.SqliteWorld
{
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270913000000_Landing_zone_hunter_weapon")]
    public class Landing_zone_hunter_weapon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(LandingZoneHunterWeapon.Up);
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(LandingZoneHunterWeapon.Down);
    }
}
