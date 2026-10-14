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
    /// It was 20261223000000, the timestamp Random_name_quotes has. A World that ran it under
    /// that id runs it again under this one: its rows are deleted and put in again, and its
    /// history row for the old id is removed.
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
            var eruptions = new AriekiVentEruptions();

            eruptions.Remove(migrationBuilder);
            eruptions.Preload(migrationBuilder);
            migrationBuilder.Sql(AriekiVentEruptions.ForgetOldId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            new AriekiVentEruptions().Remove(migrationBuilder);
        }
    }
}
