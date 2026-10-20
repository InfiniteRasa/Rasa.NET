using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// UI_INDICATOR_BOSS on the seven boss classes that were without it (BossFrameFlags): the
    /// Bane Hunter Boss, the Arioch Xanx Boss, the Plateau's Mox Alpha Male, Hominis Machina Lt.
    /// Casper and Rotting Sal get the boss border on their target frame. Data only, in plain SQL:
    /// no model of its own.
    ///
    /// Down takes the flag off them again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270127000000_Boss_frame_flags")]
    public class Boss_frame_flags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BossFrameFlags.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BossFrameFlags.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
