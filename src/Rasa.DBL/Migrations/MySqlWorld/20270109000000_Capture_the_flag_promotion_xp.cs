using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Capture the Flag's promotion at Corporal DeSimone pays 500 experience, not 43000
    /// (CaptureTheFlagPromotion). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts back 43000 where it is still 500.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270109000000_Capture_the_flag_promotion_xp")]
    public class Capture_the_flag_promotion_xp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CaptureTheFlagPromotion.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CaptureTheFlagPromotion.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
