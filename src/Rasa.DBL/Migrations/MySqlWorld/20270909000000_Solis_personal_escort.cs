using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.MySqlWorld
{
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270909000000_Solis_personal_escort")]
    public class Solis_personal_escort : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach(var sql in SolisPersonalEscort.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach(var sql in SolisPersonalEscort.Down()) migrationBuilder.Sql(sql);
        }
    }
}
