using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Capture the Flag's scene and the bootcamp experience with Youngblood's squad and the
    /// cave-in's view radius both (BootcampCaveInFortSquadV11): the two migrations that each wrote
    /// them whole share a timestamp, and the one run second undid the other. Data only: no model
    /// of its own.
    ///
    /// Down puts back the squad's bindings.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270110000000_Bootcamp_cave_in_with_fort_squad")]
    public class Bootcamp_cave_in_with_fort_squad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => BootcampCaveInFortSquadV11.Up(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) => BootcampCaveInFortSquadV11.Down(migrationBuilder);
    }
}
