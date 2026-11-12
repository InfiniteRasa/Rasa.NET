using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 14: The Core of the Matter (1878) with its crates clear of the depot door's map link
    /// (MarshesCoreOfTheMatterV2). Down writes MarshesCoreOfTheMatterV1 again. Data only.
    /// </summary>
    public partial class Marshes_core_of_the_matter_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesCoreOfTheMatterV1.Down(migrationBuilder);
            MarshesCoreOfTheMatterV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesCoreOfTheMatterV2.Down(migrationBuilder);
            MarshesCoreOfTheMatterV1.Up(migrationBuilder);
        }
    }
}
