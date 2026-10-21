using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// A Mystery Unearthed (mission 2016) (PlateauMysteryUnearthedV1). Data only.
    /// </summary>
    public partial class Plateau_mystery_unearthed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauMysteryUnearthedV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauMysteryUnearthedV1.Down(migrationBuilder);
        }
    }
}
