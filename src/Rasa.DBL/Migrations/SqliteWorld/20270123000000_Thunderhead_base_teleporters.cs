using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Thunderhead Base's local teleporters linked as its signs say: rows 980 to 983 for the four
    /// pads on the HQ's floor, and the base's ten pads linked in five pairs both ways
    /// (ThunderheadBaseTeleporters). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes them out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270123000000_Thunderhead_base_teleporters")]
    public class Thunderhead_base_teleporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThunderheadBaseTeleporters.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThunderheadBaseTeleporters.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
