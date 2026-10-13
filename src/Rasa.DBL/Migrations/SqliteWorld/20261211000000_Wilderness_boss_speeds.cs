using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// Real speeds for the five Wilderness bosses that had none and the one that crawled, and 0
    /// for the two Council Elder rows that carried a name id in run_speed
    /// (WildernessBossSpeeds). No table changes.
    ///
    /// Down puts the old values back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Wilderness_boss_speeds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessBossSpeeds.Up(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessBossSpeeds.Down(migrationBuilder);
        }
    }
}
