using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// Seven NPCs that stood on the roof of their tent, put on its floor: Colonel Bosley and
    /// Supply Sergeant Lipschitz at Camp Resistance among them (TentFloors, which says how each
    /// was measured). No table changes.
    ///
    /// Down puts each row back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270128000000_Tent_floor_npcs")]
    public partial class Tent_floor_npcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TentFloors.WorldUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TentFloors.WorldDown)
                migrationBuilder.Sql(statement);
        }
    }
}
