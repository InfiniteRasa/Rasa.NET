using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270918000000_Solis_escort_staff")]
    public class Solis_escort_staff : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in SolisEscortStaff.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in SolisEscortStaff.Down()) migrationBuilder.Sql(sql);
        }
    }
}
