using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Services.Preloader;
namespace Rasa.Migrations.MySqlWorld
{
    public partial class Ashen_dynamic_dropships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenDynamicDropshipsContent.Up()) migrationBuilder.Sql(sql);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenDynamicDropshipsContent.Down()) migrationBuilder.Sql(sql);
        }
    }
}
