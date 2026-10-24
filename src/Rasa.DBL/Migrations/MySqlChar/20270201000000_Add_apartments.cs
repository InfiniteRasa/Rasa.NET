using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// Player apartments (the game server's Managers.Apartments): apartment, one row an
    /// apartment, with its owner (kind and id, unique), its plot, name, passcode hash, whether
    /// the owner's friends may come in, and the number its next item is given; apartment_item,
    /// one row an item placed in one, a retail entity class with its place, yaw, scale and
    /// whether it is packed; apartment_visitor, one row an apartment and a character invited
    /// into it, with whether they may build; and apartment_return, one row a character inside
    /// an apartment, with the apartment and the place they came from.
    /// </summary>
    public partial class Add_apartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "apartment",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    owner_kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    owner_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    plot_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(48)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    passcode_hash = table.Column<string>(type: "varchar(64)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    friends_may_enter = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    next_item_number = table.Column<uint>(type: "int unsigned", nullable: false),
                    created_at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apartment", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "apartment_item",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    apartment_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    number = table.Column<uint>(type: "int unsigned", nullable: false),
                    class_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false),
                    scale = table.Column<double>(type: "double", nullable: false),
                    packed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    placed_by = table.Column<uint>(type: "int unsigned", nullable: false),
                    placed_at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apartment_item", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "apartment_return",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    apartment_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    pos_x = table.Column<double>(type: "double", nullable: false),
                    pos_y = table.Column<double>(type: "double", nullable: false),
                    pos_z = table.Column<double>(type: "double", nullable: false),
                    rotation = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apartment_return", x => x.character_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "apartment_visitor",
                columns: table => new
                {
                    apartment_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    may_build = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    added_at = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apartment_visitor", x => new { x.apartment_id, x.character_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "apartment_index_owner",
                table: "apartment",
                columns: new[] { "owner_kind", "owner_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "apartment_item_index_apartment_number",
                table: "apartment_item",
                columns: new[] { "apartment_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "apartment_visitor_index_character",
                table: "apartment_visitor",
                column: "character_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "apartment");

            migrationBuilder.DropTable(
                name: "apartment_item");

            migrationBuilder.DropTable(
                name: "apartment_return");

            migrationBuilder.DropTable(
                name: "apartment_visitor");
        }
    }
}
