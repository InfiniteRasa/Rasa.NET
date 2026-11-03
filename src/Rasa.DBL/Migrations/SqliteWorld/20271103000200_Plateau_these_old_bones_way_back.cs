using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Plateau;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// These Old Bones (1976): Elder Hathen's walk back from temples 1 and 2 ends on the upstream Velon Hollow navmesh
    /// (PlateauTheseOldBonesWayBackV2). Data only, plain SQL; Down puts back the V1 ends.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271103000200_Plateau_these_old_bones_way_back")]
    public class Plateau_these_old_bones_way_back : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauTheseOldBonesWayBackV2.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PlateauTheseOldBonesWayBackV2.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
