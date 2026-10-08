using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Services.Preloader.Loot;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <inheritdoc />
    public partial class Wilderness_ordinary_loot_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessOrdinaryLootV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessOrdinaryLootV1.Down(migrationBuilder);
        }
    }
}
