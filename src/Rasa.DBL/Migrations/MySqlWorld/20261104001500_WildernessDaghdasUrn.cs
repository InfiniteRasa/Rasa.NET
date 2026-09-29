using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <inheritdoc />
    public partial class WildernessDaghdasUrn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessSkeevWorldV1.Up(migrationBuilder);
            WildernessDaghdasUrnV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessDaghdasUrnV1.Down(migrationBuilder);
            WildernessSkeevWorldV1.Down(migrationBuilder);
        }
    }
}
