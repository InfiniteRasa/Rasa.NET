using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 9, Stonewall Ridge: Captain Marcus Emmons' Cycled Frequencies (1275, AshenCycledFrequenciesV1),
    /// On The Same Frequency (1277, AshenOnTheSameFrequencyV1) and King of the Hill (1278, AshenKingOfTheHillV1). Mission content
    /// only (the salvage crate, the consoles, the ambushes, the turret copies and the waves are scene actors on existing
    /// creature rows); no new world rows, no upstream row changes.
    /// </summary>
    public partial class Ashen_stonewall_ridge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AshenCycledFrequenciesV1.Up(migrationBuilder);
            AshenOnTheSameFrequencyV1.Up(migrationBuilder);
            AshenKingOfTheHillV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenKingOfTheHillV1.Down(migrationBuilder);
            AshenOnTheSameFrequencyV1.Down(migrationBuilder);
            AshenCycledFrequenciesV1.Down(migrationBuilder);
        }
    }
}
