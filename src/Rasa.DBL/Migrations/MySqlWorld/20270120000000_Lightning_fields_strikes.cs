using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Lightning strikes across Torden Plains' Lightning Fields (LightningFieldsStrikes):
    /// world_usable rows 10401-10450, a RandomExplosionAriekiLightning at each of 50 points round
    /// control point 23. Data only.
    ///
    /// Up deletes rows of those ids first, so it can run again. Down deletes the rows. The
    /// Designer carries the model, which InsertData needs for the columns' types.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Lightning_fields_strikes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var strikes = new LightningFieldsStrikes();

            strikes.Remove(migrationBuilder);
            strikes.Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            new LightningFieldsStrikes().Remove(migrationBuilder);
        }
    }
}
