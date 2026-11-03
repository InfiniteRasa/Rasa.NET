using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Plateau;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Soyuz Salvage: Plateau's three debris pieces take the use argument the client sends for their
    /// classes, 1, in place of the 3 Plateau_soyuz_salvage gave them (PlateauSoyuzSalvageV1.RepairUseUp).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down puts back the 3 on the pieces that have the 1.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271008000000_Plateau_soyuz_debris_use")]
    public class Plateau_soyuz_debris_use : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSoyuzSalvageV1.RepairUseUp())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauSoyuzSalvageV1.RepairUseDown())
                migrationBuilder.Sql(sql);
        }
    }
}
