using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// The economy log (the game server's EconomyAudit): economy_log, one row an item or a sum of
    /// credits or prestige that came to or left a character - by trade, the auction house, a
    /// wager, the clan bank, the account lockbox, a vendor, a mission, loot, a PvP kill, a
    /// transfer credit or a crafting station - with the event's other rows under one transfer id.
    /// </summary>
    public partial class Add_economy_log : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_log",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    transfer_id = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    kind = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    reference_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    account_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    other_character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "economy_log_index_character_id",
                table: "economy_log",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "economy_log_index_created_at",
                table: "economy_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "economy_log_index_item_id",
                table: "economy_log",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "economy_log_index_transfer_id",
                table: "economy_log",
                column: "transfer_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "economy_log");
        }
    }
}
