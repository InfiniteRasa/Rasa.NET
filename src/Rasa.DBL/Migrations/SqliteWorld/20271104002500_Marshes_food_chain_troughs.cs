using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 19: Fixing the Food Chain's twelve feed acceptors stand at their troughs' origins and turns
    /// (MarshesFixingTheFoodChainV2 in place of MarshesFixingTheFoodChainV1), so the food lies in the trough. Down puts back V1.
    /// Data only.
    /// </summary>
    public partial class Marshes_food_chain_troughs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesFixingTheFoodChainV1.Down(migrationBuilder);
            MarshesFixingTheFoodChainV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesFixingTheFoodChainV2.Down(migrationBuilder);
            MarshesFixingTheFoodChainV1.Up(migrationBuilder);
        }
    }
}
