using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlChar
{
    using Context.Char;

    /// <summary>
    /// Indexes for the two inventory tables, which every read of them went through a full scan
    /// for: character_inventory on (account_id, character_id), which a character's inventory is
    /// read by at login and deleted by with the character, and whose first column is the account
    /// lockbox's whole key; clan_inventory on clan_id, which a clan's lockbox is read by when a
    /// member arrives and on every change to it. No data changes.
    ///
    /// Down drops them.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlCharContext))]
    [Migration("20261215000000_Index_character_and_clan_inventory")]
    public partial class Index_character_and_clan_inventory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "character_inventory_index_account_character",
                table: "character_inventory",
                columns: new[] { "account_id", "character_id" });

            migrationBuilder.CreateIndex(
                name: "clan_inventory_index_clan_id",
                table: "clan_inventory",
                column: "clan_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "clan_inventory_index_clan_id", table: "clan_inventory");
            migrationBuilder.DropIndex(name: "character_inventory_index_account_character", table: "character_inventory");
        }
    }
}
