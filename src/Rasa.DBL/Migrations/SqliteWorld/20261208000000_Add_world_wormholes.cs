using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// The world wormholes as one network (the game server's Wormholes).
    ///
    /// wormhole_lock: a condition on a trip from one wormhole to another (from_teleporter_id,
    /// to_teleporter_id, 0 for any), kind 1 a mission taken at least once, kind 2 a level (not
    /// enforced yet), with its value. A trip no row matches is open.
    ///
    /// And the rows (WorldWormholesSeed): the Ligo Crucible wormhole, 358, which the client has
    /// and the world had as a placeholder; the Guardian Prominence Personal Wormhole's class
    /// (425, 28478 to 28474); and the locks from the Concordia Divide wormhole to the three on
    /// Arieki, which open with mission 1038.
    ///
    /// Numbered 20261207000000 at first, which Add_control_point_turrets has too. A world that
    /// ran it under that id is brought to the same place by running it again: the table is
    /// made only if it is missing, the rows are set rather than added, and the first id is
    /// taken off the history (WorldWormholesSeed.FirstMigrationId).
    ///
    /// Down puts 358 and the class back and drops the table.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_world_wormholes : Migration
    {
        /// <summary>wormhole_lock as EF's CreateTable writes it for this provider, if it is not there.</summary>
        internal const string TableIfMissing = @"CREATE TABLE IF NOT EXISTS ""wormhole_lock"" (
    ""id"" INTEGER NOT NULL CONSTRAINT ""PK_wormhole_lock"" PRIMARY KEY AUTOINCREMENT,
    ""from_teleporter_id"" INTEGER NOT NULL,
    ""to_teleporter_id"" INTEGER NOT NULL,
    ""kind"" INTEGER NOT NULL,
    ""value"" INTEGER NOT NULL,
    ""comment"" varchar(128) NULL
);";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The table as CreateTable makes it, unless a world that ran this migration under its
            // first id has it already (WorldWormholesSeed.FirstMigrationId).
            migrationBuilder.Sql(TableIfMissing);

            foreach (var statement in WorldWormholesSeed.InsertStatements)
                migrationBuilder.Sql(statement);

            migrationBuilder.Sql(WorldWormholesSeed.ForgetFirstIdStatement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in WorldWormholesSeed.DeleteStatements)
                migrationBuilder.Sql(statement);

            migrationBuilder.DropTable(name: WormholeLockEntry.TableName);
        }
    }
}
