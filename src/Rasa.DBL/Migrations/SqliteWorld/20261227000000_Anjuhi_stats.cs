using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ranger Anjuhi (creature 98) is given a creature_stat row, 4500 health and 300 armour
    /// (AnjuhiStats). He had none and spawned with 100 health, and the three Xanx of "Childhood's
    /// End" killed him in seconds. Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the row away again, only as it was put in.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261227000000_Anjuhi_stats")]
    public class Anjuhi_stats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AnjuhiStats.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AnjuhiStats.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
