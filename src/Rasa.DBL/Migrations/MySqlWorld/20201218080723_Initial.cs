using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "itemtemplate_itemclass",
                columns: table => new
                {
                    itemTemplateId = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    itemClassId = table.Column<uint>(type: "int(11) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itemtemplate_itemclass", x => x.itemTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "player_exp_for_level",
                columns: table => new
                {
                    level = table.Column<uint>(type: "int(11) unsigned", nullable: false),
                    experience = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_exp_for_level", x => x.level);
                });

            migrationBuilder.CreateTable(
                name: "player_random_name",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(64)", nullable: false),
                    type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    gender = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_random_name", x => new { x.name, x.type, x.gender });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itemtemplate_itemclass");

            migrationBuilder.DropTable(
                name: "player_exp_for_level");

            migrationBuilder.DropTable(
                name: "player_random_name");
        }
    }
}
