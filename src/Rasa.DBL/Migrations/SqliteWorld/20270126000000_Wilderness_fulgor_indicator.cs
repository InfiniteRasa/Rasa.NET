using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Lurking In The Shadows (427), objective 6 "Kill Proctor Fulgor": a map pin and radar arrow
    /// on Fulgor's spawn (WildernessFulgorIndicator). One mission_indicator row; data only.
    ///
    /// Down deletes the row.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270126000000_Wilderness_fulgor_indicator")]
    public partial class Wilderness_fulgor_indicator : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessFulgorIndicator.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessFulgorIndicator.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
