using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix job H (to-fix row 64, Your Part of the Bargain 1529): the Ultra Reconstructor's point pool 770038 in the
    /// Staal Junkyard moved off the rock ledge no client walk climbs, with its marker, and an evidence row (CrucibleFixHV1). A
    /// delta on 20271108000900. Data only, plain SQL; Down puts back the old place.
    /// </summary>
    public partial class Crucible_fix_h : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleFixHV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleFixHV1.Down(migrationBuilder);
    }
}
