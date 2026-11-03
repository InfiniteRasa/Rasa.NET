using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>The Landing Zone assault's Thrax given the Bane rifle they shoot with (LandingZoneThraxWeapons).</summary>
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270920000000_Landing_zone_thrax_weapons")]
    public class Landing_zone_thrax_weapons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneThraxWeapons.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LandingZoneThraxWeapons.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
