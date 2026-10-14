using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The eruptions of Arieki's geysers and hornitos (AriekiVentEruptions): world_usable rows
    /// 10001-10308, a RandomExplosionAriekiGeyser or RandomExplosionAriekiHornitoEruption at each
    /// vent the client's maps draw, the smoking hornitos and small geysers left out. Data only.
    ///
    /// Down deletes the rows. The Designer carries the model, which InsertData needs for the
    /// columns' types.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Arieki_vent_eruptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            new AriekiVentEruptions().Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            new AriekiVentEruptions().Remove(migrationBuilder);
        }
    }
}
