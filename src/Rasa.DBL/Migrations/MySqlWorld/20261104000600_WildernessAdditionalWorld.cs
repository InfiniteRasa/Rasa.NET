using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <inheritdoc />
    public partial class WildernessAdditionalWorld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessAdditionalWorldV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessAdditionalWorldV1.Down(migrationBuilder);
        }
    }
}
